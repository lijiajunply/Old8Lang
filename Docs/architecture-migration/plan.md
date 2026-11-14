# 架构迁移实施计划

状态：草案。目标与约束见 [spec.md](spec.md)，依赖设计见 [design.md](design.md)。每阶段完成后运行适用测试、在 `Reports/` 写报告并独立提交；测试发现无关问题记入 `Todo.md`。不要把未完成的阶段标成完成。

## 0. 固定基线与工作区

- [x] 保留并审查工作区未提交的块导入迁移；完成构建和导入/生成器回归后单独提交，或按用户决定处理。不要覆盖已暂存的 `.run/Old8Lang.App.run.xml`。
- [x] 收集解决方案构建、默认测试档及三模式代表性测试结果；复现 `Todo.md` 中与测试范围重叠的已知失败，记录命令、版本、结果和差异。
- [ ] 保存当前字节码兼容样本，验证编译、序列化、读取与执行；盘点公开 C# API 与现有调用方。

**门禁**：有可复现报告，测试失败已分类，不能将未知失败当作既有失败。

## 1. 消除节点执行反向依赖

- [x] 按控制流、表达式、函数/闭包、类、模块、异常、异步/生成器和集合等节点族，逐批迁出解释、IL 与类型推断实现；每批核对 Visitor 与旧路径行为差异。
  （2026-11-14：完成，见 Reports/2026-11-14-00-45-第一阶段完成测试.md）
- [x] 消除节点内和调用方对旧执行方法的依赖，移除兼容转发；显式覆盖生成器恢复、作用域清理及无返回值调用的 IL 栈平衡。
  （2026-11-14：完成；架构边界测试 SyntaxBackendBoundaryTests 通过）
- [x] 分离语法字面量与运行时值，调整 Visitor 生成器及生成代码；检查新增节点生成流程。
  （2026-11-14：生成器可重复运行并生成 88 节点；字面量/运行时值完成盘点与区分
  （ILiteralSyntax 标记 + 架构测试），程序集级分离（解析器产出字面量语法节点）
  按设计文档归入阶段 2/3，见 Docs/architecture-migration/literal-runtime-boundary.md。）

**门禁**：按节点族运行相关三模式测试；源码及架构测试证明语法节点不引用后端。每批独立报告与提交。

## 2. 拆分语法与共享契约

- [ ] 依据实际依赖图拆出语法模型、解析器及最小共享运行时契约，处理类型、诊断、模块接口和运行时值的循环依赖。
- [ ] 更新生成器、LSP、测试和项目引用；建立可独立构建与程序集依赖约束测试。

**门禁**：语法/解析项目独立构建，不引用任何执行后端或官方扩展；语法、诊断、LSP 回归通过或与基线一致。

## 3. 拆分执行后端

- [ ] 逐个拆分解释器、IL 和字节码/VM；将跨后端反射、函数、模块及编译期预执行依赖改为明确契约或组合服务。
- [ ] 验证后端分别构建、独立组合、无循环依赖；更新 CLI、LSP 和基准引用。
- [ ] 验证历史字节码样本、导入与三模式代表性行为；记录公开 C# API 的迁移路径。

**门禁**：每个后端单独构建及针对性回归，程序集依赖测试通过；按后端分别提交。

## 4. 宿主隔离与最终验收

- [ ] 审计并处理默认静态模块注册表的所有入口；验证多宿主隔离与官方模块显式注册。
- [ ] 运行 `dotnet build Old8Lang.sln`、默认单元测试和适当的三模式端到端测试；对照基线审查每个失败及跳过项。
- [ ] 更新架构、开发、API 迁移和模式支持文档，检查项目引用与工作区；生成最终测试报告并提交。

**完成条件**：逐条满足规格的验收标准；未解决的回归、未验证的字节码兼容或残留后端循环依赖均不能视为完成。

## 进度记录

- 2026-11-14：`FunctionSyntax` 引入并迁移 `FuncInit`/`AsyncFuncInit` 到 Syntax；解析器只产出函数语法，
  运行时函数值经 `FunctionSyntaxRuntimeFactory`/`GetFuncValue()` 按语法节点缓存创建。同步更新
  IL 顶层变量分析、VM 闭包分析、LSP、类型推断/约束收集与测试调用点。Runtime 生成节点 57 → 55，
  遗留 partial 基线 11 → 9。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476
  （2 跳过）、Architecture 43 通过；Compiler.Basic 16 / Collections+Exceptions 27 / Functions 57 /
  Statements 4 / Expressions 41 / Async 65 / Types 51 项失败与基线逐项一致。见
  `Reports/2026-11-14-15-02-函数声明语法迁移测试.md`。

