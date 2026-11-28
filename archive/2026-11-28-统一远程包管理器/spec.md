# 统一远程包管理器（Old8Lang.PackageManager）需求规范

## 1. 背景与目标

当前 `Old8Lang` 与 `Old8Lang.PackageManager` 虽已通过 `Old8Lang.PackageManager.Core (1.1.2)` 建立初步依赖，但存在多处断点（CLI 误将远程 URL 传入 `LocalPackageSource`、Server 下载接口和包解析为占位空实现、安装不解压导致 Runtime 无法 `import`、`o8package.json` 与 `Package` 模型不一致等），同时运行时还保留了独立于包管理器的 `NetworkModuleLoader`（`import "https://..."`）。

本次改造的目标是：**将 `Old8Lang.PackageManager` 确立为 Old8Lang 的唯一远程包管理器**，打通“打包 → 发布 → 搜索 → 解析 → 下载安装 → 运行时导入”完整闭环。

## 2. 范围与约束

### 2.1 范围内
1. **唯一远程协议 + 可切换私有实例**
   - 协议唯一绑定 `Old8Lang.PackageManager.Server` v3 HTTP API。
   - 默认指向官方源 `https://package.old8lang.site`，允许通过 CLI 选项、环境变量（`OLD8LANG_REGISTRY`）、项目配置（`o8package.json` 的 `registry`）或全局配置（`~/.old8lang/config.json`）切换至自建的 `Old8Lang.PackageManager.Server` 实例。
2. **移除直连 URL 模块导入**
   - 直接移除 `NetworkModuleLoader` 及 `ModuleType.NetworkModule`，不再支持 `import "http(s)://..."`；遇到 URL 导入时给出清晰错误提示，引导使用包管理器安装。
3. **本地 `.nupkg` 联调**
   - 通过本地 NuGet 源（`Old8Lang/nuget.config` 指向 `../Old8Lang.PackageManager/artifacts/nupkg`）联调新版 `Old8Lang.PackageManager.Core (1.2.0)`。
4. **全链路打通**
   - `Old8Lang.PackageManager.Core`：统一元数据解析、自动解压安装、SemVer 范围匹配、远程发布/删除与 URL 归一化。
   - `Old8Lang.PackageManager.Server`：补全下载流、补全 `.o8pkg` 元数据提取、接入认证/授权中间件与 API Key 校验。
   - `Old8Lang.Runtime` / `Old8Lang.ProjectSystem` / `Old8Lang.App`：统一包目录布局与 `main` 入口解析、移除冗余依赖、修复 `PackageService` 并补齐 `publish`（远程推送）、`search`、`registry` 命令。

### 2.2 明确不做
- 不改动 `Old8Lang.PackageManager/frontend` Vue 前端页面（保持现有 API 兼容）。
- 不发布到公网 NuGet.org（本次仅完成本地 `.nupkg` 打包与双仓联调）。
- 不引入第三方远程包协议（如 NuGet v3 外部源、Git 仓库直装等）。

## 3. 验收标准

1. `Old8Lang.PackageManager` 解决方案构建通过，且单元测试 + 真实 HTTP 端到端集成测试（打包 → 上传 → 搜索 → 查版本 → 下载安装解压 → 删除）全部通过。
2. `Old8Lang.PackageManager.Core` 成功打包为本地 `1.2.0` `.nupkg`，`Old8Lang` 通过本地源正常还原并构建全部 22 个项目。
3. `Old8Lang` 中 `import "https://..."` 被彻底移除，解析返回明确错误；`Old8Lang.Tests.Architecture` 架构边界测试通过；现有模块测试与新增包管理集成测试通过。
4. 通过 `Old8Lang` 安装本地启动的 `Old8Lang.PackageManager.Server` 上的测试包后，Old8Lang 解释模式 / IL 模式 / 虚拟机模式均能正确 `import` 并执行该包导出的函数。
