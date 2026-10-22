# 性能测试脚本

这些 `.old8` 脚本由 `Old8Lang.Tests` 的性能用例读取执行（`Interpreter/Performance/*`，
均带 `[Trait("Category", "Performance")]`，只在 `performance.runsettings` 档下运行）：

```bash
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --settings Old8Lang.Tests/performance.runsettings \
  --filter "FullyQualifiedName~Performance"
```

测试代码通过仓库根目录定位这些文件，因此**移动或改名时必须同步修改**
`Old8Lang.Tests/Interpreter/Performance/` 下对应的路径常量。
