using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// P/Invoke 调用约定类型
/// </summary>
public enum CallingConventionType
{
    /// <summary>
    /// Cdecl 调用约定（默认，C 标准）
    /// </summary>
    Cdecl,

    /// <summary>
    /// StdCall 调用约定（Windows API 标准）
    /// </summary>
    StdCall,

    /// <summary>
    /// WinApi 调用约定（等同于 StdCall）
    /// </summary>
    WinApi
}

/// <summary>
/// 外部函数声明
/// </summary>
public class ExternFunctionDeclaration
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// 参数列表（FuncInit 包含参数类型信息）
    /// </summary>
    public FuncInit? FunctionSignature { get; }

    /// <summary>
    /// 别名（可选）
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// 调用约定
    /// </summary>
    public CallingConventionType CallingConvention { get; }

    public ExternFunctionDeclaration(
        string functionName,
        FuncInit? functionSignature = null,
        string? alias = null,
        CallingConventionType callingConvention = CallingConventionType.Cdecl)
    {
        FunctionName = functionName;
        FunctionSignature = functionSignature;
        Alias = alias;
        CallingConvention = callingConvention;
    }
}

/// <summary>
/// Extern 语句类，用于处理 P/Invoke 原生函数调用
/// 支持 C/C++ 原生库函数导入
/// </summary>
public partial class ExternStatement : OldStatement
{
    /// <summary>
    /// DLL 名称
    /// </summary>
    private readonly string DllName;

    /// <summary>
    /// 外部函数声明列表
    /// </summary>
    private readonly List<ExternFunctionDeclaration> Functions;

    /// <summary>
    /// 默认调用约定
    /// </summary>
    private readonly CallingConventionType DefaultCallingConvention;

    /// <summary>
    /// 构造函数：创建 extern 语句
    /// </summary>
    /// <param name="dllName">DLL 名称</param>
    /// <param name="functions">外部函数声明列表</param>
    /// <param name="defaultCallingConvention">默认调用约定</param>
    public ExternStatement(
        string dllName,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention = CallingConventionType.Cdecl)
    {
        DllName = dllName;
        Functions = functions;
        DefaultCallingConvention = defaultCallingConvention;
    }

