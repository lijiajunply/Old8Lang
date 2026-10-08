# 解释模式测试用例

Old8Lang 项目（`o8package.json`）—— 运行时模式：`interpreter`。

## 内容

- 用例数量：**182** 个 `.old8` 文件
- 入口文件：`01_basic_literals.old8`

## 运行

整套用例由 `TestFiles/run_interpreter_tests.sh` 驱动（**从 `TestFiles/` 目录运行**）：

```bash
cd TestFiles && run_interpreter_tests.sh
```

单个文件可用项目脚本运行（在**本目录**下执行）：

```bash
old8lang run start          # 运行 o8package.json 中的 start 脚本
old8lang run <file.old8>    # 单文件，模式由 old8lang.runtime 决定
```

> 注意：`runtime` 只在 `old8lang run <file>` 时生效，取值为 `il`（旧值 `compiler` 仍兼容）
> （其余值一律按解释模式执行）。需要显式指定模式时，直接调用 `old8lang -f|-c|-s <file>`。

## 备注

另有 `../test_interpreter_comprehensive.sh`。用例失败判定约定：文件**最后一行**含 `error` 表示期望失败。

⚠️ `test_python_*.old8`（4 个）当前在 runner 中会**超时失败**。

用例逻辑本身是通的（脚本执行完、输出正确），但**进程不退出**，被 runner 的 10 秒超时杀掉（exit 124）。
采样挂起进程的调用栈显示：CLR 关闭阶段调试器线程执行 `PyGILState_Ensure` → `_PyThreadState_Attach` → `take_gil`，
永久阻塞在 Python 的 GIL 上。即 pythonnet 互操作在退出阶段发生 GIL 死锁，`PythonEngine.Shutdown()` 自身也会卡住。

本机环境为 pythonnet 3.0.5 + Python 3.13.9（是否为版本组合所致尚未验证）。
