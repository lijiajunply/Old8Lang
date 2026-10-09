# Old8Lang 项目规则

本文件是项目对代理（AI）的**硬性约定**。细化的说明、命令与示例在 `Docs/` 下，凡标注「详见」的条目请务必先读对应文档再动手。

## 一、三种运行模式

Old8Lang 有三种运行模式，新特性必须三种都支持：

- **解释模式**：逐条解释执行（走 `Run` 方法），无需编译。
- **IL 模式**：先编译成 IL 中间代码再执行（语句走 `GenerateIl`，表达式走 `LoadIlValue`）。
- **虚拟机模式**：由 Old8Lang 虚拟机和字节码运行。

各模式的对比与适用场景详见 `Docs/ARCHITECTURE.md` 与 `Docs/CLI_GUIDE.md`。

## 二、测试用 Old8Lang 代码文件

1. 文件扩展名必须是 `.old8`。
2. 必须符合 Old8Lang 语法规范，以 `Docs/Old8Lang.ebnf` 为准。
3. 注释用 `//`，不是 `#`。
4. 用 `PrintLine` 打印结果，方便查看。
5. 期望失败的用例，在**文件最后一行**写 `error`。
6. 按测试类型放到对应目录：
   - IL 模式测试 → `TestFiles/CompilerTests`
   - 解释模式测试 → `TestFiles/InterpreterTests`
   - 语法测试 → `TestFiles/SyntaxTests`
   - 虚拟机模式测试 → `TestFiles/VirtualMachine`
7. 发现不再使用的测试用 `.old8` 文件，及时删除。
8. 测试中发现与本次测试内容无关的其他错误，记录下来留到 `Todo.md`，不要顺手改。

测试时可用的运行命令、整套测试脚本与报告规范详见 `Docs/TESTING_GUIDE.md`。

## 三、新语法添加规范

按顺序执行，前一步未通过不得进入下一步：

> 前置：新增 AST 节点后必须重新生成 `IVisitor` 接口（在 `Old8Lang.CodeGen` 下跑 `dotnet run`），否则接口里没有对应方法。

1. 完成语法规则的添加和解析之后，先做**语法测试**，确保新语法能被正确解析。
2. 做**解释模式测试**，确保新语法在解释模式下正常运行。
3. 做**IL 模式测试**，确保新语法在 IL 模式下正常运行。
4. 做**虚拟机模式测试**，确保新语法在虚拟机模式下正常运行。
5. 更新 `Old8Lang.ebnf` 和 `Old8Lang_Grammar.md` 中的语法规则。
6. 在 `Old8Lang.Tests` 项目中添加新语法的单元测试，覆盖语法、解释模式、IL 模式、虚拟机模式、边界测试、异常测试等。

完整流程（含 AST 节点与错误处理规范）详见 `Docs/DEVELOPMENT_WORKFLOW.md`。

## 四、修复 IL 模式的代码生成问题

用 ILSpy 等工具查看编译后的 IL 代码，对照生成的指令定位问题所在。

## 五、任何测试结束之后

必须生成测试报告，包含测试用代码文件的运行结果。

- 位置：`Reports` 目录
- 格式：Markdown
- 文件名：`日期-小时-分钟-测试类型.md`

报告规范详见 `Docs/TESTING_GUIDE.md`。
