1. **创建测试文件** **`VMTypeConversionTest.old8`**: 包含各种类型的 `as` 转换测试用例，涵盖成功转换、失败转换（预期返回 null）以及数值精度行为测试（如 double 转 int 的截断逻辑）。
2. **运行测试**: 使用 `dotnet run --project Old8Lang.App -- -vm VMTypeConversionTest.old8` 运行测试，预期会失败（因为 VM 目前抛出异常而非返回 null，且数值转换逻辑可能不一致）。
3. **修复** **`VirtualMachine.cs`**:

   * 修改 `OpCode.Cast` 的处理逻辑。

   * 使用 `try-catch` 包裹转换逻辑，在发生异常时返回 `null`，实现安全的 `as` 语义。

   * 修正 `double` 到 `int` 的转换逻辑，使用截断（`(int)doubleVal`）而非四舍五入（`Convert.ToInt32`），以保持与解释器模式一致。
4. **再次运行测试**: 验证修复后的 VM 能通过所有测试用例。
5. **更新文档**: 更新 `Docs/TODO_VirtualMachine.md`，将“类型转换”标记为已完成。

