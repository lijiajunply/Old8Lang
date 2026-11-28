# CI 修复：Tag release 单元测试在少核机器上必现失败

## 问题

GitHub Actions `Tag release` 工作流在 `Run unit tests` 阶段失败，仅剩 2 个用例：

- `Old8Lang.Tests.Compiler.Async.AsyncFinallyTests.FinallyRunsOnceAfterRealSuspension`
- `Old8Lang.Tests.Compiler.Async.AsyncFinallyTests.FinallyRunsOnceWhenExceptionPropagates`

失败信息都是脚本内断言「期望 1/2，实际 0」，即 `finally` 里的计数器在断言时还没自增。
本地 macOS（Debug/Release）与多核 Linux 容器均稳定通过，只有 CI 少核环境失败。

## 根因

xUnit 会在每个测试线程上安装并发同步上下文（`MaxConcurrencySyncContext`）。IL 模式
编译产物里的 `await` 通过 `AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted` 捕获该上下文，
续体被排进 xUnit 的队列；而测试脚本用 `Thread.Sleep(400)` 阻塞测试线程后就没人泵送队列，
续体只能等测试方法返回，断言必然早于 `finally` 执行。核数越少、并行用例越多越容易命中。

复现证据：容器里设置 `DOTNET_PROCESSOR_COUNT=1` 后该用例必现失败；≥2 核时不复现。

## 范围

- 只改测试的等待方式，不改异步状态机实现：单跑（无同步上下文）时语义正确，
  `finally` 确实只执行一次。
- 不改 CI 工作流本身。

## 修复

1. 调用编译产物前清空当前线程的 `SynchronizationContext`，还原控制台宿主的真实条件
   （真实宿主没有同步上下文），执行完恢复。
2. 脚本内把固定 `Thread.Sleep(400)` 改成有界轮询（最多 5 秒，每 10ms 检查一次计数器），
   不再依赖某一次调度的耗时。

## 验收标准

- 容器内 `DOTNET_PROCESSOR_COUNT=1` 单用例通过（修复前必现失败）。
- 容器内全量单元测试通过（除容器缺 Python 运行时的既有环境项）。
- `TestFiles` 四个 shell 套件通过。
- 推 `v1.0.0` tag 后 GitHub Actions 全绿，并产出 GitHub Release 与 4 个平台包。
