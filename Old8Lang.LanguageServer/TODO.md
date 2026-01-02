# Old8Lang Language Server Protocol (LSP) 开发计划

本文档记录 Old8Lang LSP 的后续开发任务，包括 Language Server（C#）和 VS Code 扩展的功能改进和新特性实现。

---

## 📊 当前实现状态

### ✅ 已实现功能

- **文档同步（TextDocumentSyncHandler）**: 文件打开、编辑、关闭事件处理
- **悬停提示（HoverHandler）**: 鼠标悬停显示符号信息和文档注释
- **跳转到定义（DefinitionHandler）**: 从符号使用位置跳转到定义位置
- **查找引用（ReferencesHandler）**: 查找符号的所有引用位置，支持包含/排除定义
- **重命名符号（RenameHandler）**: 重命名符号并自动更新所有引用位置
- **自动补全（CompletionHandler）**: 关键字和符号补全（基础实现）
- **符号表构建（SymbolTableBuilder）**: 支持函数、异步函数、类、变量
- **符号查找（SymbolFinder）**: 基于位置的符号查找和引用查找
- **文档管理（DocumentManager）**: 文档解析、缓存、诊断信息管理
- **调试和性能分析（DebugProfilerHandler）**: 启用/禁用调试模式和性能分析
- **Token-Based Location Finding**: 通过token列表获取准确的符号位置（解决AST Position不准确问题）

### 🔧 部分实现功能

（无）

### ❌ 未实现功能

- 代码格式化（Formatting）
- 代码操作（Code Actions）
- 文档符号（Document Symbols）
- 工作区符号（Workspace Symbols）
- 语义高亮（Semantic Highlighting）
- 诊断信息（Diagnostics）- 语法错误、类型错误等
- 签名帮助（Signature Help）
- 代码镜头（Code Lens）
- 内联值（Inline Values）
- 折叠范围（Folding Range）
- 选择范围（Selection Range）

---

## 🎯 优先级任务列表

### P0 - 核心功能（高优先级）

#### ~~1. 实现查找引用功能（Find All References）~~ ✅ 已完成
**文件**: `Old8Lang.LanguageServer/Handlers/DefinitionHandler.cs:84-135`

**完成状态**:
- [x] 在 `SymbolFinder` 中实现 `FindReferences(document, symbolName)` 方法
- [x] 遍历所有 tokens，查找指定符号名的所有使用位置
- [x] 区分定义位置和引用位置（支持 `IncludeDeclaration` 参数）
- [x] 实现 `ReferencesHandler.Handle` 方法
- [x] 添加测试程序验证功能

**已知限制**:
- 不支持跨文件查找引用（需要 WorkspaceManager）
- 不支持作用域区分（局部变量与全局变量同名问题）
- 类成员（属性、方法）暂不支持（符号表未索引类成员）

**测试结果**: 所有测试场景通过（函数、类、变量引用查找）

---

#### ~~2. 实现重命名（Rename Symbol）~~ ✅ 已完成
**文件**: `Old8Lang.LanguageServer/Handlers/RenameHandler.cs`

**完成状态**:
- [x] 实现 `IRenameHandler` 接口
- [x] 调用 `SymbolFinder.FindReferences()` 查找所有引用
- [x] 生成 `WorkspaceEdit`，包含所有需要修改的位置
- [x] 注册到 Language Server
- [x] 添加测试程序验证功能

**已知限制**:
- 不支持跨文件重命名（需要 WorkspaceManager）
- 不验证新名称的合法性（可能与其他符号冲突）
- 不支持预览重命名（Prepare Rename）
- 类成员（属性、方法）暂不支持

**测试结果**: 所有测试场景通过（函数、类、变量重命名）

---

#### ~~3. 完善符号表 - 支持类成员（Methods & Properties）~~ ✅ 已完成
**文件**: `Old8Lang.LanguageServer/Services/SymbolTableBuilder.cs:203`

