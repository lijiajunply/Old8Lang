# 统一远程包管理器架构与接口设计

## 1. 协议与唯一远程源模型

- **唯一协议**：`Old8Lang.PackageManager.Server` HTTP v3 API：
  - `GET  /v3/index.json`（连通性检查 / 服务发现）
  - `GET  /v3/search?q=...&language=old8lang&prerelease=...`（搜索）
  - `GET  /v3/package/{id}?language=old8lang`（详情与版本列表）
  - `GET  /v3/package/{id}?version={v}&language=old8lang`（指定版本元数据）
  - `GET  /v3/package/{id}/{version}/download`（下载 `.o8pkg`）
  - `POST /v3/package`（`multipart/form-data` 上传 `.o8pkg` 与可选 `.sig`）
  - `DELETE /v3/package/{id}/{version}`（撤回/删除包）
- **URL 归一化**：`RemotePackageSource` 自动去除末尾 `/` 及末尾重复的 `/v3`，避免拼出 `/v3/v3/...`。
- **远程地址解析优先级（单一有效远程源）**：
  1. CLI 参数 `--registry <url>`
  2. 环境变量 `OLD8LANG_REGISTRY`
  3. 项目 `o8package.json` 中的 `registry`（兼容读取旧 `sources` 中第一个启用的 HTTP/HTTPS 源）
  4. 全局配置 `~/.old8lang/config.json` 中的 `registry`
  5. 默认官方地址：`https://package.old8lang.site`
- **API Key 解析优先级**：
  1. CLI 参数 `--api-key <key>`
  2. 环境变量 `OLD8LANG_API_KEY`
  3. 全局配置 `~/.old8lang/config.json` 中的 `apiKey`

## 2. 元数据格式统一（`o8package.json`）

- 默认元数据文件名统一为 `o8package.json`（兼容读取旧 `package.json`）。
- `PackageArchiveService`（`Old8Lang.PackageManager.Core`）同时支持解析：
  - 标准 `Package` JSON（`id` / `version` / `dependencies`）
  - Old8Lang `ProjectConfig` JSON（`projectName` / `version` / `description` / `author` / `license` / `main` / `tags` / `references`），其中：
    - `id` 缺失时自动回退读取 `projectName`
    - `dependencies` 缺失时自动将 `references`（排除 `isDevDependency == true`）转换为 `PackageDependency`
- `PackageArchiveService` 提供 `ReadPackageMetadataFromArchiveAsync(Stream archiveStream)`，由 `Old8Lang.PackageManager.Server` 的 `PackageManagementService.ExtractOld8LangPackageInfoAsync` 直接复用，保证打包端与服务端解析逻辑完全一致。
- 打包（`PackAsync`）自动忽略 `.git`、`dist`、`packages`、`bin`、`obj`、`node_modules` 及输出文件自身，防止递归打包。

## 3. 安装与运行时目录布局统一

- 统一安装目录结构为：`{installPath}/{packageId}@{version}/`
  - 目录内包含解压后的完整包文件（`o8package.json`、源码等）
  - 目录内保留 `{packageId}.{version}.metadata.json`（含校验和、依赖与来源元数据）
  - 可选保留原始 `.o8pkg` 或清理（保留 `{packageId}.{version}.o8pkg` 于同目录以兼容 `LocalPackageSource` 与签名校验）
- `DefaultPackageInstaller`：
  - 安装时自动通过 `PackageArchiveService.UnpackAsync` 将下载的 `.o8pkg` 解压到 `{installPath}/{packageId}@{version}/`（使用临时目录原子替换，防止半解压状态）。
  - 递归安装子依赖时，若子依赖已安装则视为跳过成功，不中断主安装流程。
  - `IsPackageInstalledAsync`、`GetInstalledPackagesAsync`、`UninstallPackageAsync` 统一支持 `{packageId}@{version}` 布局（并兼容读取旧的 `{packageId}/{version}` 布局）。
- `Old8Lang.Runtime`（`ModuleResolver` 与 `ProjectManagement.PackageManager`）：
  - `FindPackageEntryFile` 优先读取包目录内 `o8package.json` 的 `main` 字段（如 `src/index.old8`），再回退到 `index.old8`、`{packageName}.old8`、`main.old8`。
  - 支持直接导入由 `DefaultPackageInstaller` 安装的 `{packagesDir}/{packageId}@{version}/`。

## 4. SemVer 版本范围解析

- 在 `Old8Lang.PackageManager.Core.Services.VersionManager` 中补齐完整范围解析：
  - `*`、`^x.y.z`、`~x.y.z`、`>=`、`<=`、`>`、`<`、`x.y.*`、`min-max`、精确版本。
- `DefaultPackageResolver` 统一委托 `VersionManager.IsVersionInRange` 与 `GetLatestVersionInRange`，选出满足约束的最高版本。
- `PackageService`（`Old8Lang.App`）移除硬编码 `"1.0.0"`，通过包源查询可用版本并用 `VersionManager.GetLatestVersionInRange` 选出具体版本。

## 5. Server 端补齐与鉴权管道

- `IPackageManagementService` 新增 `Task<Stream?> GetPackageStreamAsync(string packageId, string version)`，由 `PackageManagementService` 转发到 `IPackageStorageService.GetPackageAsync`。
- `PackagesController.DownloadPackage` 调用 `packageService.GetPackageStreamAsync(id, version)` 返回真实文件流。
- `PackageUploadRequest` 新增可选 `IFormFile? SignatureFile`，上传时一并存储 `.o8pkg.sig` 供签名验证使用。
- `Program.cs`：
  - 注册 `AddAuthorization` 并配置 `"CanUpload"` 策略（允许已通过 Cookie/API Key 认证的请求，或在 `ApiOptions.RequireApiKey == false && !ApiOptions.RequireAuthentication` / `Authentication:AllowGuestUpload == true` 时放行）。
  - 在 `ApiKeyAuthenticationMiddleware` 验证通过后构造包含 `scope` 声明的 `ClaimsPrincipal` 写入 `context.User`。
  - 在请求管道中接入 `UseAuthentication()`、`UseMiddleware<ApiKeyAuthenticationMiddleware>()`、`UseAuthorization()`。

## 6. 移除直连 URL 导入（`NetworkModuleLoader`）

- 删除 `Old8Lang.Runtime/ModuleSystem/Loading/NetworkModuleLoader.cs`。
- 移除 `ModuleType.NetworkModule`。
- `ModuleResolver.ResolveModule` 遇到 `http://` / `https://` 直接返回失败结果，错误原因明确指出不支持 URL 直连导入，需使用 `old8lang install <包名>`。
- 清理 `ModuleSystemService`、`CompilerVisitor.Statements2.cs`、`DocumentLinkHandler.cs` 中的 `NetworkModule` 分支。

## 7. 受影响的程序集与边界检查

- `Old8Lang.PackageManager.Core`（版本升级 `1.1.2` → `1.2.0`）
- `Old8Lang.PackageManager.Server`
- `Old8Lang.PackageManager.Tests`
- `Old8Lang.ProjectSystem`（新增 `Registry`、`GlobalPackageConfig`）
- `Old8Lang.Runtime`（移除冗余 `PackageManager.Core` 引用、移除 `NetworkModuleLoader`、增强 `FindPackageEntryFile`）
- `Old8Lang.Compiler`、`Old8Lang.LanguageServer`（移除 `NetworkModule` 分支）
- `Old8Lang.App`（重构 `PackageService`、改造 `publish`/`install`/`restore`/`list`/`remove`，新增 `search`/`registry` 命令）
- `Old8Lang.Tests`（验证 `Architecture` 与新增模块/包管理测试）
