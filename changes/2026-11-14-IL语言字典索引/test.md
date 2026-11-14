# IL 模式 `dict()` 字典的索引读写生成无效 IL —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「`Compiler/Collections` 与表达式组既有 IL 失败」中的
`Dictionary_EmptyDictionary_CompilesAndExecutes`（原文记为「`dict()` 空字典首次写入字符串键后，
读取时把 `NullLangValue` 转成 `Int32` 失败」）。

## 1. 现象

```old8
func test() -> int {
    d <- dict()
    d["key"] <- 42
    return d["key"]
}
PrintLine(test())
```

| 模式 | 结果 |
|---|---|
| `-f` | `42` |
| `-il` | `RUNTIME_ERROR: Unable to cast object of type 'NullLangValue' to type 'System.Int32'` |
| `-vm` | `42` |

同样的读写放在顶层则直接生成无效 IL（验证器拦下）：

```old8
d <- dict()
d["key"] <- 42
PrintLine(d["key"])
# [IL001] 无效的IL代码: Common Language Runtime detected an invalid program.
```

## 2. 根因

`dict()` 在 IL 里返回的是解释器的语言值 `DictionaryLangValue`（不是原生
`Dictionary<object, object>`），索引读写两条路径都没有按语言值处理：

1. **写**：`SetStatement` 的索引赋值分支只处理 `object`、`Dictionary<K,V>`、数组、`List<T>`、
   字符串，`DictionaryLangValue` 落到最后的「非字典类型」兜底 —— 那里只是把集合、键、值依次压栈，
   没有任何消费指令，**栈上残留 3 个值**，生成无效 IL（顶层直接 `InvalidProgramException`，
   在函数里则表现为返回路径读到错误的栈槽）。
2. **读**：`VisitLangListItem` 对 `DictionaryLangValue` 调用 `Get(LangValueType)`，
   但键是按原生类型发射的（字符串字面量就是 `ldstr`），把 `string` 当作 `LangValueType`
   传参同样是无效 IL；同时 `Get` 返回的是 `LangValueType`，而索引表达式的静态类型是
   `object`，后续 `return d["key"]` 会对 `IntLangValue` 做 `unbox.any int32`。

## 3. 修复

两处都改成「键/值在语言值层面传递，结果还原为原生对象」：

- `CompilerVisitor.Statements.cs`（索引赋值）：新增 `DictionaryLangValue` 分支，
  用 `IlValueBridge.EmitLoadWrapped` 包装键和值后调用
  `DictionaryLangValue.Set(LangValueType, LangValueType)`；
- `CompilerVisitor.ExpressionsAndValues.cs`（索引读取）：同样包装键后调用
  `DictionaryLangValue.Get`，再调用 `IlValueBridge.Unwrap` 把结果还原为原生对象，
  与索引表达式推断出的 `object` 保持一致。

## 4. 验证

### 4.1 定向

```old8
func test() -> int { d <- dict(); d["key"] <- 42; return d["key"] }
```

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `test()` | `NullLangValue` → `Int32` 转换失败 | `42`，与 `-f`/`-vm` 一致 |
| 顶层 `dict()` 读写 | `[IL001] 无效的IL代码` | 正常输出 |
| 字符串键 / 整数键 / 覆盖写 / 缺失键 | 无效 IL | `x` / `2` / `9` / `null` |
| `Dictionary_EmptyDictionary_CompilesAndExecutes` | 失败 | 通过 |

`Compiler.Collections` 组：9 失败 → **8 失败**，其余 18 个子组逐组一致
（Classes 92/18、EdgeCases 77/22、Error 0/12、Exceptions 53/11、Expressions 191/41、
Functions 109/57、Linq 45/0、Modules 263/0、Reflection 3/14、Statements 183/3、
Threading 15/0、Types 50/51、Unit 45/0、Async 43/65、Basic 185/16、Extern 7/3、
FileHeader 29/0、Integration 50/3）。

运行期间 `Compiler.Modules` 出现过 1 项失败
（`NetLibTests.HttpWebClient_PostAsync_ShouldWorkCorrectly`，访问 jsonplaceholder），
连续复跑两次均 263/263，属于 `changes/Todo.md` 已记录的 NetLib 外部网络抖动。

## 5. 验证过程中确认的跨模式差异（未在本轮修改）

**字典缺失键的行为不一致**：

```old8
d <- {"a": 1}
PrintLine(d["missing"] == null)
```

| 模式 | 结果 |
|---|---|
| `-f` | `true`（返回 `NullLangValue`） |
| `-il` | 原生字典分支抛 `The given key 'missing' was not present in the dictionary.`；`dict()` 分支返回 `null`（同一模式内部也不一致） |
| `-vm` | `KEY_ERROR: 键 'missing' 不存在` |

三种模式对「缺失键」的约定需要统一（返回 null 还是报错），已记入 `changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il dict.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Collections.DictionaryTests"
```