**完成状态**:
- [x] 扩展 `SymbolInfo` 模型，添加 `Members`、`Parent`、`AccessModifier`、`IsStatic` 属性
- [x] 在 `VisitClass()` 中调用 `VisitClassMembers()` 遍历类的成员
- [x] 创建 `CreateMethodSymbol()` 和 `CreatePropertySymbol()` 方法
- [x] 更新 `SymbolFinder.FindSymbolAtPosition()` 支持成员访问查找（`obj.member`）
- [x] 更新 `HoverHandler` 显示成员的访问修饰符、所属类等信息
- [x] `DefinitionHandler` 自动支持类成员跳转（通过 SymbolFinder）
- [x] 更新 `CompletionHandler` 支持成员访问补全（`obj.` 触发成员列表）
- [x] 处理 `public`/`private`/`protected`/`static` 修饰符
- [x] 创建测试文件验证功能（`/tmp/ClassMembersTest.old8`）

**实现细节**:
- 符号表构建时，为每个类创建成员字典 `Members`
- 成员符号包含 `Parent` 引用指向所属类
- 悬停提示显示成员的访问修饰符和所属类信息
- 补全时检测 `.` 触发符，查找前面的对象类型并提供其成员列表

**已知限制**:
- 暂不支持继承的成员（父类成员不会出现在子类的成员列表中）
- 静态成员访问（`ClassName.staticMethod()`）的补全暂未实现
- 文档注释在类成员上可能存在解析问题（待修复）

**测试结果**: 语法解析通过，LSP 功能已实现

---

#### ~~4. 实现实时诊断（Diagnostics）~~ ✅ 已完成
**文件**: `Old8Lang.LanguageServer/Services/SemanticAnalyzer.cs`

**完成状态**:
- [x] 实现语义分析器（SemanticAnalyzer）:
  - [x] 检测未定义符号
  - [x] 检测重复定义
  - [x] 内置函数和类型白名单（PrintLine, ToInt, int, double等）
  - [x] 定义上下文识别（函数声明、类声明、赋值左侧）
  - [x] 成员访问跳过（需要类型信息，暂不支持）
- [x] 在 `DocumentManager.ParseDocument()` 中集成语义分析
- [x] 支持诊断严重级别（Error, Warning, Information, Hint）
- [x] `TextDocumentSyncHandler` 自动推送诊断信息（已有实现）
- [x] 实时编辑触发诊断（文档打开、修改、保存时自动执行）
- [x] 添加单元测试（`Old8Lang.Tests/LanguageServer/SemanticAnalyzerTests.cs`）
  - [x] 未定义符号检测测试
  - [x] 内置函数不报错测试
  - [x] 重复定义检测测试

**已知限制**:
- 类型不匹配检测未实现（需要完整的类型推断系统）
- 未使用的变量检测未实现
- 跨文件语义分析未实现（需要 WorkspaceManager）
- 成员访问的语义检测暂时跳过（`obj.unknownMethod()` 不会报错）
- 重复定义检测依赖符号表，Dictionary 会覆盖重复项（当前实现做了额外检测）

**测试结果**: 所有测试通过（3/3），包括未定义符号检测、内置函数识别、重复定义检测

---

#### 5. 改进自动补全（Code Completion）
**文件**: `Old8Lang.LanguageServer/Handlers/CompletionHandler.cs`

**当前状态**: 仅支持关键字和全局符号补全

**实现任务**:
- [ ] **成员访问补全**: 检测 `.` 触发符，根据左侧表达式类型提供成员补全
  - 例如: `user.` 应该补全 `User` 类的成员
  - 需要类型推断支持
- [ ] **上下文感知补全**:
  - 函数参数位置补全函数签名
  - 类定义内补全 `public`/`private`/`static` 修饰符
  - import 语句补全模块名
- [ ] **代码片段（Snippets）**:
  - `func` → 完整函数模板
  - `class` → 完整类模板
  - `if`, `for`, `while` → 控制流模板
- [ ] **智能排序**: 根据使用频率、距离、类型匹配度排序补全项
- [ ] **详细文档**: 在补全项中显示参数、返回值、文档注释
- [ ] 添加单元测试

