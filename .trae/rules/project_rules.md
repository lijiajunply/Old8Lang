## 解释模式和编译模式

Old8Lang 有三种运行模式：解释模式、编译模式和虚拟机模式。

### 解释模式

解释模式下，Old8Lang 代码会逐条解释执行(运行 Run 方法)，无需编译。

### 编译模式

编译模式下，Old8Lang 代码会先被编译成中间代码(运行 GenerateIl 方法)，然后再执行。

### 虚拟机模式

虚拟机模式下，Old8Lang 运行时将使用虚拟机运行代码，使用 Old8Lang 的虚拟机和字节码进行运行。

## 测试用 Old8Lang 代码文件 规范
生成 测试用 Old8Lang 代码文件 时，请使用 .old8 作为文件扩展名。
生成的 测试用代码文件 必须要符合 Old8Lang 语法规范。详情请看 /Old8Lang/Old8Lang.ebnf
测试时可使用 PrintLine 函数打印结果，方便查看。 注释为 // 而非 #

编译模式测试时，请写到 TestFiles/CompilerTests 目录下。
解释模式测试时，请写到 TestFiles/InterpreterTests 目录下。
语法测试时，请放在 TestFiles/SyntaxTests 目录下。
虚拟机模式测试时，请写到 TestFiles/VirtualMachine 目录下。

在测试时，也可以使用 Old8Lang.App 来编译测试用代码文件：

```bash
# 解释模式测试：
dotnet run --project Old8Lang.App -- -f <path-to-test-file.old8>

# 编译模式测试：
dotnet run --project Old8Lang.App -- -c <path-to-test-file.old8>

# 语法测试：
dotnet run --project Old8Lang.App -- -s <path-to-test-file.old8>

# 虚拟机模式测试：
dotnet run --project Old8Lang.App -- -vm <path-to-test-file.old8>
```

如果发现有没有使用的测试用 Old8Lang 代码文件，请及时删除。

如果在测试中发现了其他错误，但和测试内容无关，请记录到 todo 中。

## 新语法添加 规范

1. 完成语法规则的添加和解析之后，必须先进行语法测试，确保新语法可以被正确解析。
2. 完成语法测试之后，进行解释模式测试，确保新语法在解释模式下可以正常运行。
3. 完成解释模式测试之后，进行编译模式测试，确保新语法在编译模式下可以正常运行。
4. 完成编译模式测试之后，进行虚拟机模式测试，确保新语法在虚拟机模式下可以正常运行。
5. 完成虚拟机模式测试之后，更新 Old8Lang.ebnf 和 Old8Lang_Grammar.md 中的语法规则。
6. 完成所有测试之后，在 Old8Lang.Tests 项目中添加新语法的单元测试，包括语法测试、解释模式测试、编译模式测试、虚拟机模式测试、边界测试、异常测试等。

## 任何测试结束之后

请生成测试报告，包含测试用代码文件的运行结果。

测试报告请放在 Reports 目录下。

文件格式为 Markdown 格式。文件名建议为 日期-小时-分钟-测试类型.md