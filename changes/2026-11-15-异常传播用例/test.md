# IL 异常传播用例迁移与反射异常解包 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「Exceptions 组：旧语法与 catch 变量拿到 reflection 消息」。

## 1. 现象

`Compiler.Exceptions` 8 项失败，分两类：

1. **旧语法**（5 项）：裸 `throw` 重抛、`case 1 -> {}` switch 分支、`func(x:int) -> int {}`
   旧匿名函数、`class Derived : Base` 旧继承、match 分支里直接 `throw`；
2. **语义/断言**（3 项）：
   - lambda 里 `throw "Invalid value 5"`，catch 变量拿到
     `Exception has been thrown by the target of an invocation.`（解释器/VM 拿到原值）；
   - `throw` 表达式用例先触发除零，消息自然不含 `Error:`；
   - 未捕获异常的 finally 用例断言异常消息包含 `try;finally;`（三种模式都不可能）。

## 2. 修复

- `ExceptionWrapper.Bind`：逐层剥掉 `TargetInvocationException` 再解包
  `LangThrownException`，lambda/委托里的语言异常能原样到达 catch 变量。
- 用例迁移：
  - `throw` → `throw inner`；
  - switch `case N -> {}` → `case N {}`；
  - `func(x:int) -> int {}` → `(x:int) -> {}`；
  - `class Derived : Base` → `extends Base`；
  - match 分支 `throw` 改为调用 `failAtTwo()`（match 分支是表达式）；
  - 条件重抛用例的触发条件/期望值改成实际会执行到的分支（`value == 150`）；
  - 未捕获 finally 用例改为外层 catch，断言 `result == "try;finally;caught"`；
  - `throw` 表达式用例避免先除零（`y <- 2`），`Assert.Contains` 参数顺序改为
    `(text, substring)`。

## 3. 验证

- CLI：lambda 内 `throw "Invalid value 5"` 在 IL 下 catch 到原字符串（三模式一致）；
- `Compiler.Exceptions` 8 → **0**（64 项全通过）；
- 整组回归见 `changes/Todo.md`。
