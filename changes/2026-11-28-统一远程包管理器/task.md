# 任务清单

- [x] 1. 建立 `changes/2026-11-28-统一远程包管理器/` 规范、设计与计划文档
- [x] 2. 升级 `Old8Lang.PackageManager.Core`（版本解析、归档元数据兼容、自动解压安装、远程发布与 URL 归一化）并打包本地 `1.2.0` `.nupkg`
- [x] 3. 补齐 `Old8Lang.PackageManager.Server`（真实下载、真实 `.o8pkg` 元数据解析、签名上传、认证授权中间件与 `CanUpload` 策略）
- [x] 4. 新增并运行 `Old8Lang.PackageManager.Tests` 单元测试与端到端 HTTP 集成测试
- [x] 5. 在 `Old8Lang` 中移除 `NetworkModuleLoader` / `ModuleType.NetworkModule`，移除 Runtime 冗余依赖，增强 `o8package.json` `main` 入口解析
- [x] 6. 配置 `Old8Lang/nuget.config` 接入本地 `1.2.0` `.nupkg`，升级 `Old8Lang.ProjectSystem` 与 `Old8Lang.App`（重构 `PackageService`，升级 `publish`/`install`/`restore`/`list`/`remove`，新增 `search`/`registry`）
- [x] 7. 跑通双仓测试、Architecture 测试与真实 Server 端到端联调，更新文档并完成 `test.md`
