using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Instance 类的全局函数集成部分
/// </summary>
public partial class Instance
{
    /// <summary>
    /// 尝试通过全局函数注册器执行函数（解释器模式）
    /// </summary>
    /// <returns>如果找到并执行了全局函数返回 true，否则返回 false</returns>
    private bool TryExecuteGlobalFunction(VariateManager manager, out LangValueType? result)
    {
        // 确保全局函数已初始化
        GlobalFunctionInitializer.EnsureInitialized();

        // 查找全局函数
        var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(Id.IdName);
        if (globalFunc != null)
        {
            result = globalFunc.Execute(Ids, manager, Position);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// 尝试通过全局函数注册器生成 IL 代码（编译器模式）
    /// </summary>
    /// <returns>如果找到并生成了 IL 代码返回 true，否则返回 false</returns>
    private bool TryGenerateGlobalFunctionIL(ILGenerator ilGenerator, LocalManager local)
    {
        // 确保全局函数已初始化
        GlobalFunctionInitializer.EnsureInitialized();

        // 查找全局函数
        var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(Id.IdName);
        if (globalFunc != null)
        {
            globalFunc.GenerateIL(Ids, ilGenerator, local, Position);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试通过全局函数注册器获取返回类型（编译器模式）
    /// </summary>
    /// <returns>如果找到了全局函数返回其返回类型，否则返回 null</returns>
    private Type? TryGetGlobalFunctionReturnType(LocalManager local)
    {
        // 确保全局函数已初始化
        GlobalFunctionInitializer.EnsureInitialized();

        // 查找全局函数
        var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(Id.IdName);
        if (globalFunc != null)
        {
            return globalFunc.GetReturnType(Ids, local);
        }

        return null;
    }
}
