using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.ExternProviders;

/// <summary>
/// C/C++ 原生 DLL 提供者（P/Invoke）
/// </summary>
public class NativeDllProvider : IExternProvider
{
    /// <summary>
    /// 支持编译模式
    /// </summary>
    public bool SupportsCompilation => true;

    /// <summary>
    /// 解释模式：加载原生 DLL 函数
    /// </summary>
    public void LoadFunctions(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        VariateManager manager)
    {
        foreach (var funcDecl in functions)
        {
            var callingConv = funcDecl.CallingConvention != CallingConventionType.Cdecl
                ? funcDecl.CallingConvention
                : defaultCallingConvention;

            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 创建 P/Invoke 委托
            var delegateType = CreateDelegateType(funcDecl, callingConv);
            var funcPtr = NativeLibrary.GetExport(
                NativeLibrary.Load(source),
                funcDecl.FunctionName);

            var del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);

            // 将委托包装为 Old8Lang 函数
            var funcValue = new FuncLangValue(targetName, del.Method, funcDecl.FunctionSignature?.FuncLangValue);
            manager.AddClassAndFunc(funcValue);
        }
    }

    /// <summary>
    /// 编译模式：生成 P/Invoke IL 代码
    /// </summary>
    public void GenerateIL(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        ILGenerator ilGenerator,
        LocalManager localManager)
    {
        foreach (var funcDecl in functions)
        {
            var callingConv = funcDecl.CallingConvention != CallingConventionType.Cdecl
                ? funcDecl.CallingConvention
                : defaultCallingConvention;

            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 创建 P/Invoke 方法
            var pinvokeMethod = CreatePInvokeMethod(source, funcDecl, callingConv);

            // 注册到局部变量管理器
            localManager.DelegateVar.Add(targetName, pinvokeMethod);
        }
    }

    /// <summary>
    /// 创建委托类型用于 P/Invoke
    /// </summary>
    private Type CreateDelegateType(ExternFunctionDeclaration funcDecl, CallingConventionType callingConv)
    {
        if (funcDecl.FunctionSignature == null)
        {
            throw new TypeError(null, $"extern 函数 {funcDecl.FunctionName} 必须指定函数签名（参数类型和返回类型）");
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
    private MethodInfo CreatePInvokeMethod(string dllName, ExternFunctionDeclaration funcDecl, CallingConventionType callingConv)
    {
        if (funcDecl.FunctionSignature == null)
        {
            throw new TypeError(null, $"extern 函数 {funcDecl.FunctionName} 必须指定函数签名（参数类型和返回类型）");
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
            dllName,
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
            _ => throw new TypeError(null, $"不支持的 extern 类型: {old8Type}")
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
}
