# Old8Lang 开发工具

本文档汇总 Old8Lang 的开发工具，包括 Language Server、VSCode 扩展、调试器和性能分析工具。

## 目录

- [Language Server 和 VSCode 扩展](#language-server-和-vscode-扩展)
  - [快速开始](#快速开始)
  - [架构说明](#架构说明)
  - [开发指南](#开发指南)
  - [使用说明](#使用说明)
- [调试器](#调试器)
  - [主要功能](#主要功能)
  - [命令参考](#命令参考)
  - [使用示例](#使用示例)
- [性能分析工具](#性能分析工具)
  - [主要功能](#主要功能-1)
  - [使用指南](#使用指南-1)
  - [性能指标说明](#性能指标说明)

---

## Language Server 和 VSCode 扩展

### 快速开始

#### 步骤 1: 构建 Language Server

```bash
# 进入 Language Server 目录
cd Old8Lang.LanguageServer

# 构建项目
dotnet build -c Release

# 发布为独立可执行文件（根据你的操作系统选择）
# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -o ../vscode-old8lang/server

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -o ../vscode-old8lang/server

# Windows
dotnet publish -c Release -r win-x64 --self-contained -o ../vscode-old8lang/server

# Linux
dotnet publish -c Release -r linux-x64 --self-contained -o ../vscode-old8lang/server
```

#### 步骤 2: 构建 VSCode 扩展

```bash
# 进入扩展目录
cd vscode-old8lang

# 安装依赖
npm install

# 编译 TypeScript
npm run compile
```

#### 步骤 3: 安装扩展

##### 方法 A: 调试模式（开发）

1. 在 VSCode 中打开 `vscode-old8lang` 目录
2. 按 `F5` 启动扩展开发宿主
3. 在新窗口中打开包含 `.old8` 文件的项目

##### 方法 B: 安装 VSIX（用户）

```bash
# 安装打包工具
npm install -g @vscode/vsce

# 打包扩展
vsce package

# 安装扩展
code --install-extension old8lang-0.1.0.vsix
```

#### 验证安装

创建一个 `test.old8` 文件：

```old8
// 测试语法高亮
func greet(name:string) -> void {
    message <- $"Hello, {name}!"
    PrintLine(message)
}

// 测试类定义
class Person {
    public name:string
    public age:int

    public func sayHello() -> void {
        PrintLine($"I'm {name}, {age} years old")
    }
}

// 主程序
main() -> {
    greet("Old8Lang")

    person <- new Person()
    person.name <- "张三"
    person.age <- 25
    person.sayHello()
}
```

验证功能：
1. **语法高亮**：关键字应该有不同颜色
2. **错误诊断**：尝试写错误的语法，应该看到红色波浪线
3. **自动补全**：输入 `fu` 应该提示 `func` 等关键字
4. **括号匹配**：输入 `{` 应该自动补全 `}`

### 架构说明

#### Language Server (Old8Lang.LanguageServer)

Language Server 是一个独立的进程，负责提供语言智能功能：

- **文档管理 (DocumentManager)**: 管理打开的文档，维护解析结果和符号表
- **文档同步 (TextDocumentSyncHandler)**: 处理文档打开、修改、保存、关闭事件
- **自动补全 (CompletionHandler)**: 提供关键字和符号的智能补全
- **跳转定义 (DefinitionHandler)**: 支持跳转到符号定义
- **查找引用 (ReferencesHandler)**: 查找符号的所有引用
- **悬停提示 (HoverHandler)**: 显示符号的详细信息

##### 技术栈

- .NET 10.0
- OmniSharp.Extensions.LanguageServer (LSP 实现库)
- Old8Lang 核心解析器

#### VSCode 扩展 (vscode-old8lang)

VSCode 扩展作为 LSP 客户端，与 Language Server 通信：

- **语法高亮**: 基于 TextMate 语法定义
- **语言配置**: 括号匹配、自动闭合、注释等
- **LSP 客户端**: 与 Language Server 通信

##### 技术栈

- TypeScript
- VSCode Extension API
- vscode-languageclient

### 开发指南

#### 开发 Language Server

1. 确保已安装 .NET 10.0 SDK

2. 构建项目：
```bash
cd Old8Lang.LanguageServer
dotnet build -c Release
```

3. 运行测试（可选）：
```bash
dotnet test
```

4. 发布独立可执行文件：
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

#### 开发 VSCode 扩展

1. 安装依赖：
```bash
cd vscode-old8lang
npm install
```

2. 编译 TypeScript：
```bash
npm run compile
```

3. 调试扩展：
   - 在 VSCode 中打开 `vscode-old8lang` 目录
   - 按 `F5` 启动调试
   - 这会打开一个新的 VSCode 窗口，其中已加载扩展

4. 监视模式（自动编译）：
```bash
npm run watch
```

#### 打包和发布

##### 打包 Language Server

将编译好的 Language Server 可执行文件复制到扩展的 `server` 目录：

```bash
mkdir -p vscode-old8lang/server
cp Old8Lang.LanguageServer/bin/Release/net10.0/Old8Lang.LanguageServer* vscode-old8lang/server/
```

##### 打包 VSCode 扩展

1. 安装打包工具：
```bash
npm install -g @vscode/vsce
```

2. 打包扩展：
```bash
cd vscode-old8lang
vsce package
```

这会生成 `.vsix` 文件，可以分发给用户。

3. 安装 VSIX：
```bash
code --install-extension old8lang-0.1.0.vsix
```

### 使用说明

#### 安装扩展

1. 从 VSIX 文件安装：
   - 打开 VSCode
   - 按 `Ctrl+Shift+P` (Windows/Linux) 或 `Cmd+Shift+P` (macOS)
   - 输入 "Extensions: Install from VSIX"
   - 选择 `.vsix` 文件

2. 或者直接在 VSCode 扩展市场搜索 "Old8Lang"（如果已发布）

#### 配置

在 VSCode 设置中可以配置：

```json
{
  "old8lang.languageServer.path": "/path/to/Old8Lang.LanguageServer",
  "old8lang.trace.server": "verbose"
}
```

#### 功能使用

1. **语法高亮**：打开 `.old8` 文件即可自动高亮

2. **智能补全**：
   - 输入时自动弹出补全列表
   - 或按 `Ctrl+Space` 手动触发

3. **错误诊断**：
   - 语法错误会在编辑器中以波浪线标记
   - 鼠标悬停查看错误详情

4. **跳转定义**：
   - 右键点击符号 → "Go to Definition"
   - 或按 `F12`

5. **查找引用**：
   - 右键点击符号 → "Find All References"
   - 或按 `Shift+F12`

6. **悬停提示**：
   - 鼠标悬停在符号上查看详细信息

#### 扩展功能

##### 当前已实现的功能

- ✅ 语法高亮
- ✅ 文档同步
- ✅ 实时错误诊断
- ✅ 智能补全（关键字）
- ✅ LSP 基础框架

##### 待完善的功能

- ⏳ 符号表构建（变量、函数、类）
- ⏳ 跳转定义（需要符号表支持）
- ⏳ 查找引用（需要符号表支持）
- ⏳ 悬停提示（需要符号表支持）
- ⏳ 代码重构（重命名）
- ⏳ 代码格式化
- ⏳ 代码片段（Snippets）

#### 贡献指南

##### 添加新的 LSP 功能

1. 在 `Old8Lang.LanguageServer/Handlers/` 创建新的 Handler
2. 实现对应的 LSP 接口（如 `ICodeActionHandler`）
3. 在 `Program.cs` 中注册 Handler
4. 更新文档

##### 改进语法高亮

编辑 `vscode-old8lang/syntaxes/old8lang.tmLanguage.json`

##### 添加代码片段

在 `vscode-old8lang/snippets/` 目录创建片段文件，并在 `package.json` 中注册。

#### 故障排查

##### Language Server 无法启动

1. 检查 Language Server 路径配置
2. 确保 Language Server 有执行权限
3. 查看 VSCode 输出面板的 "Old8Lang Language Server" 日志

##### 语法高亮不工作

1. 确认文件扩展名为 `.old8`
2. 尝试重新加载 VSCode 窗口
3. 检查语法定义文件是否正确

##### 补全不工作

1. 检查 Language Server 是否正常运行
2. 查看是否有语法错误阻止解析
3. 启用 trace 日志查看 LSP 通信

---

## 调试器

Old8Lang 调试器是一个功能强大的调试工具，支持断点、变量监视、单步执行和调用栈分析等调试功能。

### 主要功能

#### 1. 断点管理
- **行断点**: 在指定文件的指定行设置断点
- **函数断点**: 在函数入口处设置断点
- **条件断点**: 满足特定条件时才触发的断点

#### 2. 变量监视
- 实时监视变量值的变化
- 支持复杂表达式监视
- 自动更新变量状态

#### 3. 执行控制
- **继续执行**: 继续运行程序直到下一个断点
- **单步进入**: 逐行执行，遇到函数时进入函数内部
- **单步跳过**: 逐行执行，遇到函数时跳过函数调用
- **单步跳出**: 执行完当前函数并返回到调用者

#### 4. 调用栈分析
- 显示完整的函数调用链
- 查看每个栈帧的局部变量
- 跟踪程序的执行流程

### 命令参考

#### 启动调试

```bash
# 启动调试会话
old8lang debug-start <文件路径>

# 示例
old8lang debug-start TestFiles/DebuggerTests/test_breakpoints.old8
```

#### 断点管理

```bash
# 添加行断点
old8lang debug-bp add <文件路径> <行号> [条件]

# 添加函数断点
old8lang debug-bp func <函数名>

# 列出所有断点
old8lang debug-bp list

# 移除断点
old8lang debug-bp remove <断点ID>

# 清除所有断点
old8lang debug-bp clear

# 示例
old8lang debug-bp add test.old8 10
old8lang debug-bp add test.old8 15 "x > 5"
old8lang debug-bp func main
old8lang debug-bp list
old8lang debug-bp remove 1
```

#### 调试控制

```bash
# 继续执行
old8lang debug continue

# 单步执行
old8lang debug step        # 单步进入
old8lang debug stepinto    # 单步进入（同上）
old8lang debug stepover    # 单步跳过
old8lang debug stepout     # 单步跳出

# 暂停执行
old8lang debug pause

# 停止调试
old8lang debug stop

# 显示调用栈
old8lang debug stack

# 显示当前变量
old8lang debug vars
```

### 使用示例

#### 基础调试流程

1. **启动调试会话**:
   ```bash
   old8lang debug-start example.old8
   ```

2. **设置断点**:
   ```bash
   old8lang debug-bp add example.old8 10
   old8lang debug-bp func calculate
   ```

3. **开始执行**:
   程序会在断点处暂停

4. **检查状态**:
   ```bash
   old8lang debug stack
   old8lang debug vars
   ```

5. **单步执行**:
   ```bash
   old8lang debug step
   ```

6. **继续执行**:
   ```bash
   old8lang debug continue
   ```

#### 条件断点示例

设置只在特定条件下触发的断点：

```bash
old8lang debug-bp add loop.old8 15 "i == 5"
old8lang debug-bp add logic.old8 20 "flag == true"
```

#### 变量监视

虽然当前版本通过 `debug vars` 查看变量，未来的版本将支持：

```bash
# 添加变量监视（计划功能）
old8lang debug-watch add x
old8lang debug-watch add "result * 2"

# 列出监视变量
old8lang debug-watch list
```

### 测试文件

调试器包含以下测试文件：

- `TestFiles/DebuggerTests/test_breakpoints.old8` - 断点功能测试
- `TestFiles/DebuggerTests/test_variables.old8` - 变量监视测试
- `TestFiles/DebuggerTests/test_callstack.old8` - 调用栈测试

#### 运行测试

```bash
# 编译测试
dotnet build

# 运行调试器单元测试
dotnet test Old8Lang.Tests --filter "Debugger"

# 测试调试功能
old8lang debug-start TestFiles/DebuggerTests/test_breakpoints.old8
```

### 架构说明

调试器由以下核心组件组成：

1. **BreakpointManager**: 管理断点的设置、移除和命中检测
2. **VariableWatcher**: 监视变量值的变化
3. **Debugger**: 调试器核心引擎，协调各组件工作
4. **CallStack**: 管理函数调用栈
5. **DebuggableInterpreter**: 支持调试的解释器包装器

### 事件系统

调试器提供丰富的事件通知：

- **StateChanged**: 调试状态变化
- **BreakpointHit**: 断点命中
- **ErrorOccurred**: 运行时错误

这些事件可以用于构建图形化调试界面。

### 注意事项

1. 调试器目前主要支持解释模式
2. 条件断点只支持简单的变量检查
3. 编译模式的调试支持正在开发中
4. 调试会话会在程序结束或手动停止时自动清理

### 故障排除

#### 常见问题

**Q: 断点不命中？**
A: 确保文件路径正确，行号有效，断点已启用。

**Q: 变量显示不正确？**
A: 检查变量是否在当前作用域内，确保已执行到相关代码。

**Q: 调试器启动失败？**
A: 确保文件存在且有读取权限，检查语法是否正确。

#### 调试技巧

1. 在关键逻辑处设置断点
2. 使用条件断点避免不必要的暂停
3. 结合调用栈分析程序流程
4. 利用单步执行逐步验证逻辑
5. 注意作用域对变量可见性的影响

### 未来改进

计划中的功能：

- [ ] 图形化调试界面
- [ ] 更强大的条件表达式
- [ ] 编译模式调试支持
- [ ] 远程调试功能
- [ ] 性能分析工具
- [ ] 内存使用监控

---

## 性能分析工具

Old8Lang Profiler 是一个强大的性能分析工具，用于分析 Old8Lang 代码的执行性能，识别性能瓶颈，并提供优化建议。

### 主要功能

#### 1. 性能数据收集
- **执行时间跟踪**: 测量函数执行时间
- **内存使用监控**: 实时监控托管和非托管内存使用
- **垃圾回收统计**: 跟踪 GC 次数和频率
- **函数调用统计**: 记录函数调用次数和频率
- **自定义指标**: 支持添加自定义性能数据点

#### 2. 性能瓶颈检测
- **高执行时间检测**: 识别执行时间过长的函数
- **高内存使用检测**: 监控内存泄漏和过度分配
- **频繁垃圾回收检测**: 识别 GC 频繁的代码模式
- **函数调用过频检测**: 发现可能需要缓存的热点函数
- **执行时间不稳定检测**: 识别执行时间波动大的函数

#### 3. 性能分析报告
- **文本报告**: 详细的文本格式报告
- **JSON 报告**: 结构化数据，便于自动化处理
- **CSV 报告**: 适合 Excel 分析
- **Markdown 报告**: 适合文档集成

#### 4. 性能评级系统
- **A 级别**: 90-100分，优秀性能
- **B 级别**: 80-89分，良好性能
- **C 级别**: 70-79分，一般性能
- **D 级别**: 60-69分，较差性能
- **F 级别**: 0-59分，需要优化

### 使用指南

#### 命令行接口

```bash
# 启动性能分析
old8lang profile start <文件> [会话名称]

# 停止性能分析
old8lang profile stop

# 查看分析状态
old8lang profile status

# 清除当前会话
old8lang profile clear
```

#### 编程接口集成

```csharp
using Old8Lang.Profiler;

// 创建性能分析器
var profiler = new ProfilerManager();

// 启动分析
var sessionId = profiler.StartProfiling("MyAnalysis", "script.old8", "解释模式");

// 记录函数执行
profiler.RecordFunctionStart("myFunction", "script.old8", 10);
// ... 函数执行代码 ...
profiler.RecordFunctionEnd("myFunction", "script.old8", 15);

// 添加自定义数据
profiler.RecordCustomData("CustomMetric", 42.5, "ms");

// 停止分析并获取摘要
var summary = profiler.StopProfiling();
var report = new ReportGenerator().GenerateReport(summary, ReportFormat.Text);
Console.WriteLine(report);
```

### 性能指标说明

#### 1. 函数性能指标

| 指标 | 说明 | 单位 |
|------|------|------|
| 调用次数 | 函数被调用的总次数 | 次 |
| 总执行时间 | 函数累计执行时间 | 毫秒 |
| 平均执行时间 | 函数平均执行时间 | 毫秒 |
| 最小执行时间 | 最短执行时间 | 毫秒 |
| 最大执行时间 | 最长执行时间 | 毫秒 |
| 标准差 | 执行时间的标准差 | 毫秒 |
| 中位数 | 执行时间的中位数 | 毫秒 |

#### 2. 内存使用指标

| 指标 | 说明 | 单位 |
|------|------|------|
| 托管内存 | .NET 托管的堆内存 | MB |
| 非托管内存 | 非托管堆内存 | MB |
| 工作集 | 进程占用的内存 | MB |
| 专用内存 | 进程专用内存 | MB |
| GC 代数 0 | 第 0 代 GC 次数 | 次 |
| GC 代数 1 | 第 1 代 GC 次数 | 次 |
| GC 代数 2 | 第 2 代 GC 次数 | 次 |

#### 3. 瓶颈类型

| 类型 | 严重程度 | 触发条件 | 建议 |
|------|----------|----------|------|
| 高执行时间 | 中等 | 平均执行时间 > 100ms | 优化算法、减少循环 |
| 高内存使用 | 高 | 峰值内存 > 100MB | 使用对象池、及时释放 |
| 频繁垃圾回收 | 高 | GC 次数 > 10/分钟 | 减少临时对象分配 |
| 函数调用过频 | 中等 | 调用次数 > 10000 | 使用缓存、内联函数 |
| 执行时间不稳定 | 低 | 标准差/平均值 > 0.5 | 检查条件分支 |

### 性能优化建议

#### 1. 高执行时间优化

**问题**: 函数执行时间过长

**建议**:
- 检查算法复杂度，使用更高效的算法
- 减少不必要的循环和递归
- 使用缓存机制避免重复计算
- 考虑并行处理独立的计算任务

#### 2. 内存优化

**问题**: 内存使用过高或存在内存泄漏

**建议**:
- 使用对象池减少分配开销
- 及时释放不再使用的对象
- 避免频繁的大对象分配
- 监控内存增长趋势

#### 3. 垃圾回收优化

**问题**: GC 频繁，影响性能

**建议**:
- 减少临时对象创建
- 预分配和重用缓冲区
- 使用结构体替代小对象
- 避免 finalize 方法

#### 4. 函数调用优化

**问题**: 函数调用过于频繁

**建议**:
- 使用函数缓存
- 内联频繁调用的小函数
- 使用委托和事件优化
- 减少不必要的函数调用层次

### 集成到开发流程

#### 1. 开发阶段性能测试

```csharp
// 在开发过程中启用性能分析
var profiler = ProfilerService.GetProfiler();

try
{
    // 执行业务代码
    profiler.StartProfiling("dev-test");

    // ... 运行业务逻辑 ...

    var summary = profiler.StopProfiling();

    if (summary.OverallScore < 70)
    {
        Console.WriteLine("⚠️ 性能分数较低，建议优化");
        Console.WriteLine(summary.Recommendation);
    }
}
finally
{
    profiler.ClearSession();
}
```

#### 2. CI/CD 性能回归测试

```bash
# 在持续集成中运行性能测试
old8lang profile start performance-test.old8
old8lang run performance-test.old8
old8lang profile stop

# 保存性能报告
old8lang profile report --format json --output performance-report.json
```

#### 3. 生产环境监控

```csharp
// 生产环境中的性能监控
var profiler = ProfilerService.GetProfiler();
profiler.StartProfiling("prod-monitoring", "production.old8");

// 设置性能阈值告警
profiler.HighExecutionTimeThresholdMs = 50.0;  // 50ms
profiler.HighMemoryUsageThresholdMB = 200.0;  // 200MB

// 定期检查性能状态
Timer t = new Timer(_ =>
    var status = profiler.GetSessionStatus();
    var isProfiling = (bool)status["isProfiling"];
    if (isProfiling &&
        ((double)status["collector_memoryPeakMB"] ?? 0) > 500.0))
    {
        SendAlert("内存使用过高，需要关注");
    }
}, null, TimeSpan.FromMinutes(5));
t.Start();
```

### 高级功能

#### 1. 自定义性能指标

```csharp
// 记录自定义性能数据
profiler.RecordCustomData("DatabaseQuery", 125.5, "ms", new Dictionary<string, string>
{
    ["query_type"] = "SELECT",
    ["table"] = "users"
});
```

#### 2. 条件性能分析

```csharp
// 基于条件的性能分析
analyzer.HighExecutionTimeThresholdMs = 50.0;
analyzer.FrequentGCThresholdPerMinute = 15;

// 只有当条件满足时才记录
if (executionTime > analyzer.HighExecutionTimeThresholdMs)
{
    profiler.RecordCustomData("SlowQuery", executionTime, "ms");
}
```

#### 3. 性能数据导出

```csharp
// 导出为不同格式
await profiler.SaveReportAsync("performance-report.md", ReportFormat.Markdown);
await profiler.SaveReportAsync("performance-report.json", ReportFormat.Json);
await profiler.SaveReportAsync("performance-report.csv", ReportFormat.Csv);
```

### 故障排除

#### 常见问题

1. **无法启动性能分析**
   - 确保没有其他活跃的分析会话
   - 检查源文件是否存在且可读
   - 验证权限设置

2. **性能数据不准确**
   - 确保在真实的执行环境中测试
   - 避免在调试模式下测量性能
   - 多次运行取平均值

3. **内存监控异常**
   - 检查平台兼容性
   - 确保有足够的系统权限
   - 考虑降低监控频率

4. **报告生成失败**
   - 检查输出路径权限
   - 确保磁盘空间充足
   - 验证数据格式正确性

#### 调试模式

```csharp
// 启用详细日志
analyzer.DebugMode = true;
profiler.MemoryMonitoringEnabled = true;

// 获取详细的收集状态
var status = profiler.GetSessionStatus();
foreach (var kvp in status)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

### 最佳实践

1. **定期性能分析**: 在开发过程中定期运行性能分析
2. **基准测试**: 建立性能基准并定期回归测试
3. **监控趋势**: 跟踪性能指标的变化趋势
4. **阈值告警**: 设置合理的性能阈值和告警机制
5. **持续优化**: 根据分析结果持续优化代码

---

## 总结

Old8Lang 提供了完整的开发工具链：

- **Language Server**: 提供 IDE 智能功能
- **VSCode 扩展**: 提供舒适的编辑体验
- **调试器**: 强大的调试功能
- **性能分析工具**: 性能优化利器

通过合理使用这些工具，可以显著提升开发效率和代码质量。

## 参考资料

- [Language Server Protocol 规范](https://microsoft.github.io/language-server-protocol/)
- [VSCode 扩展 API](https://code.visualstudio.com/api)
- [OmniSharp LSP 库文档](https://github.com/OmniSharp/csharp-language-server-protocol)
- [TextMate 语法](https://macromates.com/manual/en/language_grammars)