    /// <summary>
    /// 在解释模式下执行 extern 导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public override void Run(VariateManager manager)
    {
        foreach (var funcDecl in Functions)
        {
            var callingConv = funcDecl.CallingConvention != CallingConventionType.Cdecl
                ? funcDecl.CallingConvention
                : DefaultCallingConvention;

            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 创建 P/Invoke 委托
            var delegateType = CreateDelegateType(funcDecl, callingConv);
            var funcPtr = NativeLibrary.GetExport(
                NativeLibrary.Load(DllName),
                funcDecl.FunctionName);

            var del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);

            // 将委托包装为 Old8Lang 函数
            var funcValue = new FuncLangValue(targetName, del.Method, funcDecl.FunctionSignature?.FuncLangValue);
            manager.AddClassAndFunc(funcValue);
        }
    }

    /// <summary>
    /// 在编译模式下生成 extern 导入的 IL 代码
    /// </summary>
    /// <param name="ilGenerator">IL 指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        foreach (var funcDecl in Functions)
        {
            var callingConv = funcDecl.CallingConvention != CallingConventionType.Cdecl
                ? funcDecl.CallingConvention
                : DefaultCallingConvention;

            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 创建 P/Invoke 方法
            var pinvokeMethod = CreatePInvokeMethod(funcDecl, callingConv);

            // 注册到局部变量管理器
            local.DelegateVar.Add(targetName, pinvokeMethod);
        }
    }

    /// <summary>
    /// 创建委托类型用于 P/Invoke
    /// </summary>
    private Type CreateDelegateType(ExternFunctionDeclaration funcDecl, CallingConventionType callingConv)
    {
        if (funcDecl.FunctionSignature == null)
        {
            throw new TypeError(this, $"extern 函数 {funcDecl.FunctionName} 必须指定函数签名（参数类型和返回类型）");
        }

        var signature = funcDecl.FunctionSignature.FuncLangValue;
        var paramTypes = signature.Ids?
            .Select(p => ConvertOld8TypeToCSharpType(p.AssumptionType))
            .ToArray() ?? [];

        var returnType = ConvertOld8TypeToCSharpType(signature.Id?.AssumptionType);

        // 动态创建委托类型
        var assemblyName = new AssemblyName($"ExternDelegate_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("ExternModule");
        var typeBuilder = moduleBuilder.DefineType(
            $"ExternDelegate_{funcDecl.FunctionName}",
            TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(MulticastDelegate));

        // 添加 UnmanagedFunctionPointer 特性
        var unmanagedAttr = new CustomAttributeBuilder(
            typeof(UnmanagedFunctionPointerAttribute).GetConstructor([typeof(CallingConvention)])!,
            [ConvertCallingConvention(callingConv)]);
        typeBuilder.SetCustomAttribute(unmanagedAttr);

        // 定义构造函数
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(object), typeof(IntPtr)]);
        ctorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

        // 定义 Invoke 方法
        var invokeBuilder = typeBuilder.DefineMethod(
            "Invoke",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            returnType,
            paramTypes);
        invokeBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

        return typeBuilder.CreateType()!;
    }

    /// <summary>
    /// 创建 P/Invoke 方法信息
    /// </summary>
    private MethodInfo CreatePInvokeMethod(ExternFunctionDeclaration funcDecl, CallingConventionType callingConv)
    {
        if (funcDecl.FunctionSignature == null)
        {
            throw new TypeError(this, $"extern 函数 {funcDecl.FunctionName} 必须指定函数签名（参数类型和返回类型）");
        }

        var signature = funcDecl.FunctionSignature.FuncLangValue;
        var paramTypes = signature.Ids?
            .Select(p => ConvertOld8TypeToCSharpType(p.AssumptionType))
            .ToArray() ?? [];

        var returnType = ConvertOld8TypeToCSharpType(signature.Id?.AssumptionType);

        // 动态创建类型和方法
        var assemblyName = new AssemblyName($"ExternPInvoke_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("ExternPInvokeModule");
        var typeBuilder = moduleBuilder.DefineType(
            $"ExternPInvoke_{funcDecl.FunctionName}",
            TypeAttributes.Public);

        // 定义 P/Invoke 方法
        var methodBuilder = typeBuilder.DefinePInvokeMethod(
            funcDecl.FunctionName,
            DllName,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
            CallingConventions.Standard,
            returnType,
            paramTypes,
            ConvertCallingConvention(callingConv),
            CharSet.Auto);
        methodBuilder.SetImplementationFlags(
            methodBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

        var type = typeBuilder.CreateType()!;
        return type.GetMethod(funcDecl.FunctionName)!;
    }

    /// <summary>
    /// 将 Old8Lang 类型转换为 C# 类型
    /// </summary>
    private Type ConvertOld8TypeToCSharpType(string? old8Type)
    {
        return old8Type?.ToLower() switch
        {
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "float" => typeof(float),
            "bool" => typeof(bool),
            "string" => typeof(string),
            "void" => typeof(void),
            "object" => typeof(object),
            "char" => typeof(char),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "uint" => typeof(uint),
            "ulong" => typeof(ulong),
            "ushort" => typeof(ushort),
            null => typeof(void),
            _ => throw new TypeError(this, $"不支持的 extern 类型: {old8Type}")
        };
    }

    /// <summary>
    /// 转换调用约定枚举
    /// </summary>
    private CallingConvention ConvertCallingConvention(CallingConventionType type)
    {
        return type switch
        {
            CallingConventionType.Cdecl => CallingConvention.Cdecl,
            CallingConventionType.StdCall => CallingConvention.StdCall,
            CallingConventionType.WinApi => CallingConvention.Winapi,
            _ => CallingConvention.Cdecl
        };
    }

    /// <summary>
    /// 获取指定索引处的语句
    /// </summary>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量
    /// </summary>
    public override int Count => 0;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 将 extern 语句转换为字符串表示
    /// </summary>
    public override string ToString()
    {
        var convStr = DefaultCallingConvention != CallingConventionType.Cdecl
            ? $" {DefaultCallingConvention.ToString().ToLower()}"
            : "";

        if (Functions.Count == 1)
        {
            var func = Functions[0];
            var convOverrideStr = func.CallingConvention != DefaultCallingConvention
                ? $"{func.CallingConvention.ToString().ToLower()} "
                : "";
            var aliasStr = func.Alias != null ? $" as {func.Alias}" : "";
            var signature = FormatFunctionSignature(func);
            return $"native extern \"{DllName}\"{convStr} {convOverrideStr}func {func.FunctionName}{signature}{aliasStr}";
        }

        var funcs = string.Join("\n    ", Functions.Select(f =>
        {
            var convOverrideStr = f.CallingConvention != DefaultCallingConvention
                ? $"{f.CallingConvention.ToString().ToLower()} "
                : "";
            var aliasStr = f.Alias != null ? $" as {f.Alias}" : "";
            var signature = FormatFunctionSignature(f);
            return $"{convOverrideStr}func {f.FunctionName}{signature}{aliasStr}";
        }));

        return $"native extern \"{DllName}\"{convStr} {{\n    {funcs}\n}}";
    }

    /// <summary>
    /// 格式化函数签名为字符串
    /// </summary>
    private string FormatFunctionSignature(ExternFunctionDeclaration func)
    {
        if (func.FunctionSignature == null)
            return "()";

        var funcValue = func.FunctionSignature.FuncLangValue;
        var parameters = funcValue.Ids != null
            ? string.Join(", ", funcValue.Ids.Select(p =>
            {
                var type = !string.IsNullOrEmpty(p.AssumptionType) ? $":{p.AssumptionType}" : "";
                return $"{p.IdName}{type}";
            }))
            : "";

        var returnType = funcValue.Id?.AssumptionType ?? "void";
        return $"({parameters}) -> {returnType}";
    }
}
