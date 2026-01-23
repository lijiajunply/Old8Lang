using System.Collections;
using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Bytecode.Interop;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行迭代器操作指令
    /// </summary>
    private void ExecuteIteratorOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.GetIterator:
            {
                var collection = _stack.Pop();

                // 特殊处理字典：迭代键而不是键值对
                if (collection is IDictionary dict)
                {
                    var enumerator = dict.Keys.GetEnumerator();
                    _stack.Push(enumerator);
                }
                else if (collection is IEnumerable enumerable)
                {
                    var enumerator = enumerable.GetEnumerator();
                    _stack.Push(enumerator);
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"对象类型 {collection?.GetType().Name} 不可迭代");
                }
            }
                break;

            case OpCode.IteratorMoveNext:
            {
                // 栈顶应该是迭代器，调用MoveNext后将bool结果压入栈
                // 注意：迭代器对象保持在栈上，以便后续的IteratorCurrent使用
                if (_stack.Peek() is IEnumerator enumerator)
                {
                    bool hasNext = enumerator.MoveNext();
                    _stack.Push(hasNext);
                }
                else
                {
                    var top = _stack.Count > 0 ? _stack.Peek() : null;
                    var topType = top?.GetType().FullName ?? "null";
                    throw new StateError(GetPosition(instruction), $"栈顶不是迭代器对象，而是: {topType}");
                }
            }
                break;

            case OpCode.IteratorCurrent:
            {
                // 栈顶应该是迭代器
                if (_stack.Count == 0)
                {
                    throw new StateError(GetPosition(instruction), "IteratorCurrent: 栈为空");
                }

                var top = _stack.Peek();
                if (top is IEnumerator enumerator)
                {
                    _stack.Push(enumerator.Current);
                }
                else
                {
                    // 调试：输出栈的详细信息
                    var stackContents = string.Join(", ", _stack.Select(x => x?.GetType().Name ?? "null"));
                    var topType = top?.GetType().FullName ?? "null";
                    throw new StateError(GetPosition(instruction),
                        $"IteratorCurrent 失败: 栈顶类型是 {topType}, 栈内容({_stack.Count}): [{stackContents}]");
                }
            }
                break;

            case OpCode.Slice:
            {
                // 栈顶: step, end, start, collection
                var step = _stack.Pop();
                var end = _stack.Pop();
                var start = _stack.Pop();
                var collection = _stack.Pop();

                int startIdx = Convert.ToInt32(start);
                int endIdx = end != null ? Convert.ToInt32(end) : int.MaxValue;
                int stepVal = step != null ? Convert.ToInt32(step) : 1;

                if (collection is Array array)
                {
                    var result = SliceArray(array, startIdx, endIdx, stepVal);
                    _stack.Push(result);
                }
                else if (collection is IList list)
                {
                    var result = SliceList(list, startIdx, endIdx, stepVal);
                    _stack.Push(result);
                }
                else if (collection is string str)
                {
                    var result = SliceString(str, startIdx, endIdx, stepVal);
                    _stack.Push(result);
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
                    // 1. 展平 Tuple 为 List
                    var tupleAsList = new List<object?>();
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
                            tupleAsList.Add(current);
                        }
                    }

                    // 2. 对 List 进行切片
                    var sliceResult = SliceList(tupleAsList, startIdx, endIdx, stepVal);
                    var slicedList = sliceResult as List<object?>;

                    if (slicedList == null)
                    {
                        slicedList = [];
                        if (sliceResult is IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                slicedList.Add(item);
                            }
                        }
                    }

                    // 3. 将切片后的 List 重新构建为 Tuple
                    object? resultTuple;

                    if (slicedList.Count == 0)
                    {
                        resultTuple = new Tuple<object?, object?>(null, null);
                    }
                    else if (slicedList.Count == 1)
                    {
                        resultTuple = new Tuple<object?, object?>(slicedList[0], null);
                    }
                    else if (slicedList.Count == 2)
                    {
                        resultTuple = new Tuple<object?, object?>(slicedList[0], slicedList[1]);
                    }
                    else
                    {
                        // 构建嵌套元组: (1, 2, 3) -> (1, (2, 3))
                        // 从后往前构建
                        object current = new Tuple<object?, object?>(slicedList[^2],
                            slicedList[^1]);

                        for (int i = slicedList.Count - 3; i >= 0; i--)
                        {
                            current = new Tuple<object?, object?>(slicedList[i], current);
                        }

                        resultTuple = current;
                    }

                    _stack.Push(resultTuple);
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {collection?.GetType().Name} 执行切片操作");
                }
            }
                break;

            case OpCode.NewGroupDict:
            {
                // 创建一个分组字典 Dictionary<object, List<object>>
                var groupDict = new Dictionary<object, List<object?>>(new ObjectEqualityComparer());
                _stack.Push(groupDict);
            }
                break;

            case OpCode.AddToGroup:
            {
                // 栈顶: element, key, groupDict
                var element = _stack.Pop();
                var key = _stack.Pop();
                var groupDict = _stack.Pop() as Dictionary<object, List<object?>>;

                if (groupDict == null)
                {
                    throw new TypeError(GetPosition(instruction), "AddToGroup 操作需要一个分组字典");
                }

                // 如果键不存在,创建新的列表
                if (!groupDict.ContainsKey(key!))
                {
                    groupDict[key!] = [];
                }

                // 将元素添加到对应键的列表中
                groupDict[key!].Add(element);

                // 注意: 不需要将字典重新压栈,因为字典是引用类型,修改会直接反映到原对象
            }
                break;

            case OpCode.GroupDictToList:
            {
                // 将分组字典转换为分组列表
                // 每个分组是一个包含 Key 和 Values 的对象
                var groupDict = _stack.Pop() as Dictionary<object, List<object?>>;

                if (groupDict == null)
                {
                    throw new TypeError(GetPosition(instruction), "GroupDictToList 操作需要一个分组字典");
                }

                var resultList = new List<object?>();

                foreach (var kvp in groupDict)
                {
                    // 创建一个分组对象,包含 Key 和 Values
                    var group = new Dictionary<string, object?>
                    {
                        ["Key"] = kvp.Key,
                        ["Values"] = kvp.Value
                    };
                    resultList.Add(group);
                }

                _stack.Push(resultList);
            }
                break;

        }
    }
}
