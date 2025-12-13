using System.Text;
using System.Text.Json;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.LangParser;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Old8Lang.AST.Expression;

public static class AnyValueFuncStatic
{
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

[Serializable]
public static class ValueTypeFuncStatic
{
    extension(LangValueType type)
    {
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

        public TypeLangValue ToType()
        {
            return new TypeLangValue(type.TypeToString());
        }

        public StringLangValue ToStr()
        {
            return new StringLangValue(type.ToDisplayString());
        }

        public IntLangValue ToHash()
        {
            return new IntLangValue(type.GetHashCode());
        }

        public BoolLangValue Equal(LangValueType otherValue)
        {
            return new BoolLangValue(type.Equal(otherValue));
        }
    }
}

[Serializable]
public static class StringValueFuncStatic
{
    extension(StringLangValue str)
    {
        public StringLangValue Substring(IntLangValue start, IntLangValue length)
        {
            return new StringLangValue(str.Value.Substring(start.Value, length.Value));
        }

        public IntLangValue IndexOf(StringLangValue value)
        {
            return new IntLangValue(str.Value.IndexOf(value.Value, StringComparison.Ordinal));
        }

        public StringLangValue Replace(StringLangValue oldValue, StringLangValue newValue)
        {
            return new StringLangValue(str.Value.Replace(oldValue.Value, newValue.Value));
        }

        public ListLangValue Split(StringLangValue separator)
        {
            var parts = str.Value.Split(separator.Value)
                .Select(s => new StringLangValue(s) as LangValueType)
                .ToList();
            return new ListLangValue(parts);
        }

        public StringLangValue ToUpper()
        {
            return new StringLangValue(str.Value.ToUpper());
        }

        public StringLangValue ToLower()
        {
            return new StringLangValue(str.Value.ToLower());
        }

        public BoolLangValue Contains(StringLangValue value)
        {
            return new BoolLangValue(str.Value.Contains(value.Value));
        }

        public StringLangValue Trim()
        {
            return new StringLangValue(str.Value.Trim());
        }
    }
}

[Serializable]
public static class DictionaryValueFuncStatic
{
    extension(DictionaryLangValue langValue)
    {
        public TupleLangValue Add(LangValueType value1, LangValueType value2)
        {
            langValue.Value.Add((value1, value2));
            return new TupleLangValue(value1, value2);
        }

        public LangValueType GetValue(LangValueType key)
        {
            return langValue.Value.First(x => x.Key.Equal(key)).Value;
        }

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

public static class ListValueFuncStatic
{
    extension(ListLangValue langValue)
    {
        public LangValueType Add(LangValueType langValueType)
        {
            langValue.Values.Add(langValueType);
            return langValueType;
        }

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

        public LangValueType RemoveAt(IntLangValue num)
        {
            var a = langValue.Values[num.Value];
            langValue.Values.RemoveAt(num.Value);
            return a;
        }

        public VoidLangValue AddList(ListLangValue otherLangValue)
        {
            langValue.Values.AddRange(otherLangValue.Values);
            return new VoidLangValue();
        }

        public ListLangValue Sort()
        {
            QuickSort(langValue.Values, 0, langValue.Values.Count - 1);
            return langValue;
        }

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

        public ListLangValue Reverse()
        {
            langValue.Values.Reverse();
            return langValue;
        }

        public BoolLangValue Contains(LangValueType element)
        {
            return new BoolLangValue(langValue.Values.Any(item => item.Equal(element)));
        }

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

    private static void Swap(List<LangValueType> nums, int i, int j)
    {
        (nums[i], nums[j]) = (nums[j], nums[i]);
    }
}