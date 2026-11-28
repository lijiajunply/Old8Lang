# 测试记录

## 返回类型（2026-11-23）

- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~ReturnConversionRegressionTests|FullyQualifiedName~Compiler.Functions.FunctionDeclarationTests|FullyQualifiedName~Compiler.Basic.ErrorHandlingTests' --verbosity quiet`：47 通过、2 跳过、0 失败。
- 覆盖 try/finally 中 int 返回到 double、字符串返回类型不匹配的语言错误；原数值用例在修复前也通过，不能单独证明旧 IL 在当前运行时会失败。
- `uint`、`ulong`、`ushort`、`sbyte`、`decimal`、`char` 和动态返回值仍待复核。

## VM 原生集合（2026-11-23）

- 修复前 `VMNativeSequenceRegressionTests` 在数组 `Take` 抛 `ArgumentException`；修复后覆盖数组和列表混合的取值、集合运算、去重、反转、聚合及空集合。
- 数组六种专用排序对不同长度字符串按序号比较，回归覆盖 QuickSort、HeapSort、SelectionSort、InsertionSort、MergeSort、BubbleSort。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~VirtualMachine.Collections|FullyQualifiedName~Compiler.Collections' --verbosity quiet`：229 通过、3 跳过、0 失败。
- 尚未完成全部 `LangList*` 方法和边界审计，不能据此关闭该待办。

## VM 原生函数命名参数（2026-11-23）

- 修复前，逆序和混合的原生命名参数调用、未知/重复/缺失参数的四项错误校验全部失败。
- 修复后按注册表的 `ParameterNames` 和实参类型绑定重载，覆盖 `ChannelSend` 逆序与位置/命名混合调用，以及 `SemaphoreCreate` 的非法绑定；资源在测试结束时释放。
- 字节码操作数增加嵌套字符串数组编码，原生函数与具名用户函数的命名实参均有保存、加载及执行回归。静态类方法命名参数不在本项范围。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~VMNamedArgumentsTests|FullyQualifiedName~Bytecode' --verbosity quiet`：57 通过、0 跳过、0 失败。

## 元组赋值的集合右值（2026-11-23）

- 修复前 IL 对数组右值在编译期报“右侧必须是元组”；VM 数组/列表右值复测通过。新增解释器、IL、VM 的数组与列表回归。
- IL 对动态右值及已有局部变量、函数参数、全局槽位有定向覆盖，验证右值只求值一次，以及非集合和元素不足的运行期错误。CLR 元组路径也使用同一赋值目标分派。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~Compiler.Collections|FullyQualifiedName~Interpreter.Collections.TupleDestructuring|FullyQualifiedName~VirtualMachine.Collections|FullyQualifiedName~TopLevelGlobalTests' --verbosity quiet`：252 通过、3 跳过、0 失败。
- 随后调整值类型赋给引用目标的装箱判断，重跑 `--filter 'FullyQualifiedName~TupleDestructuringRegressionTests|FullyQualifiedName~TopLevelGlobalTests'`：26 通过、0 失败。
- 嵌套模式和异步作用域尚未完成，本待办仍保持未完成。

## 嵌套元组解构与异步作用域（2026-11-23）

- 按语法 → 解释器 → IL → VM 顺序推进：解析器定向 1 项通过；解释器 4 项通过；IL 12 项通过；VM 5 项通过。原语法解析因内层元组被误判为缺少箭头的 lambda 而失败。
- 异步回归使用 `Task.Delay(10)` 触发挂起：修复前局部解构变量恢复后为空，修复后状态机字段保留结果；另测参数和全局槽位。
- 补测两侧嵌套、混合数组/列表、额外元素、无效嵌套值，以及 VM 字节码保存加载和 null。VM 记录原始元组直接元素以区分嵌套元组和多元素元组。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~Parser.Expression.PrimaryParserTests|FullyQualifiedName~TupleDestructuringRegressionTests|FullyQualifiedName~VirtualMachine.Collections|FullyQualifiedName~Compiler.Async' --verbosity quiet`：238 通过、3 跳过、0 失败。
- 增加无效嵌套值测试后重跑 `--filter 'FullyQualifiedName~TupleDestructuringRegressionTests'`：32 通过、0 失败。

## 返回值转换补充（2026-11-23）

- 纠正本报告前一节的错误判断：解释器的 `ValidateAndConvertReturnType` 允许将整数、布尔值和集合转换为 `string`，此前把 `int -> string` 列为不兼容并拒绝是错误的，现已撤销并增加正确回归。
- 修复前，`return 42` 到 `string` 抛 `TYPE_ERROR`，动态集合元素 `3.9` 返回到 `int` 抛 CLR `InvalidCastException`；现在通过语言返回值规则转换，并在 CLR 值类型槽位上归一化数值。覆盖字符串解析、bool、集合显示及动态非法值的 `TYPE_ERROR`。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~Compiler.Functions|FullyQualifiedName~Compiler.Basic.ErrorHandlingTests' --verbosity quiet`：204 通过、14 跳过、0 失败。
- 其他 CLR 数值类型及可空返回值仍待复核，不关闭该待办。

## IL LINQ 连接行（2026-11-23）

