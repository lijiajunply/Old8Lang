using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.Compiler.CodeGeneration;

/// <summary>IL 模式下调用已导入的标准库模块（模块对象调用与 <c>from</c> 选择性导入的函数）。</summary>
public static class StandardLibraryILHelper
{
    public static bool TryResolve(LangExpression? left, LangExpression? right, LocalManager local,
        out UnifiedModule module, out Instance call)
    {
        module = null!;
        call = null!;
        if (left is not LangId id || right is not Instance instance ||
            local.GetLocalVar(id.IdName) is not null || local.ArgumentIndices.ContainsKey(id.IdName) ||
            local.LocalVarTypes.ContainsKey(id.IdName) || local.FieldVar.ContainsKey(id.IdName) ||
            !local.GlobalStaticClasses.TryGetValue(id.IdName, out var value) || value is not UnifiedModule library)
            return false;

        module = library;
        call = instance;
        return true;
    }

    /// <summary>
    /// 判断调用是否指向 <c>import { F } from "Module"</c> 导入的标准库函数（局部名遮蔽时不算）。
    /// </summary>
    public static bool TryResolveFunction(LangExpression? expression, LocalManager local,
        out object function, out Instance call)
    {
        function = null!;
        call = null!;
        if (expression is not Instance instance)
            return false;
        var name = instance.Id.IdName;
        if (local.GetLocalVar(name) is not null || local.ArgumentIndices.ContainsKey(name) ||
            local.LocalVarTypes.ContainsKey(name) || local.FieldVar.ContainsKey(name) ||
            local.CapturedLocals.ContainsKey(name) ||
            !local.GlobalStaticClasses.TryGetValue(name, out var value) ||
            value is not (FuncLangValue { Method: not null } or NativeAnyLangValue))
            return false;

        function = value;
        call = instance;
        return true;
    }

    public static Type GetReturnType(UnifiedModule module, Instance call)
    {
        var symbol = module.GetSymbol(call.Id.IdName);
        // 模块导出的原生类：Net.HttpWebClient() 按解释器语义实例化，结果是原生对象
        if (symbol is NativeAnyLangValue)
            return typeof(object);
        if (symbol is not FuncLangValue { Method: not null } function)
            throw new InvalidOperationError(call, $"标准库 '{module.ModuleName}' 没有方法 '{call.Id.IdName}'");
        return NativeReturnType(function.Method.ReturnType);
    }

    public static Type GetFunctionReturnType(object function) =>
        function is FuncLangValue { Method: not null } native ? NativeReturnType(native.Method.ReturnType) : typeof(object);

    public static Type EmitCall(UnifiedModule module, Instance call, ILGenerator il, LocalManager local) =>
        EmitInvoke(module, call.Id.IdName, GetReturnType(module, call), call, il, local);

    public static Type EmitFunctionCall(object function, Instance call, ILGenerator il, LocalManager local) =>
        EmitInvoke(function, call.Id.IdName, GetFunctionReturnType(function), call, il, local);

    private static Type EmitInvoke(object target, string methodName, Type resultType, Instance call, ILGenerator il,
        LocalManager local)
    {
        il.Emit(OpCodes.Ldc_I4, RuntimeConstantRegistry.Register(target));
        il.Emit(OpCodes.Call, typeof(RuntimeConstantRegistry).GetMethod(nameof(RuntimeConstantRegistry.Get))!);
        il.Emit(OpCodes.Ldstr, methodName);

        EmitObjectArray(il, local, call.Ids);

        var named = call.NamedArgs ?? [];
        il.Emit(OpCodes.Ldc_I4, named.Count);
        il.Emit(OpCodes.Newarr, typeof(string));
        for (var index = 0; index < named.Count; index++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, index);
            il.Emit(OpCodes.Ldstr, named[index].Name);
            il.Emit(OpCodes.Stelem_Ref);
        }

        EmitObjectArray(il, local, named.Select(item => item.Value).ToList());