**预期效果**: 智能、上下文感知的代码补全，提升开发效率。

---

### P1 - 增强功能（中优先级）

#### 6. 实现文档符号（Document Symbols / Outline）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/DocumentSymbolHandler.cs`

**实现任务**:
- [ ] 实现 `IDocumentSymbolHandler` 接口
- [ ] 从符号表生成文档大纲（Document Symbols）
- [ ] 支持层级结构（类 → 方法/属性，函数 → 参数）
- [ ] 支持符号的选择范围（selection range）和完整范围（full range）
- [ ] 添加单元测试

**预期效果**: VS Code 的大纲视图（Outline）显示文件结构，用户可以快速导航。

---

#### 7. 实现签名帮助（Signature Help）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/SignatureHelpHandler.cs`

**实现任务**:
- [ ] 实现 `ISignatureHelpHandler` 接口
- [ ] 检测函数调用上下文（`(` 触发）
- [ ] 查找函数定义，提取参数列表
- [ ] 高亮当前参数位置
- [ ] 显示参数类型和文档注释
- [ ] 支持重载函数（如果语言支持）
- [ ] 添加单元测试

**预期效果**: 用户输入函数调用时，显示参数提示和文档。

---

#### 8. 实现代码格式化（Formatting）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/FormattingHandler.cs`

**实现任务**:
- [ ] 实现 `IDocumentFormattingHandler` 接口
- [ ] 实现 `IDocumentRangeFormattingHandler` 接口（选区格式化）
- [ ] 创建代码格式化器（Formatter）:
  - 缩进规则（2空格/4空格/Tab）
  - 大括号风格（same line / next line）
  - 运算符前后空格
  - 逗号后空格
- [ ] 支持格式化配置（通过 VS Code 设置）
- [ ] 添加单元测试

**预期效果**: 用户可以使用 `Shift+Alt+F` 格式化代码。

---

#### 9. 实现代码操作（Code Actions）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/CodeActionHandler.cs`

**实现任务**:
- [ ] 实现 `ICodeActionHandler` 接口
- [ ] **快速修复（Quick Fixes）**:
  - 自动导入未定义符号（如果在其他文件中）
  - 移除未使用的变量
  - 添加缺失的返回语句
- [ ] **重构操作（Refactorings）**:
  - 提取函数（Extract Function）
  - 提取变量（Extract Variable）
  - 内联变量（Inline Variable）
- [ ] 与诊断信息关联（提供修复建议）
- [ ] 添加单元测试

**预期效果**: 用户可以通过灯泡图标快速修复错误或进行重构。

---

#### 10. 改进 Token-Based Location Finding
**文件**: `Old8Lang.LanguageServer/Services/SymbolTableBuilder.cs`

**当前问题**:
- Token-based location finding 解决了 AST Position 不准确的问题
- 但这是一个临时方案，理想情况下应修复 Parser 本身

**实现任务**:
- [ ] **短期方案**:
  - 扩展 `FindSymbolLocationFromTokens()` 支持更多场景（变量、参数）
  - 处理泛型、实例化表达式的位置
- [ ] **长期方案**:
  - 修复 `Old8Lang.LangParser` 中的 Position 设置
  - 在 `FunctionParser.cs`、`ClassParser.cs` 中正确设置 AST 节点的 Position
  - 修复后可移除 token-based workaround

**预期效果**: 所有符号位置信息准确无误。

---

### P2 - 高级功能（低优先级）

#### 11. 实现语义高亮（Semantic Highlighting）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/SemanticTokensHandler.cs`

**实现任务**:
- [ ] 实现 `ISemanticTokensHandler` 接口
- [ ] 定义语义 token 类型（function, class, variable, parameter, keyword 等）
- [ ] 遍历 AST 或 tokens 生成语义 token
- [ ] 支持增量更新（Semantic Tokens Range/Delta）
- [ ] 添加单元测试

**预期效果**: 更准确的语法高亮（基于语义而非正则表达式）。

---

#### 12. 实现工作区符号（Workspace Symbols）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/WorkspaceSymbolHandler.cs`

