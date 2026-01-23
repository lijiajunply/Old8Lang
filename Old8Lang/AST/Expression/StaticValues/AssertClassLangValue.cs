using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// Assert 类的全局对象,提供静态方法访问
/// </summary>
public partial class AssertClassLangValue : LangValueType
{
    private static readonly AssertClassLangValue Instance = new();

    /// <summary>
    /// 获取 Assert 类的全局单例
    /// </summary>
    public static AssertClassLangValue GetInstance() => Instance;

    public override string TypeToString() => "AssertClass";

    public override string ToDisplayString() => "Assert";

    /// <summary>
    /// 生成 IL 代码，返回 Assert 类型本身
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 对于 Assert 类静态方法，我们不需要加载实例
        // 直接返回 Assert 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(AssertHelper));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);
    }


    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }


    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 处理 Assert.AssertEqual(...) 形式的调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            // 返回一个包装函数,用于调用静态方法
            Func<List<LangValueType>, SourcePosition, LangValueType>? method = methodName switch
            {
                "AssertEqual" or "Equal" => AssertEqual,
                "AssertNotEqual" or "NotEqual" => AssertNotEqual,
                "AssertTrue" or "True" => AssertTrue,
                "AssertFalse" or "False" => AssertFalse,
                "AssertNull" or "Null" => AssertNull,
                "AssertNotNull" or "NotNull" => AssertNotNull,
                "AssertGreater" or "Greater" => AssertGreater,
                "AssertGreaterOrEqual" or "GreaterOrEqual" => AssertGreaterOrEqual,
                "AssertLess" or "Less" => AssertLess,
                "AssertLessOrEqual" or "LessOrEqual" => AssertLessOrEqual,
                "AssertContains" or "Contains" => AssertContains,
                "AssertNotContains" or "NotContains" => AssertNotContains,
                "AssertStartsWith" or "StartsWith" => AssertStartsWith,
                "AssertEndsWith" or "EndsWith" => AssertEndsWith,
                "AssertMatches" or "Matches" => AssertMatches,
                "AssertContainsItem" or "ContainsItem" => AssertContainsItem,
                "AssertNotContainsItem" or "NotContainsItem" => AssertNotContainsItem,
                "AssertEmpty" or "Empty" => AssertEmpty,
                "AssertNotEmpty" or "NotEmpty" => AssertNotEmpty,
                "AssertLength" or "Length" => AssertLength,
                "AssertThrows" or "Throws" => AssertThrows,
                "AssertNotThrows" or "NotThrows" => AssertNotThrows,
                "AssertInstanceOf" or "InstanceOf" => AssertInstanceOf,
                "AssertNotInstanceOf" or "NotInstanceOf" => AssertNotInstanceOf,
                _ => null
            };

            if (method is null)
            {
                throw new AttributeError(dotExpression.Position, methodName, "Assert");
            }

            // 使用 ExternalManager 或传入的 manager 执行参数
            var currentManager = ExternalManager ?? manager;
            var args = instance.Ids.Select(id => id.Run(currentManager)).ToList();
            return method(args, instance.Position);
        }

        // 处理 Assert.AssertEqual 形式的访问（不带调用）
        if (dotExpression is LangId memberId)
        {
            var methodName = memberId.IdName;

            // 返回一个包装函数,用于调用静态方法
            return methodName switch
            {
                "AssertEqual" => new AssertStaticMethodWrapper("AssertEqual", AssertEqual),
                "AssertNotEqual" => new AssertStaticMethodWrapper("AssertNotEqual", AssertNotEqual),
                "AssertTrue" => new AssertStaticMethodWrapper("AssertTrue", AssertTrue),
                "AssertFalse" => new AssertStaticMethodWrapper("AssertFalse", AssertFalse),
                "AssertNull" => new AssertStaticMethodWrapper("AssertNull", AssertNull),
                "AssertNotNull" => new AssertStaticMethodWrapper("AssertNotNull", AssertNotNull),
                "AssertGreater" => new AssertStaticMethodWrapper("AssertGreater", AssertGreater),
                "AssertGreaterOrEqual" => new AssertStaticMethodWrapper("AssertGreaterOrEqual", AssertGreaterOrEqual),
                "AssertLess" => new AssertStaticMethodWrapper("AssertLess", AssertLess),
                "AssertLessOrEqual" => new AssertStaticMethodWrapper("AssertLessOrEqual", AssertLessOrEqual),
                "AssertContains" => new AssertStaticMethodWrapper("AssertContains", AssertContains),
                "AssertNotContains" => new AssertStaticMethodWrapper("AssertNotContains", AssertNotContains),
                "AssertStartsWith" => new AssertStaticMethodWrapper("AssertStartsWith", AssertStartsWith),
                "AssertEndsWith" => new AssertStaticMethodWrapper("AssertEndsWith", AssertEndsWith),
                "AssertMatches" => new AssertStaticMethodWrapper("AssertMatches", AssertMatches),
                "AssertContainsItem" => new AssertStaticMethodWrapper("AssertContainsItem", AssertContainsItem),
                "AssertNotContainsItem" =>
                    new AssertStaticMethodWrapper("AssertNotContainsItem", AssertNotContainsItem),
                "AssertEmpty" => new AssertStaticMethodWrapper("AssertEmpty", AssertEmpty),
                "AssertNotEmpty" => new AssertStaticMethodWrapper("AssertNotEmpty", AssertNotEmpty),
                "AssertLength" => new AssertStaticMethodWrapper("AssertLength", AssertLength),
                "AssertThrows" => new AssertStaticMethodWrapper("AssertThrows", AssertThrows),
                "AssertNotThrows" => new AssertStaticMethodWrapper("AssertNotThrows", AssertNotThrows),
                "AssertInstanceOf" => new AssertStaticMethodWrapper("AssertInstanceOf", AssertInstanceOf),
                "AssertNotInstanceOf" => new AssertStaticMethodWrapper("AssertNotInstanceOf", AssertNotInstanceOf),
                _ => throw new AttributeError(dotExpression.Position, methodName, "Assert")
            };
        }

        throw new AttributeError(dotExpression.Position,
            dotExpression.ToString() ?? "unknown", "Assert");
    }

    /// <summary>
    /// AssertEqual 静态方法实现
    /// </summary>
    private static LangValueType AssertEqual(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertEqual 期望 2-3 个参数(expected, actual, message)，但提供了 {args.Count} 个");
        }

        var expected = args[0];
        var actual = args[1];
        var message = args.Count > 2 ? args[2] : null;

        if (!AreEqual(expected, actual))
        {
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望值 '{expected}' 但实际为 '{actual}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNotEqual 静态方法实现
    /// </summary>
    private static LangValueType AssertNotEqual(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertNotEqual 期望 2-3 个参数(notExpected, actual, message)，但提供了 {args.Count} 个");
        }

        var notExpected = args[0];
        var actual = args[1];
        var message = args.Count > 2 ? args[2] : null;

        if (AreEqual(notExpected, actual))
        {
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望值不为 '{notExpected}' 但实际相等";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertTrue 静态方法实现
    /// </summary>
    private static LangValueType AssertTrue(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertTrue 期望 1-2 个参数(condition, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not BoolLangValue condition)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "bool", args[0].TypeToString());
        }

        if (!condition.Value)
        {
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? "断言失败: 期望为 true 但实际为 false";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertFalse 静态方法实现
    /// </summary>
    private static LangValueType AssertFalse(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertFalse 期望 1-2 个参数(condition, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not BoolLangValue condition)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "bool", args[0].TypeToString());
        }

        if (condition.Value)
        {
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? "断言失败: 期望为 false 但实际为 true";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNull 静态方法实现
    /// </summary>
    private static LangValueType AssertNull(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertNull 期望 1-2 个参数(value, message)，但提供了 {args.Count} 个");
        }

        var value = args[0];
        if (!(value is NullLangValue))
        {
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望为 null 但实际为 '{value}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNotNull 静态方法实现
    /// </summary>
    private static LangValueType AssertNotNull(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertNotNull 期望 1-2 个参数(value, message)，但提供了 {args.Count} 个");
        }

        var value = args[0];
        if (value is NullLangValue)
        {
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? "断言失败: 期望不为 null 但实际为 null";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertGreater 静态方法实现
    /// </summary>
    private static LangValueType AssertGreater(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertGreater 期望 2-3 个参数(value, other, message)，但提供了 {args.Count} 个");
        }

        var value = args[0];
        var other = args[1];

        if (!TryCompare(value, other, out var compareResult))
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "comparable", $"{value.TypeToString()} 和 {other.TypeToString()}");
        }

        if (compareResult <= 0)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望 '{value}' > '{other}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertGreaterOrEqual 静态方法实现
    /// </summary>
    private static LangValueType AssertGreaterOrEqual(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertGreaterOrEqual 期望 2-3 个参数(value, other, message)，但提供了 {args.Count} 个");
        }

        var value = args[0];
        var other = args[1];

        if (!TryCompare(value, other, out var compareResult))
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "comparable", $"{value.TypeToString()} 和 {other.TypeToString()}");
        }

        if (compareResult < 0)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望 '{value}' >= '{other}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertLess 静态方法实现
    /// </summary>
    private static LangValueType AssertLess(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertLess 期望 2-3 个参数(value, other, message)，但提供了 {args.Count} 个");
        }

        var value = args[0];
        var other = args[1];

        if (!TryCompare(value, other, out var compareResult))
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "comparable", $"{value.TypeToString()} 和 {other.TypeToString()}");
        }

        if (compareResult >= 0)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望 '{value}' < '{other}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertLessOrEqual 静态方法实现
    /// </summary>
    private static LangValueType AssertLessOrEqual(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertLessOrEqual 期望 2-3 个参数(value, other, message)，但提供了 {args.Count} 个");
        }

        var value = args[0];
        var other = args[1];

        if (!TryCompare(value, other, out var compareResult))
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "comparable", $"{value.TypeToString()} 和 {other.TypeToString()}");
        }

        if (compareResult > 0)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望 '{value}' <= '{other}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertContains 静态方法实现
    /// </summary>
    private static LangValueType AssertContains(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertContains 期望 2-3 个参数(text, substring, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue text)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not StringLangValue substring)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[1].TypeToString());
        }

        if (!text.Value.Contains(substring.Value))
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 字符串 '{text.Value}' 不包含 '{substring.Value}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNotContains 静态方法实现
    /// </summary>
    private static LangValueType AssertNotContains(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertNotContains 期望 2-3 个参数(text, substring, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue text)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not StringLangValue substring)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[1].TypeToString());
        }

        if (text.Value.Contains(substring.Value))
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 字符串 '{text.Value}' 包含 '{substring.Value}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertStartsWith 静态方法实现
    /// </summary>
    private static LangValueType AssertStartsWith(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertStartsWith 期望 2-3 个参数(text, prefix, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue text)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not StringLangValue prefix)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[1].TypeToString());
        }

        if (!text.Value.StartsWith(prefix.Value))
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 字符串 '{text.Value}' 不以 '{prefix.Value}' 开头";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertEndsWith 静态方法实现
    /// </summary>
    private static LangValueType AssertEndsWith(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertEndsWith 期望 2-3 个参数(text, suffix, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue text)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not StringLangValue suffix)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[1].TypeToString());
        }

        if (!text.Value.EndsWith(suffix.Value))
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 字符串 '{text.Value}' 不以 '{suffix.Value}' 结尾";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertMatches 静态方法实现
    /// </summary>
    private static LangValueType AssertMatches(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertMatches 期望 2-3 个参数(text, pattern, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not StringLangValue text)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[0].TypeToString());
        }

        if (args[1] is not StringLangValue pattern)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[1].TypeToString());
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(text.Value, pattern.Value))
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 字符串 '{text.Value}' 不匹配正则表达式 '{pattern.Value}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertContainsItem 静态方法实现
    /// </summary>
    private static LangValueType AssertContainsItem(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertContainsItem 期望 2-3 个参数(collection, item, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList collection)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "collection", args[0].TypeToString());
        }

        var item = args[1];
        var contains = false;
        foreach (var element in collection.GetItems())
        {
            if (AreEqual(element, item))
            {
                contains = true;
                break;
            }
        }

        if (!contains)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 集合不包含元素 '{item}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNotContainsItem 静态方法实现
    /// </summary>
    private static LangValueType AssertNotContainsItem(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertNotContainsItem 期望 2-3 个参数(collection, item, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList collection)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "collection", args[0].TypeToString());
        }

        var item = args[1];
        foreach (var element in collection.GetItems())
        {
            if (AreEqual(element, item))
            {
                var message = args.Count > 2 ? args[2] : null;
                var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
                var msg = messageStr ?? $"断言失败: 集合包含元素 '{item}'";
                throw new Exception(msg);
            }
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertEmpty 静态方法实现
    /// </summary>
    private static LangValueType AssertEmpty(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertEmpty 期望 1-2 个参数(collection, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList collection)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "collection", args[0].TypeToString());
        }

        if (collection.GetItems().Any())
        {
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? "断言失败: 集合不为空";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNotEmpty 静态方法实现
    /// </summary>
    private static LangValueType AssertNotEmpty(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertNotEmpty 期望 1-2 个参数(collection, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList collection)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "collection", args[0].TypeToString());
        }

        if (!collection.GetItems().Any())
        {
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? "断言失败: 集合为空";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertLength 静态方法实现
    /// </summary>
    private static LangValueType AssertLength(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertLength 期望 2-3 个参数(collection, expectedLength, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not ILangList collection)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "collection", args[0].TypeToString());
        }

        if (args[1] is not IntLangValue expectedLength)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "int", args[1].TypeToString());
        }

        var actualLength = collection.GetItems().Count();
        if (actualLength != expectedLength.Value)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望集合长度为 {expectedLength.Value} 但实际为 {actualLength}";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertThrows 静态方法实现
    /// </summary>
    private static LangValueType AssertThrows(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertThrows 期望 1-2 个参数(action, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not FuncLangValue action)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "function", args[0].TypeToString());
        }

        try
        {
            action.Run(new VariateManager(), []);
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? "断言失败: 期望抛出异常但未抛出";
            throw new Exception(msg);
        }
        catch (Exception ex) when (!(ex.Message.StartsWith("断言失败:")))
        {
            // 预期的异常
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNotThrows 静态方法实现
    /// </summary>
    private static LangValueType AssertNotThrows(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"AssertNotThrows 期望 1-2 个参数(action, message)，但提供了 {args.Count} 个");
        }

        if (args[0] is not FuncLangValue action)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "function", args[0].TypeToString());
        }

        try
        {
            action.Run(new VariateManager(), []);
        }
        catch (Exception ex)
        {
            var message = args.Count > 1 ? args[1] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望不抛出异常但抛出了 {ex.GetType().Name}: {ex.Message}";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertInstanceOf 静态方法实现
    /// </summary>
    private static LangValueType AssertInstanceOf(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertInstanceOf 期望 2-3 个参数(obj, expectedType, message)，但提供了 {args.Count} 个");
        }

        var obj = args[0];
        if (args[1] is not StringLangValue expectedType)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[1].TypeToString());
        }

        // 简单的类型检查 - 检查类型名称是否匹配
        var actualType = obj.TypeToString();
        if (actualType != expectedType.Value)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望类型为 '{expectedType.Value}' 但实际为 '{actualType}'";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// AssertNotInstanceOf 静态方法实现
    /// </summary>
    private static LangValueType AssertNotInstanceOf(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"AssertNotInstanceOf 期望 2-3 个参数(obj, unexpectedType, message)，但提供了 {args.Count} 个");
        }

        var obj = args[0];
        if (args[1] is not StringLangValue unexpectedType)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "string", args[1].TypeToString());
        }

        // 简单的类型检查 - 检查类型名称是否匹配
        var actualType = obj.TypeToString();
        if (actualType == unexpectedType.Value)
        {
            var message = args.Count > 2 ? args[2] : null;
            var messageStr = message is StringLangValue str ? str.Value : message?.ToDisplayString();
            var msg = messageStr ?? $"断言失败: 期望类型不为 '{unexpectedType.Value}' 但实际为该类型";
            throw new Exception(msg);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// 比较两个对象是否相等
    /// </summary>
    private static bool AreEqual(LangValueType a, LangValueType b)
    {
        if (ReferenceEquals(a, b)) return true;

        // 处理 null 值
        if (a is NullLangValue && b is NullLangValue) return true;
        if (a is NullLangValue || b is NullLangValue) return false;

        // 处理字符串比较
        if (a is StringLangValue strA && b is StringLangValue strB)
            return strA.Value == strB.Value;

        // 处理数值比较
        if (a is IntLangValue intA && b is IntLangValue intB)
            return intA.Value == intB.Value;

        if (a is DoubleLangValue doubleA && b is DoubleLangValue doubleB)
            return Math.Abs(doubleA.Value - doubleB.Value) < 1e-10;

        if (a is DoubleLangValue doubleA2 && b is IntLangValue intB2)
            return Math.Abs(doubleA2.Value - intB2.Value) < 1e-10;

        if (a is IntLangValue intA2 && b is DoubleLangValue doubleB2)
            return Math.Abs(intA2.Value - doubleB2.Value) < 1e-10;

        // 处理布尔比较
        if (a is BoolLangValue boolA && b is BoolLangValue boolB)
            return boolA.Value == boolB.Value;

        // 处理集合比较
        if (a is ILangList listA && b is ILangList listB)
            return CollectionsEqual(listA.GetItems().ToList(), listB.GetItems().ToList());

        // 默认使用 ToString 比较
        return a.ToDisplayString() == b.ToDisplayString();
    }

    /// <summary>
    /// 比较两个集合是否相等
    /// </summary>
    private static bool CollectionsEqual(List<LangValueType> a, List<LangValueType> b)
    {
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (!AreEqual(a[i], b[i])) return false;
        }

        return true;
    }

    /// <summary>
    /// 尝试比较两个值，返回比较结果
    /// </summary>
    private static bool TryCompare(LangValueType a, LangValueType b, out int result)
    {
        result = 0;

        // 处理数值比较
        if (a is IntLangValue intA)
        {
            if (b is IntLangValue intB)
            {
                result = intA.Value.CompareTo(intB.Value);
                return true;
            }

            if (b is DoubleLangValue doubleB)
            {
                result = intA.Value.CompareTo(doubleB.Value);
                return true;
            }
        }

        if (a is DoubleLangValue doubleA)
        {
            if (b is IntLangValue intB)
            {
                result = doubleA.Value.CompareTo(intB.Value);
                return true;
            }

            if (b is DoubleLangValue doubleB)
            {
                result = doubleA.Value.CompareTo(doubleB.Value);
                return true;
            }
        }

        // 处理字符串比较
        if (a is StringLangValue strA && b is StringLangValue strB)
        {
            result = string.Compare(strA.Value, strB.Value, StringComparison.Ordinal);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Assert 静态方法的包装器
/// </summary>
public partial class AssertStaticMethodWrapper(
    string methodName,
    Func<List<LangValueType>, SourcePosition, LangValueType> method)
    : LangValueType
{
    public override string TypeToString() => "AssertStaticMethod";

    public override string ToDisplayString() => $"Assert.{methodName}";

    /// <summary>
    /// 执行静态方法
    /// </summary>
    public LangValueType Invoke(List<LangValueType> args, SourcePosition position)
    {
        return method(args, position);
    }


    /// <summary>
    /// 生成 IL 代码，返回对应Assert静态方法的委托
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // Assert 静态方法在 IL 中通常通过直接调用来实现
        // 这里返回 null 作为占位符
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return typeof(Delegate);
    }
}