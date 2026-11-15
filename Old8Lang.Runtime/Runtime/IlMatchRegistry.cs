using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.Runtime;

/// <summary>
/// IL 模式 match 表达式的分支注册表。
/// </summary>
/// <remarks>
/// IL 后端把每个 <see cref="MatchCase"/> 在编译期登记进来，运行期按 id 调用
/// <see cref="MatchCaseRuntimeExtensions.IsMatch"/>，直接复用解释器已验证的模式语义
/// （值匹配、变量绑定、元组解构、类型匹配、范围匹配与 guard 条件），
/// 避免在 IL 里再实现一套注定与解释器不一致的匹配逻辑。
/// 绑定出来的变量按编译期确定的名字顺序返回，供调用点写回局部变量。
/// </remarks>
public static class IlMatchRegistry
{
    private static readonly Dictionary<int, (MatchCase Case, string[] Bindings)> Cases = [];
    private static readonly Lock RegistryLock = new();
    private static int _nextId;

    /// <summary>
    /// 登记一个分支，返回运行期使用的 id。
    /// </summary>
    public static int Register(MatchCase matchCase, string[] bindingNames)
    {
        lock (RegistryLock)
        {
            var id = ++_nextId;
            Cases[id] = (matchCase, bindingNames);
            return id;
        }
    }

    /// <summary>
    /// 判断值是否匹配指定分支，并返回绑定变量的值（顺序与登记时一致）。
    /// </summary>
    public static bool TryMatch(int id, object? value, out object?[]? boundValues)
    {
        boundValues = null;

        MatchCase matchCase;
        string[] bindingNames;
        lock (RegistryLock)
        {
            if (!Cases.TryGetValue(id, out var entry))
            {
                return false;
            }

            (matchCase, bindingNames) = entry;
        }

        var manager = RuntimeGlobalWriter.CurrentManager ?? new VariateManager();
        var wrapped = IlValueBridge.Wrap(value);
        if (!matchCase.IsMatch(wrapped, manager, out var bound))
        {
            return false;
        }

        boundValues = new object?[bindingNames.Length];
        for (var index = 0; index < bindingNames.Length; index++)
        {
            boundValues[index] = bound is not null && bound.TryGetValue(bindingNames[index], out var boundValue)
                ? IlValueBridge.Unwrap(boundValue)
                : null;
        }

        return true;
    }
}