**实现任务**:
- [ ] 创建 `WorkspaceManager` 管理多个文档
- [ ] 实现 `IWorkspaceSymbolsHandler` 接口
- [ ] 索引工作区内所有符号
- [ ] 支持模糊搜索（fuzzy matching）
- [ ] 添加单元测试

**预期效果**: 用户可以使用 `Ctrl+T` 快速搜索项目中的任何符号。

---

#### 13. 实现折叠范围（Folding Range）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/FoldingRangeHandler.cs`

**实现任务**:
- [ ] 实现 `IFoldingRangeHandler` 接口
- [ ] 识别可折叠区域：
  - 函数体 `{ ... }`
  - 类定义 `{ ... }`
  - 控制流 `if/for/while { ... }`
  - 文档注释块 `/// ...`
- [ ] 添加单元测试

**预期效果**: 用户可以折叠代码块，提升大文件的可读性。

---

#### 14. 实现代码镜头（Code Lens）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/CodeLensHandler.cs`

**实现任务**:
- [ ] 实现 `ICodeLensHandler` 接口
- [ ] 显示引用计数（例如: "3 references"）
- [ ] 显示函数调用次数（需要静态分析）
- [ ] 支持点击跳转到引用列表
- [ ] 添加单元测试

**预期效果**: 在函数/类定义上方显示引用信息。

---

#### 15. 实现内联值（Inline Values）
**文件**: 新建 `Old8Lang.LanguageServer/Handlers/InlineValueHandler.cs`

**实现任务**:
- [ ] 实现 `IInlineValueHandler` 接口
- [ ] 在调试模式下显示变量值（需要与 Debugger 集成）
- [ ] 添加单元测试

**预期效果**: 调试时在代码中直接显示变量值。

---

## 🛠️ VS Code 扩展改进（vscode-old8lang）

### 当前状态
- 基础扩展结构已搭建
- Language Server 集成已完成
- 语法高亮（TextMate Grammar）已配置

### 改进任务

#### 1. 增强语法高亮
**文件**: `vscode-old8lang/syntaxes/old8lang.tmLanguage.json`

**任务**:
- [ ] 完善 TextMate Grammar 规则
- [ ] 支持字符串插值（String Templates）高亮
- [ ] 支持文档注释（`///`）的特殊高亮
- [ ] 支持嵌套结构的正确高亮

---

#### 2. 添加代码片段（Snippets）
**文件**: 新建 `vscode-old8lang/snippets/old8lang.json`

**任务**:
- [ ] 创建常用代码片段:
  - `func`: 函数模板
  - `asyncfunc`: 异步函数模板
  - `class`: 类模板
  - `if`, `elif`, `else`: 条件语句
  - `for`, `while`, `for-in`: 循环语句
  - `try-catch`: 异常处理
- [ ] 在 `package.json` 中注册 snippets

---

#### 3. 添加调试支持（Debug Adapter）
**文件**: 新建 `vscode-old8lang/src/debugAdapter.ts`

**任务**:
- [ ] 实现 Debug Adapter Protocol
- [ ] 集成 Old8Lang Debugger（如果存在）
- [ ] 支持断点、单步执行、变量查看
- [ ] 在 `package.json` 中配置调试器

---

#### 4. 添加任务支持（Tasks）
**文件**: `vscode-old8lang/package.json` 的 `contributes.taskDefinitions`

**任务**:
- [ ] 定义 Old8Lang 任务类型（运行、编译、测试）
- [ ] 集成 `Old8Lang.App` 的 CLI 命令
- [ ] 支持自定义任务配置

---

#### 5. 改进扩展配置
**文件**: `vscode-old8lang/package.json` 的 `contributes.configuration`

**任务**:
- [ ] 添加更多配置项:
  - 编译器路径
  - 运行参数
  - 格式化选项（缩进、大括号风格）
  - 诊断级别
- [ ] 添加配置验证

---

#### 6. 打包和发布
**任务**:
- [ ] 使用 `vsce` 打包扩展
- [ ] 测试扩展在不同操作系统上的兼容性
- [ ] 编写安装文档
- [ ] 发布到 VS Code Marketplace（可选）

