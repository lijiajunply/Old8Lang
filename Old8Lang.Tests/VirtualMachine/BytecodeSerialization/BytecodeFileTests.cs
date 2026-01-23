using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Metadata;

namespace Old8Lang.Tests.VirtualMachine.BytecodeSerialization;

/// <summary>
/// 字节码文件持久化测试
/// </summary>
public class BytecodeFileTests
{
    private readonly string _testFilePath = Path.Combine(Path.GetTempPath(), "test_bytecode.o8c");

    /// <summary>
    /// 清理测试文件
    /// </summary>
    private void CleanupTestFile()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    /// <summary>
    /// 测试基本的保存和加载功能
    /// </summary>
    [Fact]
    public void Test_SaveAndLoad_BasicBytecodeFile()
    {
        CleanupTestFile();

        // 创建字节码文件
        var originalFile = new BytecodeFile();

        // 添加常量
        originalFile.ConstantPool.AddConstant(123);
        originalFile.ConstantPool.AddConstant("Hello");
        originalFile.ConstantPool.AddConstant(3.14);

        // 添加全局变量
        originalFile.GlobalVariables.Add("globalVar1");
        originalFile.GlobalVariables.Add("globalVar2");

        // 保存到文件
        originalFile.SaveToFile(_testFilePath);
        Assert.True(File.Exists(_testFilePath));

        // 从文件加载
        var loadedFile = BytecodeFile.LoadFromFile(_testFilePath);

        // 验证常量池
        Assert.Equal(3, loadedFile.ConstantPool.Count);
        Assert.Equal(123, loadedFile.ConstantPool.GetConstant(0));
        Assert.Equal("Hello", loadedFile.ConstantPool.GetConstant(1));
        Assert.Equal(3.14, loadedFile.ConstantPool.GetConstant(2));

        // 验证全局变量
        Assert.Equal(2, loadedFile.GlobalVariables.Count);
        Assert.Equal("globalVar1", loadedFile.GlobalVariables[0]);
        Assert.Equal("globalVar2", loadedFile.GlobalVariables[1]);

        CleanupTestFile();
    }

    /// <summary>
    /// 测试包含函数的字节码文件
    /// </summary>
    [Fact]
    public void Test_SaveAndLoad_WithFunctions()
    {
        CleanupTestFile();

        // 创建字节码文件
        var originalFile = new BytecodeFile();

        // 添加函数
        var function = new FunctionMetadata
        {
            Name = "testFunc",
            LocalCount = 2,
            MaxStackSize = 10,
            IsAsync = false,
            IsGenerator = false
        };
        function.Parameters.Add("param1");
        function.Parameters.Add("param2");

        // 添加一些指令
        function.Instructions.Add(new Instruction(OpCode.LoadLocal, 0));
        function.Instructions.Add(new Instruction(OpCode.LoadLocal, 1));
        function.Instructions.Add(new Instruction(OpCode.Add));
        function.Instructions.Add(new Instruction(OpCode.Return));

        originalFile.Functions.Add(function);
        originalFile.EntryPointIndex = 0;

        // 保存和加载
        originalFile.SaveToFile(_testFilePath);
        var loadedFile = BytecodeFile.LoadFromFile(_testFilePath);

        // 验证函数
        Assert.Single(loadedFile.Functions);
        var loadedFunc = loadedFile.Functions[0];
        Assert.Equal("testFunc", loadedFunc.Name);
        Assert.Equal(2, loadedFunc.Parameters.Count);
        Assert.Equal("param1", loadedFunc.Parameters[0]);
        Assert.Equal("param2", loadedFunc.Parameters[1]);
        Assert.Equal(2, loadedFunc.LocalCount);
        Assert.Equal(10, loadedFunc.MaxStackSize);
        Assert.Equal(4, loadedFunc.Instructions.Count);
        Assert.Equal(OpCode.LoadLocal, loadedFunc.Instructions[0].OpCode);
        Assert.Equal(0, loadedFile.EntryPointIndex);

        CleanupTestFile();
    }

