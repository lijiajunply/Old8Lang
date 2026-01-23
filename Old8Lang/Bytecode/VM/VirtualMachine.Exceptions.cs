using Old8Lang.Bytecode.Metadata;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// 执行所有 defer 块（按 LIFO 顺序）
    /// </summary>
    private void ExecuteDefers(CallFrame frame)
    {
        // 按 LIFO 顺序执行所有 defer 块
        while (frame.DeferStack.Count > 0)
        {
            int deferStartPos = frame.DeferStack.Pop();

            // 保存当前 IP
            int savedIP = frame.IP;

            // 跳转到 defer 块的起始位置
            frame.IP = deferStartPos;

            // 执行 defer 块（直到遇到 ReturnVoid）
            while (frame.IP < frame.Function.Instructions.Count)
            {
                var instruction = frame.Function.Instructions[frame.IP];
                frame.IP++;

                // 执行指令
                ExecuteInstruction(instruction, frame);

                // 如果遇到 ReturnVoid，说明 defer 块执行完毕
                if (instruction.OpCode == OpCode.ReturnVoid)
                {
                    break;
                }
            }

            // 恢复 IP（继续执行原来的代码）
            frame.IP = savedIP;
        }
    }

    /// <summary>
    /// 处理异常 - 查找并执行匹配的异常处理器
    /// </summary>
    /// <returns>如果找到并处理了异常返回true，否则返回false</returns>
    private bool HandleException(Exception exception, CallFrame frame, FunctionMetadata function)
    {
        // 提取真实的异常对象
        object? exceptionValue = exception;
        if (exception is VmException vmException)
        {
            exceptionValue = vmException.Value;
        }

        // 获取异常发生时的指令位置（已经+1了，所以要-1）
        int exceptionIP = frame.IP - 1;

        // 遍历异常表，查找匹配的处理器（从内到外）
        bool finallyExecuted = false;
        foreach (var entry in function.ExceptionTable)
        {
            // 检查异常是否发生在这个try块或catch块中
            bool inTryBlock = entry.IsInTryBlock(exceptionIP);
            bool inCatchBlock = entry.IsInCatchBlock(exceptionIP);

            if (inTryBlock || inCatchBlock)
            {
                // 如果异常发生在catch块中，且有finally块，先执行finally块
                if (inCatchBlock && entry.FinallyStart >= 0)
                {
                    // 执行finally块
                    ExecuteFinallyBlock(frame, function, entry.FinallyStart, entry.FinallyEnd);
                    finallyExecuted = true;
                    // finally块执行完后，继续查找外层的异常处理器
                    continue;
                }

                // 如果异常发生在try块中，检查异常类型是否匹配
                if (inTryBlock && IsExceptionTypeMatch(exceptionValue, entry.ExceptionType))
                {
                    // 如果有catch块，跳转到catch块
                    if (entry.CatchStart >= 0)
                    {
                        // 将异常对象压入栈
                        _stack.Push(exceptionValue);
                        frame.IP = entry.CatchStart;
                        return true;
                    }
                    // 如果没有catch块但有finally块（如using语句或try-finally），执行finally块后重新抛出异常
                    else if (entry.FinallyStart >= 0)
                    {
                        // 执行finally块
                        ExecuteFinallyBlock(frame, function, entry.FinallyStart, entry.FinallyEnd);
                        finallyExecuted = true;

                        // finally块执行完后，继续查找外层的异常处理器
                        // 不返回true，让异常继续向外传播
                        continue;
                    }
                }
            }
        }

        // 如果执行了finally块但没有找到catch块，返回false让异常继续传播
        // 如果没有找到任何匹配的处理器，也返回false
        return false;
    }

    /// <summary>
    /// 执行finally块
    /// </summary>
    private void ExecuteFinallyBlock(CallFrame frame, FunctionMetadata function, int finallyStart, int finallyEnd)
    {
        // 保存当前IP
        int savedIP = frame.IP;

        // 跳转到finally块
        frame.IP = finallyStart;

        // 执行finally块中的指令
        while (frame.IP < finallyEnd && frame.IP < function.Instructions.Count)
        {
            var instruction = function.Instructions[frame.IP];
            frame.IP++;

            // 执行指令（不捕获异常，让finally块中的异常直接抛出）
            ExecuteInstruction(instruction, frame);
        }

        // 恢复IP（虽然可能不需要，因为异常会继续传播）
        frame.IP = savedIP;
    }

    /// <summary>
    /// 检查异常类型是否匹配
    /// </summary>
    private bool IsExceptionTypeMatch(object? exceptionValue, string? expectedType)
    {
        // 如果没有指定异常类型，匹配所有异常
        if (string.IsNullOrEmpty(expectedType))
            return true;

        if (exceptionValue == null)
            return false;

        // 1. 检查 BytecodeObjectInstance
        if (exceptionValue is BytecodeObjectInstance instance)
        {
            // 检查类名
            if (instance.ClassName == expectedType)
                return true;

            // 检查继承关系
            var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == instance.ClassName);
            while (classMetadata != null && !string.IsNullOrEmpty(classMetadata.BaseClassName))
            {
                if (classMetadata.BaseClassName == expectedType)
                    return true;

                classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == classMetadata.BaseClassName);
            }

            // 检查接口实现
            // TODO: 这里需要从 ClassMetadata 中获取接口信息，或者 BytecodeObjectInstance 应该存储接口信息
            // 假设 instance.Interfaces 包含所有实现的接口
            if (instance.Interfaces.Contains(expectedType))
                return true;

            return false;
        }

        // 2. 检查 .NET 异常类型
        if (exceptionValue is Exception ex)
        {
            // 获取异常的类型名称
            string actualType = ex.GetType().Name;

            // 精确匹配
            if (actualType == expectedType)
                return true;

            // 匹配完整类型名称
            if (ex.GetType().FullName == expectedType)
                return true;

            // 检查继承关系
            Type? currentType = ex.GetType();
            while (currentType != null)
            {
                if (currentType.Name == expectedType || currentType.FullName == expectedType)
                    return true;
                currentType = currentType.BaseType;
            }

            // 特殊情况：如果是 "Exception"，匹配所有 Exception
            if (expectedType == "Exception")
                return true;
        }

        // 3. 字符串异常匹配 (如果 expectedType 是 "string" 或具体值?)
        // Old8Lang 中通常不建议用字符串作为异常类型，但为了兼容性
        if (exceptionValue is string str && expectedType == "string")
            return true;

        return false;
    }
}
/// <summary>
/// 异常处理器 - 跟踪try-catch-finally块
/// </summary>
internal class ExceptionHandler
{
    public int CatchIP { get; set; }
    public int FinallyIP { get; set; }
    public int EndIP { get; set; }
    public bool InFinally { get; set; }
}