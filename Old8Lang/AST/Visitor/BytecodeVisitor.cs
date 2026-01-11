using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.Bytecode;

namespace Old8Lang.Bytecode;

/// <summary>
/// 字节码访问者 - 将AST节点转换为字节码指令
/// </summary>
public partial class BytecodeVisitor : IVisitor<Instruction?>
{
    private readonly BytecodeCompiler _compiler;
    private readonly List<Instruction> _instructions = new();
    private int _currentStackSize = 0;
    private int _maxStackSize = 0;

    public BytecodeVisitor(BytecodeCompiler compiler)
    {
        _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
    }

    /// <summary>
    /// 获取生成的指令列表
    /// </summary>
    public List<Instruction> GetInstructions() => _instructions;

    /// <summary>
    /// 获取最大栈深度
    /// </summary>
    public int MaxStackSize => _maxStackSize;

    /// <summary>
    /// 添加指令
    /// </summary>
    protected void Emit(OpCode opCode, object? operand = null)
    {
        _instructions.Add(new Instruction(opCode, operand));
        UpdateStackSize(opCode);
    }

    /// <summary>
    /// 添加指令(带调试信息)
    /// </summary>
    protected void Emit(OpCode opCode, object? operand, string? sourceFile, int? lineNumber)
    {
        var instruction = new Instruction(opCode, operand).WithDebugInfo(sourceFile, lineNumber);
        _instructions.Add(instruction);
        UpdateStackSize(opCode);
    }

    /// <summary>
    /// 更新栈大小
    /// </summary>
    private void UpdateStackSize(OpCode opCode)
    {
        // 简化的栈大小计算
        switch (opCode)
        {
            case OpCode.LoadConst:
            case OpCode.LoadLocal:
            case OpCode.LoadGlobal:
            case OpCode.LoadNull:
            case OpCode.LoadTrue:
            case OpCode.LoadFalse:
            case OpCode.Dup:
                _currentStackSize++;
                break;

            case OpCode.Pop:
            case OpCode.StoreLocal:
            case OpCode.StoreGlobal:
                _currentStackSize--;
                break;

            case OpCode.Add:
            case OpCode.Sub:
            case OpCode.Mul:
            case OpCode.Div:
            case OpCode.Mod:
            case OpCode.Equal:
            case OpCode.NotEqual:
            case OpCode.Greater:
            case OpCode.Less:
            case OpCode.GreaterEqual:
            case OpCode.LessEqual:
            case OpCode.And:
            case OpCode.Or:
                _currentStackSize--; // 弹出2个,压入1个
                break;

            case OpCode.Neg:
            case OpCode.Not:
                // 弹出1个,压入1个,无变化
                break;

            case OpCode.Return:
                _currentStackSize = 0;
                break;
        }

        if (_currentStackSize > _maxStackSize)
            _maxStackSize = _currentStackSize;
    }

    /// <summary>
    /// 获取当前指令位置
    /// </summary>
    protected int GetCurrentPosition() => _instructions.Count;

    /// <summary>
    /// 在指定位置插入跳转指令
    /// </summary>
    protected void PatchJump(int instructionIndex, int targetPosition)
    {
        _instructions[instructionIndex] = new Instruction(
            _instructions[instructionIndex].OpCode,
            targetPosition
        );
    }
}
