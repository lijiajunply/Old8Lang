# Old8Lang 贡献者指南

欢迎为 Old8Lang 项目做出贡献！本指南将帮助您了解如何参与项目开发。

## 目录

- [行为准则](#行为准则)
- [如何贡献](#如何贡献)
- [开发环境设置](#开发环境设置)
- [代码规范](#代码规范)
- [提交规范](#提交规范)
- [测试要求](#测试要求)
- [文档贡献](#文档贡献)
- [问题报告](#问题报告)
- [功能请求](#功能请求)
- [Pull Request 流程](#pull-request-流程)

---

## 行为准则

### 我们的承诺

为了营造开放和友好的环境,我们作为贡献者和维护者承诺,无论年龄、体型、残疾、种族、性别认同和表达、经验水平、国籍、个人外貌、种族、宗教或性认同和取向如何,参与我们的项目和社区对每个人来说都是无骚扰的体验。

### 我们的标准

**积极行为的例子包括**:
- 使用友好和包容的语言
- 尊重不同的观点和经验
- 优雅地接受建设性批评
- 专注于对社区最有利的事情
- 对其他社区成员表示同理心

**不可接受的行为包括**:
- 使用性化语言或图像
- 人身攻击
- 公开或私下骚扰
- 未经许可发布他人的私人信息
- 其他不道德或不专业的行为

### 执行

不可接受的行为可以通过 GitHub Issues 向项目团队报告。所有投诉都将被审查和调查。

---

## 如何贡献

### 贡献方式

有多种方式可以为 Old8Lang 做出贡献:

1. **报告 Bug** - 发现错误并提交详细的问题报告
2. **建议功能** - 提出新功能想法
3. **编写代码** - 修复 Bug 或实现新功能
4. **改进文档** - 完善现有文档或添加新文档
5. **编写测试** - 增加测试覆盖率
6. **审查 PR** - 帮助审查其他人的 Pull Request
7. **分享经验** - 写博客、教程或示例代码

### 适合新手的任务

寻找标记为以下标签的 Issue:
- `good first issue` - 适合新手的简单任务
- `help wanted` - 需要帮助的任务
- `documentation` - 文档相关任务

---

## 开发环境设置

### 前置要求

- **.NET 10.0 SDK** 或更高版本
- **Git** 版本控制
- **IDE**: Visual Studio 2022, VS Code, 或 JetBrains Rider

### 克隆仓库

```bash
git clone https://github.com/your-org/Old8Lang.git
cd Old8Lang
```

### 构建项目

```bash
# 构建整个解决方案
dotnet build Old8Lang.sln

# 构建特定项目
dotnet build Old8Lang/Old8Lang.csproj
```

### 运行测试

```bash
# 运行所有单元测试
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.ParsersTests"

# 运行 Old8Lang 测试脚本
./run_syntax_tests.sh          # 语法测试
./run_interpreter_tests.sh     # 解释器测试
./run_compiler_tests.sh        # 编译器测试
```

### 项目结构

```
Old8Lang/
├── Old8Lang/              # 核心语言库
│   ├── AST/              # 抽象语法树定义
│   ├── LangParser/       # 解析器
│   ├── Compiler/         # 编译器
│   └── TypeSystem/       # 类型系统
├── Old8Lang.App/         # CLI 应用
├── Old8LangLib/          # 原生库（OS、文件、网络）
├── Old8Lang.NetLib/      # 网络库（MQTT）
├── Old8Lang.Tests/       # 单元测试
├── Old8Lang.Benchmarks/  # 性能基准测试
├── TestFiles/            # 测试文件
│   ├── SyntaxTests/      # 语法测试
│   ├── InterpreterTests/ # 解释器测试
│   └── CompilerTests/    # 编译器测试
└── Docs/                 # 文档
```

---

## 代码规范

### C# 代码规范

遵循标准的 C# 编码约定:

**命名规范**:
```csharp
// 类名: PascalCase
public class LangParser { }

// 方法名: PascalCase
public void ParseExpression() { }

// 私有字段: camelCase 或 _camelCase
private int currentIndex;
private readonly string _fileName;

// 属性: PascalCase
public string FileName { get; set; }

// 常量: PascalCase
public const int MaxTokens = 1000;
```

**代码风格**:
```csharp
// 使用大括号,即使是单行语句
if (condition)
{
    DoSomething();
}

// 合理使用空行分隔逻辑块
public void Method()
{
    var result = Calculate();

    if (result > 0)
    {
        ProcessPositive(result);
    }
    else
    {
        ProcessNegative(result);
    }
}
```

### Old8Lang 代码规范

编写 `.old8` 测试文件时:

```old8
// 使用 // 注释,不是 #
// 好的注释

// 使用 <- 赋值
value <- 10

// 类型标注紧跟变量名
name:string <- "Old8"

// 函数定义保持一致的格式
func add(a:int, b:int) -> int {
    return a + b
}

// 适当的缩进和空行
if condition {
    doSomething()

    doMore()
}
```

### 文档字符串

为公共 API 添加 XML 文档注释:

```csharp
/// <summary>
/// 解析表达式并返回 AST 节点
/// </summary>
/// <param name="tokens">Token 列表</param>
/// <returns>表达式 AST 节点</returns>
/// <exception cref="SyntaxErrorException">当语法不正确时抛出</exception>
public Expression ParseExpression(List<LangToken> tokens)
{
    // ...
}
```

---

## 提交规范

### Commit Message 格式

使用清晰的 commit message:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Type 类型**:
- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式调整（不影响功能）
- `refactor`: 重构（不修复 Bug 也不添加功能）
- `test`: 添加或修改测试
- `chore`: 构建工具或辅助工具的变动

**示例**:
```
feat(parser): 添加模式匹配语法支持

实现了 match 表达式的解析和 AST 节点定义。
支持基本模式、通配符模式和守卫条件。

Closes #123
```

```
fix(compiler): 修复类型推断中的空指针异常

当处理嵌套泛型类型时,类型推断引擎可能返回 null。
现在添加了空值检查和默认值处理。

Fixes #456
```

### 分支命名

- `feature/feature-name` - 新功能
- `fix/bug-description` - Bug 修复
- `docs/documentation-update` - 文档更新
- `refactor/component-name` - 重构

---

## 测试要求

### 单元测试

**为新功能添加测试**:
```csharp
using Xunit;

public class ParserTests
{
    [Fact]
    public void ParseMatchExpression_SimplePattern_ReturnsCorrectAST()
    {
        // Arrange
        var code = "match x { 1 -> true }";
        var parser = new LangParser();

        // Act
        var ast = parser.Parse(code);

        // Assert
        Assert.NotNull(ast);
        Assert.IsType<MatchExpression>(ast);
    }
}
```

### Old8Lang 测试文件

**创建测试文件** (`.old8`):

```old8
// TestFiles/CompilerTests/test_new_feature.old8

// 测试新功能
func testFeature() -> void {
    result <- newFeature(10, 20)
    PrintLine(result)
}

testFeature()
```

**预期输出**: 在文件末尾注释说明预期结果,或者使用 "error" 标记预期失败:

```old8
// 预期输出: 30
```

或

```old8
// 这个测试应该失败
// error
```

### 测试覆盖率

- 新功能应有至少 80% 的测试覆盖率
- Bug 修复必须包含回归测试
- 运行测试确保所有测试通过

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 文档贡献

### 文档类型

1. **API 文档** - 函数和类的用法
2. **教程** - 逐步指南
3. **示例代码** - 实际用例
4. **架构文档** - 设计决策和实现细节

### 文档位置

- `Docs/` - 主要文档
- `README.md` - 项目概述
- `CHANGELOG.md` - 变更日志
- 代码中的注释 - 内联文档

### 文档规范

**Markdown 格式**:
```markdown
# 标题

## 子标题

### 三级标题

**粗体文本**

*斜体文本*

`代码`

​```old8
// 代码块
func example() -> void {
    PrintLine("Hello")
}
​```
```

**更新 CHANGELOG**:

遵循 [Keep a Changelog](https://keepachangelog.com/) 格式:

```markdown
## [Unreleased]

### Added
- 新功能描述

### Changed
- 修改的功能

### Fixed
- Bug 修复
```

**重要**: CHANGELOG 应该:
- 从开发者视角描述问题和影响
- 描述用户遇到的原始问题,而非实现细节
- 跳过次要变更(文档修复、样式优化)
- 使用 "-" 前缀标记非语法变更(工具、基础设施)

---

## 问题报告

### Bug 报告模板

创建 Bug 报告时,请包含:

```markdown
**描述**
简洁清晰地描述 Bug。

**重现步骤**
1. 执行 '...'
2. 运行 '....'
3. 查看错误

**预期行为**
应该发生什么。

**实际行为**
实际发生了什么。

**最小可重现示例**
​```old8
// 最小的代码示例
func test() -> void {
    // ...
}
​```

**环境**
- Old8Lang 版本: [例如 v1.0.0]
- .NET 版本: [例如 .NET 10.0]
- 操作系统: [例如 macOS 13, Windows 11]
- 运行模式: [编译器 or 解释器]

**额外信息**
其他相关截图或日志。
```

### 有效的 Bug 报告

**好的例子**:
```markdown
**描述**
编译器模式下,使用默认参数的函数调用时报类型错误。

**重现步骤**
1. 创建文件 `test.old8` 包含以下代码
2. 运行 `dotnet run --project Old8Lang.App -- -c test.old8`
3. 观察到类型错误

**代码**
​```old8
func greet(name:string, prefix: "Hello") -> string {
    return prefix + ", " + name
}

result <- greet("World")
​```

**预期行为**
应该输出 "Hello, World"

**实际行为**
抛出类型错误: "Missing type annotation for parameter 'prefix'"

**环境**
- Old8Lang: commit abc123
- .NET: 10.0.1
- OS: Ubuntu 22.04
- Mode: Compiler
```

---

## 功能请求

### 功能请求模板

```markdown
**功能描述**
清晰简洁的描述您想要的功能。

**动机**
为什么需要这个功能？解决什么问题？

**建议的解决方案**
您希望如何实现这个功能？

**替代方案**
您考虑过的其他解决方案或功能。

**示例代码**
​```old8
// 期望的语法示例
func example() -> void {
    // ...
}
​```

**额外信息**
其他相关信息或截图。
```

### 讨论新功能

在实现大型功能之前:
1. 先创建 Issue 讨论
2. 等待维护者反馈
3. 达成共识后再开始实现

---

## Pull Request 流程

### 提交 PR 前

1. **Fork 仓库** 到您的账户
2. **创建功能分支** (例如 `feature/my-feature`)
3. **实现更改** 并编写测试
4. **运行所有测试** 确保通过
5. **更新文档** (如适用)
6. **遵循代码规范** 和提交规范

### 创建 Pull Request

**PR 标题**: 简洁描述更改

**PR 描述模板**:
```markdown
## 更改摘要
简要描述此 PR 的更改内容。

## 相关 Issue
Closes #123
Fixes #456

## 更改类型
- [ ] Bug 修复
- [ ] 新功能
- [ ] 重构
- [ ] 文档更新
- [ ] 性能改进

## 测试
- [ ] 添加了单元测试
- [ ] 添加了 Old8Lang 测试文件
- [ ] 所有测试通过
- [ ] 手动测试通过

## 文档
- [ ] 更新了 API 文档
- [ ] 更新了 CHANGELOG
- [ ] 更新了相关指南

## 检查清单
- [ ] 代码遵循项目规范
- [ ] 提交信息清晰明确
- [ ] 没有引入新的警告
- [ ] 文档完整且准确
```

### Code Review 过程

1. **自动检查**: CI/CD 运行测试和检查
2. **维护者审查**: 代码风格、设计、测试
3. **反馈**: 维护者可能要求更改
4. **修改**: 根据反馈更新 PR
5. **批准**: 通过审查后合并

### Code Review 最佳实践

**接受反馈**:
- 保持开放心态
- 积极讨论不同意见
- 做出必要的修改

**回应评论**:
- 及时回复审查意见
- 标记已解决的评论
- 解释您的设计决策

---

## 优先级和路线图

### 当前优先级

1. **稳定性** - 修复已知 Bug
2. **性能** - 优化编译器和运行时
3. **文档** - 完善文档和示例
4. **新功能** - 实现计划中的功能

### 长期目标

- 更好的类型系统
- 改进的错误消息
- 更多的标准库功能
- IDE 集成增强

---

## 获得帮助

需要帮助时:

1. **查看文档**: [Docs/](../Docs/)
2. **搜索 Issues**: 可能已有相关讨论
3. **创建 Discussion**: 提出问题或想法
4. **加入社区**: 参与讨论和分享

---

## 许可证

通过贡献代码,您同意您的贡献将在与项目相同的许可证下发布。

---

## 致谢

感谢所有贡献者使 Old8Lang 变得更好! 🎉

您的名字将出现在:
- Git 历史记录中
- Release Notes 中 (重大贡献)
- Contributors 页面

---

## 联系方式

- **GitHub Issues**: 报告 Bug 和功能请求
- **GitHub Discussions**: 一般讨论和问题
- **Email**: (如果有的话)

感谢您为 Old8Lang 做出贡献! 🚀