- 2026-11-14：9 个 LINQ 子句类型（`LinqClause` 及 From/Where/Select/OrderBy/Let/Join/GroupBy/
  QueryContinuation）迁入 Syntax；`LinqExpression` 及其 IL 编译器 partial 暂留 Runtime。
  Parser 889、Compiler.Linq 45、Interpreter 1967/1968（1 跳过）、VM 1082、Architecture 43 通过。
  见 `Reports/2026-11-14-14-55-linq子句语法迁移测试.md`。

- 2026-11-14：`FunctionDecorator` 迁入 Syntax（纯语法数据，无后端依赖）；为后续函数声明语法节点做准备。
  Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082（一次并发用例抖动，重跑通过）、
  Architecture 43 通过。见 `Reports/2026-11-14-14-54-装饰器语法迁移测试.md`。

- 2026-11-14：lambda 改为 Syntax 的 `LambdaExpression` 语法节点；解析器不再直接构造
  `FuncLangValue`/`AsyncFuncLangValue`，求值端由 `LambdaExpressionRuntimeFactory` 适配。同步更新
  VM 闭包捕获分析、IL 顶层变量分析、IL 委托包装与 lambda 赋值路径。Runtime 生成节点保持 57。
  Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）、Architecture 43 通过；
  Compiler.Basic 16 / Collections+Exceptions 27 / Functions 57 / Statements 4 / Expressions 41 /
  Async 65 / Types 51 项失败与 e46a7f5d 基线逐项一致。见
  `Reports/2026-11-14-14-52-lambda语法节点迁移测试.md`。

- 2026-11-14：`StringTemplateValue` 由 `LangValueType` 改为 `LangExpression` 并迁入 Syntax；
  Runtime 生成节点 58 → 57，遗留 partial 保持 11。Parser 889、Interpreter 1967/1968（1 跳过）、
  VM 1082、LSP 474/476（2 跳过）通过；Compiler.Expressions 41 项失败与基线一致。见
  `Reports/2026-11-14-14-45-字符串模板迁移测试.md`。

- 2026-11-14：`GenericInstanceExpression` 迁入 Syntax；类型注解转换、类型映射与泛型调用/实例化
  IL 辅助移入 `GenericInstanceExpressionRuntimeExtensions`。Runtime 生成节点 59 → 58，遗留 partial
  基线 12 → 11。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）、
  Architecture 43 通过；Compiler.Expressions 41、Types 51 项失败与 e46a7f5d 基线逐项一致。
  见 `Reports/2026-11-14-14-44-泛型实例化迁移测试.md`。

- 2026-11-14：`MatchExpression` 迁入 Syntax；语法节点、Visitor 与程序集归属测试同步更新。
  Runtime 生成节点 60 → 59。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、
  LSP 474/476（2 跳过）通过；Compiler.Expressions 41 项失败与基线一致。见
  `Reports/2026-11-14-14-42-match表达式迁移测试.md`。

- 2026-11-14：`ListComprehension` 由 `LangValueType` 运行时值改为 `LangExpression` 语法节点并迁入 Syntax；
  条件判断与嵌套循环执行移入 `ListComprehensionRuntimeExtensions`。Runtime 生成节点 61 → 60，
  遗留 partial 基线 13 → 12。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、
  LSP 474/476（2 跳过）通过；Compiler.Collections 15 / Expressions 41 项失败与基线一致。
  见 `Reports/2026-11-14-14-40-列表推导式迁移测试.md`。

- 2026-11-14：`AsyncForInStatement` 迁入 Syntax；异步流/异步生成器/列表分支与 IL 生成移入
  `AsyncForInStatementRuntimeExtensions`。Runtime 生成节点 62 → 61，遗留 partial 基线 15 → 13。
  Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）、Architecture 43 通过；
  Compiler.Statements 4 / Expressions 41 / Async 65 项失败与基线一致。见
  `Reports/2026-11-14-14-38-asyncforin节点迁移测试.md`。

- 2026-11-14：`ForInStatement` 迁入 Syntax；集合、生成器、异步生成器/流的分支执行与 IL 字典遍历
  移入 `ForInStatementRuntimeExtensions`（两个 partial 合并为 partial static class）。Runtime 生成节点
  63 → 62，遗留 partial 基线 17 → 15。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、
  LSP 474/476（2 跳过）、Architecture 43 通过；Compiler.Statements 4 / Expressions 41 /
  Async 65 项失败与基线一致。见 `Reports/2026-11-14-14-36-forin节点迁移测试.md`。

