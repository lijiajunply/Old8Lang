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
    /// 编译函数定义
    /// </summary>
    public FunctionMetadata CompileFunction(string funcName, List<string> parameters, BlockStatement body)
    {
        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            IsAsync = false
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
            if (_locals.ContainsKey(name))
                return _locals[name];

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
}
