namespace Old8Lang.Compiler.CodeGeneration;

/// <summary>
/// lambda 闭包环境中一个变量的槽位与 CLR 类型。
/// </summary>
public sealed record CapturedLocal(int Index, Type Type);
