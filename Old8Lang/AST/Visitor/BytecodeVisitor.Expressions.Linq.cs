using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - LINQ表达式
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitLinqExpression(LinqExpression node)
    {
        // LINQ 查询表达式的字节码生成策略:
        // 1. 获取数据源并转换为迭代器
        // 2. 遍历数据源,对每个元素应用查询子句
        // 3. 收集结果到列表中
        // 4. 返回结果列表

        // 步骤1: 计算数据源表达式
        node.FromClause.DataSource.Accept(this);

        // 获取迭代器
        Emit(OpCode.GetIterator);
        int iteratorLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, iteratorLocal);

        // 检查终止子句类型,决定创建列表还是分组字典
        bool isGroupBy = node.TerminationClause is GroupByClause;

        if (isGroupBy)
        {
            // 创建分组字典 (用于 GroupBy)
            Emit(OpCode.NewGroupDict);
        }
        else
        {
            // 创建结果列表 (用于 Select)
            Emit(OpCode.NewList, 0);
        }

        int resultListLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, resultListLocal);

        // 为范围变量分配局部变量槽
        int rangeVarLocal = _compiler.AllocateLocal(node.FromClause.RangeVariable);

        // 为 let 变量分配局部变量槽
        var letVariables = new Dictionary<string, int>();
        foreach (var clause in node.BodyClauses)
        {
            if (clause is LetClause letClause)
            {
                int letVarLocal = _compiler.AllocateLocal(letClause.Variable);
                letVariables[letClause.Variable] = letVarLocal;
            }
        }

        // 步骤2: 遍历数据源
        int loopStartPos = GetCurrentPosition();

        // 检查迭代器是否有下一个元素
        Emit(OpCode.LoadLocal, iteratorLocal);
        Emit(OpCode.IteratorMoveNext);

        // 如果没有下一个元素,跳出循环
        int loopEndJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1); // 占位,稍后修补

        // 获取当前元素
        Emit(OpCode.LoadLocal, iteratorLocal);
        Emit(OpCode.IteratorCurrent);

        // 将当前元素存储到范围变量
        Emit(OpCode.StoreLocal, rangeVarLocal);

        // 步骤3: 处理查询体子句 (where, let)
        var skipElementJumps = new List<int>();
        ProcessLinqBodyClauses(node.BodyClauses, letVariables, skipElementJumps);

        // 步骤4: 执行终止子句 (select)
        ProcessLinqTerminationClause(node.TerminationClause, resultListLocal);

        // 跳回循环开始
        int continueJump = GetCurrentPosition();
        Emit(OpCode.Jump, loopStartPos);

        // 修补所有跳过元素的跳转 (where 条件不满足时跳到这里)
        foreach (var jumpIndex in skipElementJumps)
        {
            PatchJump(jumpIndex, continueJump);
        }

        // 修补循环结束跳转
        PatchJump(loopEndJump, GetCurrentPosition());

        // 步骤5: 处理 OrderBy (如果有)
        var orderByClause = node.BodyClauses.OfType<OrderByClause>().FirstOrDefault();
        if (orderByClause != null)
        {
            ProcessLinqOrderBy(orderByClause, resultListLocal);
        }

        // 步骤6: 如果是 GroupBy,将分组字典转换为分组列表
        if (isGroupBy)
        {
            Emit(OpCode.LoadLocal, resultListLocal);
            Emit(OpCode.GroupDictToList);
            Emit(OpCode.StoreLocal, resultListLocal);
        }

        // 步骤7: 加载结果列表到栈
        Emit(OpCode.LoadLocal, resultListLocal);

        // 释放局部变量
        _compiler.FreeLocal(iteratorLocal);
        _compiler.FreeLocal(resultListLocal);
        _compiler.FreeLocal(rangeVarLocal);
        foreach (var letVar in letVariables.Values)
        {
            _compiler.FreeLocal(letVar);
        }

        return null;
    }

    // ===== 泛型处理辅助方法 =====

    /// <summary>
    /// 处理泛型类实例化
    /// </summary>
    private void HandleGenericClassInstantiation(GenericInstanceExpression node, string className)
    {
        // 获取泛型类模板
        var typeTemplate = _compiler.GenericClasses[className];

        // 构建特化类名：ClassName$Type1_Type2_...
        // 解析类型参数时，需要考虑当前的类型参数映射
        var typeArgNames = node.TypeArguments.Select(typeArg =>
        {
            var resolvedType = ResolveSimpleTypeName(typeArg);
            // 如果类型参数在当前映射中，替换为实际类型
            if (_compiler.CurrentTypeParameterMapping.TryGetValue(resolvedType, out var mappedType))
            {
                return mappedType;
            }
            return resolvedType;
        }).ToArray();
        var specializedClassName = $"{className}${string.Join("_", typeArgNames)}";

        // 检查是否已经生成过特化类
        if (!_compiler.IsClassName(specializedClassName))
        {
            // 生成特化类定义
            GenerateSpecializedClass(typeTemplate, typeArgNames.ToList(), specializedClassName);
        }

        // 生成创建对象的字节码
        Emit(OpCode.NewObject, specializedClassName);

        // 查找并调用构造函数
        var classMetadata = _compiler.GetClassMetadata(specializedClassName);
        string? constructorName = null;

        if (classMetadata != null)
        {
            // 优先查找 init 方法
            if (classMetadata.Methods.Any(m => m.Name == "init"))
            {
                constructorName = "init";
            }
            // 其次查找与原始类名相同的方法
            else if (classMetadata.Methods.Any(m => m.Name == className))
            {
                constructorName = className;
            }
        }

        // 如果找到构造函数，调用它
        if (constructorName != null && node.CallArguments != null)
        {
            // 复制对象引用，因为 CallMethod 会消耗它
            Emit(OpCode.Dup);

            // 生成参数代码
            foreach (var arg in node.CallArguments)
            {
                arg.Accept(this);
            }

            // 调用构造函数
            // CallMethod 操作数: [argCount, methodName]
            // argCount 包括对象本身 + 实际参数
            int totalArgCount = node.CallArguments.Count + 1; // +1 for 'this'
            Emit(OpCode.CallMethod, new object[] { totalArgCount, constructorName });

            // 构造函数返回 void，不需要弹出返回值
        }
    }

    /// <summary>
    /// 处理泛型函数调用
    /// </summary>
    private void HandleGenericFunctionCall(GenericInstanceExpression node, string funcName)
    {
        // 获取泛型函数定义
        var genericFunc = _compiler.GenericFunctions[funcName];

        // 构建特化函数名：FuncName$Type1_Type2_...
        var typeArgNames = node.TypeArguments.Select(ResolveSimpleTypeName).ToArray();
        var specializedFuncName = $"{funcName}${string.Join("_", typeArgNames)}";

        // 检查是否已经生成过特化函数
        if (_compiler.GetFunctionIndex(specializedFuncName) == -1)
        {
            // 生成特化函数定义
            GenerateSpecializedFunction(genericFunc, node.TypeArguments, specializedFuncName);
        }

        // 生成调用参数的字节码
        if (node.CallArguments != null)
        {
            foreach (var arg in node.CallArguments)
            {
                arg.Accept(this);
            }
        }

        // 生成函数调用字节码
        int argCount = node.CallArguments?.Count ?? 0;
        Emit(OpCode.Call, new object[] { argCount, specializedFuncName });
    }
}
