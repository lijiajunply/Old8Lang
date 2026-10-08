# 调试器测试用例

Old8Lang 项目（`o8package.json`）—— 运行时模式：`interpreter`。

## 内容

- 用例数量：**4** 个 `.old8` 文件
- 入口文件：`test_breakpoints.old8`

## 运行

本目录**未接入任何 runner 脚本或 CI**，需手动运行。

单个文件可用项目脚本运行（在**本目录**下执行）：

```bash
old8lang run start          # 运行 o8package.json 中的 start 脚本
old8lang run <file.old8>    # 单文件，模式由 old8lang.runtime 决定
```

> 注意：`runtime` 只在 `old8lang run <file>` 时生效，且仅识别 `compiler`
> （其余值一律按解释模式执行）。需要显式指定模式时，直接调用 `old8lang -f|-c|-s <file>`。

## 备注

⚠️ **刻意未接入 runner 脚本与 CI**。

本目录的用例需要调试器会话（`old8lang debug-start <file.old8>`）才能正确执行，用普通 `-f` 模式跑会误判
（`test_callstack.old8`、`test_variables.old8` 在 `-f` 下失败）。目前仅被 `Docs/DEVELOPER_TOOLS.md` 作为示例引用。