- 2026-11-14：解析器为自增/自减、未初始化字段、字符串模板、动态导入和空 `return` 生成的合成
  运行时字面量改为 `ScalarLiteralExpression`，解析器不再直接构造这些 `LangValueType`。
  Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）通过；
  Compiler.Basic/Expressions/Functions/Statements/Collections/Exceptions/Async 的失败数量与失败用例
  和 `e46a7f5d` 临时基线逐项一致。见 `Reports/2026-11-14-14-34-解析器合成字面量迁移测试.md`。

- 2026-11-14：`EnumInit`、`SelectStatement` 迁入 Syntax；Select 的通道轮询与生成器路径执行移入
  `SelectStatementRuntimeExtensions`。Runtime 生成节点 65 → 63，遗留 partial 基线 18 → 17。
  Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）、Architecture 43 通过；
  Compiler.Statements 4 项与 Compiler.Async 65 项失败与基线一致。见
  `Reports/2026-11-14-14-32-enum与select节点迁移测试.md`。

- 2026-11-14：`SwitchStatement`、`CaseStatement`、`TryStatement` 迁入 Syntax；switch 跳转表生成与
  try 的生成器/标准执行、异常类型匹配移入 Runtime 扩展类。Runtime 生成节点 68 → 65，遗留 partial
  基线 20 → 18。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）、
  Architecture 43 通过；Compiler.Statements 4 项与 Compiler.Async 65 项失败与基线一致。
  见 `Reports/2026-11-14-14-30-switch与try节点迁移测试.md`。

- 2026-11-14：`FuncRunStatement` 迁入 Syntax，四个专用构造器合并为单一 `LangExpression` 构造器；
  四个 Visitor 改用 `Expression` 属性。Runtime 生成节点 69 → 68，遗留 partial 保持 20。
  Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）、Architecture 43 通过；
  Compiler.Statements 4 项与 Compiler.Async 65 项失败与基线一致。见
  `Reports/2026-11-14-14-27-表达式语句节点迁移测试.md`。

- 2026-11-14：`MatchCase`、`TuplePattern`、`TuplePatternElement`、`RangePattern`、`SelectCase`、
  `ImportItem`、`ClassFieldSetStatement` 迁入 Syntax；`MatchCase` 的模式匹配执行与运行时值判定移入
  `MatchCaseRuntimeExtensions`。Runtime 生成节点保持 69（本批无 Visitor 节点），遗留 partial 基线
  21 → 20。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476（2 跳过）、
  Architecture 43 通过；Compiler.Statements 4 项与 Compiler.Async 65 项失败与基线一致。
  见 `Reports/2026-11-14-14-25-匹配与导入模型迁移测试.md`。

- 2026-11-14：`SetStatement`、`ForStatement` 迁入 Syntax；全局槽位写入、数值转换与循环变量遮蔽
  辅助移入 Runtime 的 `SetStatementRuntimeExtensions`/`ForStatementRuntimeExtensions`。Runtime 生成节点
  71 → 69，遗留 partial 基线 23 → 21。Parser 889、VM 1082、LSP 474/476（2 跳过）、Architecture 43 通过；
  Interpreter 1966 通过 + 1 个网络用例首跑失败（单独重跑通过）；Compiler.Statements 4 项与
  Compiler.Async 65 项失败与基线一致。见 `Reports/2026-11-14-14-23-赋值与for节点迁移测试.md`。

- 2026-11-14：`WhileStatement`、`UsingStatement` 迁入 Syntax；生成器路径执行与 using 资源释放辅助
  移入 Runtime 的 `WhileStatementRuntimeExtensions`/`UsingStatementRuntimeExtensions`。Runtime 生成节点
  73 → 71，遗留 partial 基线 25 → 23。Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、
  LSP 474/476（2 跳过）、Architecture 43 通过；Compiler.Statements 4 项与 Compiler.Async 65 项失败
  与基线一致。见 `Reports/2026-11-14-14-20-while与using节点迁移测试.md`。

- 2026-11-14：`IfChild`、`IfStatement`、`DeferStatement` 迁入 Syntax；移除无人调用的
  `IfChild.GenerateConditionIl`。Runtime 生成节点 75 → 73，遗留 partial 基线 26 → 25。
  Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082（并发用例首跑抖动，重跑通过）、
  LSP 474/476（2 跳过）、Architecture 43 通过；Compiler.Statements 4 项失败与上一提交一致，
  Compiler.Async 65 项失败与基线一致。其余语句、表达式、解析器仍待拆分，见
  `Reports/2026-11-14-14-18-if与defer节点迁移测试.md`。

