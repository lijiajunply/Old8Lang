using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行模块操作指令
    /// </summary>
    private void ExecuteModuleOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.LoadModule:
            {
                // 加载模块
                string moduleName = (string)instruction.Operand!;
                LoadModule(moduleName);
            }
                break;

            case OpCode.ImportSymbol:
            {
                // 导入符号: import { symbol } from "module"
                var operands = (object[])instruction.Operand!;
                string moduleName = (string)operands[0];
                string symbolName = (string)operands[1];

                var symbol = _moduleRegistry.GetModuleSymbol(moduleName, symbolName);

                // 将符号添加到当前全局变量
                _globals[symbolName] = symbol ?? throw new ImportError(GetPosition(instruction), moduleName,
                    $"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
            }
                break;

            case OpCode.ImportSymbolAs:
            {
                // 导入符号并重命名: import { symbol as alias } from "module"
                var operands = (object[])instruction.Operand!;
                string moduleName = (string)operands[0];
                string symbolName = (string)operands[1];
                string alias = (string)operands[2];

                var symbol = _moduleRegistry.GetModuleSymbol(moduleName, symbolName);

                // 使用别名添加到全局变量
                _globals[alias] = symbol ?? throw new ImportError(GetPosition(instruction), moduleName,
                    $"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
            }
                break;

            case OpCode.ImportAll:
            {
                // 导入所有符号: import * from "module"
                string moduleName = (string)instruction.Operand!;

                var module = _moduleRegistry.GetModule(moduleName);
                if (module == null)
                {
                    throw new ImportError(GetPosition(instruction), moduleName, $"模块 '{moduleName}' 未加载");
                }

                // 导入所有导出的符号
                foreach (var symbolName in module.GetExportedSymbolNames())
                {
                    var symbol = module.GetSymbol(symbolName);
                    _globals[symbolName] = symbol;
                }
            }
                break;

            case OpCode.GetModuleSymbol:
            {
                // 获取模块符号: moduleName.symbolName
                var operands = (object[])instruction.Operand!;
                string moduleName = (string)operands[0];
                string symbolName = (string)operands[1];

                var symbol = _moduleRegistry.GetModuleSymbol(moduleName, symbolName);
                if (symbol == null)
                {
                    throw new ImportError(GetPosition(instruction), moduleName,
                        $"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
                }

                _stack.Push(symbol);
            }
                break;

            case OpCode.DebugPrint:
            {
                int messageIndex = (int)instruction.Operand!;
                var message = _bytecodeFile.ConstantPool.GetConstant(messageIndex);
                var stackContents = string.Join(", ", _stack.Select(x => x?.GetType().Name ?? "null"));
                Console.WriteLine($"{message} - 栈深度:{_stack.Count}, 内容:[{stackContents}]");
            }
                break;

        }
    }
}