---

## 📈 性能优化

### 1. 符号表缓存
**问题**: 每次编辑都重新构建符号表，大文件性能差

**优化**:
- [ ] 实现增量符号表更新
- [ ] 缓存未修改文件的符号表
- [ ] 使用后台线程构建符号表

---

### 2. Token 查找优化
**问题**: `FindTokenAtPosition` 线性遍历 tokens，O(n) 复杂度

**优化**:
- [ ] 为 tokens 构建索引（按行号）
- [ ] 使用二分查找定位 token
- [ ] 缓存常用位置的 token

---

### 3. 诊断信息优化
**问题**: 实时诊断可能导致编辑卡顿

**优化**:
- [ ] 使用防抖（Debounce）延迟诊断
- [ ] 仅诊断可见区域
- [ ] 后台线程执行语义分析

---

## 🧪 测试策略

### 单元测试
- [ ] 为每个 Handler 添加单元测试
- [ ] 测试符号表构建的各种场景
- [ ] 测试 Token-based location finding 的边界情况
- [ ] 使用 xUnit 和 Moq 框架

### 集成测试
- [ ] 创建端到端测试（Language Client ↔ Language Server）
- [ ] 测试真实的 Old8Lang 代码文件
- [ ] 测试跨文件功能（引用查找、工作区符号）

### 性能测试
- [ ] 测试大文件（>1000 行）的性能
- [ ] 测试实时诊断的延迟
- [ ] 使用 BenchmarkDotNet 进行基准测试

---

## 📚 文档

### 开发文档
- [ ] 编写 Architecture.md 介绍 LSP 架构
- [ ] 编写 Contributing.md 指导贡献者
- [ ] 为每个模块添加代码注释

### 用户文档
- [ ] 编写 User Guide（用户指南）
- [ ] 创建功能演示视频或 GIF
- [ ] 编写故障排除（Troubleshooting）文档

---

## 🐛 已知问题

### 1. AST Position 不准确
**位置**: `Old8Lang.LangParser`

**描述**: `FuncInit`, `AsyncFuncInit`, `ClassInit` 的 `Position` 默认为 (0, 0)

**临时方案**: 使用 Token-based location finding

**永久方案**: 修复 Parser，正确设置 Position

---

### 2. 符号表不支持作用域
**位置**: `Old8Lang.LanguageServer/Services/SymbolTableBuilder.cs`

**描述**: 当前符号表是全局的 `Dictionary<string, SymbolInfo>`，不支持局部变量的作用域

**问题场景**:
```old8
func foo() -> void {
    x <- 10  // 局部变量
}

func bar() -> void {
    x <- 20  // 同名局部变量，会覆盖符号表中的 x
}
```

**解决方案**:
- [ ] 设计层级符号表（支持作用域链）
- [ ] 在查找符号时考虑作用域
- [ ] 支持变量遮蔽（Shadowing）

---

### 3. 类成员未索引
**位置**: `Old8Lang.LanguageServer/Services/SymbolTableBuilder.cs:197`

**描述**: 类的方法和属性未添加到符号表

**影响**: 无法跳转到类成员定义，无法补全类成员

**解决方案**: 见 P0-2 任务

---

## 📝 备注

### 开发环境
- .NET 10.0 SDK
- VS Code + C# Extension
- Node.js + TypeScript (for vscode-old8lang)

### 相关资源
- [Language Server Protocol 规范](https://microsoft.github.io/language-server-protocol/)
- [OmniSharp.Extensions.LanguageServer 文档](https://github.com/OmniSharp/csharp-language-server-protocol)
- [VS Code Extension API](https://code.visualstudio.com/api)

### 贡献指南
欢迎贡献！请遵循以下步骤：
1. Fork 仓库
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add amazing feature'`)
4. 推送到分支 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

---

**最后更新**: 2026-01-02 (已完成查找引用、重命名、类成员支持和实时诊断功能)
**维护者**: Old8Lang Team
