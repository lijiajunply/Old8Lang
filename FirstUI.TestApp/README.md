# FirstUI.TestApp - 运行指南

这是 Old8Lang FirstUI 框架的测试应用程序。

## 运行要求

FirstUI 是一个 GUI 应用程序，基于 Avalonia UI 框架。要成功运行，您需要：

1. **图形界面环境**: 应用需要在有图形界面的环境中运行
2. **本地终端**: 必须在本地终端运行，不能通过 SSH 或远程会话
3. **macOS 桌面登录**: 需要登录到 macOS 桌面环境

## 如何运行

### 方法 1: 使用 macOS Terminal.app（推荐）

1. 打开 macOS 自带的 Terminal.app 应用
2. 进入项目目录:
   ```bash
   cd /Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang
   ```
3. 运行应用:
   ```bash
   dotnet run --project FirstUI.TestApp/FirstUI.TestApp.csproj
   ```

### 方法 2: 使用 Visual Studio for Mac 或 Rider

1. 在 IDE 中打开 `Old8Lang.sln`
2. 将 `FirstUI.TestApp` 设置为启动项目
3. 点击运行/调试按钮

### 方法 3: 直接运行可执行文件

1. 构建项目:
   ```bash
   dotnet build FirstUI.TestApp/FirstUI.TestApp.csproj -c Release
   ```
2. 直接运行:
   ```bash
   ./FirstUI.TestApp/bin/Release/net10.0/FirstUI.TestApp
   ```

## 常见问题

### 错误: "无法在当前环境中启动 GUI 应用"

**原因**: 应用无法访问图形界面环境

**解决方案**:
1. 确保您在本地 macOS 桌面环境中运行
2. 不要通过 SSH 连接运行
3. 不要在 VS Code Remote 或类似的远程开发环境中运行
4. 使用 Terminal.app 或其他本地终端应用

### 错误: "Operation is not supported on this platform"

**原因**: Avalonia 无法初始化 macOS 的 Cocoa 框架

**解决方案**:
1. 检查是否已登录 macOS 桌面
2. 尝试重启 Terminal 应用
3. 如果使用 macOS 26.x (beta)，可能存在兼容性问题，考虑降级到稳定版本

### macOS 权限问题

如果应用启动但无法显示窗口，可能需要授予权限：

1. 打开 **系统设置** (System Settings)
2. 进入 **隐私与安全性** > **辅助功能**
3. 确保 Terminal.app 或您的 IDE 有相应权限

## 技术细节

- **框架**: .NET 10.0
- **UI 库**: Avalonia UI 11.3.10
- **支持平台**: macOS (arm64/x64), Windows, Linux
- **运行模式**: 需要活动的图形会话

## 项目结构

```
FirstUI.TestApp/
├── Program.cs          # 应用入口点
├── FirstUI.TestApp.csproj
└── README.md          # 本文件
```

## 示例代码

当前的测试应用显示一个简单的文本：

```csharp
var app = FirstUIBinding.CreateApp();
app.Run(() => new Column(children: [new Text("asdf")]));
```

您可以修改 `Program.cs` 来测试不同的 UI 组件。

## 调试环境信息

运行应用时，会显示环境诊断信息：
- 操作系统版本
- .NET 运行时版本
- 是否通过 SSH 连接
- 显示环境变量

这些信息有助于诊断运行问题。

## 更多帮助

如果仍然遇到问题，请检查：
- 是否使用的是 macOS 稳定版本（非 beta）
- .NET 10.0 SDK 是否正确安装
- Avalonia 依赖项是否完整

相关文档:
- [Avalonia UI 文档](https://docs.avaloniaui.net/)
- [Old8Lang 文档](../CLAUDE.md)
