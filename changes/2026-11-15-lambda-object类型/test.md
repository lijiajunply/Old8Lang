# lambda 参数类型注解漏掉 `object` 导致解析失败 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「(f:object) -> ... 解析失败」。

## 1. 现象

```old8
x <- (f:object) -> {
    return 1
}
```

所有模式在 `-s` 阶段报
`SYNTAX_ERROR 无法识别的主表达式类型 'Arrow'`；
而 `(f:string) -> ...`、`(f:any) -> ...` 正常。

`func f(x:object)` 正常，只有 **lambda 参数** 的 `object` 注解失败。
大写 `Object` 还会报「不支持的类型注解: Object」。

## 2. 根因

`PrimaryParser.ParseLambdaOrTuple` 用两份硬编码类型名表判断
「`identifier:identifier` 是 lambda 参数还是命名元组」：

1. 预读判断表缺少 `object` / `function` → `(f:object)` 被当成命名元组，
   后续在 `->` 处报错；
2. `ParseLambdaParameter` 的注解白名单虽然列了 `object`，但用大小写敏感的
   `Contains`，`Object` 仍被拒绝。

## 3. 修复

- 两处类型表都补上 `object` / `function`（与函数参数注解保持一致）；
- 两处判断改为 `StringComparer.OrdinalIgnoreCase`，`Object` 等同 `object`。

涉及文件：`Old8Lang.Syntax/LangParser/Parsers/PrimaryParser.LambdaAndTuple.cs`。

## 4. 验证

### 4.1 CLI

| 输入 | 修复前 | 修复后 |
|---|---|---|
| `x <- (f:object) -> { return 1 }` | SyntaxError Arrow | 解析成功 |
| `x <- (f:Object) -> f` | SyntaxError 不支持类型注解 | 解析成功 |
| `x <- (f:string) -> {...}` | 正常 | 正常 |

### 4.2 用例

装饰器测试里的 wrapper lambda 参数补上 `:int` 注解（IL 要求完整类型注解），
`DecoratorCompilerTests`/`DecoratorInterpreterTests` 19 → 10 失败；
剩余为返回类型变化、闭包修改外层变量、未定义装饰器错误断言等更深缺口。

整组回归数字见 `changes/Todo.md`。
