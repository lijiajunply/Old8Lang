# 测试

## Debug 示例

- `dotnet build Old8Lang.App/Old8Lang.App.csproj -c Debug` 构建成功。
- `dotnet run --project Old8Lang.App -- -s Old8Lang.App/Ex/init.old8` 的等价 Debug DLL 调用退出码为 0。
- 用 Debug DLL 执行 `-f Old8Lang.App/Ex/init.old8` 和 `-vm Old8Lang.App/Ex/init.old8`，均打印类型转换结果 `4`、递归结果 `832040`，退出码为 0。
- 在 `Old8Lang.App/bin/Debug/net10.0` 下运行 `dotnet Old8Lang.App.dll`，默认入口成功导入 `other`，输出相同的结果，退出码为 0。
- VM 中 `Json(test)` 仍打印空行，已记入 [待办](../Todo.md)。

## 单元测试

```sh
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter 'FullyQualifiedName~Old8Lang.Tests.VirtualMachine|FullyQualifiedName~Old8Lang.Tests.Architecture'
```

覆盖本地类和导入类、缺省字段、跳过构造函数、未知和不可访问字段、字段类型校验、同名类的别名导入、继承方法以及运行时字典值。

共 1228 项通过，1 项失败。VM 用例全部通过。唯一失败是已有待办中的 `ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections`，触发 IL 对表达式内部 `await` 的限制。
