using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

var code = @"
func makeAdder(x:int) -> any {
    return (y:int) -> x + y
}

add5 <- makeAdder(5)
result <- add5(3)
";

// 解析代码为 AST
var interpreter = new LangInterpreter();
var ast = interpreter.Build(code);

// 使用 BytecodeCompiler 编译为字节码
var compiler = new BytecodeCompiler();
var bytecodeFile = compiler.Compile(ast);

// 打印所有函数的字节码
foreach (var func in bytecodeFile.Functions)
{
    Console.WriteLine($"\n=== Function: {func.Name} ===");
    Console.WriteLine($"Parameters: {string.Join(", ", func.Parameters)}");
    Console.WriteLine($"LocalCount: {func.LocalCount}");
    Console.WriteLine($"CapturedVariables: {string.Join(", ", func.CapturedVariables)}");
    Console.WriteLine("\nInstructions:");
    for (int i = 0; i < func.Instructions.Count; i++)
    {
        var inst = func.Instructions[i];
        Console.WriteLine($"  {i}: {inst.OpCode} {inst.Operand}");
    }
}

Console.WriteLine("\n=== Running VM ===\n");

// 运行虚拟机
var vm = new VirtualMachine(bytecodeFile);
try
{
    vm.Execute();
    var result = vm.GetGlobalVariable("result");
    Console.WriteLine($"Result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
