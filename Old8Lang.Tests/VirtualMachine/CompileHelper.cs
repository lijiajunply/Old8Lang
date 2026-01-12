using Old8Lang.AST.Visitor;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine;

/// <summary>
/// 字节码编译辅助类
/// </summary>
public static class CompileHelper
{
    /// <summary>
    /// 将 Old8Lang 代码编译为字节码文件
    /// </summary>
    public static BytecodeFile CompileToBytecode(string code)
    {
        // 1. 解析代码为 AST
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 2. 使用 BytecodeCompiler 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        return bytecodeFile;
    }
}
