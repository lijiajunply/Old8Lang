using System.Text;
using System.Text.Json;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.LangParser;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Old8Lang.AST.Expression;

/// <summary>
/// AnyLangValue类型的扩展方法类，提供JSON序列化和反序列化功能
/// </summary>
public static class AnyValueFuncStatic
{
    /// <summary>
    /// 将JsonElement转换为Old8Lang值类型
    /// </summary>
    /// <param name="element">要转换的JsonElement</param>
    /// <param name="node">用于错误报告的节点</param>
    /// <returns>转换后的Old8Lang值类型</returns>
    private static LangValueType GetJsonElement(JsonElement element, IOldLangTree node)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => new StringLangValue(element.GetString() ?? ""),
            JsonValueKind.Number => new IntLangValue(element.GetInt32()),
            JsonValueKind.True => new BoolLangValue(true),
            JsonValueKind.False => new BoolLangValue(false),
            JsonValueKind.Null => new VoidLangValue(),
            JsonValueKind.Array => new ArrayLangValue(
                element.EnumerateArray().Select(x => GetJsonElement(x, node)).ToList()),
            JsonValueKind.Undefined => new VoidLangValue(),
            JsonValueKind.Object => ToObj(new StringLangValue(element.ToString())),
            _ => throw new InvalidOperationError(node, "不支持的JSON值类型")
        };
    }

    /// <summary>
    /// 将AnyLangValue对象转换为JSON字符串
    /// </summary>
    /// <param name="type">要转换的AnyLangValue对象</param>
    /// <returns>包含JSON表示的StringLangValue</returns>
    public static StringLangValue ToJson(this AnyLangValue type)
    {
        var builder = new StringBuilder();
        builder.Append('{');
        for (var i = 0; i < type.Variates.Count; i++)
        {
            var variable = type.Variates.ElementAt(i);
            if (variable.Value is FuncLangValue or Instance or NativeAnyLangValue or NativeStaticAny
                or VoidLangValue) continue;
            builder.Append($"{(i == 0 ? "" : ",")}\"{variable.Key}\":{variable.Value}");
        }

        builder.Append('}');
        return new StringLangValue(builder.ToString());
    }

    /// <summary>
    /// 将JSON字符串转换为AnyLangValue对象
    /// </summary>
    /// <param name="json">包含JSON数据的StringLangValue</param>
    /// <returns>转换后的AnyLangValue对象</returns>
    public static AnyLangValue ToObj(this StringLangValue json)
    {
        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(json.Value) ??
                         new Dictionary<string, object>();
        return new AnyLangValue(jsonObject.ToDictionary<KeyValuePair<string, object>, ClassMemberId, LangExpression>
        (
            variable => new ClassMemberId(variable.Key),
            variable =>
            {
                if (variable.Value is JsonElement element)
                {
                    return GetJsonElement(element, json);
                }

                return LangValueType.ObjToValue(variable.Value);
            }));
    }
}

/// <summary>
/// LangValueType类型的扩展方法类，提供类型转换和基本操作功能
/// </summary>
[Serializable]
public static class ValueTypeFuncStatic
{
    extension(LangValueType type)
    {
        /// <summary>
        /// 将值转换为整数类型
        /// </summary>
        /// <returns>转换后的整数类型值</returns>
        public IntLangValue ToInt()
        {
            if (type is IntLangValue intValue)
            {
                return intValue;
            }

            if (type is DoubleLangValue doubleValue)
            {
                return new IntLangValue(Convert.ToInt32(doubleValue.Value));
            }

            if (type is CharLangValue charValue)
            {
                return new IntLangValue(Convert.ToInt32(charValue.Value));
            }

            return new IntLangValue(int.Parse(type.ToString()));
        }

        /// <summary>
        /// 将值转换为类型对象
        /// </summary>
        /// <returns>表示当前值类型的TypeLangValue</returns>
        public TypeLangValue ToType()
        {
            return new TypeLangValue(type.TypeToString());
        }

        /// <summary>
        /// 将值转换为字符串表示
        /// </summary>
        /// <returns>值的字符串表示</returns>
        public StringLangValue ToStr()
        {
            return new StringLangValue(type.ToDisplayString());
        }

        /// <summary>
        /// 获取值的哈希码
        /// </summary>
        /// <returns>值的哈希码</returns>
        public IntLangValue ToHash()
        {
            return new IntLangValue(type.GetHashCode());
        }

        /// <summary>
        /// 比较当前值与另一个值是否相等
        /// </summary>
        /// <param name="otherValue">要比较的另一个值</param>
        /// <returns>比较结果，相等返回true，否则返回false</returns>
        public BoolLangValue Equal(LangValueType otherValue)
        {
            return new BoolLangValue(type.Equal(otherValue));
        }
    }
}

