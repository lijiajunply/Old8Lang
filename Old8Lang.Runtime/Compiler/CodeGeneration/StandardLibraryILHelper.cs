using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.Compiler.CodeGeneration;

/// <summary>IL 模式下调用已导入的标准库模块。</summary>
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

    public static Type GetReturnType(UnifiedModule module, Instance call)
    {
        if (module.GetSymbol(call.Id.IdName) is not FuncLangValue { Method: not null } function)
            throw new InvalidOperationError(call, $"标准库 '{module.ModuleName}' 没有方法 '{call.Id.IdName}'");
        return NativeReturnType(function.Method.ReturnType);
    }

    public static Type EmitCall(UnifiedModule module, Instance call, ILGenerator il, LocalManager local)
    {
        if (call.NamedArgs.Count != 0)
            throw new InvalidOperationError(call, "IL 标准库模块调用暂不支持命名参数");
        var resultType = GetReturnType(module, call);
        il.Emit(OpCodes.Ldc_I4, RuntimeConstantRegistry.Register(module));
        il.Emit(OpCodes.Call, typeof(RuntimeConstantRegistry).GetMethod(nameof(RuntimeConstantRegistry.Get))!);
        il.Emit(OpCodes.Castclass, typeof(UnifiedModule));
        il.Emit(OpCodes.Ldstr, call.Id.IdName);
        il.Emit(OpCodes.Ldc_I4, call.Ids.Count);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var index = 0; index < call.Ids.Count; index++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, index);
            var argument = call.Ids[index];
            argument.Accept(new CompilerVisitor(il, local));
            var type = argument.Accept(new TypeInferenceVisitor(local));
            if (type?.IsValueType == true)
                il.Emit(OpCodes.Box, type);
            il.Emit(OpCodes.Stelem_Ref);
        }

        il.Emit(OpCodes.Call, typeof(StandardLibraryILHelper).GetMethod(nameof(Invoke))!);
        if (resultType == typeof(void))
            il.Emit(OpCodes.Pop);
        else if (resultType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, resultType);
        else if (resultType != typeof(object))
            il.Emit(OpCodes.Castclass, resultType);
        return resultType;
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

    public static object? Invoke(UnifiedModule module, string methodName, object?[] args)
    {
        var manager = new VariateManager();
        var expressions = args.Select(value => (LangExpression)IlValueBridge.Wrap(value)).ToList();
        var value = IlValueBridge.Unwrap(module.Dot(new Instance(new LangId(methodName), expressions), manager));
        return value is long longValue ? (double)longValue : value;
    }
}
