# 虚拟机语言特性测试用例

Old8Lang 项目（`o8package.json`）—— 运行时模式：`vm`。

## 内容

- 用例数量：**27** 个 `.old8` 文件
- 入口文件：`vm_basic_test.old8`

## 运行

整套用例由 `TestFiles/run_vm_tests.sh` 驱动（**从 `TestFiles/` 目录运行**）：

```bash
cd TestFiles && run_vm_tests.sh
```

单个文件可用项目脚本运行（在**本目录**下执行）：

```bash
old8lang run start          # 运行 o8package.json 中的 start 脚本
old8lang run <file.old8>    # 单文件，模式由 old8lang.runtime 决定
```

> 注意：`runtime` 只在 `old8lang run <file>` 时生效，且仅识别 `compiler`
> （其余值一律按解释模式执行）。需要显式指定模式时，直接调用 `old8lang -f|-c|-s <file>`。

## 备注

`vm_*` 系列语言特性用例，由 `run_vm_tests.sh` 以 `-vm` 模式运行（与 `TestFiles/VirtualMachine/` 是两批不重复的用例）。

⚠️ 已知 2 个失败用例：`control_flow.old8`、`vm_decorator.old8`。
