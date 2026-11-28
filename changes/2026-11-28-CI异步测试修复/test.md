# 测试记录：CI 异步 finally 用例修复

## 环境

- 本地：macOS arm64，.NET SDK 10.0.404
- 容器：`mcr.microsoft.com/dotnet/sdk:10.0`（Linux arm64），另建安装 `python3`(3.12.3) 的镜像
  用于解释器套件
- CI：ubuntu-latest + .NET 10.0.100 + Python 3.12，文中容器命令与之对应

## 复现（修复前）

xUnit 默认同步上下文会把 `await` 续体排进测试自己的队列；脚本 `Thread.Sleep` 阻塞测试线程后
没人泵送队列，断言先于 finally 执行。

```bash
docker run --rm -e DOTNET_PROCESSOR_COUNT=1 ... \
  dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj -c Release --no-build \
  --filter 'FullyQualifiedName~AsyncFinallyTests'
# => Failed: 1, Passed: 1（期望 1 实际 0，与 CI 报错一致）
```

`DOTNET_PROCESSOR_COUNT>=2` 时不复现，说明是调度/负载相关，而不是 Linux 平台语义差异。

## 修复后

| 验证项 | 命令 | 结果 |
|---|---|---|
| macOS 单用例 | `dotnet test -c Release --filter FullyQualifiedName~AsyncFinallyTests` | 2 通过 |
| 容器必现条件 | `DOTNET_PROCESSOR_COUNT=1` + 同上 | 2 通过 |
| 容器全量单测 | `dotnet test -c Release`（含 Python 的镜像） | 7297 通过 / 3 跳过 / 0 失败 |
| 语法套件 | `bash run_syntax_tests.sh` | 退出码 0 |
| 解释器套件 | `bash run_interpreter_tests.sh`（`PYTHONNET_PYDLL` 指向 3.12 动态库） | 退出码 0 |
| IL 套件 | `bash run_compiler_tests.sh` | 退出码 0 |
| VM 套件 | `bash run_vm_tests.sh` | 退出码 0 |
| 打包预检 | `dotnet publish Old8Lang.App -c Release -r linux-x64 --self-contained -o /tmp/dist-linux` | 成功 |

## GitHub Actions 实测

Run [46123780646](https://github.com/lijiajunply/Old8Lang/actions/runs/46123780646)，提交 `9dfecabf`：

- `test` 作业 20m13s 通过：unit / syntax / interpreter / IL / VM 全部 ✓
- `package` 四个 RID（linux-x64、win-x64、osx-x64、osx-arm64）全部 ✓
- `release` 作业 ✓，已发布 <https://github.com/lijiajunply/Old8Lang/releases/tag/v1.0.0>，
  产物 `old8lang-{linux-x64,win-x64,osx-x64,osx-arm64}.tar.gz` 齐全