/// <summary>
/// StringLangValue类型的扩展方法类，提供字符串操作功能
/// </summary>
[Serializable]
public static class StringValueFuncStatic
{
    extension(StringLangValue str)
    {
        /// <summary>
        /// 获取字符串的子串
        /// </summary>
        /// <param name="start">子串的起始位置</param>
        /// <param name="length">子串的长度</param>
        /// <returns>包含子串的StringLangValue</returns>
        public StringLangValue Substring(IntLangValue start, IntLangValue length)
        {
            return new StringLangValue(str.Value.Substring(start.Value, length.Value));
        }

        /// <summary>
        /// 查找字符串在当前字符串中第一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符串</param>
        /// <returns>包含位置索引的IntLangValue，未找到返回-1</returns>
        public IntLangValue IndexOf(StringLangValue value)
        {
            return new IntLangValue(str.Value.IndexOf(value.Value, StringComparison.Ordinal));
        }

        /// <summary>
        /// 替换字符串中的指定子串
        /// </summary>
        /// <param name="oldValue">要替换的旧子串</param>
        /// <param name="newValue">替换后的新子串</param>
        /// <returns>替换后的StringLangValue</returns>
        public StringLangValue Replace(StringLangValue oldValue, StringLangValue newValue)
        {
            return new StringLangValue(str.Value.Replace(oldValue.Value, newValue.Value));
        }

        /// <summary>
        /// 使用指定分隔符分割字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>包含分割结果的ListLangValue</returns>
        public ListLangValue Split(StringLangValue separator)
        {
            var parts = str.Value.Split(separator.Value)
                .Select(s => new StringLangValue(s) as LangValueType)
                .ToList();
            return new ListLangValue(parts);
        }

        /// <summary>
        /// 将字符串转换为大写
        /// </summary>
        /// <returns>转换为大写后的StringLangValue</returns>
        public StringLangValue ToUpper()
        {
            return new StringLangValue(str.Value.ToUpper());
        }

        /// <summary>
        /// 将字符串转换为小写
        /// </summary>
        /// <returns>转换为小写后的StringLangValue</returns>
        public StringLangValue ToLower()
        {
            return new StringLangValue(str.Value.ToLower());
        }

        /// <summary>
        /// 检查字符串是否包含指定子串
        /// </summary>
        /// <param name="value">要检查的子串</param>
        /// <returns>包含检查结果的BoolLangValue</returns>
        public BoolLangValue Contains(StringLangValue value)
        {
            return new BoolLangValue(str.Value.Contains(value.Value));
        }

        /// <summary>
        /// 去除字符串首尾的空白字符
        /// </summary>
        /// <returns>去除空白字符后的StringLangValue</returns>
        public StringLangValue Trim()
        {
            return new StringLangValue(str.Value.Trim());
        }
    }
}

/// <summary>
/// DictionaryLangValue类型的扩展方法类，提供字典操作功能
/// </summary>
[Serializable]
public static class DictionaryValueFuncStatic
{
    extension(DictionaryLangValue langValue)
    {
        /// <summary>
        /// 向字典中添加键值对
        /// </summary>
        /// <param name="value1">键</param>
        /// <param name="value2">值</param>
        /// <returns>添加的键值对作为TupleLangValue</returns>
        public TupleLangValue Add(LangValueType value1, LangValueType value2)
        {
            langValue.Value.Add((value1, value2));
            return new TupleLangValue(value1, value2);
        }

        /// <summary>
        /// 根据键获取字典中的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <returns>对应的值</returns>
        public LangValueType GetValue(LangValueType key)
        {
            return langValue.Value.First(x => x.Key.Equal(key)).Value;
        }

        /// <summary>
        /// 根据键从字典中移除键值对
        /// </summary>
        /// <param name="key">要移除的键</param>
        /// <returns>被移除的值</returns>
        /// <exception cref="KeyError">当键不存在时抛出</exception>
        public LangValueType Remove(LangValueType key)
        {
            for (var i = 0; i < langValue.Value.Count; i++)
            {
                if (!langValue.Value[i].Key.Equal(key)) continue;
                var a = langValue.Value[i].Value;
                langValue.Value.RemoveAt(i);
                return a;
            }

            throw new KeyError(langValue, "键不存在");
        }
    }
}

/// <summary>
/// ListLangValue类型的扩展方法类，提供列表操作功能
/// </summary>
public static class ListValueFuncStatic
{
    extension(ListLangValue langValue)
    {
        /// <summary>
        /// 向列表中添加元素
        /// </summary>
        /// <param name="langValueType">要添加的元素</param>
        /// <returns>添加的元素</returns>
        public LangValueType Add(LangValueType langValueType)
        {
            langValue.Values.Add(langValueType);
            return langValueType;
        }

        /// <summary>
        /// 从列表中移除指定元素
        /// </summary>
        /// <param name="num">要移除的元素</param>
        /// <returns>被移除的元素</returns>
        /// <exception cref="InvalidOperationError">当元素不存在时抛出</exception>
        public LangValueType Remove(LangValueType num)
        {
            for (var i = 0; i < langValue.Values.Count; i++)
            {
                if (!langValue.Values[i].Equal(num)) continue;
                var a = langValue.Values[i];
                langValue.Values.RemoveAt(i);
                return a;
            }

            throw new InvalidOperationError(langValue, "找不到要移除的元素");
        }

