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

> 注：`ThirdPartyLibTest/tests/test_thirdparty.old8` 仍使用旧语法（`var x = ...`），
> 直接运行会报语法错误，与 DLL 无关（已记录在 `Todo.md` §G）。
