using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 函数 ，作为一种变量存在
/// </summary>
public class FuncLangValue : ImportInfo
{
    public readonly LangId? Id;
    public readonly BlockStatement BlockStatement = new([]);

    public readonly List<LangId>? Ids;

    public readonly MethodInfo? Method;

    private readonly FuncLangValue? Func;

    // 闭包环境：捕获的作用域，用于支持闭包变量访问
    private VariateManager? CapturedScope { get; init; }

    public FuncLangValue(LangId? id, List<LangId> ids, BlockStatement blockStatement,
        SourcePosition position = default) :
        base(position)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
    }

    public FuncLangValue(string idName, MethodInfo methodInfo, FuncLangValue? func = null,
        SourcePosition position = default) : base(position)
    {
        Id = new LangId(idName);
        Method = methodInfo;
        Func = func;
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 如果这个函数没有方法引用（即是 Old8Lang 函数而非原生方法）
        // 创建一个新的 FuncLangValue 副本，并捕获当前作用域
        // 这样每次返回函数时都会捕获各自的作用域，支持闭包
        if (Method == null && Ids != null)
        {
            var closureFunc = new FuncLangValue(Id, Ids, BlockStatement, Position)
            {
                // 克隆当前作用域，保存闭包环境的快照
                // 这样即使外部函数返回后，闭包仍然可以访问捕获的变量
                CapturedScope = manager.Clone()
            };
            return closureFunc;
        }

        // 原生方法或其他情况直接返回自身
        return this;
    }

    public LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
    {
        if (Method != null)
        {
            // 检查参数数量是否匹配（Method的参数数量减去this参数）
            var expectedParams = Method.GetParameters().Length;
            if (obj != null) expectedParams--; // 如果有this参数，减去1
            var actualParams = ids.Count;
            if (expectedParams != actualParams)
            {
                throw new ArgumentError(Position,
                    $"方法 '{Method.Name}' 期望 {expectedParams} 个参数，但实际提供了 {actualParams} 个参数");
            }

            var values = ids.Select(expr => expr.Run(variateManagerFunc)).ToList();
            var a = Apis.ListToObjects(values).ToArray();

            object? invoke;
            try
            {
                invoke = Method?.Invoke(obj, a);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // 转换 .NET 异常为 Old8Lang 异常
                var innerException = ex.InnerException;

                // FileNotFoundException 和 DirectoryNotFoundException -> FileNotFoundError
                if (innerException is FileNotFoundException fileEx)
                {
                    throw new FileNotFoundError(Position, fileEx.FileName ?? "未知文件");
                }

                if (innerException is DirectoryNotFoundException dirEx)
                {
                    throw new FileNotFoundError(Position, dirEx.Message);
                }

                // ArgumentException -> ValueError
                if (innerException is ArgumentException argEx)
                {
                    throw new ValueError(Position, argEx.Message);
                }

                // UnauthorizedAccessException -> PermissionError
                if (innerException is UnauthorizedAccessException uaEx)
                {
                    throw new PermissionError(Position, uaEx.Message);
                }

                // NotImplementedException -> NotImplementedError
                if (innerException is NotImplementedException niEx)
                {
                    throw new NotImplementedError(Position, niEx.Message);
                }

                // TimeoutException -> TimeoutError
                if (innerException is TimeoutException toEx)
                {
                    throw new TimeoutError(Position, toEx.Message);
                }

                // InvalidCastException -> TypeError
                if (innerException is InvalidCastException icEx)
                {
                    throw new TypeError(this, icEx.Message);
                }

                // OverflowException -> OverflowError
                if (innerException is OverflowException ofEx)
                {
                    throw new OverflowError(Position, ofEx.Message);
                }

                // 其他异常保持原样
                throw;
            }

            if (invoke is null)
                return new VoidLangValue();

            var manager = new VariateManager();
            var convertedValue = ObjToValue(invoke);
            manager.Init(new Dictionary<string, LangValueType> { { "base", convertedValue } });
            manager.IsClass = false;
            manager.Result = convertedValue;
            Func?.Run(manager, ids);
            return manager.Result;
        }

        // 检查参数数量是否匹配，但允许省略带默认参数的实参
        if (Ids != null)
        {
            var expectedParams = Ids.Count;
            var actualParams = ids.Count;

            // 只检查最大参数数量，允许实际参数少于期望参数（如果有默认参数）
            if (actualParams > expectedParams)
            {
                throw new ArgumentError(Position,
                    $"函数 '{Id?.IdName}' 期望最多 {expectedParams} 个参数，但实际提供了 {actualParams} 个参数");
            }
        }

        // 调用方法体
        // 递归深度检查
        variateManagerFunc.RecursionDepth++;
        try
        {
            // 如果有捕获的作用域（闭包），使用捕获的作用域而不是调用时的作用域
            // 这样函数体就能访问定义时的外部变量
            VariateManager executionManager;
            if (CapturedScope != null)
            {
                // 使用捕获的作用域作为基础
                executionManager = CapturedScope;
                // 增加递归深度
                executionManager.RecursionDepth = variateManagerFunc.RecursionDepth;
            }
            else
            {
                // 没有捕获作用域，使用调用时的作用域
                executionManager = variateManagerFunc;
            }

            executionManager.AddChildren();
            executionManager.IsFunc = true; // 设置为函数上下文

            // 将静态成员添加到方法的变量管理器中
            var thisValue = executionManager.GetValue(new LangId("this"));
            if (thisValue is AnyLangValue)
            {
                // 将类的静态成员添加到方法的变量管理器中
                foreach (var importInfo in executionManager.ImportInfos)
                {
                    if (importInfo is TypeTemplate typeTemplate)
                    {
                        foreach (var staticMember in typeTemplate.StaticVariates)
                        {
                            executionManager.Set(staticMember.Key, staticMember.Value.Run(executionManager));
                        }
                    }
                }
            }

            if (Ids != null && Ids.Count != 0)
            {
                // 先计算所有传入参数的值，使用外部变量管理器
                var paramValues = ids.Select(t => t.Run(variateManagerFunc)).ToList();

                // 处理默认参数，补全缺失的参数值
                for (var i = paramValues.Count; i < Ids.Count; i++)
                {
                    var id = Ids[i];
                    if (id.DefaultValue != null)
                    {
                        // 计算默认参数值
                        var defaultValue = id.DefaultValue.Run(executionManager);
                        paramValues.Add(defaultValue);
                    }
                    else
                    {
                        // 没有默认参数且没有传入参数，抛出错误
                        throw new ArgumentError(Position,
                            $"函数 '{Id?.IdName}' 的参数 '{id.IdName}' 缺少实参且没有默认值");
                    }
                }

                // 然后将所有参数值（包括默认参数）设置到函数的变量管理器中
                for (var i = 0; i < Ids.Count; i++)
                {
                    executionManager.Set(Ids[i], paramValues[i]);
                }
            }

            // 参数设置完成后，恢复非函数上下文标志
            // 这样函数体中的赋值语句可以正常查找和修改外部作用域的变量
            executionManager.IsFunc = false;

            // 运行方法体
            BlockStatement.Run(executionManager);

            // 保存返回值
            var result = executionManager.Result;

            // 重置return标志，确保函数调用不会影响外部上下文
            executionManager.IsReturn = false;

            // 移除子作用域，但是要注意，在init方法中使用this关键字设置的值已经被保存到实例中了
            // 所以这里移除子作用域不会影响实例的状态
            executionManager.RemoveChildren();

            return result;
        }
        finally
        {
            // 确保递归深度总是被递减
            variateManagerFunc.RecursionDepth--;
        }
    }

    public override Type OutputType(LocalManager local)
    {
        var idType = Id?.OutputType(local);
        if (idType != null && idType != typeof(object)) return idType;
        var a = GetItemType(BlockStatement, local);
        return a;
    }

    private static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.OutputType(local);
                if (varType != null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                return returnStatement.OutputType(local);
            }

            if (item == null || item.Count == 0)
            {
                continue;
            }

            var innerType = GetItemType(item, local);
            if (innerType != typeof(void))
            {
                return innerType;
            }
        }

        return typeof(void);
    }

    public override string ToString()
    {
        if (Method != null)
        {
            return $"{Method}";
        }

        var paramList = Ids != null ? string.Join(", ", Ids) : string.Empty;
        return $"func {Id}({paramList}) \n {{ {BlockStatement} }}";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 如果是.NET方法，直接加载方法引用
        if (Method != null)
        {
            // 对于实例方法，需要先加载对象实例到堆栈上
            // 这里假设Method已经是正确的委托类型
        }

        // 如果是Old8Lang函数，直接返回，因为函数调用是通过Instance类处理的
        // 不需要在这里加载函数委托
    }

    public void LoadIl(MethodBuilder methodBuilder, LocalManager local)
    {
        //var funcLocal = new LocalManager();
        var parameterTypes = Ids!.Select(item => item.OutputType(local)).ToArray();

        // 创建方法的 IL 发射器
        var methodIl = methodBuilder.GetILGenerator();

        // 检查方法是否是实例方法（第一个参数是this）
        // 对于实例方法，第一个参数是this，真正的参数从索引1开始
        int startIndex = 0;
        var methodParams = methodBuilder.GetParameters();
        if (methodParams.Length > Ids!.Count)
        {
            // 有额外的参数，说明是实例方法，第一个参数是this
            startIndex = 1;
        }

        for (var i = 0; i < Ids!.Count; i++)
        {
            var id = Ids[i];
            var localVar = methodIl.DeclareLocal(parameterTypes[i]);
            local.AddLocalVar(id.IdName, localVar);
            methodIl.Emit(OpCodes.Ldarg, startIndex + i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        local.DelegateVar.Add(Id!.IdName, methodBuilder);

        // 生成方法体的 IL 代码
        BlockStatement.GenerateIl(methodIl, local);

        // 返回
        methodIl.Emit(OpCodes.Ret);
    }
}