- 2026-11-14：`BlockStatement` 迁入 Syntax，改用 `IBlockDeclaration`/`IBlockImportDeclaration` 标记接口
  分类声明与导入，去掉对 `FuncInit`/`ClassInit` 等 Runtime 声明节点的类型分支；执行、IL 生成与类成员收集
  移入 `BlockStatementRuntimeExtensions`。Runtime 生成节点 76 → 75，遗留 partial 基线 27 → 26。
  新增 4 项块分类/程序集测试；Parser 889、Interpreter 1967/1968（1 跳过）、VM 1082、LSP 474/476
  （2 跳过）、Architecture 41 通过；Compiler.Statements 的 4 项失败与上一提交逐项一致。
  尚未完成：解析器与剩余声明/控制流节点，见 `Reports/2026-11-14-14-08-块语句节点迁移测试.md`。

- 2026-11-14：`BreakStatement`、`ContinueStatement`、`ReturnStatement`、`YieldStatement` 迁入 Syntax，
  语法接口新增四个 Visit 方法，Runtime 生成节点降至 76；`ReturnStatement` 的 IL 辅助方法
  （数值转换、类型兼容判断）移入 `ReturnStatementRuntimeExtensions`，遗留 partial 基线由 28 降为 27。
  架构测试 37 项通过；Interpreter 1967 通过/1 跳过、VM 1082 通过、LSP 474 通过/2 跳过，均无失败；Compiler.Async 失败数 65 与基线一致。其余语句、表达式、
  解析器及运行时值仍待拆分，阶段 2 未完成，见 `Reports/2026-11-14-13-53-叶子语句节点迁移测试.md`。

- 2026-11-14：标识符索引改由 Syntax 的 `IndexExpression` 表示，读取、赋值与对象成员索引暂经 Runtime 适配，保留旧类型兼容；其余 AST 与解析器仍待拆分，见 `Reports/2026-11-14-12-39-索引表达式迁移测试.md`。

- 2026-11-14：标识符切片改为 Syntax 的 `SliceExpression`，保留现有切片赋值及三模式行为，并适配切片端点和步长中的 await 扫描；其余 AST 与解析器仍待拆分，见 `Reports/2026-11-14-12-36-切片表达式迁移测试.md`。

- 2026-11-14：区间字面量改为 Syntax 的 `RangeLiteralExpression`，保留端点包含选项与三模式行为，IL switch 范围分支已适配；其他 AST 节点与解析器仍待拆分，见 `Reports/2026-11-14-12-31-区间表达式迁移测试.md`。

- 2026-11-14：元组字面量和解构目标改由 Syntax 的 `TupleLiteralExpression` 表示，保留命名字段、运行时对象身份和三模式解构；其他节点及解析器仍待拆分，见 `Reports/2026-11-14-12-28-元组字面量迁移测试.md`。

- 2026-11-14：字典字面量改由 Syntax 的 `DictionaryLiteralExpression` 表示，Visitor 保留键值求值顺序、对象身份和字典更新后的既有行为；元组与解析器仍待拆分，见 `Reports/2026-11-14-12-23-字典字面量迁移测试.md`。

- 2026-11-14：列表字面量改由 Syntax 的 `ListLiteralExpression` 表示，保留空列表/字典注解、解释器身份与既有重新求值行为，并适配集合内 await 扫描；字典、元组及解析器仍待拆分，见 `Reports/2026-11-14-12-19-列表字面量迁移测试.md`。

- 2026-11-14：数组字面量改由 Syntax 的 `ArrayLiteralExpression` 表示；Visitor 转换为既有运行时数组，解释器保留同节点身份和重复求值行为。其他集合与解析器仍待拆分，见 `Reports/2026-11-14-12-15-数组字面量迁移测试.md`。

- 2026-11-14：`Operation` 迁入 Syntax；运行时类型检查、编译期类型辅助方法与缓存迁入 Runtime，生成器的 Runtime 节点数降至 80。集合字面量、其余 AST 节点及解析器仍待拆分；测试见 `Reports/2026-11-14-12-12-运算节点迁移测试.md`。

