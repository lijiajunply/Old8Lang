# 节点 partial 过渡归档

本目录保存第一阶段迁出的、仍以 AST `partial class` 形式存在的执行辅助代码。
它们必须继续编译进 `Old8Lang.Runtime`：C# 的 partial 类型不能跨程序集。
节点迁入 `Old8Lang.Syntax` 后，其辅助代码改成独立静态类；这些辅助文件仍保留在本目录
（如 `Expressions/OperationRuntimeExtensions.cs`、`Declarations/ReturnStatementRuntimeExtensions.cs`），
便于对照迁移历史。它们不再是 `partial class`，不计入遗留 partial 基线。

这不是语法模型的一部分。阶段 2 拆分语法程序集时，`Old8Lang.Syntax` 不得包含本目录；每个文件会随其依赖的解释器、IL 或运行时值契约迁至对应程序集，并删除 partial 扩展。

分类仅用于迁移顺序和依赖审计：

- `ControlFlow`：循环、选择、异常与资源控制流；
- `Declarations`：函数、类、导入和赋值的执行/IL 辅助；
- `Expressions`：表达式求值和 IL 发射辅助；
- `RuntimeValues`：尚未拆分的运行时值行为。

不得在 `Execution/Nodes` 新增 `*.Split.cs`；该旧目录只保留无法归类的非 partial 运行时辅助文件。
