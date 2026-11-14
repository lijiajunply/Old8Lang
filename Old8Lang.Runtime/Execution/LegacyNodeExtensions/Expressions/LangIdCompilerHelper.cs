using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 标识符编译的后端辅助实现。
/// </summary>
internal static class LangIdCompilerHelper
{
    /// <summary>
    /// 在已编译的委托表中查找具名函数（支持 <c>名称$参数类型</c> 形式的带签名键）。
    /// </summary>
    /// <remarks>
    /// 如果同名函数存在多个重载，返回 false，避免静默选中错误的重载。
    /// </remarks>
    internal static bool TryGetFunctionMethod(string name, LocalManager local, out MethodInfo method)
    {
        if (local.DelegateVar.TryGetValue(name, out var exactMatch))
        {
            method = exactMatch;
            return true;
        }

        MethodInfo? found = null;
        var prefix = $"{name}$";
        foreach (var (key, value) in local.DelegateVar)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (found is not null)
            {
                // 多个重载：无法在这里确定签名
                method = null!;
                return false;
            }

            found = value;
        }

        method = found!;
        return found is not null;
    }
}
