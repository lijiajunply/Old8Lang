# IL 集合注解元素类型检查 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「集合注解元素类型不匹配：IL 不报错」。

## 1. 现象

```old8
func test() -> int {
    items:list<int> <- {1, "hello", 3}
    return items[0]
}
```

| 模式 | 结果 |
|---|---|
| 解释器 | 接受（返回 1） |
| IL | 接受（返回 1） |
| 虚拟机 | 编译期报错「类型不匹配: 期望 list<int>」 |

`Compiler.Types` 里 5 个 `Compile_*TypeMismatch_ThrowsCompilerException` 因此失败。
此外 `func getNumbers() -> list<int> { return {1, 2, 3} }` 在 IL 报「返回类型不匹配」，
嵌套 `list<list<int>>` 混合元素也不报错。

## 2. 根因

1. `CollectionTypeValidator` 对 IL 的 `ScalarLiteralExpression`（标量字面量）不认识，
   `GetExpressionTypeName` 退化成 `any`，`ValidateTypeCompatibility("int","any")` 放行；
2. 嵌套字面量同样只返回 `list`/`array`，无法比较 `list<int>` 与 `list<string>`；
3. `TypeInferenceVisitor` 把注解 `list<int>` 解析成 CLR `List<int>`、`array<int>` 解析成
   `int[]`，而 IL 字面量生成的是 `List<object>` / `object[]`，返回值类型不一致；
4. 泛型约束 `comparable` 不是 CLR 类型名，`GetDotNetType` 返回 null，int 被判不满足约束。

## 3. 修复

- `GetExpressionTypeName` 识别 `ScalarLiteralExpression`（int/double/string/bool/char/null），
  并递归描述嵌套 `ListLiteralExpression` / `ArrayLiteralExpression` / `DictionaryLiteralExpression`；
- 新增 `CollectionShapesCompatible`：同基集合类型（list/array/dict）但元素类型不同时判为不匹配，
  实际类型含 `any`（推断不完整）时跳过；
- `TypeInferenceVisitor` 把 `list<X>` / `array<X>` / `dictionary<X,Y>` 统一解析为
  `List<object>` / `object[]` / `Dictionary<object, object>`，与 IL 运行期容器一致；
- `GenericConstraintValidator.GetDotNetType` 增加 `comparable`/`icomparable` → `IComparable`。

用例侧：`Run_MixedTypeCollections` 去掉「带注解混合列表也允许」的断言（IL 现在编译期拒绝）；
`GenericInheritance` 用 `init` 构造并显式写 `Container<int>(42)`；
`BasicGenericInference` 改为显式注解的 int/double 两个函数。

## 4. 验证

### 4.1 CLI

| 表达式 | 解释器 | IL（修复后） | 虚拟机 |
|---|---|---|---|
| `items:list<int> <- {1, "hello", 3}` | 接受 | 编译期「列表元素类型不匹配」 | 编译期报错 |
| `func getNumbers() -> list<int> { return {1,2,3} }` | 正常 | 正常（54 -> 无返回类型错误） | 正常 |
| `matrix:list<list<int>> <- {{1,2},{"a","b"}}` | 接受 | 编译期「元素类型不匹配」 | 接受 |
| `where T: comparable` + `int` | 正常 | 正常 | 正常 |

## 5. 结果

`Compiler.Types` 10 → 0 失败（106 项全通过）。整组回归数字见 `changes/Todo.md`。

## 6. 遗留

解释器仍接受带注解的混合元素集合（模式矩阵记录为「接受」），本次未改解释器行为。
