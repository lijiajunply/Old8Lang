# IL 循环内动态结果赋给已有变量 —— 测试报告

日期：2026-11-18
关联待办：`changes/Todo.md`「IL 动态调用结果赋给已有变量后循环内不更新」。

## 1. 现象

```old8
func reduce(list:list, initial:int, accumulator:object) -> int {
    result <- initial
    i <- 0
    while i < list.Count() {
        item <- list[i]
        result <- accumulator(result, item)
        i <- i + 1
    }
    return result
}
numbers <- {1, 2, 3, 4, 5}
PrintLine(reduce(numbers, 1, (acc:int, x:int) -> acc * x))
```

| 模式 | 结果 |
|---|---|
| 解释器 / 虚拟机 | `120` |
| IL（修复前） | `0`（每次迭代都从初值重新开始） |

`Compiler.Functions.HigherOrderTests.ReduceFunction` 因此失败。

## 2. 根因

未注解变量先按推断类型声明槽位（`result` → int）。函数值参数 `accumulator(...)`
的静态返回类型是 `object`，赋值时 `EmitDefaultSetValueToIl` 检测到类型变化，
**删除并重新声明**一个 object 局部槽。但循环体开头已经生成的 `ldloc result`
仍指向旧的 int 槽，于是：

- 赋值写进新 object 槽；
- 下一次迭代仍读旧 int 槽（初值）；
- 结果恒为初值。

## 3. 修复

- 新增 `IlValueBridge.CoerceDynamic(object?, Type)`：把运行期值（含语言值解包）
  转换成槽位类型；
- `EmitDefaultSetValueToIl` 在「动态结果（object）写入具体类型槽」时不再替换
  LocalBuilder，而是用 `CoerceDynamic` 收敛后写回**原槽位**：循环开头已生成的
  `ldloc` 始终读到最新值。
- 具体类型之间的显式类型变化（如 int 槽写入 string 字面量）仍走原替换路径
  （直线代码中后续读取会重新绑定新槽）。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `reduce3` CLI（三次迭代打印中间值） | `1 0 ...` | `2 1 2 3 2 6 4 6 24 24`（与解释器一致） |
| `HigherOrderTests.ReduceFunction` | 期望 120 实际 0 | 通过 |

顺带修复：`AssertHelper.AreEqual` 对 list/array 做元素级比较（解释器/VM 的
`Assert.Equal([1,2], {1,2})` 为 true，IL 之前按 `ToString` 比较失败）。

## 5. 用例迁移

- `ChainFunction` 里变量名 `func` 是关键字 → 改名 `fn`；
- `FunctionCall_NestedCalls` 定义名为 `double` 的函数与类型转换同名（解释器/IL 取
  类型转换，VM 取用户函数）→ 改名 `twice`，只验证嵌套调用本身。
