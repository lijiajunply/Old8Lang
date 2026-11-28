using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Bytecode.Core;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行比较运算指令
    /// </summary>
    private void ExecuteComparisonOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.Equal:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.PushBool(Equals(a, b));
            }
                break;

            case OpCode.NotEqual:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.PushBool(!Equals(a, b));
            }
                break;

            case OpCode.Greater:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.PushBool(Greater(a, b));
            }
                break;

            case OpCode.Less:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.PushBool(Less(a, b));
            }
                break;

            case OpCode.GreaterEqual:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.PushBool(GreaterEqual(a, b));
            }
                break;

            case OpCode.LessEqual:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.PushBool(LessEqual(a, b));
            }
                break;

            case OpCode.In:
            {
                var collection = Stack.PopObject();
                var value = Stack.PopObject();
                Stack.PushBool(ContainsIn(value, collection));
            }
                break;
        }
    }

    /// <summary>
    /// 语言层 <c>a in b</c> 的运行期实现。
    /// </summary>
    /// <remarks>
    /// 解释器要求右侧是 <see cref="ILangList"/> 并调用其 <c>In</c>，字符串的字符/子串、
    /// 列表的按值查找、字典的按键查找都由各自的实现决定；这里复用同一接口，
    /// 并对 VM 里可能出现的原生集合/字符串补一条回退，避免 <c>in</c> 退化成 Nop。
    /// </remarks>
    private bool ContainsIn(object? value, object? collection)
    {
        switch (collection)
        {
            case null:
                return false;
            case string text:
                return value switch
                {
                    char character => text.Contains(character),
                    string substring => text.Contains(substring, StringComparison.Ordinal),
                    _ => text.Contains(value?.ToString() ?? string.Empty, StringComparison.Ordinal)
                };
            case ILangList langList:
                return langList.In(ConvertToLangValueType(value));
            case System.Collections.IDictionary dictionary:
                // 变量里存的是原生 Dictionary<object, object?> 时按键查找。
                foreach (var key in dictionary.Keys)
                {
                    if (Equals(key, value)) return true;
                }

                return false;
            case System.Collections.IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    if (Equals(item, value)) return true;
                }

                return false;
            default:
                return false;
        }
    }
}
