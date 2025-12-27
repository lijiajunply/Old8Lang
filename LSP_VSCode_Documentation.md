# Old8Lang Language Server 和 VSCode 扩展文档

## 项目概述

本项目为 Old8Lang 编程语言提供了完整的 IDE 支持，包括：

1. **Old8Lang.LanguageServer**: 基于 LSP (Language Server Protocol) 的语言服务器
2. **vscode-old8lang**: Visual Studio Code 扩展插件

## 架构说明

### Language Server (Old8Lang.LanguageServer)

Language Server 是一个独立的进程，负责提供语言智能功能：

- **文档管理 (DocumentManager)**: 管理打开的文档，维护解析结果和符号表
- **文档同步 (TextDocumentSyncHandler)**: 处理文档打开、修改、保存、关闭事件
- **自动补全 (CompletionHandler)**: 提供关键字和符号的智能补全
- **跳转定义 (DefinitionHandler)**: 支持跳转到符号定义
- **查找引用 (ReferencesHandler)**: 查找符号的所有引用
- **悬停提示 (HoverHandler)**: 显示符号的详细信息

#### 技术栈

- .NET 10.0
- OmniSharp.Extensions.LanguageServer (LSP 实现库)
- Old8Lang 核心解析器

### VSCode 扩展 (vscode-old8lang)

VSCode 扩展作为 LSP 客户端，与 Language Server 通信：

- **语法高亮**: 基于 TextMate 语法定义
- **语言配置**: 括号匹配、自动闭合、注释等
- **LSP 客户端**: 与 Language Server 通信

#### 技术栈

- TypeScript
- VSCode Extension API
- vscode-languageclient

## 开发指南

### 构建 Language Server

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

### 开发 VSCode 扩展

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

### 打包和发布

#### 打包 Language Server

将编译好的 Language Server 可执行文件复制到扩展的 `server` 目录：

```bash
mkdir -p vscode-old8lang/server
cp Old8Lang.LanguageServer/bin/Release/net10.0/Old8Lang.LanguageServer* vscode-old8lang/server/
```

#### 打包 VSCode 扩展

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

## 使用说明

### 安装扩展

1. 从 VSIX 文件安装：
   - 打开 VSCode
   - 按 `Ctrl+Shift+P` (Windows/Linux) 或 `Cmd+Shift+P` (macOS)
   - 输入 "Extensions: Install from VSIX"
   - 选择 `.vsix` 文件

2. 或者直接在 VSCode 扩展市场搜索 "Old8Lang"（如果已发布）

### 配置

在 VSCode 设置中可以配置：

```json
{
  "old8lang.languageServer.path": "/path/to/Old8Lang.LanguageServer",
  "old8lang.trace.server": "verbose"
}
```

### 功能使用

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

## 扩展功能

### 当前已实现的功能

- ✅ 语法高亮
- ✅ 文档同步
- ✅ 实时错误诊断
- ✅ 智能补全（关键字）
- ✅ LSP 基础框架

### 待完善的功能

- ⏳ 符号表构建（变量、函数、类）
- ⏳ 跳转定义（需要符号表支持）
- ⏳ 查找引用（需要符号表支持）
- ⏳ 悬停提示（需要符号表支持）
- ⏳ 代码重构（重命名）
- ⏳ 代码格式化
- ⏳ 代码片段（Snippets）

## 贡献指南

### 添加新的 LSP 功能

1. 在 `Old8Lang.LanguageServer/Handlers/` 创建新的 Handler
2. 实现对应的 LSP 接口（如 `ICodeActionHandler`）
3. 在 `Program.cs` 中注册 Handler
4. 更新文档

### 改进语法高亮

编辑 `vscode-old8lang/syntaxes/old8lang.tmLanguage.json`

### 添加代码片段

在 `vscode-old8lang/snippets/` 目录创建片段文件，并在 `package.json` 中注册。

## 故障排查

### Language Server 无法启动

1. 检查 Language Server 路径配置
2. 确保 Language Server 有执行权限
3. 查看 VSCode 输出面板的 "Old8Lang Language Server" 日志

### 语法高亮不工作

1. 确认文件扩展名为 `.old8`
2. 尝试重新加载 VSCode 窗口
3. 检查语法定义文件是否正确

### 补全不工作

1. 检查 Language Server 是否正常运行
2. 查看是否有语法错误阻止解析
3. 启用 trace 日志查看 LSP 通信

## 参考资料

- [Language Server Protocol 规范](https://microsoft.github.io/language-server-protocol/)
- [VSCode 扩展 API](https://code.visualstudio.com/api)
- [OmniSharp LSP 库文档](https://github.com/OmniSharp/csharp-language-server-protocol)
- [TextMate 语法](https://macromates.com/manual/en/language_grammars)
