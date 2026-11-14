# IL 原生集合实例方法桥接 — 执行清单

## 阶段 1：运行期桥接

- [x] 新增 `IlNativeCollectionBridge`（包装、解析、执行、还原、变异同步）
- [x] `IlValueBridge.Unwrap` 支持元组
- [x] 字典实参与接收者包装后填充 `Value`

## 阶段 2：编译期接入

- [x] `InstanceMethodTypeResolver`：桥接解析 + 返回类型映射
- [x] `InferInstanceMethodReturnType` 兜底桥接返回类型
- [x] `DotOperatorILHelper` 发射桥接调用与返回类型转换
- [x] `InvokeDynamicObjectMethod` 动态路径兜底

## 阶段 3：类型推断一致性

- [x] `TypeInferenceVisitor` 与 IL 栈类型一致
- [x] `Count` / `ToArray` / `Length` 等既有内置返回类型不冲突

## 阶段 4：测试与文档

- [x] 新增 IL 定向测试（纯函数 / 变异 / 字典 / 数组 / 链式）
- [x] 解释模式对照
- [x] 跑定向测试与 `Compiler/Collections` 回归
- [x] 更新 `changes/Todo.md`、`Docs/CHANGELOG.md`
- [x] 写 `test.md` 测试报告

## 遗留（不在本次完成）

- [ ] 逐个删除 63 个方法中失效的 `GenerateIlInternal` / 旧 helper
- [ ] 热点集合方法恢复专用 IL（避免每次包装与反射解析）
- [ ] 嵌套字典元素在跨模式桥接中的深度填充
- [ ] `Tuple` / `Task` / `Thread` 的同类未迁移方法
