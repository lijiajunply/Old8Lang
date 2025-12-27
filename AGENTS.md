# Old8Lang 代理开发指南

## 构建和测试命令

```bash
# 构建解决方案
dotnet build Old8Lang.sln

# 运行单元测试（使用 xUnit）
dotnet test Old8Lang.Tests --configuration Debug
dotnet test Old8Lang.Tests --configuration Release

# 运行单个测试类
dotnet test Old8Lang.Tests --filter "FullyQualifiedName~TestClassName"

# 运行单个测试方法
dotnet test Old8Lang.Tests --filter "TestMethodName"

# 解释模式测试
dotnet run --project Old8Lang.App -- -f <path-to-file.old8>

# 编译模式测试  
dotnet run --project Old8Lang.App -- -c <path-to-file.old8>

# 语法测试
dotnet run --project Old8Lang.App -- -s <path-to-file.old8>

# 运行性能基准测试
dotnet run --project Old8Lang.Benchmarks --configuration Release

# 运行语法测试套件
cd TestFiles && ./run_syntax_tests.sh

# 运行解释器测试套件
cd TestFiles && ./run_interpreter_tests.sh

# 运行编译器测试套件
cd TestFiles && ./run_compiler_tests.sh

# 运行完整测试套件
cd TestFiles && ./run_comprehensive_compiler_tests.sh
```

## 项目结构

### 核心组件
- **Old8Lang**: 核心语言实现（AST、解析器、编译器、解释器）
- **Old8Lang.App**: 命令行应用程序
- **Old8LangLib**: 标准库函数
- **Old8Lang.Tests**: xUnit 测试套件
- **Old8Lang.Benchmarks**: 性能基准测试

### 扩展库
- **Old8Lang.NetLib**: 网络功能库
- **Old8Lang.SerializationLib**: 序列化库
- **Old8Lang.MachineLearningLib**: 机器学习库
- **Old8Lang.DatabaseLib**: 数据库库
- **Old8Lang.CodeGen**: 代码生成器
- **Old8Lang.LanguageServer**: LSP 服务器实现

## 代码风格规范

### 导入和命名空间
- 使用 System 命名空间别名（如 `System.Text` 而非 `System.Text.*`）
- 按字母顺序排列 using 语句
- 优先使用局部导入而非全局导入
- 命名空间结构遵循项目目录结构

### 类型系统
- 启用 `Nullable` 和 `ImplicitUsings`
- 使用 C# 10.0 语法特性
- 目标框架：.NET 10.0
- 编译模式要求函数参数必须有类型注解或默认值
- 解释模式支持类型推断

### 命名约定
- **类名**: PascalCase（如 `Operation`, `FunctionCallExpression`）
- **方法名**: PascalCase（如 `OperaToString()`, `GenerateIl()`）
- **变量名**: camelCase（如 `left`, `right`, `position`）
- **常量**: PascalCase 或全大写下划线分隔
- **接口名**: 以 I 开头（如 `ICommand`, `ILGenerator`）
- **异常类**: 以 Exception 或 Error 结尾

### AST 节点规范
- 继承自适当的基类（如 `LangExpression`, `Statement`）
- 实现访问者模式支持
- 包含位置信息参数（`SourcePosition`）
- 重写 `ToString()` 方法用于调试
- 实现 `Run()` 方法用于解释器模式
- 实现 `LoadIlValue()` 方法用于编译器模式

### 错误处理
- 使用自定义异常类型（位于 `Old8Lang.Error` 命名空间）
- 包含 `SourcePosition` 用于错误定位
- 优先使用具体的异常类型而非通用 Exception
- 异常类必须包含位置信息的构造函数

### 注释和文档
- 使用 XML 文档注释
- 中文注释，描述参数、返回值和异常
- 重要逻辑添加行内注释
- 公共 API 必须有完整的 XML 文档

## 测试规范

### 测试文件组织
- **语法测试**: `TestFiles/SyntaxTests/` 目录，使用 `.old8` 扩展名
- **解释器测试**: `TestFiles/InterpreterTests/` 目录，使用 `.old8` 扩展名
- **编译器测试**: `TestFiles/CompilerTests/` 目录，使用 `.old8` 扩展名
- **单元测试**: `Old8Lang.Tests/` 项目，使用 `.cs` 文件

### 测试文件规范
- 测试文件使用 `.old8` 扩展名
- 期望错误的文件末尾标记 "error"
- 测试时可使用 `PrintLine()` 函数打印结果
- 注释为 `//` 而非 `#`
- 测试报告保存到 `Reports/` 目录

### 测试流程
1. 语法测试 → 解释模式测试 → 编译模式测试
2. 新语法添加必须按此顺序进行测试
3. 每个测试完成后生成测试报告

## 新语法添加规范

1. **语法解析**: 更新 EBNF 语法规则，实现解析器
2. **语法测试**: 确保新语法可以被正确解析
3. **解释器实现**: 在解释器模式下支持新语法
4. **编译器实现**: 在编译器模式下支持新语法
5. **文档更新**: 更新 `Docs/Old8Lang.ebnf` 和 `Docs/Old8Lang_Grammar.md`

## 包管理说明

### 运行模式
- **项目模式**: 检测到 `o8packages.json` 时自动启用虚拟环境
- **非项目模式**: 没有项目配置时自动使用全局包（`~/.old8lang/packages`）

### 包加载优先级
1. 标准库（MathLib, OS, File 等）
2. 项目本地包（如果启用虚拟环境）
3. 全局第三方包
4. 相对路径文件

### 全局包目录
默认位置：`~/.old8lang/packages/`
包结构：
```
~/.old8lang/packages/
├── TestGlobalLib/
│   └── index.old8
└── SomeOtherLib/
    └── main.old8
```

## 双模式架构

Old8Lang 支持解释模式和编译模式：

### 解释模式
- 代码逐条解释执行（运行 `Run` 方法）
- 无需编译步骤
- 支持动态类型和类型推断
- 适合开发和调试

### 编译模式
- 代码编译成中间代码（运行 `GenerateIl` 方法）
- 然后执行编译后的代码
- 要求更严格的类型注解
- 性能更好，适合生产环境

## 开发工作流

### 1. 环境设置
```bash
# 克隆仓库
git clone <repository-url>
cd Old8Lang

# 构建解决方案
dotnet build Old8Lang.sln

# 运行基础测试确保环境正确
dotnet test Old8Lang.Tests --filter "FullyQualifiedName~BasicTests"
```

### 2. 开发新功能
- 在相应的 AST 类中添加新节点
- 更新解析器支持新语法
- 实现解释器逻辑
- 实现编译器逻辑
- 添加相应的测试

### 3. 测试验证
- 运行语法测试确保解析正确
- 运行解释器测试确保功能正确
- 运行编译器测试确保编译正确
- 运行完整测试套件确保无回归

### 4. 代码质量
- 所有公共 API 必须有 XML 文档
- 遵循命名约定和代码风格
- 异常处理必须包含位置信息
- 测试覆盖率应保持高水平

## 性能考虑

- 使用 `Old8Lang.Benchmarks` 项目进行性能测试
- 关注内存使用和执行时间
- 编译器模式通常比解释器模式快
- 大数据操作需要特别注意性能

## 调试技巧

- 使用 `PrintLine()` 函数在 Old8Lang 代码中调试
- 使用 Visual Studio 调试器调试 C# 代码
- 查看测试报告了解失败原因
- 使用 `-d` 参数启用调试模式