# Old8Lang 代理开发指南

## 构建和测试命令

```bash
# 构建解决方案
dotnet build Old8Lang.sln

# 运行单个测试
dotnet test Old8Lang.Tests --filter "FullyQualifiedName~TestClassName"
dotnet test --filter "TestMethodName"

# 解释模式测试
dotnet run --project Old8Lang.App -- -f <path-to-file.old8>

# 编译模式测试  
dotnet run --project Old8Lang.App -- -c <path-to-file.old8>

# 语法测试
dotnet run --project Old8Lang.App -- -s <path-to-file.old8>

# 运行性能测试
dotnet run --project Old8Lang.Benchmarks --configuration Release
```

## 代码风格规范

### 导入和命名空间
- 使用 System 命名空间别名（如 System.Text 而非 System.Text.*）
- 按字母顺序排列 using 语句
- 优先使用局部导入而非全局导入

### 类型系统
- 启用 `Nullable` 和 `ImplicitUsings`
- 使用 C# 10.0 语法特性
- 编译模式要求函数参数必须有类型注解或默认值
- 解释模式支持类型推断

### 命名约定
- 类名：PascalCase（如 `Operation`, `FunctionCallExpression`）
- 方法名：PascalCase（如 `OperaToString()`, `GenerateIl()`）
- 变量名：camelCase（如 `left`, `right`, `position`）
- 常量：PascalCase 或全大写下划线分隔

### 错误处理
- 使用自定义异常类型（位于 `Old8Lang.Error` 命名空间）
- 包含 `SourcePosition` 用于错误定位
- 优先使用具体的异常类型而非通用 Exception

### 注释和文档
- 使用 XML 文档注释
- 中文注释，描述参数、返回值和异常
- 重要逻辑添加行内注释

### AST 节点
- 继承自适当的基类（如 `LangExpression`, `Statement`）
- 实现访问者模式支持
- 包含位置信息参数

### 测试规范
- 测试文件使用 `.old8` 扩展名
- 期望错误的文件末尾标记 "error"
- 测试报告保存到 `Reports/` 目录
- 遵循语法测试 → 解释模式测试 → 编译模式测试的顺序

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