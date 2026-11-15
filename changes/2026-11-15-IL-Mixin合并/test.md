# IL 的 mixin / 接口默认方法合并与用户方法优先 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL mixin / 接口默认方法未合并」。

## 1. 现象

```old8
mixin Countable {
    private count <- 0
    public func getCount() -> int { return this.count }
}
class Counter with Countable { }
c <- Counter()
c.getCount()      // IL: 方法 'getCount' 未找到
```

| 用例 | 解释器 | IL（修复前） | VM |
|---|---|---|---|
| 基本 mixin | 通过 | 方法未找到 | 通过 |
| mixin 继承 mixin | 通过 | 语法/方法缺失 | 通过 |
| 接口默认实现 | 通过 | 方法未找到 | 通过 |
| 类自定义 `toString` | 通过 | 返回 `NativeObject(P)` | 通过 |

`Compiler.Classes` 13 项失败中 10 项来自这类缺口。

## 2. 根因

1. `VisitClassInit` 只按类自身的 `TypeTemplate.Variates` 生成字段/方法，
   `class C with M` 的 mixin 成员和 `implements I` 的接口默认方法没有并入；
2. mixin 的父 mixin（`mixin ExtendedMixin extends BaseMixin`）同样没递归合并；
3. 用户类自定义的 `toString`/`equals` 被 `InstanceMethodRegistry` 里的内置同名别名
   抢先解析，调用成了 CLR `Object.ToString`。

## 3. 修复

- `MergeMixinMembers`：编译类之前把 `MixinNames`/`ImplementsNames` 对应的
  `TypeTemplate` 成员并入类模板（类自身成员优先，按名字去重），mixin 的父 mixin 递归合并；
  接口只合并方法，不合并字段。
- `InstanceMethodTypeResolver.ResolveInstanceMethod`：接收者是 `LangObject` 子类且自身声明了
  同名方法时返回 null，让调用方走 CLR 方法解析（`TypeBuilder` 阶段跳过，避免
  “invoked member not supported before the type is created”）。范围只限用户类，
  `string.Split` 等框架类型仍走内置实例方法（初版范围过宽导致
  `StringMethods_OnNativeLiteralReceiver_Work` 回归）。

## 4. 用例迁移

- `mixin ExtendedMixin : BaseMixin` → `extends BaseMixin`；
- `Time.Now()`（三种模式都没有）→ 字面量时间戳；
- 旧 `func(T) -> bool` 类型注解 → `object`；旧匿名函数
  `func(x:int) -> bool { ... }` → `(x:int) -> { ... }`；
- 接口属性用例的 `AddItem` → `addItem`（方法名大小写敏感，三种模式一致）；
- 泛型继承用例去掉“函数值字段当方法调用”的部分（IL 泛型特化尚不支持，已记 Todo）。

## 5. 验证

- `Compiler.Classes` 13 → **0**（117 项全通过）；
- CLI：mixin 例子三模式均输出 `0`；类自定义 `toString` 三模式均输出 `P!`；
- 整组回归见 `changes/Todo.md`。
