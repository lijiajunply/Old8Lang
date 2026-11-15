# IL 的 `throw` 丢弃被抛出的值 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 的 `throw` 丢弃被抛出的值，catch 只能拿到异常包装」。

## 1. 现象

```old8
error_obj <- {"type": "ValidationError", "message": "Invalid input", "code": 400}
result <- {}
try {
    throw error_obj
} catch (e) {
    result <- e
}
Assert.Equal("ValidationError", result["type"])
```

| 模式 | 结果 |
|---|---|
| `-il`（修复前） | `类型 'ExceptionWrapper' 不支持键类型 'String' 的索引访问` |
| `-vm` | 正常 |

`ConstructorWithValidation`（抛字符串后 `caughtError <- e`）同样对不上。解释器与虚拟机里
`catch (e)` 绑定的是**被抛出的那个值**（字典 / 字符串），IL 里绑定的是异常包装。

## 2. 根因

`VisitThrowStatement` 对非异常值只有一条路：`ToString()` → `new Exception(string)` → `throw`。
原始值在抛出点就被转成了字符串丢掉，catch 处只能拿到 `ExceptionWrapper`，
既不能索引也不能和原值比较。

## 3. 修复

- 新增 `LangThrownException`：携带被抛出的原始语言值（`LangValueType Value`）；
- `VisitThrowStatement`：值类型先装箱，再 `IlValueBridge.Wrap` 成语言值挂到
  `LangThrownException` 上抛出；显式抛 `Exception` / `ExceptionWrapper` 的分支保持不变；
- `ExceptionWrapper.Bind(Exception)`：catch 变量绑定时，`LangThrownException` 解包回
  原生值（`IlValueBridge.Unwrap`），其它异常仍包装成 `ExceptionWrapper`；
- catch 变量槽位类型由 `ExceptionWrapper` 改为 `object`（两处绑定：带 filter 与不带 filter）；
- `Assert` 的 `string` 形参接受任意实参：运行期用 `VMValueFormatter.ToDisplayString` 转换，
  这样 `Assert.Contains("msg", e.ToStr())` 这类拿 object 结果做子串断言的写法不再报
  「Assert方法 'Contains' 未找到」。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| 抛字典后 `result <- e`，`result["type"]` | 索引报错 | `ValidationError`（与 `-vm` 一致） |
| 抛字符串后 `caught <- e` 再比较 | 实际为空 | 等于被抛出的字符串 |
| `catch (e) { e.ToStr() }` | 正常 | 正常（`Assert.Contains` 不再报方法未找到） |
| `Compiler.Exceptions` 组 | 10 失败 / 54 通过 | **9 失败 / 55 通过**（`ThrowComplexObject` 修复） |

其余 18 个 `Compiler` 子组与非 `Compiler` 组（Interpreter 1967、VirtualMachine 1083、
Parser 889、Libraries 95、TypeSystem 65）全部不变。

## 5. 遗留

`ThrowExpression` 仍失败，但属用例前提问题：`throw "Error: " + (x / y).ToStr()` 里
`x / y` 先抛除零错误，被抛出的不是那个字符串；解释器同样不满足该断言。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il throw_value.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Exceptions.ThrowTests"
```
