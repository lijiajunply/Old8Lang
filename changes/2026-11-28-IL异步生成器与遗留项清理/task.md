# Task：IL 异步生成器非阻塞挂起、遗留项清理与文档校正

- [x] 建立 change 留痕文档（`spec.md`、`design.md`、`task.md`）
- [x] 重构 `IlAsyncYieldEmitter` / `IlAsyncGeneratorStream` 为非阻塞 `Task` 握手
- [x] 将 `YieldStatement` 接入 `AsyncStateMachineGenerator` 挂起点与受保护区域派发
- [x] 补充 IL 异步生成器非阻塞并发暂停测试与性能压力测试
- [x] 预登记 `TypeBuilder` 的 `MethodBuilder` 并在 `DotOperatorILHelper` / `TypeInferenceVisitor` 恢复直接 `callvirt`
- [x] 补充类内 `this.method()` 直接 `callvirt` 回归与 IL 指令验证测试
- [x] 修复 VM 类字段集合初值深拷贝与导入类字段默认初始化，并补三模式回归测试
- [x] 修复 `EnumLangValue.TypeToString()` 与 `TypeFunction` / `TypeOf` 三模式语言类型名统一，并补回归测试
- [x] 校正 `Docs/MODE_SUPPORT.md`、`Docs/CHANGELOG.md` 与 `changes/Todo.md`
- [x] 运行全量单元测试、Architecture 测试、性能并发测试与脚本测试，输出 `test.md`
