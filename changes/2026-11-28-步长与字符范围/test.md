# 测试记录

按顺序执行下列命令，四阶段均通过。

| 阶段 | 命令 | 结果 |
| --- | --- | --- |
| 语法 | `dotnet run --project Old8Lang.App -- -s TestFiles/SyntaxTests/stepped_char_ranges.old8` | 解析步长表达式、字符端点和开区间 |
| 解释 | `dotnet run --project Old8Lang.App -- -f TestFiles/InterpreterTests/stepped_char_ranges.old8` | 所有断言通过 |
| IL | `dotnet run --project Old8Lang.App -- -il TestFiles/CompilerTests/stepped_char_ranges.old8` | 所有断言通过 |
| VM | `dotnet run --project Old8Lang.App -- -vm TestFiles/VirtualMachine/stepped_char_ranges.old8` | 所有断言通过 |

执行用例覆盖变量步长、整数和字符数组、升降序、端点排除、相同端点和 32 位整数上界。单测另覆盖非法步长、混合类型端点、缺少步长表达式、IL 函数体内的异步步长以及 VM 字节码序列化和重新载入。

## 单元测试

| 测试范围 | 命令 | 结果 |
| --- | --- | --- |
| 范围专项回归 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter 'FullyQualifiedName~ParseArrayOrRange\|FullyQualifiedName~RangeBoundaryTests\|FullyQualifiedName~RangeSyntax\|FullyQualifiedName~SteppedAndCharacterRanges\|FullyQualifiedName~RangesRejectInvalid\|FullyQualifiedName~SteppedRange_Awaits'` | 28 通过，0 失败 |
| 默认档全量 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build` | 7,291 通过，0 失败，5 跳过 |
| IL 范围测试 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter 'FullyQualifiedName~Old8Lang.Tests.Compiler.Collections.Range.RangeTests'` | 10 通过，0 失败，0 跳过 |

默认档全量运行后，重新启用了两个此前因功能未实现而跳过的 IL 用例，两项均在 IL 范围测试中通过。全量结果包含当时的两个跳过。
