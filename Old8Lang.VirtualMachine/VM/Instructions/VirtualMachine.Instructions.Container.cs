using System.Collections;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    // VM 元组历史上使用右嵌套 Tuple 表示多元素元组；记录源元组的直接元素，
    // 才能区分 (1, 2, 3) 与 (1, (2, 3))。弱键避免延长元组生命周期。
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, object?[]> TupleElements = new();

    /// <summary>
    /// 把可能为负的下标换算成实际下标并做越界检查
    /// （与解释器一致：<c>-1</c> 表示最后一个元素）。
    /// </summary>
    private static int NormalizeIndex(int index, int count, SourcePosition position)
    {
        var normalized = index < 0 ? index + count : index;
        if (normalized < 0 || normalized >= count)
        {
            throw new IndexError(position, index, count);
        }

        return normalized;
    }

    /// <summary>
    /// 执行容器操作指令
    /// </summary>
    private void ExecuteContainerOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.NewArray:
            {
                int count = instruction.IntOperand;
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = Stack.PopObject();
                }

                Stack.PushRef(elements);
            }
                break;

            case OpCode.NewList:
            {
                int count = instruction.IntOperand;
                var list = new List<object?>(count);
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = Stack.PopObject();
                }

                list.AddRange(elements);
                Stack.PushRef(list);
            }
                break;

            case OpCode.NewTuple:
            {
                int count = instruction.IntOperand;
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = Stack.PopObject();
                }

                object tuple;
                if (count == 0)
                {
                    tuple = new Tuple<object?, object?>(null, null);
                }
                else if (count == 1)
                {
                    tuple = new Tuple<object?, object?>(elements[0], null);
                }
                else if (count == 2)
                {
                    tuple = new Tuple<object?, object?>(elements[0], elements[1]);
                }
                else
                {
                    // 构建嵌套元组: (1, 2, 3) -> (1, (2, 3))
                    // 从后往前构建
                    object? current = new Tuple<object?, object?>(elements[count - 2], elements[count - 1]);

                    for (int i = count - 3; i >= 0; i--)
                    {
                        current = new Tuple<object?, object?>(elements[i], current);
                    }

                    tuple = current;
                }
                TupleElements.Add(tuple, elements);
                Stack.PushRef(tuple);
            }
                break;

            case OpCode.NewDict:
            {
                int pairCount = instruction.IntOperand;
                var dict = new Dictionary<object, object?>();
                // 每个键值对作为一个元组在栈上，栈顶是最后一个键值对。
                // 先按倒序收回数组再正序写入，否则 Dictionary 的插入顺序（也就是遍历顺序）
                // 会是源码顺序的逆序，与解释器/IL 模式不一致。
                var pairs = new Tuple<object?, object?>[pairCount];
                for (int i = pairCount - 1; i >= 0; i--)
                {
                    if (Stack.PopObject() is Tuple<object?, object?> { Item1: not null } tuple)
                    {
                        pairs[i] = tuple;
                    }
                }

                foreach (var pair in pairs)
                {
                    if (pair is not null)
                    {
                        dict[pair.Item1!] = pair.Item2;
                    }
                }

                Stack.PushRef(dict);
            }
                break;

            case OpCode.ArrayLength:
            {
                var collection = Stack.PopObject();
                int length = 0;

                if (collection is Array array)
                {
                    length = array.Length;
                }
                else if (collection is ICollection<object?> list)
                {
                    length = list.Count;
                }
                else if (collection is ICollection col)
                {
                    length = col.Count;
                }
                else if (collection is string str)
                {
                    length = str.Length;
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
                    // 递归计算嵌套元组的长度
                    length = 0;
                    var traverseStack = new Stack<object?>();
                    traverseStack.Push(tuple);

                    while (traverseStack.Count > 0)
                    {
                        var current = traverseStack.Pop();
                        if (current is Tuple<object?, object?> t)
                        {
                            traverseStack.Push(t.Item2);
                            traverseStack.Push(t.Item1);
                        }
                        else if (current != null)
                        {
                            length++;
                        }
                    }
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法获取类型 {DescribeValueType(collection)} 的长度");
                }

                Stack.PushInt(length);
            }
                break;

            case OpCode.GetIndex:
            {
                // 栈顶: index, collection
                var indexVal = Stack.Pop();
                var collection = Stack.PopObject();

                if (collection is Array array)
                {
                    Stack.PushObject(array.GetValue(NormalizeIndex(indexVal.ToInt32(), array.Length,
                        GetPosition(instruction))));
                }
                else if (collection is IList list)
                {
                    Stack.PushObject(list[NormalizeIndex(indexVal.ToInt32(), list.Count, GetPosition(instruction))]);
                }
                else if (collection is IDictionary dict)
                {
                    var indexObj = indexVal.ToObject();
                    if (!dict.Contains(indexObj!))
                    {
                        throw new KeyError(GetPosition(instruction), indexObj!);
                    }

                    Stack.PushObject(dict[indexObj!]);
                }
                else if (collection is DictionaryLangValue dictLangValue)
                {
                    var indexObj = indexVal.ToObject();
                    var keyToFind = ConvertToLangValueType(indexObj);

                    bool found = false;
                    foreach (var (key, value) in dictLangValue.Value)
                    {
                        if (key.Equal(keyToFind))
                        {
                            Stack.PushObject(value);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new KeyError(GetPosition(instruction), indexObj!);
                    }
                }
                else if (collection is ILangList langList)
                {
                    var items = langList.GetItems().ToList();
                    int idx = indexVal.ToInt32();
                    if (idx < 0)
                    {
                        idx += items.Count;
                    }

                    if (idx < 0 || idx >= items.Count)
                    {
                        throw new IndexError(GetPosition(instruction), idx, items.Count);
                    }

                    Stack.PushObject(items[idx]);
                }
                else if (collection is string str)
                {
                    Stack.PushChar(str[NormalizeIndex(indexVal.ToInt32(), str.Length, GetPosition(instruction))]);
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
                    var tupleItems = new List<object?>();
                    var traverseStack = new Stack<object?>();
                    traverseStack.Push(tuple);

                    while (traverseStack.Count > 0)
                    {
                        var current = traverseStack.Pop();
                        if (current is Tuple<object?, object?> t)
                        {
                            traverseStack.Push(t.Item2);
                            traverseStack.Push(t.Item1);
                        }
                        else if (current != null)
                        {
                            tupleItems.Add(current);
                        }
                    }

                    Stack.PushObject(tupleItems[NormalizeIndex(indexVal.ToInt32(), tupleItems.Count,
                        GetPosition(instruction))]);
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {DescribeValueType(collection)} 执行索引访问");
                }
            }
                break;

            case OpCode.SetIndex:
            {
                // 栈顶: value, index, collection
                var value = Stack.PopObject();
                var indexVal = Stack.Pop();
                var collection = Stack.PopObject();

                if (collection is Array array)
                {
                    array.SetValue(value, NormalizeIndex(indexVal.ToInt32(), array.Length,
                        GetPosition(instruction)));
                }
                else if (collection is IList list)
                {
                    list[NormalizeIndex(indexVal.ToInt32(), list.Count, GetPosition(instruction))] = value;
                }
                else if (collection is IDictionary dict)
                {
                    dict[indexVal.ToObject()!] = value;
                }
                else if (collection is DictionaryLangValue dictLangValue)
                {
                    var keyToSet = ConvertToLangValueType(indexVal.ToObject());
                    var valueToSet = ConvertToLangValueType(value);

                    bool found = false;
                    for (int i = 0; i < dictLangValue.Value.Count; i++)
                    {
                        var (key, _) = dictLangValue.Value[i];
                        if (key.Equal(keyToSet))
                        {
                            dictLangValue.Value[i] = (key, valueToSet);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        dictLangValue.Value.Add((keyToSet, valueToSet));
                    }
                }
                else if (collection is ILangList langList)
                {
                    langList.Set(ConvertToLangValueType(indexVal.ToObject()), ConvertToLangValueType(value));
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {DescribeValueType(collection)} 执行索引赋值");
                }
            }
                break;

            case OpCode.NewRange:
            {
                bool includeEnd = ToBool(Stack.Pop());
                bool includeStart = ToBool(Stack.Pop());
                var step = instruction.Operand is true ? Stack.PopObject() : 1;
                var end = Stack.PopObject();
                var start = Stack.PopObject();
                Stack.PushRef(RangeLangValue.CreateRange(start, end, step, includeStart, includeEnd,
                    GetPosition(instruction)));
            }
                break;
        }
    }
}
