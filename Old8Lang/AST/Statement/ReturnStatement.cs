using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

public partial class ReturnStatement(LangExpression returnExpression, SourcePosition position = default) : OldStatement(position)
{
    public LangExpression Expression => returnExpression;
    
    public override void Run(VariateManager manager)
    {
        var result = returnExpression.Run(manager);

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
                this,
                functionName,
                manager.CurrentFunctionTypeArgumentMapping);
        }

        manager.Result = result;
        manager.IsReturn = true;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 检查是否在finally块中使用了return语句，这在.NET IL中是不允许的
        if (local.IsInFinallyBlock)
        {
            throw new Error.CompilerException("在finally块中不允许使用return语句", Position);
        }

        // 获取返回值表达式的类型
        var returnType = returnExpression.OutputType(local);

        // 验证返回类型是否与函数声明的返回类型匹配
        if (local.ReturnValueLocal is not null)
        {
            var expectedReturnType = local.ReturnValueLocal.LocalType;

            // 检查返回类型是否兼容
            if (returnType is not null && expectedReturnType != typeof(object))
            {
                // 检查类型是否兼容：完全匹配、可赋值、或基本类型转换
                bool isCompatible = expectedReturnType == returnType ||
                                   expectedReturnType.IsAssignableFrom(returnType) ||
                                   IsBasicTypeConversionAllowed(expectedReturnType, returnType);

                // 如果返回类型不匹配且不能自动转换，报告错误
                if (!isCompatible)
                {
                    var errorMsg = $"[编译模式错误] 返回类型不匹配\n\n" +
                                  $"期望返回类型: {expectedReturnType.Name}\n" +
                                  $"实际返回类型: {returnType.Name}\n\n" +
                                  $"修复建议：\n" +
                                  $"- 确保return语句返回的值类型与函数声明的返回类型一致\n" +
                                  $"- 或者修改函数的返回类型注解以匹配实际返回值";
                    local.ReportError(errorMsg, Position);
                }
            }
        }

        // 加载返回表达式的值到栈上（对于void类型，LoadIlValue不会加载任何值）
        returnExpression.LoadIlValue(ilGenerator, local);

        // 检查是否在异步状态机中且没有被 finally 块捕获
        if (local.AsyncStateMachineGenerator != null && local.ReturnLabel == null)
        {
            var type = returnExpression.OutputType(local) ?? typeof(void);
            local.AsyncStateMachineGenerator.EmitReturn(ilGenerator, type);
            return;
        }

        // 如果函数使用了try-finally结构（ReturnLabel不为null），必须使用Leave指令
        if (local.ReturnLabel is not null)
        {
            // 如果有返回值局部变量（非void函数），存储返回值
            if (local.ReturnValueLocal is not null)
            {
                // 获取返回值局部变量的类型
                var returnValueType = local.ReturnValueLocal.LocalType;

                // 如果返回表达式的类型与返回值局部变量的类型不匹配，进行类型转换
                if (returnType != returnValueType)
                {
                    if (returnValueType.IsValueType && returnType == typeof(object))
                    {
                        // object 到值类型，需要拆箱
                        ilGenerator.Emit(OpCodes.Unbox_Any, returnValueType);
                    }
                    else if (!returnValueType.IsValueType && returnType!.IsValueType)
                    {
                        // 值类型到引用类型，需要装箱
                        ilGenerator.Emit(OpCodes.Box, returnType);
                    }
                }

                // 存储返回值到局部变量
                ilGenerator.Emit(OpCodes.Stloc, local.ReturnValueLocal);
            }
            else if (returnType != null && returnType != typeof(void))
            {
                // 如果没有返回值局部变量（如顶层代码或void函数），但表达式产生了值，需要将其弹出
                // 否则 Leave 指令会因为堆栈非空而导致无效 IL
                ilGenerator.Emit(OpCodes.Pop);
            }
            // 使用Leave指令退出try块，跳转到函数结束标签
            ilGenerator.Emit(OpCodes.Leave, local.ReturnLabel.Value);
        }
        else
        {
            // 没有使用defer，直接返回
            // 只有当返回值类型是 Task<object> 时，才进行异步包装
            // 这表明当前函数是异步函数
            if (returnType == typeof(Task<object>))
            {
                // 返回值已经是 Task<object>，直接返回
                ilGenerator.Emit(OpCodes.Ret);
            }
            else
            {
                // 对于同步函数，直接返回值，不进行任何包装
                ilGenerator.Emit(OpCodes.Ret);
            }
        }
    }

    /// <summary>
    /// 检查是否允许基本类型转换
    /// </summary>
    private static bool IsBasicTypeConversionAllowed(Type expected, Type actual)
    {
        // 数值类型之间的转换
        var numericTypes = new[] { typeof(int), typeof(long), typeof(double), typeof(float), typeof(short), typeof(byte) };
        if (numericTypes.Contains(expected) && numericTypes.Contains(actual))
            return true;

        // 字符串转换
        if (expected == typeof(string))
            return true;

        return false;
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public Type OutputType(LocalManager local) => returnExpression.OutputType(local)!;

    public override string ToString() => $"return {returnExpression}";
}