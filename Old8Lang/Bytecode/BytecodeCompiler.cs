using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;

namespace Old8Lang.Bytecode;

/// <summary>
/// 字节码编译器 - 将AST编译为字节码
/// </summary>
public class BytecodeCompiler
{
    public ConstantPool ConstantPool { get; } = new();
    private readonly BytecodeFile _bytecodeFile = new();
    private readonly Stack<Scope> _scopes = new();
    private FunctionMetadata? _currentFunction;

    public BytecodeCompiler()
    {
        // 初始化全局作用域
        _scopes.Push(new Scope(null));
    }

    /// <summary>
    /// 编译AST到字节码文件
    /// </summary>
    public BytecodeFile Compile(BlockStatement ast)
    {
        _bytecodeFile.ConstantPool = ConstantPool;

        // 第一阶段：预处理，注册所有类定义
        PreprocessClassDefinitions(ast);

        // 创建主函数(入口点)
        var mainFunc = new FunctionMetadata
        {
            Name = "<main>",
            Parameters = new List<string>()
        };

        _currentFunction = mainFunc;
        EnterScope();

        // 使用BytecodeVisitor生成字节码
        var visitor = new BytecodeVisitor(this);
        ast.Accept(visitor);

        mainFunc.Instructions = visitor.GetInstructions();
        mainFunc.MaxStackSize = visitor.MaxStackSize;
        mainFunc.LocalCount = _scopes.Peek().LocalCount;

        LeaveScope();

        _bytecodeFile.Functions.Add(mainFunc);
        _bytecodeFile.EntryPointIndex = _bytecodeFile.Functions.Count - 1;

        return _bytecodeFile;
    }

    /// <summary>
    /// 预处理阶段：遍历AST，注册所有类定义
    /// </summary>
    private void PreprocessClassDefinitions(BlockStatement ast)
    {
        // ClassInit 存储在 ImportStatements 中
        foreach (var statement in ast.ImportStatements)
        {
            if (statement is ClassInit classInit)
            {
                var typeTemplate = classInit.AnyLangValue;
                string className = typeTemplate.ClassName;
                var fields = new List<string>();

                // 提取字段名
                foreach (var member in typeTemplate.Variates.Keys)
                {
                    fields.Add(member.IdName);
                }

                // 注册类定义
                DeclareClass(className, fields);
            }
        }
    }

    /// <summary>
    /// 编译函数定义
    /// </summary>
    public FunctionMetadata CompileFunction(string funcName, List<string> parameters, List<object?> defaultValues, BlockStatement body)
    {
        // 检测函数是否包含yield语句
        bool isGenerator = ContainsYieldStatement(body);

        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            DefaultValues = defaultValues,
            IsAsync = false,
            IsGenerator = isGenerator  // 设置生成器标记
        };

        var oldFunc = _currentFunction;
        _currentFunction = func;
        EnterScope();

        // 声明参数为局部变量
        foreach (var param in parameters)
            DeclareLocalVariable(param);

        // 编译函数体
        var visitor = new BytecodeVisitor(this);
        body.Accept(visitor);

        func.Instructions = visitor.GetInstructions();
        func.MaxStackSize = visitor.MaxStackSize;
        func.LocalCount = _scopes.Peek().LocalCount;

        LeaveScope();
        _currentFunction = oldFunc;

        _bytecodeFile.Functions.Add(func);

