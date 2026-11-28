# 测试报告：统一远程包管理器（Old8Lang.PackageManager）

日期：2026-11-28

## 1. 测试范围

1. `Old8Lang.PackageManager` 仓库单元测试 + 真实 `WebApplicationFactory<Program>` HTTP 端到端集成测试（打包、鉴权拒绝、上传、搜索、SemVer 解析、传递依赖自动下载解压、卸载、远程删除）。
2. 本地 `.nupkg` 打包与 `Old8Lang.sln` 全量构建（22 个程序集）。
3. `Old8Lang.Tests`：新增 `UnifiedRemotePackageManagerTests`（禁止 HTTP/HTTPS URL 导入、唯一远程仓库优先级解析与 `/v3` 归一化、带自定义 `main` 的解压包在解释/IL/VM 三模式导入）、`Old8Lang.Tests.Architecture` 程序集边界测试及全量 `Modules` 回归测试。
4. 真实双仓进程级端到端联调：启动本地 `Old8Lang.PackageManager.Server`，使用 `Old8Lang.App` CLI 跑通 `registry set/get/test` → `publish` → `search` → `install` → `list` → `-f`/`-il`/`-vm` 运行 → `remove`。

## 2. 执行命令与实测结果

| # | 验证项 | 命令 | 结果 |
|---|---|---|---|
| 1 | `Old8Lang.PackageManager` 全量测试（含 Core 单测与 HTTP E2E） | `dotnet test Old8Lang.PackageManager.sln --nologo` | **通过**：114/114（0 失败，0 跳过） |
| 2 | 本地打包 `Old8Lang.PackageManager.Core 1.2.0` | `dotnet pack Old8Lang.PackageManager.Core.csproj -c Release -o artifacts/nupkg --nologo` | **通过**：生成 `Old8Lang.PackageManager.Core.1.2.0.nupkg` |
| 3 | `Old8Lang` 解决方案全量构建（本地 `.nupkg` 源） | `dotnet build Old8Lang.sln --nologo` | **通过**：全部 22 个项目 0 错误 |
| 4 | 新增包管理单测 + 架构边界测试 | `dotnet test Old8Lang.Tests.csproj --filter "FullyQualifiedName~UnifiedRemotePackageManagerTests\|FullyQualifiedName~Old8Lang.Tests.Architecture" --nologo` | **通过**：77/77（0 失败，0 跳过） |
| 5 | 全量模块与架构测试回归 | `dotnet test Old8Lang.Tests.csproj --filter "FullyQualifiedName~Old8Lang.Tests.Architecture\|FullyQualifiedName~Modules" --nologo` | **通过**：639/639（首次回归通过；后续修正 `ModuleResolver` 目录基路径与 `ModuleSystemService` 包名登记后，新单测与架构 77/77 全过） |
| 6 | 真实 Server + CLI 双仓端到端联调 | 本地启动 `Old8Lang.PackageManager.Server`（端口 15125），隔离 `HOME` 依次执行 `registry set/test`、`publish --no-sign --force`、`search`、`install`、`list`、`-f`/`-il`/`-vm`、`remove --yes` | **通过**：`-f` / `-il` / `-vm` 均输出 `42`，`remove` 成功卸载 `~/.old8lang/packages/demo_math@1.0.0` |

## 3. 测试期间发现并修复的额外问题

1. **`ModuleResolver.GetPackageSearchPaths` 目录基路径偏差**：VM 的 `ModuleLoader` 传入目录路径 `baseDirectory` 而非文件路径，原逻辑直接对目录做 `Path.GetDirectoryName` 会向上偏一层目录。已修复为先判断 `Directory.Exists`。
2. **`ModuleSystemService` 第三方包模块名登记偏差**：第三方包入口文件为 `src/index.old8` 时，原逻辑按入口文件名注册为 `index` 而非包名 `demo_calc`。已修复为优先使用 `resolution.PackageInfo.PackageName`。
3. **`PackageManagementService.UploadPackageAsync` 字段覆盖缺陷**：`request.Description`/`request.Author` 默认为空字符串导致 `??` 不会回退到 `o8package.json` 的值。已改为 `string.IsNullOrWhiteSpace` 判定。

## 4. 结论

验收标准全部满足：`Old8Lang.PackageManager` 已打通并确立为 `Old8Lang` 的唯一远程包管理器（协议唯一为 `Old8Lang.PackageManager.Server`，支持切换自建私有实例），直连 HTTP/HTTPS URL 导入已彻底移除，本地 `.nupkg` 联调闭环验证通过。
