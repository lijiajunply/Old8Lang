# 更新日志

## [0.3.0] - 2026-10-24

### 新增功能
- **代码片段**：`snippets/old8lang.json`，覆盖函数、异步函数、类、条件、循环、异常处理、
  switch、import、Lambda、await 和 PrintLine
- **任务支持**：注册 `old8lang` 任务类型，提供解释模式、IL 模式、虚拟机模式和语法检查任务
- **命令**：`Old8Lang: 运行当前文件`、`Old8Lang: 语法检查当前文件`、`Old8Lang: 重启 Language Server`
- **代码镜头消费**：通过 `old8lang.showReferences` 命令显示声明上的引用数量与引用列表
- **诊断级别过滤**：`old8lang.diagnostics.level`
- **配置校验**：服务器/编译器路径不存在、枚举取值无效时给出警告

### 改进
- Language Server 启动路径自动探测：可执行文件 → `.dll` → 源码项目 `dotnet run`
- 服务器以工作区根目录作为工作目录，保证工作区索引可用
- 用 esbuild 打包扩展及其依赖，VSIX 不再需要携带 `node_modules`
- 打包脚本会先 `dotnet publish` Language Server 到 `server/`，再执行 `vsce package`
- 配置项补充 `languageServer.project`、`compiler.path`、`compiler.project`、`compiler.mode`、`dotnet.path`

### 已知限制
- 暂无 Debug Adapter（DAP）；现有调试入口是 Language Server 的自定义请求和 VM 交互式终端调试器

## [0.2.0] - 2026-01-02

### 新增功能
- **查找引用**：支持查找符号的所有引用位置（Shift+F12）
  - 可以选择是否包含定义位置
  - 支持函数、类、变量的引用查找
- **重命名符号**：支持重命名符号并自动更新所有引用（F2）
  - 支持函数重命名
  - 支持类重命名
  - 支持变量重命名
  - 自动更新所有引用位置

### 改进
- 改进悬停提示的文档注释显示
- 优化符号查找性能

### 已知限制
- 不支持跨文件查找引用和重命名
- 类成员（属性、方法）暂不支持查找引用和重命名

## [0.1.0] - 2025-12-XX

### 初始发布
- 语法高亮支持
- 智能补全（关键字和符号）
- 跳转到定义（F12）
- 悬停提示
- 基础的错误诊断
- Language Server 集成
