# 实施计划

## 阶段 1：升级 `Old8Lang.PackageManager.Core`（`1.2.0`）
1. 完善 `VersionManager`（支持 `*`、`^`、`~`、`>=`、`<=`、`>`、`<`、`x.y.*`、`min-max`）并接入 `DefaultPackageResolver`。
2. 升级 `PackageArchiveService`：默认支持 `o8package.json`（兼容 `package.json` 与 `ProjectConfig` 字段映射），打包排除无关目录，新增 `ReadPackageMetadataFromArchiveAsync(Stream)`。
3. 升级 `DefaultPackageInstaller`：统一安装布局为 `{installPath}/{packageId}@{version}/` 并自动解压 `.o8pkg`，修复共享子依赖已安装时的重复安装失败问题。
4. 升级 `RemotePackageSource`（并新增 `IRemotePackagePublisher`）：URL 自动归一化 `/v3`，支持 `language=old8lang`，新增 `PublishPackageAsync` 与 `DeletePackageAsync`。
5. 版本号升至 `1.2.0`，打包输出至 `Old8Lang.PackageManager/artifacts/nupkg`。

## 阶段 2：补齐 `Old8Lang.PackageManager.Server`
1. 补齐 `IPackageManagementService.GetPackageStreamAsync` 与 `PackagesController.DownloadPackage` 真实文件下载。
2. 补齐 `PackageManagementService.ExtractOld8LangPackageInfoAsync`，复用 Core 的 `ReadPackageMetadataFromArchiveAsync` 真实解析 `.o8pkg` 中的 `o8package.json`；支持可选 `.sig` 签名文件上传存储。
3. 补齐 `Program.cs` 认证/授权管道、`CanUpload` 策略与 `ApiKeyAuthenticationMiddleware` 身份注入。
4. 编写端到端 HTTP 集成测试并运行 `Old8Lang.PackageManager.Tests` 全量验证。

## 阶段 3：清理 `Old8Lang` 冗余与直连 URL 导入
1. 删除 `NetworkModuleLoader.cs`，移除 `ModuleType.NetworkModule` 及 Runtime / Compiler / LanguageServer 中的分支，URL 导入改为明确报错。
2. 移除 `Old8Lang.Runtime.csproj` 冗余的 `Old8Lang.PackageManager.Core` 引用。
3. 升级 `ModuleResolver` 与 `ProjectManagement.PackageManager` 的入口解析：优先读取 `o8package.json` 的 `main` 字段。

## 阶段 4：接入本地 `.nupkg` 源并升级 `Old8Lang.ProjectSystem` & `Old8Lang.App`
1. 在 `Old8Lang/nuget.config` 配置本地源 `../Old8Lang.PackageManager/artifacts/nupkg`，升级 `Old8Lang.PackageManager.Core` 至 `1.2.0`。
2. `Old8Lang.ProjectSystem`：`ProjectConfig` 新增 `Registry` 与 `Tags` 等字段；新增 `GlobalPackageConfig` 管理 `~/.old8lang/config.json`。
3. `Old8Lang.App`：
   - 重构 `PackageService`：收口为唯一 `RemotePackageSource`（支持配置/环境变量/参数覆盖私有地址）、真实远程版本解析、远程发布/搜索/删除。
   - 升级 `PublishCommand`（支持远程推送与 `--local-only`）、`InstallCommand`、`RestoreCommand`、`ListCommand`、`RemoveCommand`。
   - 新增 `SearchCommand`、`RegistryCommand`，并更新 `Program.cs`、`BasicInfo.cs`。

## 阶段 5：双仓联调、全量测试与文档同步
1. 在 `Old8Lang.Tests` 补充单元测试与端到端联调测试（含解释/IL/VM 三模式导入已安装包、URL 导入拒绝测试、Architecture 边界测试）。
2. 启动真实本地 `Old8Lang.PackageManager.Server`（或同进程 TestServer / CLI 流程）跑通完整 `init → pack/publish → search → install → run` 联调。
3. 更新 `Docs/CLI_GUIDE.md`、`Docs/CHANGELOG.md`，并写入 `changes/2026-11-28-统一远程包管理器/test.md`。
