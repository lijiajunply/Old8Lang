# IL 模式反射 API 不可用（编译期 ArgumentNullException + 只认解释器对象） —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
class Person {
    public name <- "Unknown"
}
p <- Person()
SetField(p, "name", "Bob")
PrintLine(GetField(p, "name"))          # -f: Bob / -il: CompilerException
o <- CreateInstance("Person", {})       # -il: ArgumentNullException (Parameter 'field')
```

| 模式 | 结果 |
|---|---|
| `-f` | 正常 |
| `-il`（修复前） | `ArgumentNullException: Value cannot be null. (Parameter 'field')` 或 `对象不是类实例` |
| `-vm` | 正常 |

`Compiler/Reflection/ReflectionTests` 17 项里有 14 项失败。

## 2. 根因（四处）

1. **编译期 `Ldsfld` 空 FieldInfo**：`CreateInstance` / `InvokeMethod` 的 IL 发射写的是
   `typeof(LocalManager).GetField("Interpreter")`，但 `Interpreter` 是实例属性、没有对应字段，
   `GetField` 返回 null，`Emit(OpCodes.Ldsfld, null)` 直接抛 `ArgumentNullException`。
   同样的写法还散落在 7 个 `TypeValue` 实例方法里。
2. **反射只认解释器对象**：`ReflectionHelper.GetField/SetField/InvokeMethod/CreateInstance`
   一律要求 `obj is AnyLangValue`；IL 模式的对象是动态程序集里的原生类型，直接报「对象不是类实例」。
3. **`SetField` 的值没有装箱**：形参是 `object`，实参 `42` 按 `int` 压栈，生成无效 IL。
4. **同名动态程序集互相污染**：`CreateInstance` 按类名在 `AppDomain` 的所有程序集里找类型，
   而测试逐条编译/执行会累积多个同名 `Old8LangDynamicAssembly`，命中上一个用例的同名类
   （`Member 'Person.age' not found`）。

## 3. 修复

- `RuntimeGlobalWriter.CurrentManager`：新增当前线程解释器管理器的公开访问器
  （`SetManager` 本来就在 delegate 执行前安装它）；`CreateInstance` / `InvokeMethod`
  以及 7 个 `TypeValue` 实例方法的 IL 发射改用 `call get_CurrentManager`，不再 `ldsfld` 不存在的字段。
- `ReflectionHelper` 增加原生对象分支：字段/方法用 CLR 反射（含基类链、公有与私有成员），
  参数列表同时接受解释器的 `ListLangValue` 与 IL 的原生集合；
  `CreateInstance` 先在动态程序集里找原生类型并调用 `init`，找不到再回退到解释器类模板。
- `SetFieldFunction` 的 IL 发射对值类型实参补 `box`。
- `FindNativeType` 改为**倒序**遍历程序集，优先使用最近创建的那个。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `SetField` / `GetField`（公有字段） | `对象不是类实例` | `Bob` / 正常 |
| `SetField` 值类型实参（`42`） | 无效 IL | 正常 |
| `InvokeMethod(p, "greet", {})` | 编译期 `ArgumentNullException` | `hi Bob` |
| `CreateInstance("Person", {"Alice", 25})` | 编译期异常 / 命中旧同名类 | 与 `-f` 一致 |
| `Compiler.Reflection` 组 | 14 失败 / 3 通过 | **0 失败 / 17 通过** |

其余 18 个 `Compiler` 子组与修复前逐组一致（Async 43/65、Basic 188/13、Classes 95/15、
Collections 168/6、EdgeCases 77/22、Error 0/12、Exceptions 54/10、Expressions 197/35、
Extern 7/3、FileHeader 29/0、Functions 110/56、Integration 50/3、Linq 45/0、Modules 263/0、
Statements 186/0、Threading 15/0、Types 86/15、Unit 45/0）。

非 `Compiler` 组全部通过：Interpreter 1967、VirtualMachine 1083、Parser 889、Libraries 95、
TypeSystem 65、Architecture 73、InstanceMethods 33。

## 5. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il reflection.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Reflection"
```
