using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

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
    /// 解释器实例，用于类型检查
    /// </summary>
    public LangInterpreter? Interpreter { get; set; }

    /// <summary>
    /// 是否启用类型检查（默认：true）
    /// </summary>
    public bool EnableTypeChecking { get; set; } = true;

    /// <summary>
    /// 是否启用类型推断（默认：true）
    /// </summary>
    public bool EnableTypeInference { get; set; } = true;

    // ===== 泛型支持 =====

    /// <summary>
    /// 泛型类定义缓存：key = 类名
    /// </summary>
    public readonly Dictionary<string, TypeTemplate> GenericClasses = new();

    /// <summary>
    /// 泛型函数定义缓存：key = 函数名
    /// </summary>
    public readonly Dictionary<string, FuncLangValue> GenericFunctions = new();

    /// <summary>
    /// 泛型类特化缓存：key = "ClassName$Type1_Type2_..."
    /// </summary>
    private readonly Dictionary<string, string> _genericClassSpecializations = new();

    /// <summary>
    /// 泛型函数特化缓存：key = "FuncName$Type1_Type2_..."
    /// </summary>
    private readonly Dictionary<string, string> _genericFunctionSpecializations = new();

    /// <summary>
    /// 当前类型参数映射（用于编译泛型特化类/函数）
    /// key = 类型参数名（如 "T"），value = 实际类型名（如 "Number"）
    /// </summary>
    public Dictionary<string, string> CurrentTypeParameterMapping { get; set; } = new();

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

        // 第零阶段：类型检查（如果启用）
        if (EnableTypeChecking && Interpreter != null)
        {
            PerformTypeChecking(ast);
        }

        // 第一阶段：预处理，扫描所有顶层变量声明（全局变量）
        PreprocessGlobalVariables(ast);

        // 第二阶段：预处理，注册所有类定义
        PreprocessClassDefinitions(ast);

        // 第三阶段：预处理，注册所有函数定义
        PreprocessFunctionDefinitions(ast);

        // 创建主函数(入口点)
        var mainFunc = new FunctionMetadata
        {
            Name = "<main>",
            Parameters = []
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
    /// 执行类型检查
    /// </summary>
    private void PerformTypeChecking(BlockStatement ast)
    {
        if (Interpreter == null)
        {
            return;
        }

        try
        {
            // 初始化类型检查器
            TypeChecker.Initialize(Interpreter.Manager);

            // 只注册函数和类定义，不执行其他语句
            // 这样可以避免在类型检查阶段执行 PrintLine 等语句
            RegisterTypeDefinitions(ast, Interpreter.Manager);

            // 如果启用类型推断，使用 TypeInferenceEngine 进行类型推断
            if (EnableTypeInference && TypeInferenceConfig.Instance.EnableTypeInference)
            {
                PerformTypeInference(ast);
            }
        }
        catch (Exception ex)
        {
            // 类型检查失败时，输出警告但不中断编译
            Console.WriteLine($"[虚拟机类型检查警告] {ex.Message}");
        }
    }

    /// <summary>
    /// 只注册函数和类定义，不执行其他语句
    /// </summary>
    private void RegisterTypeDefinitions(BlockStatement ast, VariateManager manager)
    {
        // 注册 ImportStatements 中的函数和类定义
        foreach (var statement in ast.ImportStatements)
        {
            if (statement is FuncInit or ClassInit or AsyncFuncInit)
            {
                statement.Run(manager);
            }
        }

        // 注册 OtherStatements 中的函数和类定义
        foreach (var statement in ast.OtherStatements)
        {
            if (statement is FuncInit or ClassInit or AsyncFuncInit)
            {
                statement.Run(manager);
            }
        }
    }

    /// <summary>
    /// 执行类型推断
    /// </summary>
    private void PerformTypeInference(BlockStatement ast)
    {
        if (Interpreter == null)
        {
            return;
        }

        try
        {
            // 创建一个临时的 LocalManager 用于类型推断
            var localManager = new LocalManager
            {
                Interpreter = Interpreter
            };

            // 创建类型推断引擎
            var inferenceEngine = new TypeInferenceEngine(localManager);

            // 执行类型推断
            bool success = inferenceEngine.InferTypes(ast);

            if (TypeInferenceConfig.Instance.DebugOutput)
            {
                var (totalConstraints, resolvedTypes, unresolvedTypes) = inferenceEngine.GetStatistics();
                Console.WriteLine($"[虚拟机类型推断] 约束数: {totalConstraints}, 已解析: {resolvedTypes}, 未解析: {unresolvedTypes}");
                Console.WriteLine($"[虚拟机类型推断] 推断{(success ? "成功" : "失败")}");
            }
        }
        catch (Exception ex)
        {
            // 类型推断失败时，输出警告但不中断编译
            if (TypeInferenceConfig.Instance.DebugOutput)
            {
                Console.WriteLine($"[虚拟机类型推断警告] {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 预处理阶段：扫描所有顶层变量声明（全局变量）
    /// 这样在编译方法体时，可以正确识别全局变量
    /// </summary>
    private void PreprocessGlobalVariables(BlockStatement ast)
    {
        // 遍历 OtherStatements 中的变量声明
        foreach (var statement in ast.OtherStatements)
        {
            if (statement is SetStatement { Id: not null } setStatement)
            {
                // 顶层的变量声明是全局变量
                DeclareGlobalVariable(setStatement.Id.IdName);
            }
        }
    }

    /// <summary>
    /// 预处理阶段：遍历AST，注册所有类定义
    /// </summary>
    private void PreprocessClassDefinitions(BlockStatement ast)
    {
        // 第一遍：遍历所有语句，找到类定义并编译它们
        // 这样在编译类实例化表达式时，类元数据已经存在
        var visitor = new BytecodeVisitor(this);

        // 遍历 ImportStatements 中的类定义
        foreach (var statement in ast.ImportStatements)
        {
            if (statement is ClassInit classInit)
            {
                // 编译类定义
                classInit.Accept(visitor);
            }
        }

        // 遍历 OtherStatements 中的类定义
        foreach (var statement in ast.OtherStatements)
        {
            if (statement is ClassInit classInit)
            {
                // 编译类定义
                classInit.Accept(visitor);
            }
        }
    }

    /// <summary>
    /// 预处理阶段：遍历AST，注册所有函数定义
    /// </summary>
    private void PreprocessFunctionDefinitions(BlockStatement ast)
    {
        // 遍历所有语句，找到函数定义并编译它们
        // 这样在编译 Spawn(funcName) 时，函数索引已经存在
        var visitor = new BytecodeVisitor(this);

        // 遍历 ImportStatements 中的函数定义
        foreach (var statement in ast.ImportStatements)
        {
            if (statement is FuncInit funcInit)
            {
                // 编译函数定义
                funcInit.Accept(visitor);
            }
            else if (statement is AsyncFuncInit asyncFuncInit)
            {
                // 编译异步函数定义
                asyncFuncInit.Accept(visitor);
            }
        }

        // 遍历 OtherStatements 中的函数定义
        foreach (var statement in ast.OtherStatements)
        {
            if (statement is FuncInit funcInit)
            {
                // 编译函数定义
                funcInit.Accept(visitor);
            }
            else if (statement is AsyncFuncInit asyncFuncInit)
            {
                // 编译异步函数定义
                asyncFuncInit.Accept(visitor);
            }
        }
    }

    /// <summary>
    /// 编译函数定义
    /// </summary>
    public FunctionMetadata CompileFunction(string funcName, List<string> parameters, List<string> parameterTypes, List<object?> defaultValues,
        BlockStatement body, int paramsParameterIndex = -1, List<string>? capturedVars = null, string returnType = "")
    {
        // 检测函数是否包含yield语句
        bool isGenerator = ContainsYieldStatement(body);

        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            ParameterTypes = parameterTypes,
            DefaultValues = defaultValues,
            ReturnType = returnType,
            ParamsParameterIndex = paramsParameterIndex,
            IsAsync = false,
            IsGenerator = isGenerator // 设置生成器标记
        };

        var oldFunc = _currentFunction;
        _currentFunction = func;
        // 函数有独立的作用域和局部变量索引空间，不继承父作用域
        _scopes.Push(new Scope(null));

        // 声明参数为局部变量
        foreach (var param in parameters)
            DeclareLocalVariable(param);

        // 如果有捕获的变量，声明它们为局部变量
        var capturedVarIndices = new Dictionary<string, int>();
        if (capturedVars != null && capturedVars.Count > 0)
        {
            foreach (var varName in capturedVars)
            {
                int index = DeclareLocalVariable(varName);
                capturedVarIndices[varName] = index;
            }
        }

        // 保存捕获的变量列表到函数元数据
        if (capturedVars != null && capturedVars.Count > 0)
        {
            func.CapturedVariables = new List<string>(capturedVars);
        }

        // 编译函数体
        var visitor = new BytecodeVisitor(this);

        // 如果有捕获的变量，在函数体开始时加载它们的值
        if (capturedVars != null && capturedVars.Count > 0)
        {
            // 需要在函数体之前插入加载指令
            var tempInstructions = new List<Instruction>();

            // 加载并立即存储每个捕获的变量（避免栈顺序错乱）
            foreach (var varName in capturedVars)
            {
                tempInstructions.Add(new Instruction(OpCode.LoadGlobal, varName));
                int localIndex = capturedVarIndices[varName];
                tempInstructions.Add(new Instruction(OpCode.StoreLocal, localIndex));
            }

            // 编译函数体
            body.Accept(visitor);

            // 将加载指令插入到函数体指令之前
            var bodyInstructions = visitor.GetInstructions();

            // 重要：调整函数体中所有跳转指令的目标地址
            // 因为插入了额外的指令，所有跳转目标都需要偏移
            int offset = tempInstructions.Count;
            for (int i = 0; i < bodyInstructions.Count; i++)
            {
                var instr = bodyInstructions[i];
                // 检查是否是跳转指令
                if (instr.OpCode == OpCode.Jump ||
                    instr.OpCode == OpCode.JumpIfFalse ||
                    instr.OpCode == OpCode.JumpIfTrue)
                {
                    if (instr.Operand is int target)
                    {
                        // 调整跳转目标，加上偏移量
                        bodyInstructions[i] = new Instruction(instr.OpCode, target + offset);
                    }
                }
            }

            var allInstructions = tempInstructions.Concat(bodyInstructions).ToList();

            func.Instructions = allInstructions;
            func.MaxStackSize = visitor.MaxStackSize;
        }
        else
        {
            body.Accept(visitor);
            func.Instructions = visitor.GetInstructions();
            func.MaxStackSize = visitor.MaxStackSize;
        }

        func.LocalCount = _scopes.Peek().LocalCount;

        LeaveScope();
        _currentFunction = oldFunc;

        _bytecodeFile.Functions.Add(func);

        return func;
    }

    /// <summary>
    /// 编译异步函数
    /// </summary>
    public FunctionMetadata CompileAsyncFunction(string funcName, List<string> parameters, List<string> parameterTypes, List<object?> defaultValues,
        BlockStatement body, int paramsParameterIndex = -1, string returnType = "")
    {
        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            ParameterTypes = parameterTypes,
            DefaultValues = defaultValues,
            ReturnType = returnType,
            ParamsParameterIndex = paramsParameterIndex,
            IsAsync = true // 标记为异步函数
        };

        var oldFunc = _currentFunction;
        _currentFunction = func;
        // 函数有独立的作用域和局部变量索引空间，不继承父作用域
        _scopes.Push(new Scope(null));

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
    public FunctionMetadata CompileAsyncGeneratorFunction(string funcName, List<string> parameters, List<string> parameterTypes,
        List<object?> defaultValues, BlockStatement body, int paramsParameterIndex = -1, string returnType = "")
    {
        // 检测函数是否包含yield语句
        bool isGenerator = ContainsYieldStatement(body);

        var func = new FunctionMetadata
        {
            Name = funcName,
            Parameters = parameters,
            ParameterTypes = parameterTypes,
            DefaultValues = defaultValues,
            ReturnType = returnType,
            ParamsParameterIndex = paramsParameterIndex,
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

    public int DeclareLocalVariable(string name, string type = "")
    {
        return _scopes.Peek().DeclareLocal(name, type);
    }

    public bool IsLocalVariable(string name)
    {
        return _scopes.Peek().HasLocal(name);
    }

    public int GetLocalIndex(string name)
    {
        return _scopes.Peek().GetLocalIndex(name);
    }

    public string GetLocalType(string name)
    {
        return _scopes.Peek().GetLocalType(name);
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

    /// <summary>
    /// 检查变量是否在当前函数的捕获变量列表中
    /// </summary>
    public bool IsCapturedVariable(string name)
    {
        return _currentFunction?.CapturedVariables.Contains(name) ?? false;
    }

    /// <summary>
    /// 检查当前是否在主函数的顶层作用域中
    /// </summary>
    /// <returns>如果在主函数的顶层作用域中返回true</returns>
    public bool IsInMainFunctionTopLevel()
    {
        // 主函数名为 "<main>"，且作用域深度为 2（全局作用域 + 主函数作用域）
        return _currentFunction?.Name == "<main>" && _scopes.Count == 2;
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
                "SemaphoreCreate" or "SemaphoreAcquire" or "SemaphoreRelease" or
                // 线程相关
                "Spawn" or "spawn" => true,
            _ => false
        };
    }

    // ===== 类定义管理 =====

    public void DeclareClass(string className, List<string> fields)
    {
        DeclareClass(className, fields.Select(f => (f, "", (LangExpression?)null)).ToList(), [], [], null);
    }

    public void DeclareClass(string className, List<(string fieldName, LangExpression? initialValue)> fields,
        List<(string fieldName, LangExpression initialValue)> staticFields,
        List<(string methodName, FuncLangValue funcValue, bool isStatic, AccessModifier accessModifier)> methods,
        string? parentClassName,
        List<string>? implementsNames = null,
        List<string>? mixinNames = null)
    {
        // 转换为带类型的字段列表（类型为空字符串）
        var fieldsWithTypes = fields.Select(f => (f.fieldName, "", f.initialValue)).ToList();
        var staticFieldsWithTypes = staticFields.Select(f => (f.fieldName, "", f.initialValue)).ToList();
        DeclareClass(className, fieldsWithTypes, staticFieldsWithTypes, methods, parentClassName, implementsNames, mixinNames);
    }

    public void DeclareClass(string className, List<(string fieldName, string fieldType, LangExpression? initialValue)> fields,
        List<(string fieldName, string fieldType, LangExpression initialValue)> staticFields,
        List<(string methodName, FuncLangValue funcValue, bool isStatic, AccessModifier accessModifier)> methods,
        string? parentClassName,
        List<string>? implementsNames = null,
        List<string>? mixinNames = null)
    {
        var classMetadata = new ClassMetadata
        {
            Name = className,
            BaseClassName = parentClassName,
            InterfaceNames = implementsNames ?? [],
            ImplementsInterfaces = implementsNames ?? [],
            Mixins = mixinNames ?? []
        };

        // 处理实例字段
        foreach (var (fieldName, fieldType, initialValue) in fields)
        {
            var fieldMetadata = new FieldMetadata
            {
                Name = fieldName,
                IsStatic = false,
                TypeName = string.IsNullOrEmpty(fieldType) ? null : fieldType
            };

            // 如果有初始值，计算并添加到常量池
            if (initialValue != null)
            {
                var constantValue = EvaluateConstantExpression(initialValue);

                // 处理 null 默认值
                if (constantValue == null)
                {
                    fieldMetadata.IsDefaultNull = true;
                    fieldMetadata.DefaultValueIndex = -1;
                }
                else
                {
                    int constantIndex = ConstantPool.AddConstant(constantValue);
                    fieldMetadata.DefaultValueIndex = constantIndex;
                }
            }
            else
            {
                // 没有初始值，默认为 null
                fieldMetadata.IsDefaultNull = true;
                fieldMetadata.DefaultValueIndex = -1;
            }

            classMetadata.Fields.Add(fieldMetadata);
        }

        // 处理静态字段
        foreach (var (fieldName, fieldType, initialValue) in staticFields)
        {
            // 计算静态字段的初始值并添加到常量池
            var constantValue = EvaluateConstantExpression(initialValue);

            // 创建静态字段元数据
            var staticFieldMetadata = new FieldMetadata
            {
                Name = fieldName,
                IsStatic = true,
                TypeName = string.IsNullOrEmpty(fieldType) ? null : fieldType
            };

            // 处理 null 默认值
            if (constantValue == null)
            {
                staticFieldMetadata.IsDefaultNull = true;
                staticFieldMetadata.DefaultValueIndex = -1;
            }
            else
            {
                int constantIndex = ConstantPool.AddConstant(constantValue);
                staticFieldMetadata.DefaultValueIndex = constantIndex;
            }

            classMetadata.StaticFields.Add(staticFieldMetadata);
        }

        // 先将类元数据添加到字节码文件中
        // 这样在编译方法体时，IsClassName 可以正确识别类名
        _bytecodeFile.Classes.Add(classMetadata);

        // 编译所有方法
        foreach (var (methodName, funcValue, isStatic, accessModifier) in methods)
        {
            // 提取方法参数
            var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? [];
            var paramTypes = funcValue.Ids?.Select(id => id.AssumptionType ?? "").ToList() ?? [];

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

            // 实例方法需要在参数列表开头添加 this 参数
            // 因为 CallMethod 指令会将对象作为第一个参数传递
            if (!isStatic)
            {
                paramNames.Insert(0, "this");
                paramTypes.Insert(0, className); // this 的类型是类名
                defaultValues.Insert(0, null);
            }

            // 编译方法体
            var functionMetadata = CompileFunction(
                $"{className}.{methodName}",
                paramNames,
                paramTypes,
                defaultValues,
                funcValue.BlockStatement
            );

            // 创建方法元数据
            var methodMetadata = new MethodMetadata
            {
                Name = methodName,
                IsStatic = isStatic,
                AccessModifier = accessModifier,
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
    }

    public bool IsClassName(string name)
    {
        return _bytecodeFile.Classes.Any(c => c.Name == name);
    }

    /// <summary>
    /// 获取类元数据
    /// </summary>
    public ClassMetadata? GetClassMetadata(string name)
    {
        return _bytecodeFile.Classes.FirstOrDefault(c => c.Name == name);
    }

    // ===== 泛型类和函数管理 =====

    /// <summary>
    /// 注册泛型类定义
    /// </summary>
    public void RegisterGenericClass(string className, TypeTemplate typeTemplate)
    {
        GenericClasses[className] = typeTemplate;
    }

    /// <summary>
    /// 注册泛型函数定义
    /// </summary>
    public void RegisterGenericFunction(string funcName, FuncLangValue funcValue)
    {
        GenericFunctions[funcName] = funcValue;
    }

    /// <summary>
    /// 检查是否是泛型类
    /// </summary>
    public bool IsGenericClass(string name)
    {
        return GenericClasses.ContainsKey(name);
    }

    /// <summary>
    /// 检查是否是泛型函数
    /// </summary>
    public bool IsGenericFunction(string name)
    {
        return GenericFunctions.ContainsKey(name);
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
        private readonly Dictionary<string, string> _localTypes = new();
        private int _nextIndex = parent?._nextIndex ?? 0;

        public int DeclareLocal(string name, string type = "")
        {
            if (_locals.TryGetValue(name, out var local))
            {
                // 如果变量已存在，更新类型（如果有新类型）
                if (!string.IsNullOrEmpty(type))
                {
                    _localTypes[name] = type;
                }

                return local;
            }

            int index = _nextIndex++;
            _locals[name] = index;
            if (!string.IsNullOrEmpty(type))
            {
                _localTypes[name] = type;
            }

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

        public string GetLocalType(string name)
        {
            if (_localTypes.TryGetValue(name, out string? type))
                return type;
            return parent?.GetLocalType(name) ?? "";
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

    // ===== 接口和 Mixin 支持 =====

    /// <summary>
    /// 声明接口定义
    /// </summary>
    public void DeclareInterface(string interfaceName, List<string> methods, List<string> parentInterfaces)
    {
        var interfaceMetadata = new InterfaceMetadata
        {
            Name = interfaceName,
            Methods = methods,
            ParentInterfaces = parentInterfaces
        };

        _bytecodeFile.Interfaces.Add(interfaceMetadata);
    }

    /// <summary>
    /// 声明 Mixin 定义
    /// </summary>
    public void DeclareMixin(string mixinName, List<(string methodName, FuncLangValue funcValue)> methods)
    {
        var mixinMetadata = new MixinMetadata
        {
            Name = mixinName,
            Methods = []
        };

        // 编译所有 Mixin 方法
        foreach (var (methodName, funcValue) in methods)
        {
            var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? [];
            var paramTypes = funcValue.Ids?.Select(id => id.AssumptionType ?? "").ToList() ?? [];
            var defaultValues = new List<object?>();

            if (funcValue.Ids != null)
            {
                foreach (var param in funcValue.Ids)
                {
                    defaultValues.Add(param.DefaultValue != null
                        ? EvaluateConstantExpression(param.DefaultValue)
                        : null);
                }
            }

            // Mixin 方法需要在参数列表开头添加 this 参数
            // 因为 CallMethod 指令会将对象作为第一个参数传递
            paramNames.Insert(0, "this");
            paramTypes.Insert(0, mixinName); // this 的类型是 mixin 名
            defaultValues.Insert(0, null);

            var functionMetadata = CompileFunction(
                $"{mixinName}.{methodName}",
                paramNames,
                paramTypes,
                defaultValues,
                funcValue.BlockStatement
            );

            mixinMetadata.Methods.Add(new MethodMetadata
            {
                Name = methodName,
                IsStatic = false,
                Function = functionMetadata
            });
        }

        _bytecodeFile.Mixins.Add(mixinMetadata);
    }
}