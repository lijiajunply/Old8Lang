# 虚拟机：集合字面量作为字段初值 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「虚拟机不支持集合字面量作为字段初值」。

## 1. 现象

```old8
class C {
    public items <- {1, 2, 3}
}
```

`-vm` 在字节码编译期抛
`NotSupportedException: 虚拟机模式不支持非常量默认参数表达式: ListLiteralExpression`
（`BytecodeCompiler.EvaluateConstantExpression` 只处理标量）；`-f` / `-il` 正常。
字典/数组字面量字段同样失败。

## 2. 根因

VM 的类字段初值在 `DeclareClass` 阶段被当作常量求值塞进 `ClassMetadata`，
而 `EvaluateConstantExpression` 的 switch 只列出标量/字符串/char/bool/null。

## 3. 修复

`EvaluateConstantExpression` 增加集合字面量分支，递归求值各元素：

- `{...}` → `List<object?>`；
- `[...]` → `object[]`；
- `{key: value, ...}` → `Dictionary<object, object>`。

类型与 `NewList` / `NewArray` / `NewDict` 指令压栈的运行期表示一致。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/VirtualMachine/Collections/VMCollectionFieldInitializerTests.cs`
（1 项）：列表、字典、数组字段初值 + `Count()` 求和 + `.ToStr()` 显示。

修复前字节码编译期直接抛 `NotSupportedException`；修复后通过。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `class C { public items <- {1,2,3} }` | ✅ | ✅ | ✅（修复前 NotSupportedException） |
| 字段字典 `{"a":1,"b":2}` 的 `Count/ToStr` | ✅ | ✅ | ✅ |

### 4.3 语义备注

字段初值的集合对象在**解释器与虚拟机中由实例共享**（`a.items.Add(2)` 会影响 `b.items`），
而 IL 是每实例独立。这是既有的跨模式差异，本轮未改变；已记录到 Todo。

## 5. 遗留

解释器/VM 的共享语义是否符合预期，需要定案；若要改成每实例复制，应在构造流程里
重新求值字段初值，而不是在类元数据阶段常量求值。
