using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 基础语句
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitBlockStatement(BlockStatement node)
    {
        // 先处理导入语句（函数定义、类定义等）
        foreach (var statement in node.ImportStatements)
        {
            if (statement is OldStatement oldStatement)
            {
                oldStatement.Accept(this);
            }
        }

        // 再处理其他语句
        foreach (var statement in node.OtherStatements)
        {
            statement.Accept(this);
        }

        return null;
    }

    public Instruction? VisitSetStatement(SetStatement node)
    {
        // 检查是普通变量赋值还是索引/成员访问赋值
        if (node.Id != null)
        {
            // 普通变量赋值: x <- value
            string varName = node.Id.IdName;

            // 生成右侧表达式的代码
            node.Value.Accept(this);

            string typeToCheck = node.Id.AssumptionType;

            // DEBUG: 输出类型注解信息
            // Console.WriteLine($"[DEBUG] Variable: {varName}, TypeAnnotation: '{typeToCheck}'");

            // 如果没有显式类型注解，尝试查找变量的已知类型
            if (string.IsNullOrEmpty(typeToCheck) && _compiler.IsLocalVariable(varName))
            {
                typeToCheck = _compiler.GetLocalType(varName);
            }

            // 检查是否有类型注解
            if (!string.IsNullOrEmpty(typeToCheck))
            {
                // 复制栈顶值用于检查
                Emit(OpCode.Dup);
                
                // 执行类型检查
                Emit(OpCode.IsType, typeToCheck);
                
                // 如果检查通过，跳转到存储
                int jumpIfTrue = GetCurrentPosition();
                Emit(OpCode.JumpIfTrue, -1);
                
                // 检查失败，抛出异常
                // 加载错误消息
                var errorMsg = $"变量 '{varName}' 类型不匹配: 期望 {typeToCheck}";
                var msgIndex = _compiler.ConstantPool.AddConstant(errorMsg);
                Emit(OpCode.LoadConst, msgIndex);
                Emit(OpCode.Throw);
                
                // 修补跳转
                PatchJump(jumpIfTrue, GetCurrentPosition());
            }

            // 检查是否是局部变量
            if (_compiler.IsLocalVariable(varName))
            {
                int localIndex = _compiler.GetLocalIndex(varName);

                // 如果有显式类型注解，更新变量类型
                if (!string.IsNullOrEmpty(node.Id.AssumptionType))
                {
                    _compiler.DeclareLocalVariable(varName, node.Id.AssumptionType);
                }

                Emit(OpCode.StoreLocal, localIndex);
            }
            else if (_compiler.IsGlobalVariable(varName))
            {
                // 全局变量更新
                Emit(OpCode.StoreGlobal, varName);
            }
            else if (_compiler.IsClassField(varName))
            {
                // 字段赋值：this.field <- value
                // SetField期望栈布局(从栈顶到栈底): value, object
                // 当前栈布局：value（在栈顶）
                // 我们需要：先加载 this，然后交换栈顶两个元素

                // 加载 this（第一个局部变量）
                Emit(OpCode.LoadLocal, 0);

                // 交换栈顶两个元素：现在栈布局是 value, object（从栈顶到栈底）
                Emit(OpCode.Swap);

                // 发出 SetField 指令
                Emit(OpCode.SetField, varName);
            }
            else
            {
                // 新变量：根据作用域决定是全局变量还是局部变量
                // 参考解释器模式：主函数顶层的变量应该是全局变量
                if (_compiler.IsInMainFunctionTopLevel())
                {
                    // 在主函数顶层：声明为全局变量
                    _compiler.DeclareGlobalVariable(varName);
                    Emit(OpCode.StoreGlobal, varName);
                }
                else
                {
                    // 在其他作用域：声明为局部变量
                    int localIndex = _compiler.DeclareLocalVariable(varName, node.Id.AssumptionType);
                    Emit(OpCode.StoreLocal, localIndex);
                }
            }
        }
        else
        {
            // 索引/成员访问赋值: array[i] <- value 或 obj.field <- value
            var leftExpr = node.LeftExpression;

            if (leftExpr is LangListItem listItem)
            {
                // 简单索引赋值: array[index] <- value
                // SetIndex期望栈布局(从栈顶到栈底): value, index, collection
                // 所以我们需要按相反顺序压栈: collection, index, value

                // 加载集合
                listItem.ListId.Accept(this);

                // 加载索引
                listItem.Key.Accept(this);

                // 加载值
                node.Value.Accept(this);

                // 发出SetIndex指令
                Emit(OpCode.SetIndex);
            }
            else if (leftExpr is NestedIndexAccess nestedIndexAccess)
            {
                // 嵌套索引赋值: array[i][j] <- value
                // 需要分解为两步：
                // 1. temp <- array[i]  (获取内层数组)
                // 2. temp[j] <- value  (设置内层数组的元素)

                // 加载基础表达式 (可能是 LangListItem 或另一个 NestedIndexAccess)
                nestedIndexAccess.BaseExpression.Accept(this);

                // 加载嵌套索引
                nestedIndexAccess.NestedIndex.Accept(this);

                // 加载值
                node.Value.Accept(this);

                // 设置索引
                Emit(OpCode.SetIndex);
            }
            else if (leftExpr is Operation operation && operation.Opera == LangTokenType.Dot)
            {
                // 成员访问赋值: obj.field <- value 或 super.field <- value

                // 获取字段名
                string fieldName;
                if (operation.Right is LangId rightId)
                {
                    fieldName = rightId.IdName;
                }
                else if (operation.Right is ClassMemberId rightClassMemberId)
                {
                    fieldName = rightClassMemberId.IdName;
                }
                else if (operation.Right is LangListItem nestedListItem)
                {
                    // 这是索引赋值：obj.field[index] <- value
                    // 例如：this.groups[key] <- newGroup
                    // 这应该被处理为索引赋值，而不是成员访问赋值

                    // 实际上，this.groups[key] 应该被解析为 LangListItem(Operation(this, Dot, groups), key)
                    // 但是解析器将其解析为 Operation(this, Dot, LangListItem(groups, key))

                    // 我们需要重新构造正确的表达式：
                    // 1. 加载 this.groups
                    operation.Left?.Accept(this);  // 加载 this
                    Emit(OpCode.GetField, nestedListItem.ListId.IdName);  // 加载 groups 字段

                    // 2. 加载索引
                    nestedListItem.Key.Accept(this);

                    // 3. 加载值
                    node.Value.Accept(this);

                    // 4. 执行 SetIndex 指令
                    Emit(OpCode.SetIndex);

                    // 直接返回，不执行后面的 SetField 逻辑
                    return null;
                }
                else
                {
                    // 字节码模式目前只支持简单的成员访问（obj.field）
                    // 不支持复杂的成员访问表达式（如 obj.method().field）
                    throw new NotSupportedException($"字节码模式下不支持的成员访问右侧类型: {operation.Right?.GetType().Name}，只支持简单标识符");
                }

                // 检查是否是 super.field <- value
                if (operation.Left is SuperExpression)
                {
                    // super.field <- value
                    // SetSuperField期望栈布局(从栈顶到栈底): value, this

                    // 加载 this (通过访问 SuperExpression,它会发出 LoadSuper 指令)
                    operation.Left.Accept(this);

                    // 加载值
                    node.Value.Accept(this);

                    // 发出SetSuperField指令
                    Emit(OpCode.SetSuperField, fieldName);
                }
                else
                {
                    // obj.field <- value
                    // SetField期望栈布局(从栈顶到栈底): value, object
                    // 所以我们需要按相反顺序压栈: object, value

                    // 加载对象
                    operation.Left?.Accept(this);

                    // 加载值
                    node.Value.Accept(this);

                    // 发出SetField指令
                    Emit(OpCode.SetField, fieldName);
                }
            }
            else if (leftExpr is TupleLangValue tupleLHS)
            {
                // 元组解构赋值: (a, b) <- (1, 2)
                // 1. 生成 RHS 代码 (栈顶: Tuple)
                node.Value.Accept(this);
                
                // 2. 将 RHS 存储到临时变量，避免重复计算
                int tupleLocalIndex = _compiler.AllocateLocal("<temp_tuple_destruct>");
                Emit(OpCode.StoreLocal, tupleLocalIndex);
                
                // 3. 展平并赋值
                // 注意：新版 TupleLangValue 已经是扁平存储的，直接使用 Elements
                
                var elements = tupleLHS.Elements;
                
                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];
                    if (element is LangId id)
                    {
                        // 提取第 i 个元素
                        Emit(OpCode.LoadLocal, tupleLocalIndex);
                        Emit(OpCode.LoadConst, i);
                        Emit(OpCode.GetIndex);
                        
                        // 赋值给变量
                        string elementName = id.IdName;
                        if (_compiler.IsLocalVariable(elementName))
                        {
                            Emit(OpCode.StoreLocal, _compiler.GetLocalIndex(elementName));
                        }
                        else if (_compiler.IsGlobalVariable(elementName))
                        {
                            Emit(OpCode.StoreGlobal, elementName);
                        }
                        else
                        {
                            int newLocal = _compiler.DeclareLocalVariable(elementName);
                            Emit(OpCode.StoreLocal, newLocal);
                        }
                    }
                    else
                    {
                        // 不支持嵌套解构赋值 (a, (b, c)) <- ... 目前仅支持单层
                         throw new NotSupportedException($"字节码模式下元组解构仅支持变量名，不支持: {element.GetType().Name}");
                    }
                }
                
                // 清理临时变量
                _compiler.FreeLocal(tupleLocalIndex);
            }
            else if (leftExpr != null)
            {
                // 字节码模式目前只支持以下赋值类型：
                // 1. 简单变量赋值 (x <- value)
                // 2. 索引赋值 (arr[i] <- value)
                // 3. 成员访问赋值 (obj.field <- value)
                // 4. 元组解构赋值 ((a, b) <- value)
                throw new NotSupportedException($"字节码模式下不支持的赋值左侧表达式类型: {leftExpr.GetType().Name}");
            }
        }

        return null;
    }


}