        il.Emit(OpCodes.Call, typeof(StandardLibraryILHelper).GetMethod(nameof(InvokeTarget))!);
        if (resultType == typeof(void))
            il.Emit(OpCodes.Pop);
        else if (resultType == typeof(double))
            il.Emit(OpCodes.Call, typeof(StandardLibraryILHelper).GetMethod(nameof(ToDoubleResult))!);
        else if (resultType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, resultType);
        else if (resultType != typeof(object))
            il.Emit(OpCodes.Castclass, resultType);
        return resultType;
    }

    private static void EmitObjectArray(ILGenerator il, LocalManager local, IReadOnlyList<LangExpression> values)
    {
        il.Emit(OpCodes.Ldc_I4, values.Count);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var index = 0; index < values.Count; index++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, index);
            var argument = values[index];
            argument.Accept(RuntimeVisitors.CreateCompiler(il, local));
            var type = argument.Accept(new TypeInferenceVisitor(local));
            if (type?.IsValueType == true)
                il.Emit(OpCodes.Box, type);
            il.Emit(OpCodes.Stelem_Ref);
        }
    }

    private static Type NativeReturnType(Type type)
    {
        if (type == typeof(void) || type == typeof(int) || type == typeof(double) || type == typeof(bool) ||
            type == typeof(char) || type == typeof(string)) return type;
        if (type == typeof(byte) || type == typeof(short) || type == typeof(sbyte)) return typeof(int);
        // Old8Lang IL 的数值运算以 int / double 为边界；标准库的 long（例如
        // Unix 毫秒时间戳）需要在此处统一为 double，不能作为 object 泄漏。
        if (type == typeof(long) || type == typeof(float) || type == typeof(decimal)) return typeof(double);
        return typeof(object);
    }

    /// <summary>把运行期结果（可能是 int / long / double 等）统一为 double。</summary>
    public static double ToDoubleResult(object? value) => value switch
    {
        null => 0.0,
        double d => d,
        _ => Convert.ToDouble(value)
    };

    public static object? Invoke(UnifiedModule module, string methodName, object?[] args) =>
        InvokeTarget(module, methodName, args, [], []);

    private static LangValueType InstantiateNativeClass(NativeAnyLangValue nativeClass,
        List<LangExpression> expressions, VariateManager manager)
    {
        var holder = nativeClass.CreateInstanceHolder();
        var values = expressions.Select(item => item.Accept(RuntimeVisitors.CreateInterpreter(manager))).ToList();
        holder.New([.. Apis.ListToObjects(values)]);
        return holder;
    }

    /// <summary>读取 <c>from</c> 导入的原生类的静态成员（如 <c>Vector2.Zero</c>）。</summary>
    public static object? GetStaticMember(object target, string memberName)
    {
        if (target is not NativeAnyLangValue { ClassType: { } type })
            throw new InvalidOperationException($"'{memberName}' 不是可访问静态成员的类型");

        var property = type.GetProperty(memberName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var raw = property is not null
            ? property.GetValue(null)
            : type.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                ?.GetValue(null)
              ?? throw new InvalidOperationException($"类型 '{type.Name}' 没有静态成员 '{memberName}'");
        return raw;
    }

    /// <summary><c>Vector2.Zero</c>：左侧是 <c>from</c> 导入的原生类，右侧是成员名。</summary>
    public static bool TryResolveStaticMember(LangExpression? left, LangExpression? right, LocalManager local,
        out NativeAnyLangValue target, out string memberName)
    {
        target = null!;
        memberName = string.Empty;
        if (left is not LangId id || right is not LangId member ||
            local.GetLocalVar(id.IdName) is not null || local.ArgumentIndices.ContainsKey(id.IdName) ||
            local.LocalVarTypes.ContainsKey(id.IdName) || local.FieldVar.ContainsKey(id.IdName) ||
            !local.GlobalStaticClasses.TryGetValue(id.IdName, out var value) || value is not NativeAnyLangValue native)
            return false;

        target = native;
        memberName = member.IdName;
        return true;
    }

    public static Type EmitStaticMember(NativeAnyLangValue target, string memberName, ILGenerator il)
    {
        il.Emit(OpCodes.Ldc_I4, RuntimeConstantRegistry.Register(target));
        il.Emit(OpCodes.Call, typeof(RuntimeConstantRegistry).GetMethod(nameof(RuntimeConstantRegistry.Get))!);
        il.Emit(OpCodes.Ldstr, memberName);
        il.Emit(OpCodes.Call, typeof(StandardLibraryILHelper).GetMethod(nameof(GetStaticMember))!);
        return typeof(object);
    }

    /// <summary>调用标准库模块方法或 <c>from</c> 导入的函数；命名实参按形参名绑定。</summary>
    public static object? InvokeTarget(object target, string methodName, object?[] args, string[] namedNames,
        object?[] namedValues)
    {
        var manager = new VariateManager();
        var expressions = args.Select(value => (LangExpression)IlValueBridge.Wrap(value)).ToList();
        var named = new List<NamedArgument>();
        for (var index = 0; index < namedNames.Length; index++)
            named.Add(new NamedArgument(namedNames[index], IlValueBridge.Wrap(namedValues[index])));

        var raw = target switch
        {
            UnifiedModule module => module.Dot(new Instance(new LangId(methodName), expressions, named), manager),
            FuncLangValue function => function.Run(manager, expressions, named, default),
            NativeAnyLangValue nativeClass => InstantiateNativeClass(nativeClass, expressions, manager),
            _ => throw new InvalidOperationException($"不支持的标准库调用目标: {target.GetType().Name}")
        };

        var value = IlValueBridge.Unwrap(raw);
        return value is long longValue ? (double)longValue : value;
    }
}
