# TestProjects 夹具

这些是包管理 / 项目结构的**手动测试夹具**，不接入 xUnit 或 CI，按需在各自目录运行。

| 夹具 | 用途 |
|------|------|
| `PackageIntegrationTest/` | 包导入、依赖、别名（Logger / HttpClient） |
| `ThirdPartyLibTest/` | 第三方包 + `OldLib` 的 `extern` 调用 |
| `CompatibilityTest/` | 兼容性冒烟 |
| `Environment/` | `--env` 环境配置 |

## OldLib 的 DLL

`packages/OldLib/dll/` 下的 `Old8LangLib.dll`、`Old8Lang.NetLib.dll` **不再入库**。
需要「包自带 DLL」的布局时运行：

```bash
bash TestFiles/TestProjects/prepare-dlls.sh          # Debug
bash TestFiles/TestProjects/prepare-dlls.sh Release  # Release
```

不生成也能运行：`DllPathResolver` 会回退到构建输出（`Old8Lang.App/bin/<Config>/net10.0/`）。

> `packages/OldLib/index.old8` 直接声明 `Old8LangLib` 的 `extern`，`import "OldLib"` 后
> MathLib / Time / CryptoLib / FileLib / OS / Csv / Terminal 的静态方法进入当前作用域；
> `Async` / `CollectionLib` / `Json` 模块因对应 C# 类已移除而删除。
