using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST;

public abstract class LangExpression : IOldLangTree
{
    /// <inheritdoc />
    public SourcePosition Position { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    protected LangExpression(SourcePosition position = default)
    {
        Position = position;
    }

    /// <summary>
    /// 解释器模式执行
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>表达式的值</returns>
    /// <remarks>
    /// 注意：Visitor 模式已实现。推荐使用 Accept 方法配合 InterpreterVisitor。
    /// </remarks>
    public virtual LangValueType Run(VariateManager manager)
    {
        // 默认实现：使用 Visitor 模式
        var visitor = new InterpreterVisitor(manager);
        return Accept(visitor);
    }

    /// <summary>
    /// 编译器模式：加载表达式的IL值
    /// </summary>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <remarks>
    /// 注意：Visitor 模式已实现。推荐使用 Accept 方法配合 CompilerVisitor。
    /// </remarks>
    public virtual void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 默认实现：使用 Visitor 模式
        var visitor = new CompilerVisitor(ilGenerator, local);
        Accept(visitor);
    }

    public virtual void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 1. 检查是否为字段赋值 (在类的方法中)
        if (local.FieldVar.TryGetValue(idName, out var fieldInfo))
        {
            // 确保我们在实例方法中（有 this 指针）
            // 在编译器模式下，实例方法的参数0是this
            
            // 加载 this
            ilGenerator.Emit(OpCodes.Ldarg_0);
            
            // 加载值
            LoadIlValue(ilGenerator, local);
            
            // 检查类型兼容性
            var fieldValueType = OutputType(local) ?? typeof(object);
            if (fieldInfo.FieldType != fieldValueType)
            {
                // 如果需要，进行类型转换
                if (fieldInfo.FieldType == typeof(object) && fieldValueType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, fieldValueType);
                }
                else if (fieldInfo.FieldType.IsValueType && fieldValueType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
                }
            }
            
            // 设置字段
            ilGenerator.Emit(OpCodes.Stfld, fieldInfo);
            return;
        }

        // 2. 检查是否为函数参数赋值 (优化访问)
        if (local.ArgumentIndices.TryGetValue(idName, out var argIndex))
        {
            // 验证类型兼容性 (参数类型通常是固定的)
            var targetType = local.LocalVarTypes.TryGetValue(idName, out var type) ? type : typeof(object);
            var argValueType = OutputType(local) ?? typeof(int);
            
            // 如果需要更新类型信息 (非严格模式下参数类型可能被视为动态?)
            // 通常参数类型是固定的，所以这里主要是验证
            if (!local.ValidateType(targetType, argValueType, Position))
            {
                 // ValidateType 会报告错误
            }

            // 加载值
            LoadIlValue(ilGenerator, local);
            
            // 存储到参数
            if (argIndex <= 255) ilGenerator.Emit(OpCodes.Starg_S, (byte)argIndex);
            else ilGenerator.Emit(OpCodes.Starg, argIndex);
            return;
        }

        // 3. 局部变量赋值处理
        // 先获取值的类型
        var valueType = OutputType(local) ?? typeof(int); // 默认类型为int

        // 检查变量是否已经在LocalVarTypes中有类型注解
        if (local.LocalVarTypes.TryGetValue(idName, out var existingType))
        {
            // 验证新值的类型与现有类型注解匹配
            if (local.ValidateType(existingType, valueType, Position))
            {
                // 如果类型兼容但不完全相同（非严格模式），更新类型记录
                if (existingType != valueType && !local.StrictTypeChecking)
                {
                    local.LocalVarTypes[idName] = valueType;
                }
            }
        }
        else
        {
            // 如果变量还没有类型注解，保存新值的类型到LocalVarTypes
            local.LocalVarTypes[idName] = valueType;
        }

        // 先声明变量，确保在使用前已经存在
        var localVar = local.GetLocalVar(idName);
        if (localVar is not null)
        {
            if (localVar.LocalType != valueType)
            {
                // 类型不匹配，重新声明变量
                local.RemoveLocalVar(idName);
                localVar = ilGenerator.DeclareLocal(valueType);
                local.AddLocalVar(idName, localVar);
            }
        }
        else
        {
            // 首次声明变量
            localVar = ilGenerator.DeclareLocal(valueType);
            local.AddLocalVar(idName, localVar);
        }

        // 然后加载值
        LoadIlValue(ilGenerator, local);

        // 最后存储到变量
        ilGenerator.Emit(OpCodes.Stloc, localVar);
    }

    /// <summary>
    /// 类型推断
    /// </summary>
    /// <param name="local">局部变量管理器</param>
    /// <returns>推断出的类型</returns>
    /// <remarks>
    /// 注意：Visitor 模式已实现。推荐使用 Accept 方法配合 TypeInferenceVisitor。
    /// </remarks>
    public virtual Type? OutputType(LocalManager local)
    {
        // 默认实现：使用 Visitor 模式
        var visitor = new TypeInferenceVisitor(local);
        return Accept(visitor);
    }

    /// <inheritdoc />
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
}