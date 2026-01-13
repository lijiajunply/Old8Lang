using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

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

    /// <summary>
    /// 获取当前正在编译的函数是否是异步函数
    /// </summary>
    public bool IsCurrentFunctionAsync => _currentFunction?.IsAsync ?? false;

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
        // 注意：类定义现在在 BytecodeVisitor.VisitClassInit 中完整处理
        // 包括字段和方法的编译，所以这里不需要预注册
        // 保留这个方法是为了将来可能需要的其他预处理逻辑
    }

    /// <summary>
    /// 编译函数定义
    /// </summary>
    public FunctionMetadata CompileFunction(string funcName, List<string> parameters, List<object?> defaultValues,
        BlockStatement body)
    {
        // 检测函数是否包含yield语句
        bool isGenerator = ContainsYieldStatement(body);

        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            DefaultValues = defaultValues,
            IsAsync = false,
            IsGenerator = isGenerator // 设置生成器标记
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
    public FunctionMetadata CompileAsyncFunction(string funcName, List<string> parameters, List<object?> defaultValues,
        BlockStatement body)
    {
        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            DefaultValues = defaultValues,
            IsAsync = true // 标记为异步函数
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
    /// 编译异步生成器函数
    /// </summary>
    public FunctionMetadata CompileAsyncGeneratorFunction(string funcName, List<string> parameters,
        List<object?> defaultValues, BlockStatement body)
    {
        // 检测函数是否包含yield语句
        bool isGenerator = ContainsYieldStatement(body);

        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            DefaultValues = defaultValues,
            IsAsync = true, // 标记为异步函数
            IsGenerator = isGenerator // 标记为生成器函数
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

    /// <summary>
    /// 检查是否是生成器函数
    /// </summary>
    public bool IsGeneratorFunction(string funcName)
    {
        return _bytecodeFile.Functions.Any(f => f.Name == funcName && f.IsGenerator);
    }

    /// <summary>
    /// 获取函数在字节码文件中的索引
    /// </summary>
    public int GetFunctionIndex(string funcName)
    {
        for (int i = 0; i < _bytecodeFile.Functions.Count; i++)
        {
            if (_bytecodeFile.Functions[i].Name == funcName)
            {
                return i;
            }
        }

        return -1;
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
        _currentFunction?.ExceptionTable.Add(entry);
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
        DeclareClass(className, fields, new List<(string, FuncLangValue, bool)>(), null);
    }

    public void DeclareClass(string className, List<string> fields,
        List<(string methodName, FuncLangValue funcValue, bool isStatic)> methods,
        string? parentClassName)
    {
        var classMetadata = new ClassMetadata
        {
            Name = className,
            BaseClassName = parentClassName,
            Fields = fields.Select(f => new FieldMetadata { Name = f }).ToList()
        };

        // 编译所有方法
        foreach (var (methodName, funcValue, isStatic) in methods)
        {
            // 提取方法参数
            var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? new List<string>();

            // 提取默认参数值
            var defaultValues = new List<object?>();
            if (funcValue.Ids != null)
            {
                foreach (var param in funcValue.Ids)
                {
                    if (param.DefaultValue != null)
                    {
                        var defaultValue = EvaluateConstantExpression(param.DefaultValue);
                        defaultValues.Add(defaultValue);
                    }
                    else
                    {
                        defaultValues.Add(null);
                    }
                }
            }

            // 编译方法体
            var functionMetadata = CompileFunction(
                $"{className}.{methodName}",
                paramNames,
                defaultValues,
                funcValue.BlockStatement
            );

            // 创建方法元数据
            var methodMetadata = new MethodMetadata
            {
                Name = methodName,
                IsStatic = isStatic,
                Function = functionMetadata
            };

            // 添加到类元数据
            if (isStatic)
            {
                classMetadata.StaticMethods.Add(methodMetadata);
            }
            else
            {
                classMetadata.Methods.Add(methodMetadata);
            }
        }

        _bytecodeFile.Classes.Add(classMetadata);
    }

    public bool IsClassName(string name)
    {
        return _bytecodeFile.Classes.Any(c => c.Name == name);
    }

    /// <summary>
    /// 计算常量表达式的值（用于默认参数）
    /// </summary>
    private object? EvaluateConstantExpression(LangExpression expr)
    {
        return expr switch
        {
            IntLangValue intVal => intVal.Value,
            DoubleLangValue doubleVal => doubleVal.Value,
            StringLangValue stringVal => stringVal.Value,
            BoolLangValue boolVal => boolVal.Value,
            CharLangValue charVal => charVal.Value,
            NullLangValue => null,
            _ => throw new NotSupportedException($"虚拟机模式不支持非常量默认参数表达式: {expr.GetType().Name}")
        };
    }

    /// <summary>
    /// 局部作用域
    /// </summary>
    private class Scope(Scope? parent)
    {
        private readonly Dictionary<string, int> _locals = new();
        private int _nextIndex = parent?._nextIndex ?? 0;

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
            return _locals.ContainsKey(name) || (parent?.HasLocal(name) ?? false);
        }

        public int GetLocalIndex(string name)
        {
            if (_locals.TryGetValue(name, out int index))
                return index;
            return parent?.GetLocalIndex(name) ?? -1;
        }

        public int LocalCount => _nextIndex;
    }

    /// <summary>
    /// 检测语句块中是否包含yield语句
    /// </summary>
    public bool ContainsYieldStatement(BlockStatement block)
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