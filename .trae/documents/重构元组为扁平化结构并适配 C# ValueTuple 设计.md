我将重构 Old8Lang 的元组（Tuple）实现，使其从目前的**递归二元结构**转变为**扁平化结构**，并在编译模式下生成与 C# `ValueTuple` 完全兼容的 IL 代码（支持 >7 个元素的自动嵌套）。

### 1. 核心重构：`TupleLangValue.cs`
- **数据结构变更**：
  - 移除 `V1`, `V2` 字段。
  - 新增 `List<LangExpression> Elements` 存储所有子表达式。
  - 新增 `List<LangValueType> ItemValues` 存储解释模式下的运行时值。
- **解释器逻辑 (`Run`)**：
  - 扁平化执行所有 `Elements`，结果存入 `ItemValues`。
  - 移除递归调用的开销。
- **编译器逻辑 (`LoadIlValue`, `OutputType`)**：
  - 实现 C# 标准的 `ValueTuple` 生成算法：
    - 元素数量 ≤ 7：生成 `ValueTuple<T1...Tn>`。
    - 元素数量 > 7：生成 `ValueTuple<T1...T7, TRest>`，其中 `TRest` 递归嵌套下一个 `ValueTuple`。
  - 确保生成的 IL 代码与 C# 编译器生成的代码结构一致。
- **访问与操作**：
  - 重写 `Get(index)`、`Slice`、`Length` 等方法，直接基于列表操作，移除递归查找，提升性能。
  - 更新 `ToString()` 以正确显示扁平化结构。

### 2. 解析器适配：`PrimaryParser.cs`
- 修改 `ParseLambdaOrTuple` 方法。
- 移除解析时创建嵌套 `TupleLangValue` 的逻辑。
- 直接将解析到的表达式列表传递给 `TupleLangValue` 的新构造函数。

### 3. 兼容性与互操作
- **Type System**：更新类型推断逻辑，支持生成嵌套的泛型元组类型定义。
- **GetValue()**：使用反射动态创建对应的 .NET `ValueTuple` 实例，确保与 C# 代码交互时表现一致。

### 4. 验证计划
- 创建测试文件 `TupleTest.old8`：
  - 测试短元组 `(1, 2)`。
  - 测试长元组（>7个元素）`(1, 2, 3, 4, 5, 6, 7, 8, 9)`，验证编译器是否报错及运行结果。
  - 测试混合类型和嵌套元组。
  - 测试索引访问和切片。
- 执行解释模式和编译模式测试，确保行为一致。
