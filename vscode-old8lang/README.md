# Old8Lang VSCode 扩展

这是 Old8Lang 编程语言的 Visual Studio Code 扩展，提供语法高亮、智能提示、错误检测等功能。

## 功能特性

- **语法高亮**：为 `.old8` 文件提供完整的语法高亮支持
- **智能补全**：提供关键字和符号的自动补全
- **错误诊断**：实时显示语法错误和警告
- **跳转定义**：支持跳转到符号定义位置（F12）
- **查找引用**：查找符号的所有引用位置（Shift+F12）
- **重命名符号**：重命名符号并自动更新所有引用（F2）
- **悬停提示**：鼠标悬停显示符号信息和文档注释

## 安装

### 从 VSIX 安装

1. 下载 `.vsix` 文件
2. 在 VSCode 中打开命令面板 (`Ctrl+Shift+P` 或 `Cmd+Shift+P`)
3. 输入 "Install from VSIX" 并选择下载的文件

### 从源码安装

1. 克隆仓库
2. 进入 `vscode-old8lang` 目录
3. 运行 `npm install`
4. 运行 `npm run compile`
5. 按 `F5` 启动调试模式

## 配置

在 VSCode 设置中可以配置以下选项：

- `old8lang.languageServer.path`: Language Server 可执行文件路径
- `old8lang.trace.server`: 跟踪 Language Server 通信（用于调试）

## 构建 Language Server

在使用此扩展之前，需要先构建 Language Server：

```bash
cd Old8Lang.LanguageServer
dotnet build -c Release
```

构建完成后，将生成的可执行文件复制到扩展的 `server` 目录中。

## 开发

### 要求

- Node.js 18+
- .NET 10.0 SDK
- VSCode 1.75+

### 构建

```bash
npm install
npm run compile
```

### 打包

```bash
npm install -g @vscode/vsce
vsce package
```

## 许可证

与 Old8Lang 主项目保持一致。
