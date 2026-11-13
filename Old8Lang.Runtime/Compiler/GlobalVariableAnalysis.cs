using System.Collections;
using System.Reflection;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.Compiler;

/// <summary>
/// IL 模式下顶层变量的全局槽位信息。
/// </summary>
/// <remarks>
/// 不是所有顶层变量都需要全局槽位：只有那些会被函数体（具名函数、lambda、类方法）访问的
/// 变量才需要，因为它们的值是跨 <see cref="System.Reflection.Emit.DynamicMethod"/> 访问的。
/// 其余顶层变量继续使用普通的局部变量，以保留 IL 模式的性能。
/// </remarks>
public sealed class GlobalVariableScope
{
    /// <summary>顶层变量名 → 静态类型。</summary>
    public Dictionary<string, Type> Types { get; } = new(StringComparer.Ordinal);

    /// <summary>需要提升为全局槽位的变量名。</summary>
    public HashSet<string> SlotNames { get; } = new(StringComparer.Ordinal);

    public bool IsGlobal(string name) => Types.ContainsKey(name);

    public bool IsSlot(string name) => SlotNames.Contains(name);

    public Type GetType(string name) => Types.TryGetValue(name, out var type) ? type : typeof(object);

    /// <summary>把一个顶层变量提升为全局槽位，返回是否是本次新提升。</summary>
    public bool Promote(string name) => SlotNames.Add(name);

    /// <summary>仅供运行时初始化使用的槽位类型表。</summary>
    public Dictionary<string, Type> GetSlotTypes()
    {
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var name in SlotNames)
        {
            result[name] = GetType(name);
        }

        return result;
    }
}

/// <summary>
/// 分析顶层变量中哪些需要跨动态方法共享。
/// </summary>
public static class GlobalVariableAnalysis
{
    /// <summary>
    /// 扫描程序，收集顶层变量、静态类型，以及被函数体引用的变量名。
    /// </summary>
    public static GlobalVariableScope Analyze(BlockStatement program, string path, LangInterpreter interpreter)
    {
        var scope = new GlobalVariableScope();

        try
        {
            var assignments = new List<TopLevelAssignment>();
            var functions = new List<FuncLangValue>();
            CollectTopLevelDeclarations(program, assignments, functions);

            if (assignments.Count == 0)
            {
                return scope;
            }

            foreach (var assignment in assignments)
            {
                scope.Types.TryAdd(assignment.Name, typeof(object));
            }

            var referencedNames = new HashSet<string>(StringComparer.Ordinal);
            CollectFunctionReferences(program, referencedNames);

            InferTopLevelTypes(assignments, functions, scope, path, interpreter);

            foreach (var assignment in assignments)
            {
                if (referencedNames.Contains(assignment.Name))
                {
                    scope.SlotNames.Add(assignment.Name);
                }
            }
        }
        catch
        {
            // 分析失败时退回旧行为：所有顶层变量保持为局部变量，
            // 由后续编译阶段按原逻辑报告未声明变量。
            return new GlobalVariableScope();
        }

        return scope;
    }

    private sealed record TopLevelAssignment(string Name, LangId Id, LangExpression Value);

    /// <summary>
    /// 收集顶层作用域中的变量赋值和具名函数定义。函数、类、导入的体不属于顶层作用域。
    /// </summary>
    private static void CollectTopLevelDeclarations(
        OldStatement statement,
        List<TopLevelAssignment> assignments,
        List<FuncLangValue> functions)
    {
        switch (statement)
        {
            case BlockStatement block:
                foreach (var child in block.OtherStatements)
                {
                    CollectTopLevelDeclarations(child, assignments, functions);
                }

                return;

            case FuncInit funcInit:
                if (funcInit.FuncValue.Id is not null && funcInit.FuncValue.Method is null)
                {
                    functions.Add(funcInit.FuncValue);
                }

                return;

            case ClassInit:
            case ExtensionDeclaration:
            case ImportStatement:
            case NativeStatement:
            case ExternStatement:
                return;

            case SetStatement { Id: { } id } set when !string.IsNullOrEmpty(id.IdName):
                assignments.Add(new TopLevelAssignment(id.IdName, id, set.Value));
                return;

            default:
                // 控制流语句（if/for/while/try）的块在解释器中是子作用域：
                // 在其中首次赋值的变量不会成为顶层变量，因此不继续下探。
                return;
        }
    }

