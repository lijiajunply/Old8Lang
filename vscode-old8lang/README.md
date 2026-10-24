# Old8Lang VSCode 扩展

这是 Old8Lang 编程语言的 Visual Studio Code 扩展，提供语法高亮、智能提示、错误检测、任务运行等功能。

## 功能特性

- **语法高亮**：为 `.old8` 文件提供完整的语法高亮支持
- **代码片段**：函数、异步函数、类、条件、循环、异常处理、switch、import、Lambda 等
- **智能补全**：提供关键字和符号的自动补全
- **错误诊断**：实时显示语法错误和警告，可按严重级别过滤
- **跳转定义**：支持跳转到符号定义位置（F12），顶层符号支持跨文件
- **查找引用**：查找符号的所有引用位置（Shift+F12），顶层符号支持跨文件
- **重命名符号**：重命名符号并自动更新所有引用（F2），顶层符号跨文件生效
- **悬停提示**：鼠标悬停显示符号信息和文档注释
- **代码镜头**：在函数、类和方法声明上显示引用数量，点击可查看引用列表
- **选择范围**：按 `Shift+Alt+→` / `Shift+Alt+←` 在 token、表达式、语句、声明块之间扩展/收缩选区
- **任务支持**：通过 `Tasks: Run Task` 运行解释模式、IL 模式、虚拟机模式或语法检查
- **命令**：运行当前文件、语法检查当前文件、重启 Language Server

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

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `old8lang.languageServer.path` | `""` | Language Server 可执行文件或 `.dll` 路径；留空时自动探测 `server/` 目录或从源码项目启动 |
| `old8lang.languageServer.project` | `Old8Lang.LanguageServer` | 源码开发模式下要启动的 Language Server 项目 |
| `old8lang.compiler.path` | `""` | `Old8Lang.App` 可执行文件或 `.dll` 路径 |
| `old8lang.compiler.project` | `Old8Lang.App` | 编译器项目路径，`compiler.path` 为空时使用 |
| `old8lang.compiler.mode` | `interpreter` | “运行当前文件”使用的模式：`interpreter`、`il` 或 `vm` |
| `old8lang.dotnet.path` | `dotnet` | `dotnet` 可执行文件路径或命令名 |
| `old8lang.diagnostics.level` | `hint` | 客户端显示诊断的最高严重级别：`error`、`warning`、`information`、`hint` |
| `old8lang.trace.server` | `off` | 跟踪 Language Server 通信 |

配置项会在修改时校验：路径不存在、模式或诊断级别取值无效时会给出警告。

## 运行与任务

命令面板中的命令：

- `Old8Lang: 运行当前文件`：按 `old8lang.compiler.mode` 运行当前 `.old8` 文件
- `Old8Lang: 语法检查当前文件`：等价于 `-s`
- `Old8Lang: 重启 Language Server`：修改服务器配置后重新启动

也可以在 `tasks.json` 中直接引用任务类型：

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Old8Lang: 语法检查",
      "type": "old8lang",
      "command": "check",
      "file": "${file}"
    },
    {
      "label": "Old8Lang: IL 模式运行",
      "type": "old8lang",
      "command": "il",
      "file": "${file}",
      "args": ["--debug"]
    }
  ]
}
```

`command` 支持 `interpreter`（`-f`）、`il`（`-il`）、`vm`（`-vm`）、`check`（`-s`）。

## 构建 Language Server

打包扩展前会把 Language Server 发布到 `server/` 目录：

```bash
npm run prepare:server
```

也可以手动执行：

```bash
cd ../Old8Lang.LanguageServer
dotnet publish -c Release -o ../vscode-old8lang/server
```

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

`npm run compile` 先由 `tsc` 做类型检查，再用 `esbuild` 把扩展及其依赖打包为 `out/extension.js`。

### 打包

```bash
npm run package:vsix
```

该命令会先发布 Language Server，再执行 `vsce package --no-dependencies`，产物为 `old8lang-<version>.vsix`。

## 已知限制

- 暂无 Debug Adapter（DAP）支持。Language Server 暴露的 `old8lang/startDebugging` 等是自定义扩展请求，
  VM 的 `--debug-session` 是交互式终端调试器，两者都不是 DAP。
- 跨文件能力基于 token 级索引：若另一个文件自己声明了同名顶层符号，会被视为独立程序并跳过，
  以避免把互不相关的脚本算进引用。
- 方法、参数等文件内作用域的符号仍只在当前文件内查找引用和重命名。

## 许可证

与 Old8Lang 主项目保持一致（MIT）。
