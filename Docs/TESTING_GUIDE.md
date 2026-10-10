# Old8Lang 测试指南

本文是 Old8Lang 测试相关约定的**唯一权威说明**：测试怎么跑、测试代码文件怎么写、测试结果往哪放，都以此为准。
代理（AI）在动手写测试前请先读本文件，不要凭记忆臆测。

相关文档：

- 语法本身的定义：[Old8Lang.ebnf](./Old8Lang.ebnf)、[Old8Lang_Grammar.md](./Old8Lang_Grammar.md)
- 开发流程（含新语法测试顺序的上下文）：[DEVELOPMENT_WORKFLOW.md](./DEVELOPMENT_WORKFLOW.md)
- 代码规范与提交规范：[DEVELOPMENT_WORKFLOW.md §9-§10](./DEVELOPMENT_WORKFLOW.md#9-代码规范)

---

## 1. 单元测试（xUnit）

测试项目是 `Old8Lang.Tests`，使用 xUnit。

```bash
# 跑全部单元测试（默认档，已自动排除性能用例，见 1.1）
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 按测试全名筛选
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~TestName"
```

### 1.1 性能/并发用例分档

断言墙钟耗时、GC/内存占用，或用 `Sleep` 同步线程的用例，与其他用例并行时会因 CPU 资源争抢而抖动。
这类用例必须显式标记：

```csharp
[Trait("Category", "Performance")]
```

**例外：关键场景的「粗阈值」用例**用 `Category=PerformanceSmoke`，它们同样断言耗时与分配量，
但阈值放到实测值的 5~10 倍，只拦截灾难性退化（掉回慢路径、重复分配等），
因此可以留在默认档里先于 BenchmarkDotNet 挡住明显退化。示例见
`Old8Lang.Tests/VirtualMachine/Performance/VMPerformanceSmokeTests.cs`。

它们被拆成三档，由 `Old8Lang.Tests/` 下的两个 runsettings 控制：

| 档位 | runsettings | 过滤条件 | 如何启用 |
|------|-------------|----------|----------|
| 默认档 | `default.runsettings` | `(Category!=Performance)\|(Category=PerformanceSmoke)` | 无需额外参数，已通过 csproj 的 `RunSettingsFilePath` 自动生效 |
| 性能档 | `performance.runsettings` | `(Category=Performance)\|(Category=PerformanceSmoke)` | 必须显式传 `--settings` |

```bash
# 默认档：普通 dotnet test / --filter 即为这一档（含 PerformanceSmoke）
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 性能档：跑 Category=Performance（含 PerformanceSmoke）的用例
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --settings Old8Lang.Tests/performance.runsettings

# 性能档里再挑单个用例
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --settings Old8Lang.Tests/performance.runsettings --filter "FullyQualifiedName~TestName"
```

> **容易踩的坑**：`--filter` 与 runsettings 里的 `TestCaseFilter` 是 **AND** 关系。
> 所以在默认档下用 `--filter` 指定一个性能用例，结果是**匹配不到任何测试**，而不是报错。
> 想跑性能用例，必须带上 `--settings Old8Lang.Tests/performance.runsettings`。

---

## 2. 测试用 Old8Lang 代码文件（`.old8`）

### 2.1 文件规范

- 扩展名必须是 **`.old8`**。
- 必须符合 Old8Lang 语法，以 [Old8Lang.ebnf](./Old8Lang.ebnf) 为准。
- 注释用 **`//`**，不是 `#`。
- 用 `PrintLine` 打印结果，方便人工核对。
- **期望失败的用例**：在文件**最后一行**写 `error`（大小写不敏感）。
  测试脚本会用 `tail -n 1 | grep -i error` 判定该用例「应当失败」，从而把「非零退出码」视为通过。

### 2.2 目录归属

按测试的模式放到对应目录：

| 测试类型 | 目录 | 运行方式 |
|----------|------|----------|
| 语法测试 | `TestFiles/SyntaxTests/` | `-s` |
| 解释模式测试 | `TestFiles/InterpreterTests/` | `-f` |
| IL 模式测试 | `TestFiles/CompilerTests/` | `-il` |
| 虚拟机模式测试 | `TestFiles/VirtualMachine/` | `-vm` |

`TestFiles/` 下还有若干专项用例目录：`VMTests/`、`BytecodeTests/`、`DebuggerTests/`、`ExternTests/`、`ProfilerTests/`、`Verification/`。
每个目录都有一份自己的 `README.md`，写明用例数量、入口文件和运行方式，动手前先看一眼。

---

## 3. 运行测试

### 3.1 单个测试文件

```bash
# 语法测试（只解析，不执行）
dotnet run --project Old8Lang.App -- -s <path-to-test-file.old8>

# 解释模式测试
dotnet run --project Old8Lang.App -- -f <path-to-test-file.old8>

# IL 模式测试（-c 是 -il 的等价别名）
dotnet run --project Old8Lang.App -- -il <path-to-test-file.old8>

# 虚拟机模式测试
dotnet run --project Old8Lang.App -- -vm <path-to-test-file.old8>
```

加 `-d` 可打开调试输出（Token 流、AST、IL 生成步骤、作用域变化等），也可以配合 `--log-level debug|info|warning|error`。

### 3.2 整套测试脚本

`TestFiles/` 下有一组驱动脚本，**需要先 `cd TestFiles`** 再运行：

```bash
cd TestFiles

./run_syntax_tests.sh                    # 语法测试
./run_interpreter_tests.sh               # 解释模式测试
./run_compiler_tests.sh                  # IL 模式测试
./run_vm_tests.sh                        # 虚拟机模式测试（含 VirtualMachine/、VMTests/、BytecodeTests/）
./run_comprehensive_compiler_tests.sh    # IL 模式综合套件
```

每个脚本会逐个跑目录下的 `.old8` 文件、按 2.1 的 `error` 约定判定通过与否，最后汇总 passed/failed。CI 中会调用其中的语法、解释、IL 与 VM 套件。

---

## 4. 新语法的测试顺序

新增语法时，测试必须按下面的顺序逐级推进，前一级没过就不要进下一级：

**语法测试 → 解释模式 → IL 模式 → 虚拟机模式**

完整的新语法开发流程（含文档更新与单元测试）见 [DEVELOPMENT_WORKFLOW.md](./DEVELOPMENT_WORKFLOW.md#1-新功能新语法添加流程)。

---

## 5. 测试报告

**任何一次测试结束之后都要生成测试报告**（不只是新语法测试）。

- 目录：仓库根目录的 `Reports/`
- 格式：Markdown
- 文件名建议：`日期-小时-分钟-测试类型.md`，例如 `Reports/2026-01-18-23-22-IL 模式测试补充报告.md`
- 内容：包含测试用代码文件的运行结果

另外：

- 测试中发现**与本次测试内容无关**的其他错误，不要顺手改，记录下来留到 `Todo.md` 里。
- 测试用 `.old8` 文件如果已经不再使用，**及时删除**，不要留在仓库里。

---

## 6. 覆盖率

```bash
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --collect:"XPlat Code Coverage"
```

新功能建议至少 80% 覆盖率；Bug 修复必须补回归测试。
