## 问题分析

1. **错误信息**：`语法错误：期望 Identifier，但得到了 Import。在 3:2`
2. **错误位置**：`ParseImportStatement` 方法的第 366 行，调用 `Expect(LangTokenType.Identifier)` 时
3. **根本原因**：词法分析器在处理关键字时，会调用 `Func` 函数两次，导致重复添加关键字标记

## 问题详情

词法分析器 `LangTokenizer` 中的关键字处理逻辑存在问题：

```csharp
var enumList = Enum.GetNames<KeywordType>().Select(x => x.ToLower()).ToFrozenSet();
if (enumList.Where(x => x[0] == code[i]).Any(x => Func(x, i)))
{
    var matchedLength = enumList.First(x => x[0] == code[i] && Func(x, i)).Length;
    i += matchedLength - 1;
    continue;
}
```

这段代码会调用 `Func` 函数两次：
1. 第一次在 `Any(x => Func(x, i))` 中，用于检查是否有匹配的关键字
2. 第二次在 `First(x => x[0] == code[i] && Func(x, i))` 中，用于获取匹配关键字的长度

但 `Func` 函数有副作用，它会向 `tokens` 列表中添加标记，导致重复添加关键字标记。

## 修复方案

1. **修改关键字处理逻辑**：将 `Func` 函数的副作用移除，或者修改为只调用一次
2. **优化 `Func` 函数**：让它只检查是否匹配，而不添加标记，然后在确定匹配后