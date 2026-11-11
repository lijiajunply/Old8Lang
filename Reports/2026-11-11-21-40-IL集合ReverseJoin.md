# IL 集合 `Reverse` / `Join` 测试报告

- 时间：2026-11-11 21:40（Asia/Shanghai）
- 范围：原生列表接收者的 `Reverse()`、`Join()` 与带分隔符的 `Join(separator)`
- 命令：`dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter 'FullyQualifiedName~ListReverseMethod_CompilesAndExecutesCorrectly|FullyQualifiedName~ListJoinMethod_WithSupportedArity_CompilesAndExecutesCorrectly' --no-restore`

## 结果

- 通过：2
- 失败：0
- 跳过：0

`Reverse()` 会返回可继续调用 `Count()` 的原生列表；`Join()` 支持默认分隔符及一个显式分隔符。历史用例 `ListJoinMethod_CompilesAndExecutesCorrectly` 拼出了 API 不支持的三个实参，已记入待办，未作为本项通过依据。
