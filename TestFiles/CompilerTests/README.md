# IL 模式测试用例

Old8Lang 项目（`o8package.json`）—— 运行时模式：`compiler`。

## 内容

- 用例数量：**161** 个 `.old8` 文件
- 入口文件：`01_basic_literals.old8`

## 运行

整套用例由 `TestFiles/run_compiler_tests.sh` 驱动（**从 `TestFiles/` 目录运行**）：

```bash
cd TestFiles && run_compiler_tests.sh
```

单个文件可用项目脚本运行（在**本目录**下执行）：

```bash
old8lang run start          # 运行 o8package.json 中的 start 脚本
old8lang run <file.old8>    # 单文件，模式由 old8lang.runtime 决定
```

> 注意：`runtime` 只在 `old8lang run <file>` 时生效，取值为 `il`（旧值 `compiler` 仍兼容）
> （其余值一律按解释模式执行）。需要显式指定模式时，直接调用 `old8lang -f|-c|-s <file>`。

## 备注

另有 `run_comprehensive_compiler_tests.sh` 会逐个文件编译并把结果写入 `Reports/<时间戳>-编译器全面测试.md`（该脚本依赖 stdout 中出现 `[编译错误]` / `编译成功`，且自身不会以非零码退出）。

⚠️ 本套用例当前**并非全绿**（抽样前 15 个文件有 5 个失败）。
`TupleTest.old8` 是已知的 IL 模式功能缺口：命名元组字段访问（`t_named.x`）在 `-il` 下报
「类型 ValueTuple`2 没有属性 x」，但在 `-f` 下正常。
