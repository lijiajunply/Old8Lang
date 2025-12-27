# Old8Lang LSP + VSCode 插件快速开始

## 一、快速构建和安装

### 步骤 1: 构建 Language Server

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

### 步骤 2: 构建 VSCode 扩展

```bash
# 进入扩展目录
cd vscode-old8lang

# 安装依赖
npm install

# 编译 TypeScript
npm run compile
```

### 步骤 3: 安装扩展

#### 方法 A: 调试模式（开发）

1. 在 VSCode 中打开 `vscode-old8lang` 目录
2. 按 `F5` 启动扩展开发宿主
3. 在新窗口中打开包含 `.old8` 文件的项目

#### 方法 B: 安装 VSIX（用户）

```bash
# 安装打包工具
npm install -g @vscode/vsce

# 打包扩展
vsce package

# 安装扩展
code --install-extension old8lang-0.1.0.vsix
```

## 二、验证安装

### 创建测试文件

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

### 验证功能

1. **语法高亮**：关键字应该有不同颜色
2. **错误诊断**：尝试写错误的语法，应该看到红色波浪线
3. **自动补全**：输入 `fu` 应该提示 `func` 等关键字
4. **括号匹配**：输入 `{` 应该自动补全 `}`

## 三、配置选项

在 VSCode 设置（`settings.json`）中配置：

```json
{
  // 自定义 Language Server 路径（可选）
  "old8lang.languageServer.path": "/path/to/Old8Lang.LanguageServer",

  // 调试 LSP 通信（开发用）
  "old8lang.trace.server": "verbose"
}
```

## 四、故障排查

### Language Server 无法启动

1. 检查 Language Server 是否有执行权限：
   ```bash
   chmod +x vscode-old8lang/server/Old8Lang.LanguageServer
   ```

2. 手动测试 Language Server：
   ```bash
   cd vscode-old8lang/server
   ./Old8Lang.LanguageServer
   # 应该等待输入，不报错
   ```

3. 查看 VSCode 输出面板：
   - 查看 → 输出
   - 选择 "Old8Lang Language Server"

### 语法高亮不工作

1. 确认文件扩展名为 `.old8`
2. 重新加载窗口：`Ctrl+Shift+P` → "Reload Window"
3. 检查扩展是否已启用

### 补全功能不工作

1. 确认 Language Server 正在运行
2. 检查是否有语法错误
3. 尝试手动触发：`Ctrl+Space`

## 五、下一步

### 开发 Language Server 功能

1. 完善符号表构建逻辑（`DocumentManager.BuildSymbolTable`）
2. 实现跳转定义（`DefinitionHandler`）
3. 实现查找引用（`ReferencesHandler`）
4. 实现悬停提示（`HoverHandler`）

### 改进 VSCode 扩展

1. 添加代码片段（Snippets）
2. 添加自定义命令
3. 改进语法高亮规则
4. 添加调试支持

## 六、开发工作流

### 修改 Language Server

```bash
# 修改代码后
cd Old8Lang.LanguageServer
dotnet build

# 重新发布到扩展目录
dotnet publish -c Release -r osx-arm64 --self-contained -o ../vscode-old8lang/server

# 在 VSCode 中重新加载窗口
```

### 修改 VSCode 扩展

```bash
# 启动监视模式
cd vscode-old8lang
npm run watch

# 在调试窗口中测试
# 按 Ctrl+R 重新加载扩展开发宿主
```

## 七、发布

### 发布到 VSCode 市场

1. 注册 Azure DevOps 账号
2. 创建个人访问令牌
3. 创建发布者账号
4. 发布扩展：
   ```bash
   vsce publish
   ```

### 本地分发

生成 VSIX 文件后，可以通过以下方式分发：

- 直接发送 `.vsix` 文件
- 上传到内部服务器
- 发布到 GitHub Releases

## 需要帮助？

- 查看 [完整文档](./LSP_VSCode_Documentation.md)
- 提交 Issue 到项目仓库
- 查看 [LSP 规范](https://microsoft.github.io/language-server-protocol/)
