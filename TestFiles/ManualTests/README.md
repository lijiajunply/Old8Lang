# 手动测试用例（ManualTests）

本目录的用例**刻意不接入任何 runner 脚本或 CI**，需要按下面的方式手动运行。
用例文件仍遵守统一约定：扩展名 `.old8`、注释用 `//`、期望失败时最后一行写 `error`。

## 子目录

| 目录 | 用例数 | 用途 | 运行方式 |
|------|--------|------|----------|
| `Debugger/` | 4 | 调试器断点、调用栈、变量监视 | `old8lang debug-start <file.old8>` |
| `Extern/` | 4 | `extern` 调用原生动态库 | `old8lang -f` / `-il` / `-vm <file.old8>` |

## 为什么单独放在这里

- **`Debugger/`**：需要调试器会话才能正确执行，用普通 `-f` 模式跑会误判
  （`test_callstack.old8`、`test_variables.old8` 在 `-f` 下失败）。
- **`Extern/`**：前三个用例把原生库名硬编码为 macOS 的 `libc.dylib`，代码里
  （`DllPathResolver` / `NativeDllProvider`）没有平台名归一化，在 Linux CI 上必然报「找不到库」。
  `test_extern_vm.old8` 只验证字节码生成，不实际调用。

若要接入 CI，需要先把库名改成平台无关形式（按 OS 选择 `libc.dylib` / `libc.so.6`），
并给调试器用例准备专用的 runner。
