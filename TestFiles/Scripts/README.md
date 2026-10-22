# 临时验证脚本

这里放的是开发过程中用于手工验证的 `.old8` 脚本（`test_*.old8`），**不属于任何测试套件**，
默认不被 runner 或 CI 扫描，按需手动运行：

```bash
cd TestFiles
dotnet run --project ../Old8Lang.App -- -f Scripts/<file>.old8
```

需要长期保留的用例请按模式移到 `SyntaxTests/`、`InterpreterTests/`、`CompilerTests/`、`VirtualMachine/` 下。
