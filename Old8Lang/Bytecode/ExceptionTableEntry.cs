namespace Old8Lang.Bytecode;

/// <summary>
/// 异常表条目 - 描述一个异常处理区域
/// </summary>
public class ExceptionTableEntry
{
    /// <summary>Try块起始指令位置</summary>
    public int TryStart { get; set; }

    /// <summary>Try块结束指令位置（不包含）</summary>
    public int TryEnd { get; set; }

    /// <summary>Catch块起始指令位置（-1表示无catch块）</summary>
    public int CatchStart { get; set; } = -1;

    /// <summary>Catch块结束指令位置（不包含）</summary>
    public int CatchEnd { get; set; } = -1;

    /// <summary>Finally块起始指令位置（-1表示无finally块）</summary>
    public int FinallyStart { get; set; } = -1;

    /// <summary>Finally块结束指令位置（不包含）</summary>
    public int FinallyEnd { get; set; } = -1;

    /// <summary>捕获的异常类型（null表示捕获所有异常）</summary>
    public string? ExceptionType { get; set; }

    /// <summary>异常变量名称（null表示不绑定变量）</summary>
    public string? ExceptionVariable { get; set; }

    /// <summary>异常变量的局部变量索引（-1表示不绑定）</summary>
    public int ExceptionVariableIndex { get; set; } = -1;

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(TryStart);
        writer.Write(TryEnd);
        writer.Write(CatchStart);
        writer.Write(CatchEnd);
        writer.Write(FinallyStart);
        writer.Write(FinallyEnd);

        // 异常类型（可选）
        writer.Write(ExceptionType != null);
        if (ExceptionType != null)
            writer.Write(ExceptionType);

        // 异常变量（可选）
        writer.Write(ExceptionVariable != null);
        if (ExceptionVariable != null)
            writer.Write(ExceptionVariable);

        writer.Write(ExceptionVariableIndex);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static ExceptionTableEntry ReadFrom(BinaryReader reader)
    {
        var entry = new ExceptionTableEntry
        {
            TryStart = reader.ReadInt32(),
            TryEnd = reader.ReadInt32(),
            CatchStart = reader.ReadInt32(),
            CatchEnd = reader.ReadInt32(),
            FinallyStart = reader.ReadInt32(),
            FinallyEnd = reader.ReadInt32()
        };

        // 异常类型
        bool hasExceptionType = reader.ReadBoolean();
        if (hasExceptionType)
            entry.ExceptionType = reader.ReadString();

        // 异常变量
        bool hasExceptionVariable = reader.ReadBoolean();
        if (hasExceptionVariable)
            entry.ExceptionVariable = reader.ReadString();

        entry.ExceptionVariableIndex = reader.ReadInt32();

        return entry;
    }

    /// <summary>
    /// 检查指令位置是否在try块范围内
    /// </summary>
    public bool IsInTryBlock(int ip)
    {
        return ip >= TryStart && ip < TryEnd;
    }

    /// <summary>
    /// 检查指令位置是否在catch块范围内
    /// </summary>
    public bool IsInCatchBlock(int ip)
    {
        return CatchStart >= 0 && ip >= CatchStart && ip < CatchEnd;
    }

    /// <summary>
    /// 检查指令位置是否在finally块范围内
    /// </summary>
    public bool IsInFinallyBlock(int ip)
    {
        return FinallyStart >= 0 && ip >= FinallyStart && ip < FinallyEnd;
    }

    public override string ToString()
    {
        var parts = new List<string>
        {
            $"try[{TryStart}..{TryEnd})"
        };

        if (CatchStart >= 0)
            parts.Add($"catch[{CatchStart}..{CatchEnd})");

        if (FinallyStart >= 0)
            parts.Add($"finally[{FinallyStart}..{FinallyEnd})");

        if (ExceptionType != null)
            parts.Add($"type:{ExceptionType}");

        if (ExceptionVariable != null)
            parts.Add($"var:{ExceptionVariable}");

        return string.Join(" ", parts);
    }
}
