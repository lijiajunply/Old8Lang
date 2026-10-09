# Old8Lang 开发流程指南

本文说明在 Old8Lang 上做开发时的流程与约定：新增功能/语法怎么推进、AST 节点和错误处理要守哪些规范、出了问题怎么查。
架构原理本身不在这里，见 [ARCHITECTURE.md](./ARCHITECTURE.md)。

相关文档：

- 测试怎么跑、测试文件怎么写：[TESTING_GUIDE.md](./TESTING_GUIDE.md)
- 三种执行模式的对比与选择：[ARCHITECTURE.md §1.3](./ARCHITECTURE.md#13-执行模式-execution-modes)、[CLI_GUIDE.md](./CLI_GUIDE.md)
- 代码规范与贡献流程：[CONTRIBUTING.md](./CONTRIBUTING.md)

---

## 1. 新功能/新语法添加流程

Old8Lang 一个特性要同时落进三种执行模式，所以流程必须按顺序走完，**前一步没过不要进下一步**：

1. **定义 AST 节点**
   在 `Old8Lang/AST/Expression/` 或 `Old8Lang/AST/Statement/` 新建节点类并实现 `Accept()`，规范见第 2 节。

2. **重新生成 Visitor 接口**
   `IVisitor<TResult>` 是**自动生成**的（`Old8Lang/AST/Visitor/Generated/IVisitor.generated.cs`）。新增节点后必须重新生成，
   否则接口里没有对应的方法：

   ```bash
   cd Old8Lang.CodeGen
   dotnet run -- --preview      # 先预览要生成的内容
   dotnet run                   # 确认后正式生成
   ```

   同目录下还有 `make-partial`（把 AST 节点类标记为 `partial`）和 `generate-stubs`（生成四个 Visitor 的实现骨架）两个命令。

3. **实现解析**
   在 `Old8Lang/LangParser/Parsers/` 里加入对应语法结构的解析逻辑，然后先跑**语法测试**，确认新语法能被正确解析。

4. **解释模式测试**
   确认新语法在解释模式下运行结果正确。

5. **IL 模式测试**
   确认新语法在 IL 模式下编译并运行正确。IL 模式要求完整类型注解，注意补齐。

6. **虚拟机模式测试**
   确认新语法在 VM 模式下运行正确。

7. **更新语法文档**
   同步更新 `Docs/Old8Lang.ebnf` 与 `Docs/Old8Lang_Grammar.md`。

8. **补单元测试**
   在 `Old8Lang.Tests` 中添加新语法的单元测试，覆盖：语法测试、解释模式、IL 模式、虚拟机模式、边界情况、异常情况。

> 第 3–6 步用到的命令、测试文件放哪个目录、`error` 标记约定，见 [TESTING_GUIDE.md](./TESTING_GUIDE.md)。
> 每一步测试完成后都要生成测试报告，规则同上。

---

## 2. AST 节点规范

新建 AST 节点时逐条对照（基类见 `Old8Lang/AST/LangExpression.cs`、`Old8Lang/AST/OldStatement.cs`）：

- **继承**：表达式继承 `LangExpression`，语句继承 `OldStatement`；两者都实现 `IOldLangTree`，带 `Position`（类型 `SourcePosition`）。
- **构造函数**：接受 `SourcePosition position`（基类默认参数为 `default`），便于报错定位。
- **实现 `Accept`**：`Accept<TResult>(IVisitor<TResult> visitor)` 是 Visitor 分发入口，必须由节点自己实现。
  基类已经给出其余方法的默认实现——`Run()` / `LoadIlValue()` 会各自构造 `InterpreterVisitor` / `CompilerVisitor` 再转发给 `Accept`，
  所以通常不必在节点里手写解释与 IL 逻辑：

  | 方法 | 所属基类 | 签名 |
  |------|----------|------|
  | `Run` | `LangExpression` | `virtual LangValueType Run(VariateManager manager)` |
  | `LoadIlValue` | `LangExpression` | `virtual void LoadIlValue(ILGenerator ilGenerator, LocalManager local)` |
  | `Run` | `OldStatement` | `abstract void Run(VariateManager manager)` |
  | `GenerateIl` | `OldStatement` | `abstract void GenerateIl(ILGenerator ilGenerator, LocalManager local)` |
  | `Accept` | 两者 | `abstract TResult Accept<TResult>(IVisitor<TResult> visitor)` |

  注意表达式与语句的 IL 入口不同：表达式是 `LoadIlValue`（把值压栈），语句是 `GenerateIl`。
- **重写 `ToString()`**，用于调试时打印。
- 新增节点后记得重新生成 `IVisitor`（见第 1 节第 2 步）。

Visitor 四个实现（`InterpreterVisitor` / `CompilerVisitor` / `BytecodeVisitor` / `TypeInferenceVisitor`，均在 `Old8Lang/AST/Visitor/`）与接口的关系见 [ARCHITECTURE.md §4](./ARCHITECTURE.md#4-visitor-模式详解)。

---

## 3. 错误处理规范

- 使用 `Old8Lang.Error` 命名空间下的自定义异常（源码在 `Old8Lang/Error/`，按 `Syntax`、`Runtime`、`Semantic`、`IO`、`Concurrency` 等分目录），基类是 `Old8Exception`。不要直接抛 `Exception`。
- 异常必须携带位置信息，构造函数要能接受 `SourcePosition`，例如 `SyntaxError(SourcePosition position, string message)`。
- 优先选具体异常类型而非基类：语法错误用 `SyntaxError`，类型错误用 `TypeError` / `TypeInferenceError`，运行时错误用 `RuntimeError`，名称解析用 `NameError`，VM 不支持的特性用 `VmUnsupportedError`。

---

## 4. 解析器要点

- 词法：`LangTokenizer.TokenizeWithDirectives(code, preprocessorSymbols)` 返回 `(tokens, headerDirectives)`；
  Token 定义与词法实现都在 `Old8Lang/LangParser/LangToken.cs` / `LangTokenType.cs`。
- 语法：`new LangParser(tokens, headerDirectives, sourceCode, fileName)`，调用 `ParseProgram()` 得到 `BlockStatement`。
  两段衔接的实际样例见 `Old8Lang/Interpreter/LangInterpreter.cs`。
- 采用**递归下降**解析：主文件 `Old8Lang/LangParser/LangParser.cs`，公共基类在 `Old8Lang/LangParser/Core/ParserBase.cs`；
  每种语言结构在 `Old8Lang/LangParser/Parsers/` 下有独立解析器，且普遍以 `partial class` 拆成多个文件
  （`StatementParser.*`、`PrimaryParser.*`、`ExpressionParser`、`FunctionParser`、`ClassParser`、`ExtensionParser`、`LinqParser`）。
- 改语法时，`Docs/Old8Lang.ebnf` 要同步改（见第 1 节里更新语法文档那一步）。

---

## 5. 类型系统要点

- 解释模式：动态类型 + 运行时检查，类型注解可选。
- IL 模式：静态类型 + 编译时检查，**函数参数必须有类型注解或默认值**。
- 泛型支持 `list<T>`、`array<T>`、`dict<K,V>` 等。
- 类型推断引擎能从上下文推断出很多类型，不必处处写注解；IL 模式推断不了的地方再补。

---

## 6. 调试

### 6.1 Old8Lang 代码

用 `PrintLine` 在 `.old8` 脚本里打印中间结果。

### 6.2 运行器

```bash
# 打开调试输出
dotnet run --project Old8Lang.App -- -f test.old8 -d

# 指定日志级别
dotnet run --project Old8Lang.App -- -f test.old8 --log-level debug
```

调试输出包含：词法分析得到的 Token 流、AST 结构、IL 生成步骤（编译模式）、变量作用域变化、类型推断决策。

C# 侧则直接用 IDE 调试器。

---

## 7. 排查 IL 模式的代码生成问题

IL 模式的问题往往出在生成出来的 IL 上，而不是源代码逻辑上。用 **ILSpy** 等工具反编译查看编译产物，对照生成的指令定位问题。

---

## 8. 性能

- 用 `Old8Lang.Benchmarks` 项目做基准测试：`dotnet run --project Old8Lang.Benchmarks --configuration Release`。
- 关注内存占用与执行时间；大数据量操作尤其要留意。
- 优化建议与既有优化技术栈见 [PERFORMANCE_GUIDE.md](./PERFORMANCE_GUIDE.md)、[PERFORMANCE_OPTIMIZATION.md](./PERFORMANCE_OPTIMIZATION.md)。

---

## 9. 代码风格

通用的 C# 命名、代码风格与 XML 文档注释要求见 **[CONTRIBUTING.md § 代码规范](./CONTRIBUTING.md#代码规范)**，此处不重复。

Old8Lang 项目额外要求：

- 公共 API 必须有完整的 XML 文档注释，注释用中文，说明参数、返回值与可能抛出的异常。
- 重要逻辑补行内注释。
- 新增 AST 节点遵循第 2 节的规范；新增错误类型遵循第 3 节的规范。
