using Xunit;

namespace Old8Lang.Tests;

/// <summary>
/// 需要独占执行的测试集合：这些用例会修改全局静态配置（如 TypeInferenceConfig、
/// Compiler.IlVerificationEnabled），并行运行会让其它测试的编译路径失去类型推断，
/// 严重时导致测试进程栈溢出崩溃。
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection;
