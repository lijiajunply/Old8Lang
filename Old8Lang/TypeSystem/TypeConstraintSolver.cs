namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型约束求解器：基于收集到的约束求解类型变量的实际类型
/// </summary>
public class TypeConstraintSolver(TypeInferenceContext context, TypeInferenceConfig config)
{
    /// <summary>
    /// 求解所有约束
    /// </summary>
    public bool Solve()
    {
        if (context.Constraints.Count == 0)
            return true;

        // 多轮求解，直到收敛
        const int maxIterations = 10;
        int iteration = 0;
        bool changed;

        do
        {
            changed = false;
            iteration++;

            if (config.DebugOutput)
            {
                Console.WriteLine($"\n=== 类型推断迭代 {iteration} ===");
            }

            // 按优先级排序约束（置信度高的优先）
            var sortedConstraints = context.Constraints
                .OrderByDescending(c => c.Confidence)
                .ThenBy(c => c.Kind)
                .ToList();

            foreach (var constraint in sortedConstraints)
            {
                if (SolveConstraint(constraint))
                {
                    changed = true;
                }
            }

            // 传播约束
            if (PropagateConstraints())
            {
                changed = true;
            }

        } while (changed && iteration < maxIterations);

        if (config.DebugOutput)
        {
            PrintSolutionSummary();
        }

        // 检查未解决的约束
        return ValidateSolution();
    }

    /// <summary>
    /// 求解单个约束
    /// </summary>
    private bool SolveConstraint(TypeConstraint constraint)
    {
        var typeVar = constraint.TypeVariable;

        // 如果已经有绑定，检查是否一致
        var existingBinding = context.GetTypeBinding(typeVar);

        if (existingBinding is not null)
        {
            // 已经有绑定，检查新约束是否兼容
            if (constraint.TargetType is not null)
            {
                if (!AreTypesCompatible(existingBinding, constraint.TargetType, constraint.Kind))
                {
                    if (config.DebugOutput)
                    {
                        Console.WriteLine($"  ⚠️  约束冲突: {typeVar} 已绑定为 {existingBinding.Name}，新约束要求 {constraint.TargetType.Name}");
                    }
                    return false;
                }

                // 尝试精化类型（选择更具体的类型）
                var refinedType = RefineType(existingBinding, constraint.TargetType, constraint.Confidence);
                if (refinedType != existingBinding)
                {
                    context.BindTypeVariable(typeVar, refinedType);
                    if (config.DebugOutput)
                    {
                        Console.WriteLine($"  ✓ 精化类型: {typeVar} = {refinedType.Name}");
                    }
                    return true;
                }
            }
            return false;
        }

        // 新绑定
        if (constraint.TargetType is not null)
        {
            // 检查置信度阈值
            if (constraint.Confidence >= config.MinimumConfidence)
            {
                context.BindTypeVariable(typeVar, constraint.TargetType);

                if (config.DebugOutput)
                {
                    Console.WriteLine($"  ✓ 绑定类型: {typeVar} = {constraint.TargetType.Name} (置信度: {constraint.Confidence:F2})");
                }
                return true;
            }
            else if (config.DebugOutput)
            {
                Console.WriteLine($"  ⚠️  置信度过低: {typeVar} = {constraint.TargetType.Name} (置信度: {constraint.Confidence:F2} < {config.MinimumConfidence:F2})");
            }
        }

        return false;
    }