- 2026-11-14：`FunctionCallExpression` 迁入 Syntax，动态委托执行逻辑移入 Runtime；该节点批次的测试与基线比较见 `Reports/2026-11-14-12-08-函数调用节点迁移测试.md`。第二阶段整体仍未完成。

- 2026-11-14：`AwaitExpression` 与 `ThrowStatement` 迁入 Syntax；await 的 IL 辅助代码改为 Runtime 扩展类。语法、LSP、异步及异常筛选结果见 `Reports/2026-11-14-07-16-await与throw程序集迁移测试.md`；其余节点和解析器仍待拆分。

- 2026-11-14：`LangId`、`ClassMemberId`、`ThisExpression`、`SuperExpression`、`TernaryExpression` 与 `NamedArgument` 迁入 Syntax；`LangId` 的编译器查找辅助方法已脱离 partial。Runtime Visitor 生成节点减至 84，剩余节点及解析器尚待迁移，见 `Reports/2026-11-14-07-11-表达式程序集迁移测试.md`。

- 2026-11-14：`LangExpression`、`OldStatement`、`IOldLangTree`、标量表达式与基础 `IVisitor` 已迁至 Syntax；Runtime 节点暂经 `IRuntimeVisitor` 分派。Syntax-only 宿主、语法/LSP 与解释器/VM 回归通过，完整 AST 与解析器仍未迁移，详见 `Reports/2026-11-14-07-05-语法Visitor程序集测试.md`。

- 2026-11-14：标量源码字面量的解析结果改为 `ScalarLiteralExpression`，四个 Visitor 处理该节点；解释器保留同节点的值身份，VM 默认参数和字段常量支持该节点。此节点仍位于 Runtime，解析器与其他 AST 节点尚未拆分，见 `Reports/2026-11-14-06-56-标量语法迁移测试.md`。

- 2026-11-14：将模式匹配、访问修饰符及 extern 声明的五个纯语法枚举迁入 Syntax，保留命名空间与枚举值；程序集归属门禁通过。完整 AST 和解析器仍待拆分，见 `Reports/2026-11-14-06-54-阶段二语法模型测试.md`。

- 2026-11-14：保存固定字节码样本并新增读取执行回归，编译、序列化及读取执行均通过；
  样本来源和哈希见 `Reports/2026-11-14-07-10-字节码固定样本.md`。公开 API 的完整盘点仍未完成。

- 2026-11-14：将不依赖后端的泛型参数和约束模型迁入 Syntax；保持命名空间，记录程序集级 API 迁移。
  完整 AST 与解析器拆分仍未完成。

- 2026-11-14（阶段 2，首批）：固定程序集基线并将 31 个阶段 1 遗留
  `Execution/Nodes/*.Split.cs` 归档至 `Execution/LegacyNodeExtensions`，新增项目引用图门禁。
  新建只引用 Core 的 `Old8Lang.Syntax`：标量 `LiteralSyntax`/`LiteralKind`/`ILiteralSyntax`
  已独立，`PrimaryParser` 经唯一 Runtime 转换工厂恢复既有标量运行时值；Syntax、Runtime 与
  语法/三模式代表性回归通过。集合字面量与完整 AST/解析器程序集迁移尚未完成，阶段 2 不标记完成。

- 2026-11-14：阶段 0 基线已采集（构建 0 错误；测试按命名空间分组结果记录在
  `Reports/2026-11-14-00-20-第一阶段节点迁移进展.md`）。Compiler 组存在既有挂起/中止用例，尚未完全定位。
- 2026-11-14：阶段 1 门禁已通过：类型推断、解释执行、IL 生成实现迁入 Visitor；删除 212 个节点旧执行入口
  与基类兼容入口；语句与表达式结构节点的后端辅助方法移入 `Old8Lang.Runtime/Execution`；
  新增 `SyntaxBackendBoundaryTests` 两条边界测试并全部通过；Interpreter/VM/LSP 与 Compiler 各子目录
  测试结果与基线一致。**未完成**：语法字面量与运行时值分离、Visitor 生成器完整重新生成、
  文档同步更新。详见 `Reports/2026-11-14-00-45-第一阶段完成测试.md`。
- 2026-11-14（续）：Visitor 生成器修复为可重复运行（排除 Visitor 自身、partial 接口 + 手工扩展），
  新增字面量/运行时值边界盘点文档，更新 ARCHITECTURE.md §4.2/§4.5；
  最终验证：架构门禁测试 2 项通过、Runtime 0 警告、Interpreter/VM/LSP 全通过、
  Compiler 各子目录与 other 组失败数与基线一致。
