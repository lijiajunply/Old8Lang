# 语法字面量与运行时值的边界盘点

日期：2026-11-14
状态：阶段 2 首批标量字面量已迁移；集合与运行时值仍待阶段 2/3。
依据：`Docs/architecture-migration/design.md`「语法与运行时分界」

## 1. 现状

`LangValueType` 同时承担两种职责：

1. **语法节点**：解析器直接把源码字面量解析成对应 `LangValueType` 子类（如 `IntLangValue`），
   它们参与 Visitor 分派、持有源位置。
2. **运行时值**：同一批对象承载运算、成员访问、模块与异步行为，并作为求值结果在解释器/IL/VM
   三种后端之间传递。

因此 `LangValueType` 及其子类无法简单整体搬入「语法模型」程序集——这是阶段 2 拆分的核心障碍。

## 2. 分类盘点

`Old8Lang.Runtime/AST/Expression` 下共有 **52 个 `LangValueType` 子类**，按职责可分为：

| 类别 | 代表类型 | 构造来源 | 阶段 2 归属建议 |
|------|----------|----------|------------------|
| A. 标量字面量语法节点 | `IntLangValue`、`DoubleLangValue`、`StringLangValue`、`BoolLangValue`、`CharLangValue`、`NullLangValue`、`VoidLangValue` | 解析器直接构造（约 25 处） | 语法模型（仅保留字面量数据 + 位置；求值语义由处理器实现） |
| B. 结构/集合字面量 | `ArrayLangValue`、`ListLangValue`、`DictionaryLangValue`、`TupleLangValue`、`RangeLangValue`、`SliceLangValue`、`ListComprehension`、`StringTemplateValue` | 解析器构造 + 求值构造 | 语法模型持有语法形态；可变的运行时集合归共享运行时契约 |
| C. 运行时值 | `FuncLangValue`、`AsyncFuncLangValue`、`Instance`、`AnyLangValue`、`EnumLangValue`、`TypeLangValue`、`ErrorLangValue`、`LockedVariableLangValue`、`GeneratorLangValue`、`AsyncGeneratorLangValue`、`AsyncStreamLangValue` | 求值/执行期产生 | 执行后端或共享运行时契约 |
| D. 平台/静态包装值 | `Task*LangValue`、`Thread*LangValue`、`CancellationToken*`、`Assert*`、`MockLib*`、`TestRunner*`、`VMThreadLangValue`、`PythonFunctionLangValue`、`JavaScriptFunctionLangValue` | 运行时构造 | 执行后端（官方扩展通过组合根注册） |
| E. 模块/辅助对象 | `ImportInfo`、`MethodOverloadList`、`LangListItem` | 模块加载/求值 | 共享运行时契约 |

解析器侧的构造点共 **49 处**（`new IntLangValue` 7、`NullLangValue` 6、`StringLangValue` 6、
`FuncLangValue` 5、`TupleLangValue` 5、`AsyncFuncLangValue` 5，其余约 15 处）。
这些都属于「语法 → 运行时」的直接耦合点。

## 3. 阶段 1 已完成的部分

- 节点上的执行方法（`Run/GenerateIl/LoadIlValue/OutputType/SetValueToIl`）已全部迁出，
  语法节点不再引用解释器/IL/VM 类型（`SyntaxBackendBoundaryTests` 证明）。
- 引入 `ILiteralSyntax` 标记接口与 `LiteralValue` 属性，标量字面量
  （`IntLangValue`/`DoubleLangValue`/`StringLangValue`/`BoolLangValue`/`CharLangValue`/
  `NullLangValue`/`VoidLangValue`）已实现，明确「语法字面量」与「运行时值」的区分；
  架构测试 `ScalarLiteralNodes_AreMarkedAsLiteralSyntax` 固化该约定。
- 语句与表达式结构节点的后端辅助方法已移至 `Old8Lang.Runtime/Execution`。
- Visitor 生成器已可重复运行（88 节点），生成代码与手工扩展通过 `partial interface` 共存。

## 4. 分离实施建议（阶段 2/3）

1. **标量语法字面量（已完成首批）**：`Old8Lang.Syntax` 提供
   `LiteralSyntax(LiteralKind, object? rawValue, SourcePosition)`、`LiteralKind` 与 `ILiteralSyntax`，
   只依赖 Core。`PrimaryParser` 先产出该语法数据，再由 Runtime 中唯一的
   `ScalarLiteralRuntimeValueFactory` 转换为既有 `IntLangValue` 等值，避免解析规则直接构造运行时值。
   标量运行时值暂时实现同一接口，以保持现有公开 AST/Visitor 兼容；它们尚未从 Runtime 移出。
2. **后续 AST 节点化**：将 `LiteralSyntax` 作为可被 Visitor 分派的独立表达式节点，届时解析器
   不再在同一轮调用中转换为 `LangValueType`。
3. **求值边界**：`InterpreterVisitor`/`CompilerVisitor`/`BytecodeVisitor` 在访问字面量节点时
   创建或复用运行时值；保持现有对象身份、闭包与可变集合语义（同一字面量多次求值不应共享可变状态）。
4. **类型推断**：`TypeInferenceVisitor` 对字面量节点直接返回静态类型，不再依赖运行时值对象。
5. **兼容期**：保留 `LangValueType` 子类的公开构造（已声明接受 C# 源码 API 不兼容，需提供迁移说明）。
6. **验证**：以三模式代表性测试 + 字节码兼容样本 + 闭包/可变集合回归为门禁，
   并新增「解析器不引用运行时值类型」的边界测试。

## 5. 风险

- 解析器与运行时值的构造点分散（49 处），改动面大，必须逐类迁移、逐批测试。
- 字面量被多处当作文本复用（`ToString`、源位置、错误报告），迁移时需保持位置与显示语义。
- 集合/区间字面量同时是可变运行时对象，分离时需明确「语法形态」与「运行时实例」的对应关系。