    /// <summary>
    /// 传播约束：从已知类型推断相关类型
    /// </summary>
    private bool PropagateConstraints()
    {
        bool changed = false;

        // 按类型变量分组约束
        var constraintsByVar = context.Constraints
            .Where(c => c.TargetType is not null)
            .GroupBy(c => c.TypeVariable)
            .ToList();

        foreach (var group in constraintsByVar)
        {
            var typeVar = group.Key;
            var constraints = group.ToList();

            // 如果该变量还没有绑定
            if (context.GetTypeBinding(typeVar) is null)
            {
                // 尝试从多个约束推断
                var inferredType = InferFromMultipleConstraints(constraints);
                if (inferredType is not null)
                {
                    context.BindTypeVariable(typeVar, inferredType);
                    changed = true;

                    if (config.DebugOutput)
                    {
                        Console.WriteLine($"  ✓ 多约束推断: {typeVar} = {inferredType.Name}");
                    }
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// 从多个约束推断类型
    /// </summary>
    private Type? InferFromMultipleConstraints(List<TypeConstraint> constraints)
    {
        if (constraints.Count == 0)
            return null;

        // 1. 优先选择 Equality 约束
        var equalityConstraints = constraints
            .Where(c => c is { Kind: TypeConstraintKind.Equality, TargetType: not null })
            .OrderByDescending(c => c.Confidence)
            .ToList();

        if (equalityConstraints.Count > 0)
        {
            // 检查所有相等约束是否一致
            var firstType = equalityConstraints[0].TargetType!;
            if (equalityConstraints.All(c => c.TargetType == firstType))
            {
                return firstType;
            }

            // 有冲突，选择置信度最高的
            return equalityConstraints[0].TargetType;
        }

        // 2. 处理子类型约束
        var subtypeConstraints = constraints
            .Where(c => c is { Kind: TypeConstraintKind.Subtype, TargetType: not null })
            .ToList();

        if (subtypeConstraints.Count > 0)
        {
            // 找到最具体的子类型
            var types = subtypeConstraints.Select(c => c.TargetType!).ToList();
            return FindMostSpecificType(types);
        }

        // 3. 处理调用约束和赋值约束
        var otherConstraints = constraints
            .Where(c => c.TargetType is not null)
            .OrderByDescending(c => c.Confidence)
            .ToList();

        if (otherConstraints.Count > 0)
        {
            var types = otherConstraints.Select(c => c.TargetType!).ToList();

            // 检查类型是否兼容
            if (types.All(t => AreTypesCompatible(types[0], t, TypeConstraintKind.Assignment)))
            {
                return types[0];
            }

            // 查找公共类型
            return FindCommonType(types);
        }

        return null;
    }

    /// <summary>
    /// 检查两个类型是否兼容
    /// </summary>
    private bool AreTypesCompatible(Type type1, Type type2, TypeConstraintKind constraintKind)
    {
        if (type1 == type2)
            return true;

        // object 可以兼容任何类型
        if (type1 == typeof(object) || type2 == typeof(object))
            return true;

        switch (constraintKind)
        {
            case TypeConstraintKind.Equality:
                return type1 == type2;

            case TypeConstraintKind.Subtype:
                return type1.IsAssignableFrom(type2);

            case TypeConstraintKind.Assignment:
            case TypeConstraintKind.Call:
                // 允许隐式类型转换
                if (type1.IsAssignableFrom(type2))
                    return true;

                // 数值类型转换
                if (IsNumericType(type1) && IsNumericType(type2))
                    return true;

                // int -> double
                if (type1 == typeof(double) && type2 == typeof(int))
                    return true;

                // char -> int/double
                if ((type1 == typeof(int) || type1 == typeof(double)) && type2 == typeof(char))
                    return true;

                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// 类型精化：选择更具体的类型
    /// </summary>
    private Type RefineType(Type existingType, Type newType, double confidence)
    {
        // 如果类型相同，不需要精化
        if (existingType == newType)
            return existingType;

        // object 可以被任何类型精化
        if (existingType == typeof(object) && confidence >= config.MinimumConfidence)
            return newType;

        // int 可以被 double 精化（如果需要）
        if (existingType == typeof(int) && newType == typeof(double) && confidence >= 0.8)
            return newType;

        // 选择更具体的子类型
        if (existingType.IsAssignableFrom(newType))
            return newType;

        if (newType.IsAssignableFrom(existingType))
            return existingType;

        // 无法精化，保持原类型
        return existingType;
    }

    /// <summary>
    /// 找到最具体的类型（最派生的类型）
    /// </summary>
    private Type FindMostSpecificType(List<Type> types)
    {
        if (types.Count == 0)
            return typeof(object);

        if (types.Count == 1)
            return types[0];

        Type mostSpecific = types[0];
        foreach (var type in types.Skip(1))
        {
            if (mostSpecific.IsAssignableFrom(type))
            {
                mostSpecific = type;  // type 更具体
            }
            else if (!type.IsAssignableFrom(mostSpecific))
            {
                // 无法确定更具体的，返回 object
                return typeof(object);
            }
        }

        return mostSpecific;
    }

    /// <summary>
    /// 找到多个类型的公共类型
    /// </summary>
    private Type FindCommonType(List<Type> types)
    {
        if (types.Count == 0)
            return typeof(void);

        if (types.Count == 1)
            return types[0];

        // 检查是否所有类型相同
        if (types.All(t => t == types[0]))
            return types[0];

        // 处理数值类型
        var hasDouble = types.Any(t => t == typeof(double));
        var allNumeric = types.All(IsNumericType);

        if (allNumeric && hasDouble)
            return typeof(double);

        if (allNumeric)
            return typeof(int);

        // 查找公共基类
        Type commonType = types[0];
        foreach (var type in types.Skip(1))
        {
            commonType = FindCommonBaseClass(commonType, type);
            if (commonType == typeof(object))
                break;
        }

        return commonType;
    }

    private Type FindCommonBaseClass(Type? type1, Type type2)
    {
        if (type1 is null)
            return type2;

        if (type1 == type2)
            return type1;

        if (type1.IsAssignableFrom(type2))
            return type1;

        if (type2.IsAssignableFrom(type1))
            return type2;

        var current = type1.BaseType;
        while (current is not null)
        {
            if (current.IsAssignableFrom(type2))
                return current;
            current = current.BaseType;
        }

        return typeof(object);
    }

    private bool IsNumericType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(double) ||
               type == typeof(char) ||
               type == typeof(float) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte);
    }

    /// <summary>
    /// 验证求解结果
    /// </summary>
    private bool ValidateSolution()
    {
        var unresolvedConstraints = context.Constraints
            .Where(c => c.TargetType is null || context.GetTypeBinding(c.TypeVariable) is null)
            .ToList();

        if (unresolvedConstraints.Count > 0)
        {
            if (config.DebugOutput)
            {
                Console.WriteLine($"\n⚠️  {unresolvedConstraints.Count} 个约束未解决:");
                foreach (var constraint in unresolvedConstraints)
                {
                    Console.WriteLine($"  - {constraint}");
                }
            }

            // 如果允许回退到动态类型
            if (config.FallbackToDynamic)
            {
                foreach (var constraint in unresolvedConstraints)
                {
                    if (context.GetTypeBinding(constraint.TypeVariable) is null)
                    {
                        context.BindTypeVariable(constraint.TypeVariable, typeof(object));
                        if (config.DebugOutput)
                        {
                            Console.WriteLine($"  ✓ 回退到动态类型: {constraint.TypeVariable} = object");
                        }
                    }
                }
                return true;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// 打印求解结果摘要
    /// </summary>
    private void PrintSolutionSummary()
    {
        Console.WriteLine("\n=== 类型推断结果 ===");
        Console.WriteLine($"总约束数: {context.Constraints.Count}");
        Console.WriteLine($"已解决类型变量数: {context.TypeVariableBindings.Count}");

        Console.WriteLine("\n类型绑定:");
        foreach (var (varName, type) in context.TypeVariableBindings.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"  {varName} = {type.Name}");
        }
    }
}
