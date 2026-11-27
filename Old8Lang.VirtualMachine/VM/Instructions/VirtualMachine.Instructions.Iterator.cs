using System.Collections;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    private static readonly object NullGroupKey = new();

    /// <summary>
    /// 执行迭代器操作指令
    /// </summary>
    private void ExecuteIteratorOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.GetIterator:
            {
                var collection = _stack.PopObject();

                // 字典要放在 IEnumerable 之前判断：Dictionary 本身也是 IEnumerable，
                // 落到下面那支会拿到 KeyValuePair 而不是 (键, 值) 元组。
                // 迭代元素统一为 (键, 值) 元组，与解释器一致：单标识符绑定整个元组，
                // 多标识符由 __for_in_unpack 按下标解构。顺序为插入顺序（依赖 NewDict 保序）。
                if (collection is IDictionary dict)
                {
                    var pairs = new List<object?>(dict.Count);
                    foreach (DictionaryEntry entry in dict)
                    {
                        pairs.Add(new Tuple<object?, object?>(entry.Key, entry.Value));
                    }

                    _stack.PushObject(pairs.GetEnumerator());
                }
                else if (collection is IEnumerable enumerable)
                {
                    var enumerator = enumerable.GetEnumerator();
                    _stack.PushRef(enumerator);
                }
                else if (collection is ILangList langList)
                {
                    // Old8Lang 列表只实现 ILangList（既不是 IList 也不是 IEnumerable），
                    // 必须单独处理，否则 await Task.WhenAll(...) 之类产出的列表值无法 for-in。
                    _stack.PushRef(langList.GetItems().GetEnumerator());
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"对象类型 {DescribeValueType(collection)} 不可迭代");
                }
            }
                break;

            case OpCode.IteratorMoveNext:
            {
                // 栈顶应该是迭代器，调用MoveNext后将bool结果压入栈
                // 注意：迭代器对象保持在栈上，以便后续的IteratorCurrent使用
                if (_stack.PeekObject() is IEnumerator enumerator)
                {
                    bool hasNext = enumerator.MoveNext();
                    _stack.PushBool(hasNext);
                }
                else
                {
                    var top = _stack.Count > 0 ? _stack.PeekObject() : null;
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

                var top = _stack.PeekObject();
                if (top is IEnumerator enumerator)
                {
                    _stack.PushObject(enumerator.Current);
                }
                else
                {
                    // 调试：输出栈的详细信息
                    var stackContents = string.Join(", ",
                        _stack.ToObjectArrayTopToBottom().Select(x => x is null ? "null" : DescribeValueType(x)));
                    var topType = top?.GetType().FullName ?? "null";
                    throw new StateError(GetPosition(instruction),
                        $"IteratorCurrent 失败: 栈顶类型是 {topType}, 栈内容({_stack.Count}): [{stackContents}]");
                }
            }
                break;

            case OpCode.Slice:
            {
                // 栈顶: step, end, start, collection
                var stepVal = _stack.Pop();
                var endVal = _stack.Pop();
                var startVal = _stack.Pop();
                var collection = _stack.PopObject();

                int startIdx = startVal.ToInt32();
                int endIdx = !endVal.IsNull ? endVal.ToInt32() : int.MaxValue;
                int stepValInt = !stepVal.IsNull ? stepVal.ToInt32() : 1;

                if (collection is Array array)
                {
                    var result = SliceArray(array, startIdx, endIdx, stepValInt);
                    _stack.PushObject(result);
                }
                else if (collection is IList list)
                {
                    var result = SliceList(list, startIdx, endIdx, stepValInt);
                    _stack.PushObject(result);
                }
                else if (collection is string str)
                {
                    var result = SliceString(str, startIdx, endIdx, stepValInt);
                    _stack.PushObject(result);
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
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

                    var sliceResult = SliceList(tupleAsList, startIdx, endIdx, stepValInt);
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
                        object current = new Tuple<object?, object?>(slicedList[^2],
                            slicedList[^1]);

                        for (int i = slicedList.Count - 3; i >= 0; i--)
                        {
                            current = new Tuple<object?, object?>(slicedList[i], current);
                        }

                        resultTuple = current;
                    }

                    _stack.PushObject(resultTuple);
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {DescribeValueType(collection)} 执行切片操作");
                }
            }
                break;

            case OpCode.NewGroupDict:
            {
                var groupDict = new Dictionary<object, List<object?>>(new ObjectEqualityComparer());
                _stack.PushRef(groupDict);
            }
                break;

            case OpCode.AddToGroup:
            {
                var element = _stack.PopObject();
                var key = _stack.PopObject();
                var groupDict = _stack.PopObject() as Dictionary<object, List<object?>>;

                if (groupDict == null)
                {
                    throw new TypeError(GetPosition(instruction), "AddToGroup 操作需要一个分组字典");
                }

                var actualKey = key ?? NullGroupKey;
                if (!groupDict.ContainsKey(actualKey))
                {
                    groupDict[actualKey] = [];
                }

                groupDict[actualKey].Add(element);
            }
                break;

            case OpCode.GroupDictToList:
            {
                var groupDict = _stack.PopObject() as Dictionary<object, List<object?>>;

                if (groupDict == null)
                {
                    throw new TypeError(GetPosition(instruction), "GroupDictToList 操作需要一个分组字典");
                }

                var resultList = new List<object?>();

                foreach (var kvp in groupDict)
                {
                    var group = new Dictionary<string, object?>
                    {
                        ["Key"] = ReferenceEquals(kvp.Key, NullGroupKey) ? null : kvp.Key,
                        ["Values"] = kvp.Value
                    };
                    resultList.Add(group);
                }

                _stack.PushRef(resultList);
            }
                break;
        }
    }
}