- 修复前重现：`join ... where y > 2` 得到 `[2, 3, 4]`，分组连接得到 `[[3], [3], [3]]`。修复后范围变量和 let 值按行保存并恢复，两个历史复现通过。
- 增加连续连接、重复匹配、空分组、连接后 let、内层值参与投影，以及连接后按内层键排序再过滤的回归。非流式排序现计算各排序表达式并稳定排序；范围变量乘法通过语言值运算。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~Compiler.Linq|FullyQualifiedName~Compiler.Expressions' --verbosity quiet`：300 通过、1 跳过、0 失败。
- 分组终止、查询延续、关联内层数据源等其他查询路径尚未逐项复核，待办保持未完成。

## LINQ 分组、延续与作用域（2026-11-24）

- 解释器的 `group ... by` 原先返回未分组的输入，现按键聚合并产生包含 `Key`、`Values` 的分组；IL 的分组表达式、动态索引及集合显示也已修正。基础分组、计算键/元素和 null 键在三模式执行通过。
- `select/group ... into` 的范围变量现逐元素绑定；解释器的延续 `let` 原先复用最后一行，VM 原先忽略延续，均已修复。覆盖过滤、排序、let、分组键投影和同名外层变量。
- 增加查询作用域隔离；`join y in y` 仍可读取外层数据源。关联内层源按当前外层行求值，原先 IL 生成无效程序；三模式回归通过。
- 定向测试 `--filter 'FullyQualifiedName~Compiler.Linq.LinqJoinWhereTests'`：21 通过、0 失败。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --filter 'FullyQualifiedName~Compiler.Linq|FullyQualifiedName~Interpreter.Linq|FullyQualifiedName~VirtualMachine.Linq|FullyQualifiedName~Compiler.Expressions|FullyQualifiedName~Compiler.Collections' --verbosity quiet`：590 通过、4 跳过、0 失败。
- 尚未完成复杂查询组合与 VM 排序键语义的全面核对；不关闭 LINQ 待办。
- 查询作用域变更后执行 `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --filter 'FullyQualifiedName~Old8Lang.Tests.Architecture' --verbosity quiet`：72 通过、1 失败；失败为已有待办 `ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections`（现有表达式内 await 限制），单项复跑同样失败。
- 再补充 `let` 与外层同名变量的隔离：解释器修复前把外层值由 99 改为 6，修复后三模式通过；相关套件重跑 591 通过、4 跳过。再次运行 Architecture 为 72 通过、1 失败，仍是上述既有 await 用例。

## 2026-11-28 IL 模式与跨模式缺陷清零验证

针对当前工作区全部改动（含本轮新增的 LINQ 复杂组合与 VM 排序键语义、解释器并发作用域克隆、`TypeChecker` `async_func -> function` 兼容、VM match 多元组解构、三模式函数值字段调用、解释器 `private static` 静态方法内访问、三模式集合注解递归元素校验、以及 `VisitSuperExpression` 基类类型推断修复）执行定向与整组复测：

- 既有工作区新增/修改回归（`AsyncLambdaAndDecoratorTests`、`StandardLibraryCrossModeImportTests`、`MethodChainingRegressionTests`、`NativeCollectionMethodBridgeTests`、`GenericListExtensionRegressionTests`、`ClosureTests`、`ReturnConversionRegressionTests`、`VMNativeSequenceRegressionTests`、`VMDecoratorTests`）：**168 通过、0 失败**。
- LINQ 全量三模式（`Compiler.Linq | Interpreter.Linq | VirtualMachine.Linq`，含新增多键计算排序、混合数值/序号字符串、`orderby + group by`、多层 `join + let + orderby + group + into` 及 VM 字节码往返）：**165 通过、0 失败**。
- 解释器异步全组（`Interpreter.Async`，含新增 `VariateManager_ConcurrentCloneAndScopeMutation_DoesNotThrow` 并发克隆压测与 `Await_WithVariableCaptures_CapturesCorrectlyInAsync`）：**144 通过、0 失败、1 跳过**。
- 跨模式缺陷回归 + 程序集边界（`CrossModeDefectsRegressionTests` + 四个 Architecture 边界测试类）：**31 通过、0 失败**（其中边界测试 27/27 全通过）。
- `Compiler.Exceptions | Compiler.Classes`（含 `ErrorPropagatesThroughInheritance_CompilesAndExecutesCorrectly`）：**181 通过、0 失败**。
- `Compiler.Async`：**0 失败**（与 `Architecture` 同跑 210 通过、1 失败、3 跳过，唯一失败仍为既有 `ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections`）。
- `VirtualMachine` 全组：**1,164 通过、0 失败、0 跳过**。
- `Compiler` 11 个主组（`Collections | Types | Expressions | Functions | Statements | Basic | Reflection | EdgeCases | Integration | Linq | Modules`）：**1,639 通过、1 失败、23 跳过**；唯一失败为 `Compiler.Modules.AdvancedImport.ConditionalImportTests.Import_ConditionalWithComplexExpression_ShouldEvaluateExpression`（与 2026-11-23 已修的解释器同名用例相同，仍断言旧的字符串长度比较分支）。