        return func;
    }

    /// <summary>
    /// 编译异步函数
    /// </summary>
    public FunctionMetadata CompileAsyncFunction(string funcName, List<string> parameters, List<object?> defaultValues, BlockStatement body)
    {
        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            DefaultValues = defaultValues,
            IsAsync = true  // 标记为异步函数
        };

        var oldFunc = _currentFunction;
        _currentFunction = func;
        EnterScope();

        // 声明参数为局部变量
        foreach (var param in parameters)
            DeclareLocalVariable(param);

        // 编译函数体
        var visitor = new BytecodeVisitor(this);
        body.Accept(visitor);

        func.Instructions = visitor.GetInstructions();
        func.MaxStackSize = visitor.MaxStackSize;
        func.LocalCount = _scopes.Peek().LocalCount;

        LeaveScope();
        _currentFunction = oldFunc;

        _bytecodeFile.Functions.Add(func);

        return func;
    }

    /// <summary>
    /// 检查是否是异步函数
    /// </summary>
    public bool IsAsyncFunction(string funcName)
    {
        return _bytecodeFile.Functions.Any(f => f.Name == funcName && f.IsAsync);
    }

    // ===== 作用域管理 =====

    public void EnterScope()
    {
        _scopes.Push(new Scope(_scopes.Peek()));
    }

    public void LeaveScope()
    {
        _scopes.Pop();
    }

    public int DeclareLocalVariable(string name)
    {
        return _scopes.Peek().DeclareLocal(name);
    }

    public bool IsLocalVariable(string name)
    {
        return _scopes.Peek().HasLocal(name);
    }

    public int GetLocalIndex(string name)
    {
        return _scopes.Peek().GetLocalIndex(name);
    }

    /// <summary>
    /// 分配局部变量（用于异常变量等临时变量）
    /// </summary>
    public int AllocateLocal(string name = "")
    {
        // 如果没有提供名称，生成一个临时名称
        if (string.IsNullOrEmpty(name))
        {
            name = $"<temp_{Guid.NewGuid():N}>";
        }
        return DeclareLocalVariable(name);
    }

    /// <summary>
    /// 释放局部变量（用于临时变量）
    /// 注意：这是一个占位方法，实际的局部变量管理由作用域处理
    /// </summary>
    public void FreeLocal(int localIndex)
    {
        // 在当前实现中，局部变量在作用域结束时自动释放
        // 这个方法主要用于代码可读性，表明临时变量不再使用
    }

    /// <summary>
    /// 添加常量到常量池
    /// </summary>
    public int AddConstant(object? value)
    {
        return ConstantPool.AddConstant(value);
    }

    /// <summary>
    /// 添加异常表条目到当前函数
    /// </summary>
    public void AddExceptionTableEntry(ExceptionTableEntry entry)
    {
        if (_currentFunction != null)
        {
            _currentFunction.ExceptionTable.Add(entry);
        }
    }

    public void DeclareGlobalVariable(string name)
    {
        if (!_bytecodeFile.GlobalVariables.Contains(name))
            _bytecodeFile.GlobalVariables.Add(name);
    }

    public bool IsGlobalVariable(string name)
    {
        return _bytecodeFile.GlobalVariables.Contains(name);
    }

    // ===== 原生函数检查 =====

    public bool IsNativeFunction(string name)
    {
        return name switch
        {
            // 基础IO
            "PrintLine" or "Print" or "ReadLine" or
                // 类型转换
                "ToStr" or "ToInt" or "ToDouble" or "ToBool" or
                // 并发原语
                "Sleep" or "GetCurrentThreadId" or
                "MutexCreate" or "MutexLock" or "MutexUnlock" or "MutexDispose" or
                "ChannelCreate" or "ChannelSend" or "ChannelReceive" or "ChannelClose" or
                "SemaphoreCreate" or "SemaphoreAcquire" or "SemaphoreRelease" => true,
            _ => false
        };
    }

    // ===== 类定义管理 =====

    public void DeclareClass(string className, List<string> fields)
    {
        var classMetadata = new ClassMetadata
        {
            Name = className,
            Fields = fields.Select(f => new FieldMetadata { Name = f }).ToList()
        };
        _bytecodeFile.Classes.Add(classMetadata);
    }

    public bool IsClassName(string name)
    {
        return _bytecodeFile.Classes.Any(c => c.Name == name);
    }

    /// <summary>
    /// 局部作用域
    /// </summary>
    private class Scope
    {
        private readonly Dictionary<string, int> _locals = new();
        private readonly Scope? _parent;
        private int _nextIndex;

        public Scope(Scope? parent)
        {
            _parent = parent;
            _nextIndex = parent?._nextIndex ?? 0;
        }

        public int DeclareLocal(string name)
        {
            if (_locals.TryGetValue(name, out var local))
                return local;

            int index = _nextIndex++;
            _locals[name] = index;
            return index;
        }

        public bool HasLocal(string name)
        {
            return _locals.ContainsKey(name) || (_parent?.HasLocal(name) ?? false);
        }

        public int GetLocalIndex(string name)
        {
            if (_locals.TryGetValue(name, out int index))
                return index;
            return _parent?.GetLocalIndex(name) ?? -1;
        }

        public int LocalCount => _nextIndex;
    }

    /// <summary>
    /// 检测语句块中是否包含yield语句
    /// </summary>
    private bool ContainsYieldStatement(BlockStatement block)
    {
        return ContainsYieldInStatements(block.OtherStatements);
    }

    /// <summary>
    /// 递归检测语句列表中是否包含yield语句
    /// </summary>
    private bool ContainsYieldInStatements(IEnumerable<OldStatement> statements)
    {
        foreach (var stmt in statements)
        {
            if (stmt is YieldStatement)
                return true;

            // 递归检查子语句（使用索引器遍历）
            for (int i = 0; i < stmt.Count; i++)
            {
                var child = stmt[i];
                if (child != null && ContainsYieldInStatement(child))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检测单个语句中是否包含yield
    /// </summary>
    private bool ContainsYieldInStatement(OldStatement stmt)
    {
        if (stmt is YieldStatement)
            return true;

        if (stmt is BlockStatement blockStmt)
            return ContainsYieldStatement(blockStmt);

        // 递归检查子语句
        for (int i = 0; i < stmt.Count; i++)
        {
            var child = stmt[i];
            if (child != null && ContainsYieldInStatement(child))
                return true;
        }

        return false;
    }
}