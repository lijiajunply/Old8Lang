# extern 外部库调用测试用例

Old8Lang 项目（`o8package.json`）—— 运行时模式：`interpreter`（各文件实际模式见下表）。

## 内容

测试 `extern` 语句调用原生动态库的能力。

| 文件 | 运行模式 | 依赖的原生库 | 平台 |
|------|---------|------------|------|
| `test_extern_simple.old8` | `-f` | `libc.dylib` | macOS |
| `test_extern_real_call.old8` | `-f` | `libc.dylib` | macOS |
| `test_extern_compiler_debug.old8` | `-c` | `libc.dylib` | macOS |
| `test_extern_vm.old8` | `-vm` | `msvcrt.dll` / `kernel32.dll`（仅生成字节码，不实际调用） | 任意 |

## 运行

```bash
old8lang -f test_extern_simple.old8   # 解释模式
old8lang -c test_extern_compiler_debug.old8
old8lang -vm test_extern_vm.old8
```

## 备注

⚠️ **本目录刻意未接入任何 runner 脚本或 CI。**

前三个用例把原生库名硬编码为 macOS 的 `libc.dylib`，代码里（`DllPathResolver` / `NativeDllProvider`）没有平台名归一化，
因此在 Linux CI 上必然报「找不到库」。把它们放进 `InterpreterTests/`、`CompilerTests/` 这类被扫描的目录会让 CI 变红，
所以单独归档在此，按需手动运行。

若要接入 CI，需要先把库名改成平台无关形式（例如按 OS 选择 `libc.dylib` / `libc.so.6`）。
