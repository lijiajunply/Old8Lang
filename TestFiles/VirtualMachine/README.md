# 虚拟机特性测试用例

Old8Lang 项目（`o8package.json`）—— 运行时模式：`vm`。

## 内容

- 用例数量：**77** 个 `.old8` 文件
- 入口文件：`GenericTest.old8`
- 子目录：
  - `legacy/`（27 个）：原 `TestFiles/VMTests/` 的 `vm_*` 语言特性用例
  - `bytecode/`（5 个）：原 `TestFiles/BytecodeTests/` 的泛型字节码、LINQ groupBy 等用例

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

> 注意：`runtime` 只在 `old8lang run <file>` 时生效，取值为 `il`（旧值 `compiler` 仍兼容）
> （其余值一律按解释模式执行）。需要显式指定模式时，直接调用 `old8lang -f|-c|-s <file>`。

## 备注

覆盖解构赋值、异常过滤、泛型集合、闭包、切片、联合类型，以及 `vm_*` 语言特性与字节码泛型用例。
`run_vm_tests.sh` 会递归扫描本目录（含 `legacy/`、`bytecode/`）并以 `-vm` 模式运行。

⚠️ 当前全量运行有 5 个已知失败用例（均与目录合并无关，详见 `Todo.md` §G）：
`DestructuringAssignmentTest.old8`、`vm_gap_6_error_position.old8`、
`legacy/control_flow.old8`、`legacy/vm_operator_overload.old8`、`legacy/vm_decorator.old8`。
