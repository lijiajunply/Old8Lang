using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public class SwitchStatement(
    LangExpression switchExpression,
    List<CaseStatement> switchCaseList,
    BlockStatement? defaultBlockStatement = null,
    SourcePosition position = default)
    : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        var switchValue = switchExpression.Run(manager);

        foreach (var oldCase in switchCaseList)
        {
            var caseValue = oldCase.expression.Run(manager);
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
                oldCase.BlockStatement.Run(manager);
                // 如果case块中执行了return语句，直接返回
                return;
            }
        }

        defaultBlockStatement?.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var labelEnd = ilGenerator.DefineLabel();
        var defaultLabel = defaultBlockStatement != null ? ilGenerator.DefineLabel() : labelEnd;

        // 保存switch表达式的值到局部变量
        switchExpression.LoadIlValue(ilGenerator, local);
        var switchValueType = switchExpression.OutputType(local) ?? typeof(object);
        var switchValueLocal = ilGenerator.DeclareLocal(switchValueType);
        ilGenerator.Emit(OpCodes.Stloc, switchValueLocal.LocalIndex);

        // 为每个case创建标签
        var caseLabels = switchCaseList.Select(_ => ilGenerator.DefineLabel()).ToList();

        // 生成case匹配逻辑
        for (int i = 0; i < switchCaseList.Count; i++)
        {
            var oldCase = switchCaseList[i];
            var caseLabel = caseLabels[i];

            // 检查是否是范围匹配
            if (oldCase.expression is RangeLangValue)
            {
                // 范围匹配：检查 switchValue 是否在范围数组中
                // 加载case值（数组）
                oldCase.expression.LoadIlValue(ilGenerator, local);

                // 加载switch值
                ilGenerator.Emit(OpCodes.Ldloc, switchValueLocal.LocalIndex);

                // 装箱 switch 值（因为 Array.IndexOf 需要 object 参数）
                if (switchValueType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, switchValueType);
                }

                // 调用 Array.IndexOf 方法
                var indexOfMethod = typeof(Array).GetMethod("IndexOf", [typeof(Array), typeof(object)])!;
                ilGenerator.Emit(OpCodes.Call, indexOfMethod);

                // 检查返回值是否 >= 0（表示找到了）
                // IndexOf >= 0 等价于 IndexOf > -1
                ilGenerator.Emit(OpCodes.Ldc_I4_M1);
                ilGenerator.Emit(OpCodes.Cgt);

                // 如果找到，跳转到对应的case标签
            }
            else
            {
                // 普通匹配
                // 重新加载switch值
                ilGenerator.Emit(OpCodes.Ldloc, switchValueLocal.LocalIndex);

                // 加载case值并比较
                oldCase.expression.LoadIlValue(ilGenerator, local);

                // 比较操作
                if (switchValueType == typeof(int) || switchValueType == typeof(bool))
                {
                    // 整数和布尔值使用Ceq指令比较
                    ilGenerator.Emit(OpCodes.Ceq);
                }
                else if (switchValueType == typeof(string))
                {
                    // 字符串比较需要调用string.Equals方法
                    var equalsMethod = typeof(string).GetMethod("Equals", [typeof(string), typeof(string)])!;
                    ilGenerator.Emit(OpCodes.Call, equalsMethod);
                }
                else
                {
                    // 其他类型比较，调用Equals方法
                    // 尝试获取精确匹配的Equals方法
                    var equalsMethod = switchValueType.GetMethod("Equals", [switchValueType]);

                    // 如果没有找到精确匹配，尝试获取接受object参数的Equals方法
                    if (equalsMethod == null)
                    {
                        equalsMethod = switchValueType.GetMethod("Equals", [typeof(object)]);
                    }

                    if (equalsMethod != null)
                    {
                        ilGenerator.Emit(OpCodes.Call, equalsMethod);
                    }
                    else
                    {
                        // 如果都没有找到，使用引用比较
                        ilGenerator.Emit(OpCodes.Ceq);
                    }
                }

                // 如果相等，跳转到对应的case标签
            }

            ilGenerator.Emit(OpCodes.Brtrue, caseLabel);
        }

        // 所有case都不匹配，跳转到default或结束
        ilGenerator.Emit(OpCodes.Br, defaultLabel);

        // 生成各个case块
        for (var i = 0; i < switchCaseList.Count; i++)
        {
            var oldCase = switchCaseList[i];
            var caseLabel = caseLabels[i];

            // 标记case标签
            ilGenerator.MarkLabel(caseLabel);

            // 生成case块的IL代码
            oldCase.BlockStatement.GenerateIl(ilGenerator, local);

            // 检查case块是否以return语句结尾
            var lastStatement = oldCase.BlockStatement.Count > 0
                ? oldCase.BlockStatement[^1]
                : null;

            // 如果不是return语句，跳转到结束标签
            if (lastStatement is not ReturnStatement)
            {
                ilGenerator.Emit(OpCodes.Br, labelEnd);
            }
        }

        // 生成default块
        if (defaultBlockStatement != null)
        {
            ilGenerator.MarkLabel(defaultLabel);
            defaultBlockStatement.GenerateIl(ilGenerator, local);
        }

        // 结束标签
        ilGenerator.MarkLabel(labelEnd);
    }

    public override OldStatement this[int index] => switchCaseList[index];

    public override int Count => switchCaseList.Count;
}

public class CaseStatement(LangExpression expression, BlockStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression expression { get; } = expression;
    public BlockStatement BlockStatement { get; } = blockStatement;

    public override void Run(VariateManager manager)
    {
        BlockStatement.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var labelCase = ilGenerator.DefineLabel();
        expression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Br, labelCase);

        ilGenerator.MarkLabel(labelCase);
        BlockStatement.GenerateIl(ilGenerator, local);
    }

    public override OldStatement this[int index] => BlockStatement[index];

    public override int Count => BlockStatement.Count;
}