    /// <summary>
    /// 测试包含类的字节码文件
    /// </summary>
    [Fact]
    public void Test_SaveAndLoad_WithClasses()
    {
        CleanupTestFile();

        // 创建字节码文件
        var originalFile = new BytecodeFile();

        // 创建类元数据
        var classMetadata = new ClassMetadata
        {
            Name = "TestClass",
            BaseClassName = "BaseClass",
            IsInterface = false,
            IsAbstract = false
        };
        classMetadata.InterfaceNames.Add("ITestInterface");

        // 添加字段
        classMetadata.Fields.Add(new FieldMetadata
        {
            Name = "field1",
            AccessModifier = AccessModifier.Public,
            IsStatic = false,
            TypeName = "int"
        });

        // 添加方法
        var method = new MethodMetadata
        {
            Name = "testMethod",
            AccessModifier = AccessModifier.Public,
            IsStatic = false,
            Function = new FunctionMetadata { Name = "testMethod" }
        };
        classMetadata.Methods.Add(method);

        originalFile.Classes.Add(classMetadata);

        // 保存和加载
        originalFile.SaveToFile(_testFilePath);
        var loadedFile = BytecodeFile.LoadFromFile(_testFilePath);

        // 验证类
        Assert.Single(loadedFile.Classes);
        var loadedClass = loadedFile.Classes[0];
        Assert.Equal("TestClass", loadedClass.Name);
        Assert.Equal("BaseClass", loadedClass.BaseClassName);
        Assert.Single(loadedClass.InterfaceNames);
        Assert.Equal("ITestInterface", loadedClass.InterfaceNames[0]);
        Assert.Single(loadedClass.Fields);
        Assert.Equal("field1", loadedClass.Fields[0].Name);
        Assert.Single(loadedClass.Methods);
        Assert.Equal("testMethod", loadedClass.Methods[0].Name);

        CleanupTestFile();
    }

    /// <summary>
    /// 测试包含调试信息的字节码文件
    /// </summary>
    [Fact]
    public void Test_SaveAndLoad_WithDebugInfo()
    {
        CleanupTestFile();

        // 创建字节码文件
        var originalFile = new BytecodeFile();

        // 创建调试信息
        var debugInfo = new DebugInfo();

        // 添加指令位置映射
        debugInfo.AddInstructionLocation(0, "test.old8", 1, 1);
        debugInfo.AddInstructionLocation(1, "test.old8", 2, 5);
        debugInfo.AddInstructionLocation(2, "test.old8", 3, 10);

        // 添加函数调试信息
        var funcDebugInfo = new FunctionDebugInfo
        {
            FunctionName = "testFunc",
            StartOffset = 0,
            EndOffset = 10
        };
        funcDebugInfo.LocalVariables.Add(new LocalVariableInfo
        {
            Index = 0,
            Name = "localVar1",
            StartOffset = 0,
            EndOffset = 10
        });
        debugInfo.Functions.Add(funcDebugInfo);

        originalFile.DebugInfo = debugInfo;

        // 保存和加载
        originalFile.SaveToFile(_testFilePath);
        var loadedFile = BytecodeFile.LoadFromFile(_testFilePath);

        // 验证调试信息
        Assert.NotNull(loadedFile.DebugInfo);
        Assert.Equal(3, loadedFile.DebugInfo.InstructionLocations.Count);

        var location = loadedFile.DebugInfo.GetSourceLocation(0);
        Assert.NotNull(location);
        Assert.Equal("test.old8", location.FilePath);
        Assert.Equal(1, location.Line);

        Assert.Single(loadedFile.DebugInfo.Functions);
        var loadedFuncDebug = loadedFile.DebugInfo.Functions[0];
        Assert.Equal("testFunc", loadedFuncDebug.FunctionName);
        Assert.Single(loadedFuncDebug.LocalVariables);
        Assert.Equal("localVar1", loadedFuncDebug.LocalVariables[0].Name);

        CleanupTestFile();
    }
}
