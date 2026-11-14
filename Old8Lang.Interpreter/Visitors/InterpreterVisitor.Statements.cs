using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;

using Old8Lang.Concurrency;
using Old8Lang.ExternProviders;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Symbols;
using Old8Lang.ProjectManagement;
using Old8Lang.Runtime;
using Old8Lang.TypeSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// InterpreterVisitor - Statement 节点的 Visit 方法实现
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>
    /// 访问 ExtensionDeclaration 节点
    /// </summary>
    public LangValueType VisitExtensionDeclaration(ExtensionDeclaration node)
    {

        // 确保实例方法系统已初始化
        InstanceMethodInitializer.EnsureInitialized();

        // 获取目标类型
        var targetType = ExtensionDeclarationRuntimeExtensions.ResolveTargetType(node.TargetTypeName);
        if (targetType == null)
        {
            throw new InvalidOperationError(node, $"无法找到类型 '{node.TargetTypeName}'");
        }

        // 注册每个扩展方法
        foreach (var method in node.GetExtensionMethods())
        {
            // 创建扩展方法包装器
            var extensionMethod = new ExtensionMethodWrapper(
                targetType,
                method,
                manager,
                node.TargetGenericParameters
            );

            // 注册到实例方法注册器
            InstanceMethodRegistry.Instance.Register(extensionMethod);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 BreakStatement 节点
    /// </summary>
    public LangValueType VisitBreakStatement(BreakStatement node)
    {
        // 迁移自 BreakStatement.Run()
        manager.ControlFlowManager.BreakFlag = true;
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ContinueStatement 节点
    /// </summary>
    public LangValueType VisitContinueStatement(ContinueStatement node)
    {
        // 迁移自 ContinueStatement.Run()
        manager.ControlFlowManager.ContinueFlag = true;
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 IfStatement 节点
    /// </summary>
    public LangValueType VisitIfStatement(IfStatement node)
    {
        // 迁移自 IfStatement.Run()
        var r = true;

        // 保存原始的 IsFunc 状态
        var originalIsFunc = manager.IsFunc;

        // 处理 if 块
        manager.AddChildren();
        // 在 if 语句块中，临时禁用函数上下文，允许修改外部变量
        manager.IsFunc = false;

        // 访问 ifChildBlock（直接调用其逻辑，因为 IfChild 不支持 Visitor）
        var ifChild = node[0] as IfChild;
        if (ifChild is not null && r)
        {
            var predicate = ifChild.Condition.Accept(this);
            if (predicate is BoolLangValue { Value: true })
            {
                ifChild.Block.Accept(this);
                r = false;
            }
        }

        manager.RemoveChildren();

        // 处理 elif 块
        for (int i = 1; i < node.Count; i++)
        {
            var elifChild = node[i] as IfChild;
            if (elifChild is not null)
            {
                manager.AddChildren();
                // 在 elif 语句块中，临时禁用函数上下文，允许修改外部变量
                manager.IsFunc = false;
                if (r)
                {
                    var predicate = elifChild.Condition.Accept(this);
                    if (predicate is BoolLangValue { Value: true })
                    {
                        elifChild.Block.Accept(this);
                        r = false;
                    }
                }
                manager.RemoveChildren();
            }
            else if (node[i] is BlockStatement elseBlock)
            {
                // else 块
                if (r)
                {
                    elseBlock.Accept(this);
                }
            }
        }

        // 恢复原始的 IsFunc 状态
        manager.IsFunc = originalIsFunc;

        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 BlockStatement 节点
    /// </summary>
    public LangValueType VisitBlockStatement(BlockStatement node)
    {
        // 迁移自 BlockStatement.Run()
        // 检查是否有生成器上下文，决定执行模式
        if (manager.GeneratorContext is not null)
        {
            RunWithGeneratorContext(node);
        }
        else
        {
            // 标准执行模式（非生成器）
            // 先执行导入语句
            ExecuteImports(node);

            // 顺序执行所有语句
            for (int i = 0; i < node.Count; i++)
            {
                var statement = node[i];
                if (statement is not null)
                {
                    // 未迁移的节点仍使用其旧执行方法；逐节点切换到 Visitor。
                    statement.Accept(this);

                    // 检查 return 语句
                    if (manager.IsReturn)
                    {
                        return new VoidLangValue();
                    }

                    // 检查 break 和 continue 语句
                    if (manager.ControlFlowManager.BreakFlag || manager.ControlFlowManager.ContinueFlag)
                    {
                        return new VoidLangValue();
                    }
                }
            }
        }

        return new VoidLangValue();
    }

    private void ExecuteImports(BlockStatement node)
    {
        foreach (var statement in node.ImportStatements)
        {
            statement.Accept(this);
        }
    }

    /// <summary>
    /// 使用新架构运行（基于路径的状态恢复）
    /// </summary>
    /// <param name="node">正在执行的块语句。</param>
    private void RunWithGeneratorContext(BlockStatement node)
    {
        var context = manager.GeneratorContext!;

        // 先执行导入语句
        ExecuteImports(node);

        // 检查是否需要恢复执行
        bool isResuming = !string.IsNullOrEmpty(context.ExecutionPath);

        // 从头开始执行，使用路径匹配决定是否跳过语句
        for (int i = 0; i < node.OtherStatements.Count; i++)
        {
            var statement = node.OtherStatements[i];

            // 构建当前语句的路径
            var statementPath = $"/block[{i}]";
            context.PathStack.Push(statementPath);

            try
            {
                var currentPath = context.GetCurrentPath();

                // 如果正在恢复执行，检查是否应该跳过当前语句
                if (isResuming)
                {
                    // 如果执行路径等于当前路径+"/yield"，说明这是上次yield的位置
                    // 应该跳过这个语句，从下一个语句开始执行
                    if (context.ExecutionPath == currentPath + "/yield")
                    {
                        // 清除 ExecutionPath 和恢复标志，从下一个语句开始正常执行
                        context.ExecutionPath = "";
                        isResuming = false;
                        continue;
                    }

                    // 如果执行路径不以当前路径开头，说明这个语句在恢复点之前，跳过
                    if (!context.ExecutionPath!.StartsWith(currentPath))
                    {
                        continue;
                    }
                }

                // 保存当前的 IsReturn 标志，以防止嵌套函数调用影响生成器的控制流
                var savedIsReturn = manager.IsReturn;

                // 执行语句
                statement.Accept(this);

                // 恢复 IsReturn 标志（如果语句是普通函数调用，它会设置并重置 IsReturn，但我们需要确保生成器的 IsReturn 不受影响）
                // 注意：如果语句本身是 return 语句，我们需要保留它设置的 IsReturn
                if (statement is not ReturnStatement)
                {
                    manager.IsReturn = savedIsReturn;
                }

                // 检查是否遇到yield
                if (context.HasYielded)
                {
                    return;
                }

                // 执行完语句后，如果 ExecutionPath 被清除，说明该语句已完成（比如循环结束）
                // 此时应该取消恢复模式，继续正常执行后续语句
                if (isResuming && string.IsNullOrEmpty(context.ExecutionPath))
                {
                    isResuming = false;
                }

                // 检查是否遇到return或其他控制流
                if (manager.IsReturn || context.IsCompleted)
                {
                    context.IsCompleted = true;
                    return;
                }

                // 检查 break 和 continue 语句
                if (manager.ControlFlowManager.BreakFlag || manager.ControlFlowManager.ContinueFlag)
                {
                    return;
                }
            }
            finally
            {
                // 弹出当前路径
                context.PathStack.Pop();
            }
        }

        // 执行完毕，只有主函数体的 BlockStatement 才设置 IsCompleted
        // 循环体等嵌套的 BlockStatement 不应该设置 IsCompleted
        // 通过检查路径栈来判断：如果栈为空，说明是主函数体
        if (context.PathStack.Count == 0)
        {
            context.IsCompleted = true;
        }
    }

    /// <summary>
    /// 访问 WhileStatement 节点
    /// </summary>
    public LangValueType VisitWhileStatement(WhileStatement node)
    {

        // 检查是否使用新的生成器架构
        if (manager.GeneratorContext is not null)
        {
            // 新架构：标准 while 循环，生成器断点由 BlockStatement 处理
            node.RunWithGeneratorContext(manager);
        }
        else
        {
            // 标准 while 循环（非生成器）
            node.RunStandard(manager);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 ForStatement 节点
    /// </summary>
    public LangValueType VisitForStatement(ForStatement node)
    {

        manager.AddChildren();
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            // 使用 SetLocal 确保循环变量在当前作用域中创建，防止污染外层同名变量
            // 例如：外层 for i <- 0, i < 20 调用函数，函数内 for i <- 0, i < 5 不应修改外层 i
            if (node.Init.Id is not null)
            {
                var initValue = node.Init.Value.Accept(this);
                manager.SetLocal(node.Init.Id, initValue);
            }
            else
            {
                node.Init.Accept(this);
            }

            // 性能优化：检测简单循环模式（可以优化的循环）
            bool isSimpleLoop = node.IsSimpleCountingLoop();

            while (true)
            {
                // 重置控制流状态，确保每次循环迭代开始时清除之前的break/continue标志
                manager.ControlFlowManager.ResetCurrentState();

                var varExpr = node.Condition.Accept(this);
                bool expr1;
                if (varExpr is BoolLangValue value)
                {
                    expr1 = value.Value;
                    // 优化：将临时布尔对象归还到对象池
                    value.ReturnToPool();
                }
                else
                    throw new TypeError(node, "期望布尔类型", $"实际得到了 {varExpr.GetType().Name}");

                if (expr1)
                {
                    // 记录循环迭代（性能监控）
                    manager.Interpreter?.PerformanceMonitor?.RecordLoopIteration();

                    node.Block.Accept(this);

                    // 处理yield：如果循环体中遇到yield，返回以暂停执行
                    if (manager.IsYield)
                    {
                        // 提前执行循环增量，确保生成器恢复时继续推进
                        node.Operation.Accept(this);
                        return new VoidLangValue();
                    }

                    // 处理break
                    if (manager.ControlFlowManager.BreakFlag)
                    {
                        manager.ControlFlowManager.BreakFlag = false;
                        break;
                    }

                    // 处理continue，执行循环增量操作
                    if (manager.ControlFlowManager.ContinueFlag)
                    {
                        manager.ControlFlowManager.ContinueFlag = false;
                        node.Operation.Accept(this);
                        continue;
                    }

                    // 正常执行，执行循环增量操作
                    node.Operation.Accept(this);
                }
                else
                    break;
            }
        }
        finally
        {
            // 弹出当前控制流状态
            manager.ControlFlowManager.PopState();
            manager.RemoveChildren();
        }
    
    
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ForInStatement 节点
    /// </summary>
    public LangValueType VisitForInStatement(ForInStatement node)
    {

        // 检查是否在生成器上下文中
        if (manager.GeneratorContext is not null)
        {
            node.RunWithGeneratorContext(manager);
        }
        else
        {
            node.RunStandard(manager);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 SetStatement 节点
    /// </summary>
    public LangValueType VisitSetStatement(SetStatement node)
    {

        var result = node.Value.Accept(this);

        var tuplePattern = node.LeftExpression switch
        {
            TupleLiteralExpression syntax => syntax.Elements,
            TupleLangValue legacy => (IReadOnlyList<LangExpression>)legacy.Elements,
            _ => null
        };
        if (tuplePattern is not null)
        {
            if (result is not ILangList values)
                throw new TypeError(node, "元组解构赋值右侧必须是可索引的集合");

            var items = values.GetItems().ToList();
            for (var i = 0; i < tuplePattern.Count; i++)
            {
                if (tuplePattern[i] is not LangId id)
                    throw new InvalidOperationError(node, "元组解构赋值只支持变量名");
                if (i >= items.Count)
                    throw new InvalidOperationError(node, $"元组解构赋值索引 {i} 超出范围");
                manager.Set(id, items[i]);
            }
            return new VoidLangValue();
        }

        // 检查是否为首次赋值
        if (node.Id is not null)
        {
            var existingVariable = manager.GetValue(node.Id);
            var isInitialAssignment = existingVariable is null;

            // 如果有类型注解，进行类型检查
            if (!string.IsNullOrEmpty(node.Id.AssumptionType))
            {
                TypeChecker.ValidateVariableAssignment(node.Id.AssumptionType, result, node, node.Id.IdName, isInitialAssignment);
            }
            else if (!isInitialAssignment)
            {
                // 没有类型注解但是修改已存在的变量，检查是否为 const 变量
                if (TypeChecker.IsConstVariable(node.Id.IdName))
                {
                    throw new TypeError(node, "any", TypeChecker.GetLangValueType(result),
                        $"不能修改 const 变量 '{node.Id.IdName}'");
                }
            }
        }

        // 处理嵌套索引访问赋值：array[0][0] <- value
        if (Old8Lang.Runtime.LiteralCollectionRuntimeFactory.TryCreateNestedIndex(node.LeftExpression, out var nestedIndexAccess))
        {
            // 运行基础表达式，获取容器对象
            var baseResult = nestedIndexAccess.BaseExpression.Accept(this);

            // 获取嵌套索引值
            var nestedIndexValue = nestedIndexAccess.NestedIndex.Accept(this);

            // 检查基础结果是否支持索引访问
            if (baseResult is ILangList baseCollection)
            {
                baseCollection.Set(nestedIndexValue, result);
                return new VoidLangValue();
            }

            throw new InvalidOperationError(node, $"不支持的嵌套索引赋值类型: {baseResult.GetType().Name}");
        }

        // 处理成员访问赋值：node.name <- value, person.name <- value
        if (node.LeftExpression is Operation operation)
        {
            // 处理数组解构赋值：[a, b] <- [1, 2]
            if (operation is { Left: LangId { IdName: "array_destruct" }, Opera: LangTokenType.LeftBracket })
            {
                // 从字符串中解析解构的标识符列表
                var identifiersStr = operation.Right?.ToString() ??
                                     throw new InvalidOperationError(node, "数组解构赋值需要有效的标识符列表");

                // 解析标识符列表字符串，格式："a,b,null"
                var identStrs = identifiersStr.Split(',');

                // 获取右侧数组值
                if (result is ListLangValue listValue)
                {
                    // 列表类型
                    for (int i = 0; i < identStrs.Length; i++)
                    {
                        var identStr = identStrs[i].Trim();
                        if (identStr == "null") continue; // 跳过空元素

                        // 获取列表元素
                        if (i < listValue.Values.Count)
                        {
                            var elementValue = listValue.Values[i];
                            // 执行赋值
                            manager.Set(new LangId(identStr), elementValue);
                        }
                    }
                }
                else if (result is ArrayLangValue arrayValue)
                {
                    // 数组类型
                    var items = arrayValue.GetItems().ToList();
                    for (int i = 0; i < identStrs.Length; i++)
                    {
                        var identStr = identStrs[i].Trim();
                        if (identStr == "null") continue; // 跳过空元素

                        // 获取数组元素
                        if (i < items.Count)
                        {
                            var elementValue = items[i];
                            // 执行赋值
                            manager.Set(new LangId(identStr), elementValue);
                        }
                    }
                }
                else
                {
                    throw new TypeError(node, "数组解构赋值右侧必须是数组或列表类型");
                }

                return new VoidLangValue();
            }

            // 处理对象解构赋值：{name, age} <- person
            if (operation is { Left: LangId { IdName: "object_destruct" }, Opera: LangTokenType.LeftBrace })
            {
                // 从字符串中解析解构的属性列表
                var propertiesStr = operation.Right?.ToString() ??
                                    throw new InvalidOperationError(node, "对象解构赋值需要有效的属性列表");

                // 获取右侧对象值
                if (result is not AnyLangValue objectValue)
                {
                    throw new TypeError(node, "对象解构赋值右侧必须是对象类型");
                }

                // 解析属性列表字符串，格式："name,age,id:userId"
                var propStrs = propertiesStr.Split(',');

                // 执行解构赋值
                foreach (var propStr in propStrs)
                {
                    var propStrTrimmed = propStr.Trim();
                    if (propStrTrimmed.Contains(":"))
                    {
                        // 处理带别名的对象解构：name: newName
                        var parts = propStrTrimmed.Split(':');
                        var propName = parts[0].Trim();
                        var aliasName = parts[1].Trim();

                        // 获取属性值
                        if (objectValue.InstanceData.TryGetValue(propName, out var propValue))
                        {
                            // 执行赋值
                            manager.Set(new LangId(aliasName), propValue);
                        }
                        else
                        {
                            throw new NameError(node, propName);
                        }
                    }
                    else
                    {
                        // 处理基本对象解构：name
                        var propName = propStrTrimmed;

                        // 获取属性值
                        if (objectValue.InstanceData.TryGetValue(propName, out var propValue))
                        {
                            // 执行赋值
                            manager.Set(new LangId(propName), propValue);
                        }
                        else
                        {
                            throw new NameError(node, propName);
                        }
                    }
                }

                return new VoidLangValue();
            }

            // 检查是否是 DOT 操作（成员访问或嵌套索引）
            if (operation.Opera == LangTokenType.Dot)
            {
                // 首先检查是否是嵌套索引赋值：matrix[0][1] <- value
                // 左侧应该是LangListItem（第一层索引），右侧应该是索引表达式
                if (operation.Right is { } finalIndex && Old8Lang.Runtime.LiteralCollectionRuntimeFactory.TryCreateIndex(operation.Left, out var outerListItem))
                {
                    // 获取外层索引的值：matrix[0] 返回内层数组
                    var outerCollectionValue = manager.GetValue(outerListItem.ListId);
                    var outerIndexValue = outerListItem.Key.Accept(this);

                    if (outerCollectionValue is ArrayLangValue outerArray)
                    {
                        // 获取内层数组
                        var innerArray = outerArray.Get(outerIndexValue as IntLangValue ??
                                                        throw new TypeError(node, "IntLangValue",
                                                            outerIndexValue.GetType().Name));

                        // 检查内层数组是否是ILangList类型
                        if (innerArray is ILangList innerListCollection)
                        {
                            // 获取最终索引
                            var finalIndexValue = finalIndex.Accept(this);

                            // 设置内层数组的元素
                            innerListCollection.Set(finalIndexValue, result);
                            return new VoidLangValue();
                        }
                    }
                    else if (outerCollectionValue is ListLangValue outerList)
                    {
                        // 获取内层数组
                        var innerArray = outerList.Get(outerIndexValue as IntLangValue ??
                                                       throw new TypeError(node, "IntLangValue",
                                                           outerIndexValue.GetType().Name));

                        // 检查内层数组是否是ILangList类型
                        if (innerArray is ILangList innerListCollection)
                        {
                            // 获取最终索引
                            var finalIndexValue = finalIndex.Accept(this);

                            // 设置内层数组的元素
                            innerListCollection.Set(finalIndexValue, result);
                            return new VoidLangValue();
                        }
                    }
                }

                // 处理 ClassName.staticField <- value 形式的静态字段赋值
                if (operation is { Left: LangId className, Right: LangId staticMemberName })
                {
                    // 获取类类型
                    if (manager.GetAny(className) is TypeTemplate classType)
                    {
                        // 设置静态字段值
                        classType.SetStaticMember(staticMemberName.IdName, result, manager);
                        return new VoidLangValue();
                    }
                }

                // 处理 node.member <- value 形式的赋值
                if (operation is { Left: LangId { IdName: "this" }, Right: LangId memberName })
                {
                    // 查找当前实例 - 支持 V1 和 V2
                    var thisValue = manager.GetValue(new LangId("this"));

                    if (thisValue is AnyLangValue anyValue)
                    {
                        // V2 架构：直接使用 SetField
                        anyValue.SetField(memberName.IdName, result, manager);
                        return new VoidLangValue();
                    }

                    // 如果没有找到，可能是在init方法中，此时需要检查manager.IsFunc标志
                    if (manager.IsFunc)
                    {
                        // 在init方法中，当前实例应该是manager中的 node
                        if (thisValue is AnyLangValue anyValueInit)
                        {
                            // V2 架构
                            anyValueInit.SetField(memberName.IdName, result, manager);
                            return new VoidLangValue();
                        }

                        throw new NameError(node, "this");
                    }
                }
                // 处理普通对象成员访问：person.name <- value
                else if (operation is { Left: { } leftExpr, Right: LangId memberNameObj })
                {
                    // 获取左侧对象的值
                    var leftValue = leftExpr.Accept(this);
                    if (leftValue is AnyLangValue anyObjV2)
                    {
                        anyObjV2.SetField(memberNameObj.IdName, result, manager);
                        return new VoidLangValue();
                    }
                    // 处理内置类型（ThreadLangValue 等）的属性设置
                    else if (leftValue is ThreadLangValue threadValue)
                    {
                        switch (memberNameObj.IdName)
                        {
                            case "IsBackground":
                                if (result is BoolLangValue boolValue)
                                {
                                    threadValue.IsBackground = boolValue.Value;
                                    return new VoidLangValue();
                                }

                                throw new TypeError(node, "bool", result.TypeToString());
                            case "Name":
                                if (result is StringLangValue stringValue)
                                {
                                    threadValue.Name = stringValue.Value;
                                    return new VoidLangValue();
                                }

                                throw new TypeError(node, "string", result.TypeToString());
                            case "Priority":
                                if (result is IntLangValue intValue)
                                {
                                    threadValue.Priority = intValue.Value;
                                    return new VoidLangValue();
                                }

                                throw new TypeError(node, "int", result.TypeToString());
                            default:
                                throw new AttributeError(leftValue, memberNameObj.IdName, "Thread");
                        }
                    }
                }
            }
        }
        // 处理切片赋值：array[start:end] <- values
        else if (node.LeftExpression is SliceExpression or SliceLangValue)
        {
            var sliceValue = node.LeftExpression is SliceExpression syntax
                ? Old8Lang.Runtime.LiteralCollectionRuntimeFactory.CreateSlice(syntax)
                : (SliceLangValue)node.LeftExpression;
            // 获取集合对象
            var collectionValue = sliceValue.Id.Accept(this);

            // 检查集合是否是ILangList类型
            if (collectionValue is not ILangList listCollection)
            {
                throw new InvalidOperationError(node,
                    $"类型 '{collectionValue.GetType().Name}' 不支持切片赋值操作");
            }

            // 计算切片参数
            var length = listCollection.GetLength();
            var start1 = sliceValue.Start?.Accept(new InterpreterVisitor(manager));
            var end1 = sliceValue.End?.Accept(new InterpreterVisitor(manager));
            var step1 = sliceValue.Step?.Accept(new InterpreterVisitor(manager));

            var stepValue = step1?.GetValue<int>() ?? 1;

            if (stepValue == 0)
                throw new InvalidOperationError(node, "切片步长不能为0");

            if (stepValue != 1)
            {
                throw new InvalidOperationError(node,
                    "切片赋值不支持步长参数。如果需要使用步长，请使用循环逐个赋值");
            }

            // 计算起始和结束索引
            var startValue = start1?.GetValue<int>() ?? 0;
            var endValue = end1?.GetValue<int>() ?? length;

            // 获取要赋值的值列表
            IEnumerable<LangValueType> valuesList;
            if (result is ILangList resultList)
            {
                valuesList = resultList.GetItems();
            }
            else
            {
                throw new TypeError(node, "ILangList", result.GetType().Name,
                    "切片赋值的右侧必须是列表、数组或其他可迭代类型");
            }

            // 调用SetSlice方法
            listCollection.SetSlice(startValue, endValue, valuesList);
            return new VoidLangValue();
        }
        // 处理索引访问赋值：array[index] <- value, list[index] <- value, dict[key] <- value
        else if (Old8Lang.Runtime.LiteralCollectionRuntimeFactory.TryCreateIndex(node.LeftExpression, out var listItem))
        {
            // 获取集合对象
            var collectionValue = manager.GetValue(listItem.ListId);
            // 获取索引或键
            var indexValue = listItem.Key.Accept(this);

            // 检查是否是 AnyLangValue（类实例）且定义了 _setitem 方法
            if (collectionValue is AnyLangValue anyValue)
            {
                var setitemMethods = anyValue.Metadata.MethodTable.LookupMethod("_setitem");
                if (setitemMethods is not null && setitemMethods.Count > 0)
                {
                    // 创建一个 Instance 表达式来调用 _setitem 方法
                    var setitemCall = new Instance(
                        new LangId("_setitem"),
                        [indexValue, result],
                        [],
                        node.Position
                    );

                    // 通过 Dot 操作调用方法
                    anyValue.Dot(setitemCall, manager);
                    return new VoidLangValue();
                }
            }

            // 检查集合是否是ILangList类型，如果是则调用其Set方法
            if (collectionValue is ILangList listCollection)
            {
                listCollection.Set(indexValue, result);
                return new VoidLangValue();
            }
        }

        // 处理普通变量赋值：name <- value
        if (node.Id is not null && !string.IsNullOrEmpty(node.Id.IdName))
        {
            manager.Set(node.Id, result);
        }
    
    
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ReturnStatement 节点
    /// </summary>
    public LangValueType VisitReturnStatement(ReturnStatement node)
    {

        var result = node.Expression.Accept(this);

        // 如果当前函数有返回类型注解，进行类型检查和转换
        if (!string.IsNullOrEmpty(manager.CurrentFunctionReturnType))
        {
            var functionName = "anonymous";
            if (manager.GetValue(new LangId("this")) is AnyLangValue anyValue)
            {
                functionName = anyValue.ClassId.IdName;
            }
            // 使用新的带转换的验证方法，传递泛型类型参数映射
            result = TypeChecker.ValidateAndConvertReturnType(
                manager.CurrentFunctionReturnType,
                result,
                node,
                functionName,
                manager.CurrentFunctionTypeArgumentMapping);
        }

        manager.Result = result;
        manager.IsReturn = true;

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 ThrowStatement 节点
    /// </summary>
    public LangValueType VisitThrowStatement(ThrowStatement node)
    {

        var value = node.Expression.Accept(this);
        
        // 如果值已经是ErrorLangValue，直接抛出其包含的原始异常
        if (value is ErrorLangValue errorValue)
        {
            throw errorValue.Exception;
        }
        
        // 否则，创建一个新的CustomError
        throw new CustomError(
            node,
            value.ToDisplayString());

    
    }

    /// <summary>
    /// 访问 FuncInit 节点
    /// </summary>
    public LangValueType VisitFuncInit(FuncInit node)
    {

        // 验证 params 参数的合法性
        node.ValidateParamsParameter();

        // 检查函数是否已存在（只有当函数名、参数数量、参数类型和返回类型都相同时才视为重复）
        // 但对于来自不同模块的函数，允许重复（它们可能通过别名导入）
        // 在函数内部定义的函数（嵌套函数）不检查重复，因为每次调用外层函数都会重新定义
        if (node.GetFuncValue().Id is not null && !manager.IsFunc)
        {
            var existingFunc = manager.ImportInfos.FirstOrDefault(info =>
                info is FuncLangValue func &&
                func.Id?.IdName == node.GetFuncValue().Id.IdName &&
                func.Ids?.Count == node.GetFuncValue().Ids?.Count &&
                func.Ids?.Zip(node.GetFuncValue().Ids!, (a, b) => a.AssumptionType == b.AssumptionType).All(x => x) == true &&
                func.Id?.AssumptionType == node.GetFuncValue().Id?.AssumptionType);

            // 只有当存在完全相同的函数，并且正在导入栈为空时（即在主文件中重复定义），才报错
            // 如果导入栈不为空，说明是在导入的模块中定义函数，允许不同模块的同名函数共存
            if (existingFunc is not null && manager.ImportStack.Count == 0)
            {
                throw new DuplicateNameError(node, node.GetFuncValue().Id.IdName, "函数");
            }
        }

        // 不立即应用装饰器，而是保留装饰器信息
        // 装饰器将在函数第一次被调用时应用（延迟应用）
        // 这样可以确保装饰器可以访问全局变量
        var finalFunc = node.GetFuncValue();

        // 如果函数还没有捕获作用域，并且当前在模块导入上下文中（ImportStack不为空），
        // 则捕获当前作用域，以便函数能访问模块中导入的符号
        if (finalFunc.GetCapturedScope() is null && manager.ImportStack.Count > 0)
        {
            // 创建带有捕获作用域的函数副本
            finalFunc = new FuncLangValue(
                finalFunc.Id,
                finalFunc.Ids ?? [],
                finalFunc.BlockStatement,
                finalFunc.GenericParameters,
                finalFunc.Position,
                finalFunc.Id is null) // IsLambda
            {
                CapturedScope = manager.Clone(),
                DocComment = finalFunc.DocComment,
                Decorators = finalFunc.Decorators,
                TypeArgumentMapping = finalFunc.TypeArgumentMapping
            };
        }

        // 注册函数
        // 在函数内部定义的嵌套函数：添加到当前作用域（函数返回时自动清理）
        // 在顶层定义的函数：添加到 ImportInfosList（全局可见）
        if (manager.IsFunc && node.GetFuncValue().Id is not null)
        {
            manager.SetLocal(node.GetFuncValue().Id, finalFunc);
        }
        else
        {
            manager.AddClassAndFunc(finalFunc);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 AsyncFuncInit 节点
    /// </summary>
    public LangValueType VisitAsyncFuncInit(AsyncFuncInit node)
    {

        // 检查函数重复声明
        if (node.GetAsyncFuncValue().Id is not null)
        {
            var existingFunc = manager.ImportInfos.FirstOrDefault(info =>
                info is AsyncFuncLangValue func &&
                func.Id?.IdName == node.GetAsyncFuncValue().Id.IdName &&
                func.Ids?.Count == node.GetAsyncFuncValue().Ids?.Count);

            if (existingFunc is not null)
            {
                throw new DuplicateNameError(
                    node,
                    node.GetAsyncFuncValue().Id.IdName,
                    "异步函数"
                );
            }
        }

        // 应用装饰器（如果有）
        var finalFunc = AsyncFuncInitRuntimeExtensions.ApplyDecorators(node.GetAsyncFuncValue(), manager);

        // 添加到导入信息列表
        manager.AddClassAndFunc(finalFunc);

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 ClassInit 节点
    /// </summary>
    public LangValueType VisitClassInit(ClassInit node)
    {

        // 注册类型到类型假注系统
        node.RegisterTypeToTypeSystem();

        // 首先注册所有嵌套类
        node.RegisterNestedClasses(manager);

        // 检查类是否已存在
        var existingClass = manager.GetAny(new LangId(node.GetTypeTemplate().ClassName));

        if (existingClass is not null)
        {
            throw new DuplicateNameError(node, node.GetTypeTemplate().ClassName, "类");
        }

        // 立即将类添加到ImportInfos中，以便在类定义内部访问
        manager.AddClassAndFunc(node.GetTypeTemplate());

        // 注册类型到全局反射注册表（用于反射功能）
        TypeTemplate.RegisterType(node.GetTypeTemplate().ClassName, node.GetTypeTemplate());

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 SwitchStatement 节点
    /// </summary>
    public LangValueType VisitSwitchStatement(SwitchStatement node)
    {

        var switchValue = node.SwitchExpression.Accept(this);

        foreach (var oldCase in node.SwitchCaseList)
        {
            var caseValue = oldCase.Expression.Accept(this);
            bool isMatch;

            // 处理范围匹配：如果 caseValue 是数组，检查 switchValue 是否在数组中
            if (caseValue is ArrayLangValue arrayValue)
            {
                isMatch = arrayValue.GetItems().Any(item => switchValue.Equal(item));
            }
            // 普通相等匹配
            else
            {
                isMatch = switchValue.Equal(caseValue);
            }

            if (isMatch)
            {
                oldCase.BlockStatement.Accept(this);

                // 如果case块中执行了return语句，直接返回
                if (manager.IsReturn)
                {
                    return new VoidLangValue();
                }

                // 如果case块中执行了break，保留break标志（让它能够跳出外层循环）
                if (manager.ControlFlowManager.BreakFlag)
                {
                    return new VoidLangValue();
                }

                // 如果case块中执行了continue，保留continue标志（让外层循环处理）
                if (manager.ControlFlowManager.ContinueFlag)
                {
                }

                return new VoidLangValue();
            }
        }

        node.DefaultBlockStatement?.Accept(this);

        // default块中的break和continue保持原样，不需要特殊处理
        // break和continue标志会被保留，让外层循环处理
    
    
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 CaseStatement 节点
    /// </summary>
    public LangValueType VisitCaseStatement(CaseStatement node)
    {
        node.BlockStatement.Accept(this);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 TryStatement 节点
    /// </summary>
    public LangValueType VisitTryStatement(TryStatement node)
    {

        // 检查是否在生成器上下文中
        if (manager.GeneratorContext is not null)
        {
            node.RunWithGeneratorContext(manager);
        }
        else
        {
            node.RunStandard(manager);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 YieldStatement 节点
    /// </summary>
    public LangValueType VisitYieldStatement(YieldStatement node)
    {

        // 计算yield表达式的值
        var yieldValue = node.YieldExpression.Accept(this);

        // 通过生成器上下文设置yield值和标志
        var genContext = manager.GeneratorContext;
        if (genContext is not null)
        {
            genContext.CurrentValue = yieldValue;
            genContext.HasYielded = true;

            // 保存当前执行路径，以便下次从这里恢复
            genContext.ExecutionPath = genContext.GetCurrentPath() + "/yield";
        }
        else
        {
            // 如果在非生成器上下文中使用yield，设置全局标志（用于兼容旧代码）
            manager.Result = yieldValue;
            manager.IsYield = true;
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 ImportStatement 节点
    /// </summary>
    public LangValueType VisitImportStatement(ImportStatement node)
    {

        var moduleName = node.ImportString;

        // 动态导入处理
        if (node.IsDynamicImport)
        {
            node.HandleDynamicImport(manager);
            return new VoidLangValue();
        }

        // 懒导入处理
        if (node.IsLazy)
        {
            node.HandleLazyImport(manager);
            return new VoidLangValue();
        }

        var symbolAliases = node.Specifiers
            .Where(item => item.Name != item.Alias)
            .ToDictionary(item => item.Name, item => item.Alias);

        var options = new ImportOptions
        {
            IsFromClause = node.FromClause,
            ModuleAlias = node.ModuleAlias,
            ImportSpecifiers = node.Specifiers.Select(item => item.Name).ToList(),
            SymbolAliases = symbolAliases.Count > 0 ? symbolAliases : null,
            IsLazy = node.IsLazy,
            IsSelective = node.IsSelective
        };

        var result = ImportStatementRuntimeExtensions.ModuleService.ImportModule(moduleName, manager, options);

        if (!result.IsSuccess)
        {
            if (result.Error is not null)
            {
                throw result.Error;
            }

            // 使用 ModuleResolutionResult 提供详细错误信息
            if (result.ResolutionResult is not null)
            {
                throw new ImportError(node, moduleName, result.ResolutionResult);
            }

            throw new ImportError(node, moduleName, "模块导入失败");
        }
    
    
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 NativeStatement 节点
    /// </summary>
    public LangValueType VisitNativeStatement(NativeStatement node)
    {

        // 使用 DllPathResolver 查找 DLL 路径
        string path;
        try
        {
            path = DllPathResolver.ResolveDllPath(
                node.DllName,
                null, // 不再使用 importPath
                manager.Path,
                manager.StandardLibraries);
        }
        catch (FileNotFoundException ex)
        {
            // 包装异常，添加源代码位置信息
            throw new ImportError(node.Position, $"导入原生库失败：\n{ex.Message}");
        }

        // 加载程序集并获取类型
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFile(path);
        }
        catch (Exception ex)
        {
            throw new ImportError(node.Position, $"加载 DLL 文件失败：{path}\n错误：{ex.Message}");
        }

        var type = assembly.GetType($"{node.DllName}.{node.ClassName}");

        if (!string.IsNullOrEmpty(node.Name))
        {
            type = assembly.GetType($"{node.Name}.{node.ClassName}");
            if (type is null)
            {
                type = Type.GetType($"{node.Name}.{node.ClassName}");
                if (type is null)
                    throw new TypeError(node, $"找不到类型 {node.Name}.{node.ClassName}");
            }

            manager.AddClassAndFunc(new NativeStaticAny(node.ClassName, type));
            return new VoidLangValue();
        }

        if (!string.IsNullOrEmpty(node.MethodName))
        {
            var methodInfo = type?.GetMethod(node.MethodName);
            if (methodInfo is null) throw new InvalidOperationError(node, $"找不到方法 {node.MethodName} 在 {node.ClassName} 类中");
            if (string.IsNullOrEmpty(node.NativeName))
                node.NativeName = node.MethodName;
            var func = new FuncLangValue(node.NativeName, methodInfo, node.GetFuncValue());
            manager.AddClassAndFunc(func);
            return new VoidLangValue();
        }

        // 处理批量导入所有方法：native "DllName" ClassName *
        if (node.ImportAll)
        {
            var methods = type?.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if (methods is null || methods.Length == 0)
            {
                throw new InvalidOperationError(node, $"类 {node.ClassName} 中没有找到公共静态方法");
            }

            foreach (var method in methods)
            {
                // 过滤掉 Object 基类的方法
                if (method.DeclaringType == typeof(object))
                    continue;

                var func = new FuncLangValue(method.Name, method);
                manager.AddClassAndFunc(func);
            }

            return new VoidLangValue();
        }

        // 处理选择性导入多个方法：native "DllName" ClassName { Method1, Method2 }
        if (node.MethodList is { Count: > 0 })
        {
            foreach (var methodName in node.MethodList)
            {
                var methodInfo = type?.GetMethod(methodName);
                if (methodInfo is null)
                {
                    throw new InvalidOperationError(node, $"找不到方法 {methodName} 在 {node.ClassName} 类中");
                }

                var func = new FuncLangValue(methodName, methodInfo);
                manager.AddClassAndFunc(func);
            }

            return new VoidLangValue();
        }

        // 处理类导入（支持别名）：native "DllName" ClassName 或 native "DllName" ClassName as Alias
        var registerName = !string.IsNullOrEmpty(node.ClassAlias) ? node.ClassAlias : node.ClassName;
        var nativeClass = new NativeAnyLangValue(node.DllName, node.ClassName, path, registerName);
        var importInfo = (ImportInfo)nativeClass.Accept(this);
        manager.AddClassAndFunc(importInfo);
    
    
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 AsyncForInStatement 节点
    /// </summary>
    public LangValueType VisitAsyncForInStatement(AsyncForInStatement node)
    {

        // 检查是否在生成器上下文中
        if (manager.GeneratorContext is not null)
        {
            node.RunWithGeneratorContext(manager);
        }
        else
        {
            node.RunStandard(manager);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 DeferStatement 节点
    /// </summary>
    public LangValueType VisitDeferStatement(DeferStatement node)
    {

        // defer语句不立即执行，而是注册到defer栈中
        // 由函数执行器负责在函数返回前执行
        manager.RegisterDefer(node.Statement);

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 SelectStatement 节点
    /// </summary>
    public LangValueType VisitSelectStatement(SelectStatement node)
    {

        // 检查是否在生成器上下文中
        if (manager.GeneratorContext is not null)
        {
            node.RunWithGeneratorContext(manager);
        }
        else
        {
            node.RunStandard(manager);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 EnumInit 节点
    /// </summary>
    public LangValueType VisitEnumInit(EnumInit node)
    {

        // 检查枚举是否已存在
        var existingEnum = manager.GetAny(new LangId(node.EnumName));
        if (existingEnum is not null)
        {
            throw new DuplicateNameError(node, node.EnumName, "枚举");
        }

        // 计算枚举成员的实际值
        var enumValues = new Dictionary<string, int>();
        int currentValue = 0;

        foreach (var (memberName, memberValueExpr) in node.Members)
        {
            if (memberValueExpr is not null)
            {
                // 有显式赋值，计算表达式的值
                var result = memberValueExpr.Accept(this);
                if (result is IntLangValue intValue)
                {
                    currentValue = intValue.Value;
                }
                else
                {
                    throw new SyntaxError(node.Position, $"枚举成员 '{memberName}' 的值必须是整数");
                }
            }

            // 检查成员名是否重复
            if (!enumValues.TryAdd(memberName, currentValue))
            {
                throw new DuplicateNameError(node, memberName, "枚举成员");
            }

            currentValue++; // 下一个未赋值的成员值自动递增
        }

        // 创建枚举模板并注册
        var enumTemplate = new EnumTemplate(node.EnumName, enumValues, node.Position);
        manager.AddClassAndFunc(enumTemplate);

        // 注册枚举类型到类型系统
        try
        {
            TypeChecker.RegisterEnumType(node.EnumName, enumValues.Keys.ToList());
        }
        catch
        {
            // 类型注册失败不影响枚举定义的正常执行
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 ExternStatement 节点
    /// </summary>
    public LangValueType VisitExternStatement(ExternStatement node)
    {

        // 使用工厂创建对应的提供者
        var provider = ExternProviderFactory.CreateProvider(node.ExternType);

        // 委托给提供者执行
        provider.LoadFunctions(node.DllName, node.Functions, node.DefaultCallingConvention, manager);

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 FileHeaderDirective 节点
    /// </summary>
    public LangValueType VisitFileHeaderDirective(FileHeaderDirective node)
    {
        // 文件头指令在解释器模式下不需要执行
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 UsingStatement 节点
    /// </summary>
    public LangValueType VisitUsingStatement(UsingStatement node)
    {

        // 检查是否在生成器上下文中
        if (manager.GeneratorContext is not null)
        {
            node.RunWithGeneratorContext(manager);
        }
        else
        {
            node.RunStandard(manager);
        }

        return new VoidLangValue();
    
    }

    /// <summary>
    /// 访问 FuncRunStatement 节点（表达式语句）
    /// </summary>
    public LangValueType VisitFuncRunStatement(FuncRunStatement node)
    {
        // 迁移自 FuncRunStatement.Run()
        node.Expression?.Accept(this);
        return new VoidLangValue();
    }
}