    /// <summary>
    /// 收集所有会在独立方法中执行的代码（具名函数、lambda、类字段初始化与方法）中出现的标识符。
    /// </summary>
    private static void CollectFunctionReferences(BlockStatement program, HashSet<string> names)
    {
        WalkTree(program, node =>
        {
            switch (node)
            {
                case FuncLangValue { Method: null } function:
                    // 包括参数默认值和函数体；它们都在独立的 DynamicMethod 中求值。
                    CollectIdentifiers(function, names);
                    return true;

                case ClassInit classInit:
                    // 字段初始化表达式与方法体编译进动态类型的方法，不能访问 OldLangRun 的局部变量。
                    CollectIdentifiers(classInit, names);
                    return true;

                case AsyncFuncLangValue asyncFunction:
                    CollectIdentifiers(asyncFunction, names);
                    return true;

                default:
                    return false;
            }
        });
    }

    /// <summary>
    /// 递归收集 AST 子树的标识符名称。
    /// </summary>
    private static void CollectIdentifiers(IOldLangTree root, HashSet<string> names)
    {
        WalkTree(root, node =>
        {
            if (node is LangId id && !string.IsNullOrEmpty(id.IdName))
            {
                names.Add(id.IdName);
            }

            return false;
        });
    }

    /// <summary>
    /// 通用 AST 遍历。访问回调返回 true 表示不再深入该节点的子节点。
    /// </summary>
    private static void WalkTree(object? root, Func<IOldLangTree, bool> visit)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<object>();

        if (root is not null)
        {
            stack.Push(root);
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is null || current is string || !visited.Add(current))
            {
                continue;
            }

            if (current is IOldLangTree tree && visit(tree))
            {
                continue;
            }

            foreach (var child in GetChildren(current))
            {
                stack.Push(child);
            }
        }
    }

    private static IEnumerable<object> GetChildren(object node)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var type = node.GetType();

        foreach (var property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(node);
            }
            catch
            {
                continue;
            }

            foreach (var child in EnumerateTrees(value))
            {
                yield return child;
            }
        }

        foreach (var field in type.GetFields(flags))
        {
            object? value;
            try
            {
                value = field.GetValue(node);
            }
            catch
            {
                continue;
            }

            foreach (var child in EnumerateTrees(value))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<object> EnumerateTrees(object? value)
    {
        switch (value)
        {
            case null:
            case string:
                yield break;
            case IOldLangTree tree:
                yield return tree;
                yield break;
            case IDictionary dictionary:
                // 字典的键值对不是 IOldLangTree，需要单独展开；
                // 类方法的定义存放在 TypeTemplate 的字典中，必须从这里下探。
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is IOldLangTree keyTree)
                    {
                        yield return keyTree;
                    }

                    if (entry.Value is IOldLangTree valueTree)
                    {
                        yield return valueTree;
                    }
                }

                yield break;
            case IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    if (item is IOldLangTree itemTree)
                    {
                        yield return itemTree;
                    }
                }

                yield break;
        }
    }

    /// <summary>
    /// 推断顶层变量的静态类型。多次迭代以处理前向引用（例如变量由稍后定义的函数初始化）。
    /// </summary>
    private static void InferTopLevelTypes(
        List<TopLevelAssignment> assignments,
        List<FuncLangValue> functions,
        GlobalVariableScope scope,
        string path,
        LangInterpreter interpreter)
    {
        var analysisLocal = new LocalManager
        {
            FilePath = path,
            Interpreter = interpreter,
            Globals = scope
        };
        Compiler.RegisterGlobalStaticClasses(analysisLocal);

        foreach (var assignment in assignments)
        {
            analysisLocal.LocalVarTypes[assignment.Name] = typeof(object);
        }

        // 预先登记顶层函数的返回类型，使 `g <- f()` 这类初始化表达式也能推断出类型。
        foreach (var function in functions)
        {
            if (function.Id is null)
            {
                continue;
            }

            var returnType = function.Id.OutputType(analysisLocal);
            if (returnType is not null && returnType != typeof(void))
            {
                analysisLocal.LocalDelegateReturnTypes.TryAdd(function.Id.IdName, returnType);
            }
        }

        for (var pass = 0; pass < 4; pass++)
        {
            var changed = false;

            foreach (var assignment in assignments)
            {
                Type? type = null;

                if (!string.IsNullOrEmpty(assignment.Id.AssumptionType))
                {
                    type = assignment.Id.OutputType(analysisLocal);
                }

                if (type is null || type == typeof(object) || type == typeof(void))
                {
                    type = assignment.Value.OutputType(analysisLocal);
                }

                if (type is null || type == typeof(void))
                {
                    type = typeof(object);
                }

                if (!scope.Types.TryGetValue(assignment.Name, out var existing) || existing != type)
                {
                    scope.Types[assignment.Name] = type;
                    changed = true;
                }

                analysisLocal.LocalVarTypes[assignment.Name] = type;
            }

            if (!changed)
            {
                break;
            }
        }
    }
}
