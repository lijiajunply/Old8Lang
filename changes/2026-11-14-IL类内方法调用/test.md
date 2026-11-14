# IL 类体内调用自身方法编译失败 —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误」
（新增条目「IL 类型推断在未完成的 `TypeBuilder` 上调用 `GetMethod`」）。

## 1. 现象

类方法里调用同类方法（`this.method()`）时，IL 模式在**编译期**失败：

```
System.NotSupportedException : The invoked member is not supported before the type is created.
   at System.Reflection.Emit.RuntimeTypeBuilder.GetMethodImpl(...)
   at Old8Lang.AST.Visitor.TypeInferenceVisitor.VisitOperation(Operation node)
        in TypeInferenceVisitor.ExpressionsAndValues.cs:line 548
   at ... CompilerVisitor.VisitReturnStatement / VisitBlockStatement
   at ... ClassInitRuntimeExtensions.DefineMethodInternal
```

失败用例（`Compiler/Classes/MemberAccessTests`）：

- `PublicPrivateAccess_CompilesAndExecutesCorrectly`（`public func callPrivateMethod() { return this.privateMethod() }`）
- `MemberAccessWithThis_CompilesAndExecutesCorrectly`

相应地 `Compiler/Exceptions/ErrorPropagationTests.ErrorPropagatesThroughClassMethods` 也因为
在类方法内调用同类方法而失败。

## 2. 根因

类方法体编译时 `this` 的静态类型是 `local.InClassEnv`，即尚未 `CreateType()` 的
`TypeBuilder`。`VisitOperation` 在实例方法解析的最后兜底调用了
`leftType.GetMethod(name, paramTypes)`，而 `TypeBuilder.GetMethod` 在 `CreateType` 之前
一律抛 `NotSupportedException`。

即便跳过推断，发射侧 `DotOperatorILHelper.GenerateRegularInstanceMethodCall` 同样会在
`leftType.GetMethod(...)` 处抛同一个异常。当前类的方法也无法提前拿到 `MethodBuilder`：
方法按声明顺序逐个创建，而方法之间的调用相互独立，前向引用时 `MethodBuilder` 尚不存在。

## 3. 修复

把「接收者是正在创建的类型」等同于「接收者静态类型为 `object`」：两侧都用运行期分派。

1. `Old8Lang.Runtime/AST/Visitor/TypeInferenceVisitor.ExpressionsAndValues.cs`
   —— `leftType is TypeBuilder` 时不再调用 `GetMethod`，返回 `object`；
2. `Old8Lang.Compiler/OperationHelpers/DotOperatorILHelper.cs`
   —— `GenerateRegularInstanceMethodCall` 对 `TypeBuilder` 接收者改走
   `GenerateDynamicObjectMethodCall`（发射 `this` + 方法名 + 实参数组，运行期按实际类型分派）；
3. 同一个文件里的 `InvokeDynamicObjectMethod`
   —— 公有方法找不到时再查 `BindingFlags.NonPublic`，否则语言层的 `private` 方法
   （CLR 私有）无法被运行期分派命中。

推断与发射都落在 `object` 上，是同一条自洽路径；此前凡是走到这里的代码都会直接编译失败，
因此不存在「原本可用、现在变慢」的路径（性能上后续可用 `MethodBuilder` 预登记替代，
已记入 `changes/Todo.md`）。

## 4. 验证

### 4.1 定向

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `MemberAccessTests.PublicPrivateAccess_...` | `NotSupportedException` | 通过 |
| `MemberAccessTests.MemberAccessWithThis_...` | `NotSupportedException` | 通过 |
| `MemberAccessTests` 整组 | 4 通过 / 5 失败 | 6 通过 / 3 失败 |
| `ExceptionTests.ErrorPropagationTests.ErrorPropagatesThroughClassMethods_...` | 失败 | 通过 |

### 4.2 基线对比（`Old8Lang.Tests.Compiler` 19 个子组）

| 组 | 修复前（通过/失败） | 修复后（通过/失败） |
|---|---|---|
| Classes | 87 / 23 | **89 / 21** |
| Exceptions | 52 / 12 | **53 / 11** |
| 其余 17 组 | — | 与修复前逐组一致（Async 43/65、Basic 185/16、Collections 165/9、EdgeCases 77/22、Error 0/12、Expressions 191/41、Extern 7/3、FileHeader 29/0、Functions 109/57、Integration 50/3、Linq 45/0、Modules 263/0、Reflection 3/14、Statements 183/3、Threading 15/0、Types 50/51、Unit 45/0） |

`-vm` / `-f` 未受影响（改动只在 IL 后端与类型推断的 `object` 兜底）。

## 5. 遗留

`MemberAccessTests` 剩余 3 项失败是彼此独立的缺口，已分别记入 `changes/Todo.md`：

1. `MemberAccessErrorHandling_...`：`int?` 函数 `return null` 触发 NRE（可空返回类型缺口）；
2. `MemberAccessWithComplexTypes_...`：结果 5 与期望 6 不符（数值/成员访问结果错误）；
3. `StaticMemberAccess_...`：静态成员读到 0 而不是 3.14159。

`this.method()` 现在走运行期反射分派，性能低于直接 `callvirt`；
后续可在 `LocalManager` 预登记 `MethodBuilder` 恢复直接调用。

## 6. 复现命令

```bash
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Classes.MemberAccessTests"
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Exceptions.ErrorPropagationTests"
```