        /// <summary>
        /// 根据索引从列表中移除元素
        /// </summary>
        /// <param name="num">要移除的元素索引</param>
        /// <returns>被移除的元素</returns>
        public LangValueType RemoveAt(IntLangValue num)
        {
            var a = langValue.Values[num.Value];
            langValue.Values.RemoveAt(num.Value);
            return a;
        }

        /// <summary>
        /// 将另一个列表的所有元素添加到当前列表
        /// </summary>
        /// <param name="otherLangValue">要添加的列表</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue AddList(ListLangValue otherLangValue)
        {
            langValue.Values.AddRange(otherLangValue.Values);
            return new VoidLangValue();
        }

        /// <summary>
        /// 对列表进行排序
        /// </summary>
        /// <returns>排序后的列表（原地排序）</returns>
        public ListLangValue Sort()
        {
            QuickSort(langValue.Values, 0, langValue.Values.Count - 1);
            return langValue;
        }

        /// <summary>
        /// 使用谓词函数过滤列表元素
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>包含满足条件元素的新列表</returns>
        public ListLangValue Filter(FuncLangValue predicate)
        {
            var filtered = new List<LangValueType>();
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();
                manager.Set(new LangId("item"), item);

                // 执行谓词函数
                var result = predicate.Run(manager);

                // 如果结果为真，则保留该元素
                if (result is BoolLangValue { Value: true })
                {
                    filtered.Add(item);
                }
            }

            return new ListLangValue(filtered);
        }

        /// <summary>
        /// 使用转换函数映射列表元素
        /// </summary>
        /// <param name="transform">转换函数，将元素转换为新值</param>
        /// <returns>包含转换后元素的新列表</returns>
        public ListLangValue Map(FuncLangValue transform)
        {
            var mapped = new List<LangValueType>();
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();
                manager.Set(new LangId("item"), item);

                // 执行转换函数
                var result = transform.Run(manager);
                mapped.Add(result);
            }

            return new ListLangValue(mapped);
        }

        /// <summary>
        /// 使用归约函数将列表元素归约为单个值
        /// </summary>
        /// <param name="reducer">归约函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <param name="initialValue">初始累加器值</param>
        /// <returns>归约后的结果值</returns>
        public LangValueType Reduce(FuncLangValue reducer, LangValueType initialValue)
        {
            var accumulator = initialValue;
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();
                manager.Set(new LangId("accumulator"), accumulator);
                manager.Set(new LangId("item"), item);

                // 执行归约函数
                accumulator = reducer.Run(manager);
            }

            return accumulator;
        }

        /// <summary>
        /// 反转列表元素顺序
        /// </summary>
        /// <returns>反转后的列表（原地反转）</returns>
        public ListLangValue Reverse()
        {
            langValue.Values.Reverse();
            return langValue;
        }

        /// <summary>
        /// 检查列表是否包含指定元素
        /// </summary>
        /// <param name="element">要检查的元素</param>
        /// <returns>包含检查结果的BoolLangValue</returns>
        public BoolLangValue Contains(LangValueType element)
        {
            return new BoolLangValue(langValue.Values.Any(item => item.Equal(element)));
        }

        /// <summary>
        /// 查找元素在列表中第一次出现的索引
        /// </summary>
        /// <param name="element">要查找的元素</param>
        /// <returns>包含索引的IntLangValue，未找到返回-1</returns>
        public IntLangValue IndexOf(LangValueType element)
        {
            for (var i = 0; i < langValue.Values.Count; i++)
            {
                if (langValue.Values[i].Equal(element))
                {
                    return new IntLangValue(i);
                }
            }

            return new IntLangValue(-1); // 未找到返回-1
        }
    }

    /// <summary>
    /// 使用快速排序算法对列表进行排序
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    private static void QuickSort(List<LangValueType> nums, int left, int right)
    {
        while (true)
        {
            if (left < right)
            {
                int pivotIndex = Partition(nums, left, right);
                QuickSort(nums, left, pivotIndex - 1);
                left = pivotIndex + 1;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// 快速排序的分区函数，选择最右侧元素作为枢轴
    /// </summary>
    /// <param name="nums">要分区的列表</param>
    /// <param name="left">分区范围的左边界</param>
    /// <param name="right">分区范围的右边界</param>
    /// <returns>枢轴元素的最终位置</returns>
    private static int Partition(List<LangValueType> nums, int left, int right)
    {
        var pivot = nums[right];
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (!nums[j].Less(pivot)) continue;
            i++;
            Swap(nums, i, j);
        }

        Swap(nums, i + 1, right);
        return i + 1;
    }

    /// <summary>
    /// 交换列表中两个元素的位置
    /// </summary>
    /// <param name="nums">列表</param>
    /// <param name="i">第一个元素的索引</param>
    /// <param name="j">第二个元素的索引</param>
    private static void Swap(List<LangValueType> nums, int i, int j)
    {
        (nums[i], nums[j]) = (nums[j], nums[i]);
    }
}