using System.Collections;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行容器操作指令
    /// </summary>
    private void ExecuteContainerOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.NewArray:
            {
                int count = (int)instruction.Operand!;
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = _stack.Pop();
                }

                _stack.Push(elements);
            }
                break;

            case OpCode.NewList:
            {
                int count = (int)instruction.Operand!;
                var list = new List<object?>();
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = _stack.Pop();
                }

                list.AddRange(elements);
                _stack.Push(list);
            }
                break;

            case OpCode.NewTuple:
            {
                int count = (int)instruction.Operand!;
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = _stack.Pop();
                }

                if (count == 0)
                {
                    _stack.Push(new Tuple<object?, object?>(null, null));
                }
                else if (count == 1)
                {
                    _stack.Push(new Tuple<object?, object?>(elements[0], null));
                }
                else if (count == 2)
                {
                    _stack.Push(new Tuple<object?, object?>(elements[0], elements[1]));
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

                    _stack.Push(current);
                }
            }
                break;

            case OpCode.NewDict:
            {
                int pairCount = (int)instruction.Operand!;
                var dict = new Dictionary<object, object?>();
                // 每个键值对作为一个元组在栈上
                for (int i = 0; i < pairCount; i++)
                {
                    if (_stack.Pop() is Tuple<object?, object?> { Item1: not null } tuple)
                    {
                        dict[tuple.Item1] = tuple.Item2;
                    }
                }

                _stack.Push(dict);
            }
                break;

            case OpCode.ArrayLength:
            {
                var collection = _stack.Pop();
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
                    throw new TypeError(GetPosition(instruction), $"无法获取类型 {collection?.GetType().Name} 的长度");
                }

                _stack.Push(length);
            }
                break;

            case OpCode.GetIndex:
            {
                // 栈顶: index, collection
                var index = _stack.Pop();
                var collection = _stack.Pop();

                if (collection is Array array)
                {
                    int idx = Convert.ToInt32(index);
                    _stack.Push(array.GetValue(idx));
                }
                else if (collection is IList list)
                {
                    int idx = Convert.ToInt32(index);
                    _stack.Push(list[idx]);
                }
                else if (collection is IDictionary dict)
                {
                    if (!dict.Contains(index))
                    {
                        throw new KeyError(GetPosition(instruction), index);
                    }

                    _stack.Push(dict[index]);
                }
                else if (collection is DictionaryLangValue dictLangValue)
                {
                    // 处理 DictionaryLangValue 类型
                    // 将索引转换为 LangValueType
                    var keyToFind = ConvertToLangValueType(index);

                    // 在字典中查找键
                    bool found = false;
                    foreach (var (key, value) in dictLangValue.Value)
                    {
                        if (key.Equal(keyToFind))
                        {
                            _stack.Push(value);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new KeyError(GetPosition(instruction), index);
                    }
                }
                else if (collection is string str)
                {
                    int idx = Convert.ToInt32(index);
                    _stack.Push(str[idx]);
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
                    int idx = Convert.ToInt32(index);
                    int currentIdx = 0;
                    bool found = false;

                    // 使用栈进行迭代遍历，确保能处理任意嵌套结构的元组
                    var traverseStack = new Stack<object?>();
                    traverseStack.Push(tuple);

                    while (traverseStack.Count > 0)
                    {
                        var current = traverseStack.Pop();
                        if (current is Tuple<object?, object?> t)
                        {
                            // 保持顺序：先处理 Item1，再处理 Item2
                            // 栈是后进先出，所以先压入 Item2，再压入 Item1
                            traverseStack.Push(t.Item2);
                            traverseStack.Push(t.Item1);
                        }
                        else if (current != null) // 跳过 null (与 TupleLangValue 行为一致)
                        {
                            if (currentIdx == idx)
                            {
                                _stack.Push(current);
                                found = true;
                                break;
                            }

                            currentIdx++;
                        }
                    }

                    if (!found)
                    {
                        throw new IndexError(GetPosition(instruction), idx, currentIdx);
                    }
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {collection?.GetType().Name} 执行索引访问");
                }
            }
                break;

            case OpCode.SetIndex:
            {
                // 栈顶: value, index, collection
                var value = _stack.Pop();
                var index = _stack.Pop();
                var collection = _stack.Pop();

                if (collection is Array array)
                {
                    int idx = Convert.ToInt32(index);
                    array.SetValue(value, idx);
                }
                else if (collection is IList list)
                {
                    int idx = Convert.ToInt32(index);
                    list[idx] = value;
                }
                else if (collection is IDictionary dict)
                {
                    dict[index] = value;
                }
                else if (collection is DictionaryLangValue dictLangValue)
                {
                    // 处理 DictionaryLangValue 类型
                    // 将索引和值转换为 LangValueType
                    var keyToSet = ConvertToLangValueType(index);
                    var valueToSet = ConvertToLangValueType(value);

                    // 在字典中查找键并更新，如果不存在则添加
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
                        // 键不存在，添加新的键值对
                        dictLangValue.Value.Add((keyToSet, valueToSet));
                    }
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {collection?.GetType().Name} 执行索引赋值");
                }
            }
                break;

            case OpCode.NewRange:
            {
                // 栈顶: includeEnd, includeStart, end, start
                var includeEndObj = _stack.Pop();
                var includeStartObj = _stack.Pop();
                var endObj = _stack.Pop();
                var startObj = _stack.Pop();

                int start = Convert.ToInt32(startObj);
                int end = Convert.ToInt32(endObj);
                bool includeStart = Convert.ToBoolean(includeStartObj);
                bool includeEnd = Convert.ToBoolean(includeEndObj);

                var results = new List<int>();

                // 根据包含规则调整起始值
                var startNum = start;
                var endNum = end;

                if (!includeStart)
                    startNum++;
                if (!includeEnd)
                    endNum--;

                // 检查范围是否有效
                // 如果start原本就大于end,说明是反向范围
                if (start > end)
                {
                    // 反向范围:从大到小
                    for (var i = startNum; i >= endNum; i--)
                    {
                        results.Add(i);
                    }
                }
                else if (startNum <= endNum)
                {
                    // 正向范围:从小到大
                    for (var i = startNum; i <= endNum; i++)
                    {
                        results.Add(i);
                    }
                }
                // 如果调整后startNum > endNum但原本start <= end,说明排除导致范围为空,返回空数组

                _stack.Push(results.ToArray());
            }
                break;

        }
    }
}
