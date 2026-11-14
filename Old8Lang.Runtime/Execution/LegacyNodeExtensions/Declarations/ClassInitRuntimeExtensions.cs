using Old8Lang.Syntax;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using LangObject = Old8Lang.Runtime.LangObject;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// ClassInit 的执行/编译辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
public static partial class ClassInitRuntimeExtensions
{
    /// <summary>运算符方法名集合。</summary>
    private static readonly HashSet<string> OperatorNames =
    [
        "_add", "_sub", "_mul", "_div", "_mod", "_pow",
        "_eq", "_lt", "_gt", "_le", "_ge",
        "_getitem", "_setitem"
    ];

    /// <summary>
    /// 递归注册嵌套类
    /// </summary>
    /// <param name="manager">变量管理器</param>
    internal static void RegisterNestedClasses(this ClassInit node, VariateManager manager)
    {
        // 查找Variates中的嵌套类
        foreach (var (_, memberExpr) in node.GetTypeTemplate().Variates)
        {
            if (memberExpr is TypeTemplate nestedTypeTemplate)
            {
                // 递归注册嵌套类的嵌套类
                var nestedClassInit = ClassInitRuntimeExtensions.CreateForTemplate(nestedTypeTemplate);
                nestedClassInit.RegisterNestedClasses(manager);

                // 注册嵌套类到管理器
                manager.AddClassAndFunc(nestedTypeTemplate);
            }
        }

        // 查找StaticVariates中的嵌套类
        foreach (var (_, memberExpr) in node.GetTypeTemplate().StaticVariates)
        {
            if (memberExpr is TypeTemplate nestedTypeTemplate)
            {
                // 递归注册嵌套类的嵌套类
                var nestedClassInit = ClassInitRuntimeExtensions.CreateForTemplate(nestedTypeTemplate);
                nestedClassInit.RegisterNestedClasses(manager);

                // 注册嵌套类到管理器
                manager.AddClassAndFunc(nestedTypeTemplate);
            }
        }
    }
    /// <summary>
    /// 定义类的字段
    /// </summary>
    internal static List<(FieldBuilder, LangExpression)> DefineFields(this ClassInit node, TypeBuilder typeBuilder, LocalManager local)
    {
        var fields = new List<(ClassMemberId, LangExpression)>();
        var fieldBuilders = new List<(FieldBuilder, LangExpression)>();

        // 首先，如果有父类，将父类的字段信息复制到当前类的FieldVar中
        if (node.GetTypeTemplate().ParentClassName is not null &&
            local.ClassVar.TryGetValue(node.GetTypeTemplate().ParentClassName, out var parentType))
        {
            // 继承字段（含 protected）需要在子类方法中参与类型推断与直接访问。
            var parentFields = parentType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var parentField in parentFields)
            {
                // 将父类字段添加到FieldVar中，这样子类方法就能访问父类字段
                local.FieldVar[parentField.Name] = parentField;
            }
        }

        foreach (var variate in node.GetTypeTemplate().Variates.Concat(node.GetTypeTemplate().StaticVariates))
        {
            if (variate.Value is not FuncLangValue && variate.Value is not MethodOverloadList)
            {
                fields.Add((variate.Key, variate.Value));
            }
        }

        // 定义实例字段
        foreach (var (memberId, expr) in fields)
        {
            // 获取字段类型
            Type fieldType;
            try
            {
                fieldType = !string.IsNullOrEmpty(memberId.AssumptionType)
                    ? memberId.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)
                    : expr is ListLiteralExpression { Elements.Count: 0 } or ListLangValue { Value.Count: 0, Values.Count: 0 }
                        ? typeof(object)
                        : expr.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            }
            catch
            {
                // 如果无法确定字段类型，使用object
                fieldType = typeof(object);
            }

            // 根据修饰符设置字段属性
            var fieldAttributes = FieldAttributes.Public; // 默认是公共字段
            
            if (memberId.HasModifier(AccessModifierType.Private))
            {
                fieldAttributes = FieldAttributes.Private;
            }
            else if (memberId.HasModifier(AccessModifierType.Protected))
            {
                fieldAttributes = FieldAttributes.Family;
            }
            else if (!memberId.HasModifier(AccessModifierType.Public))
            {
                // 如果没有明确指定访问修饰符，使用默认的公共访问
                fieldAttributes = FieldAttributes.Public;
            }
            
            if (memberId.HasModifier(AccessModifierType.Static))
            {
                fieldAttributes |= FieldAttributes.Static;
            }
            
            // 定义字段
            var fieldBuilder = typeBuilder.DefineField(
                memberId.IdName,
                fieldType,
                fieldAttributes);

            // 保存字段信息到 LocalManager，以便在方法中访问
            // 如果父类已经有同名字段，子类的字段会覆盖它
            local.FieldVar[memberId.IdName] = fieldBuilder;

            // 保存字段信息用于构造函数初始化
            if (!memberId.HasModifier(AccessModifierType.Static))
            {
                fieldBuilders.Add((fieldBuilder, expr));
            }
        }

        return fieldBuilders;
    }
    /// <summary>
    /// 定义类的方法
    /// </summary>
    internal static void DefineMethods(this ClassInit node, TypeBuilder typeBuilder, LocalManager local)
    {
        var methods = new List<(ClassMemberId, FuncLangValue)>();

        foreach (var variate in node.GetTypeTemplate().Variates)
        {
            if (variate.Value is FuncLangValue funcValue)
            {
                methods.Add((variate.Key, funcValue));
            }
            else if (variate.Value is MethodOverloadList overloadList)
            {
                foreach (var overload in overloadList.Overloads)
                {
                    methods.Add((variate.Key, overload));
                }
            }
        }

        // 定义实例方法
        foreach (var (memberId, funcValue) in methods)
        {
            node.DefineMethod(typeBuilder, memberId, funcValue, local);
        }

        // 静态成员存放在独立的 StaticVariates 表中，不能遗漏，否则 IL 类型只会
        // 声明实例成员，诸如 Base.count / Derived.increment() 会被当作普通变量处理。
        foreach (var (memberId, memberValue) in node.GetTypeTemplate().StaticVariates)
        {
            if (memberValue is FuncLangValue funcValue)
            {
                node.DefineMethodInternal(typeBuilder, memberId, funcValue, local,
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
            }
        }
    }
    /// <summary>
    /// 定义接口的成员（只能有方法，不能有字段）
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="local">局部变量管理器</param>
    internal static void DefineInterfaceMembers(this ClassInit node, TypeBuilder typeBuilder, LocalManager local)
    {
        // 接口只能有方法，不能有字段
        foreach (var variate in node.GetTypeTemplate().Variates)
        {
            if (variate.Value is FuncLangValue funcValue)
            {
                // 接口方法必须是抽象的，不能有实现
                node.DefineInterfaceMethod(typeBuilder, variate.Key, funcValue, local);
            }
        }
    }
    /// <summary>
    /// 定义类的构造函数
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="baseType">基类类型</param>
    /// <param name="fieldBuilders">字段列表</param>
    /// <param name="local">局部变量管理器</param>
    internal static void DefineConstructor(this ClassInit node, TypeBuilder typeBuilder, Type baseType, List<(FieldBuilder, LangExpression)> fieldBuilders, LocalManager local)
    {
        // 定义无参数构造函数
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
            CallingConventions.Standard | CallingConventions.HasThis,
            Type.EmptyTypes);

        // 保存到LocalManager，以便在方法中创建实例
        local.CurrentConstructorBuilder = constructorBuilder;
        
        var ctorIl = constructorBuilder.GetILGenerator();

        // 1. 调用基类的无参数构造函数
        ctorIl.Emit(OpCodes.Ldarg_0);
        var baseCtor = baseType.GetConstructor(Type.EmptyTypes) ??
                       baseType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           .FirstOrDefault(ctor => ctor.GetParameters().Length == 0);

        if (baseCtor is not null)
        {
            ctorIl.Emit(OpCodes.Call, baseCtor);
        }

        // 2. 初始化实例字段
        foreach (var (fieldBuilder, initExpr) in fieldBuilders)
        {
            // 未初始化的值类型字段由 CLR 赋予默认值，不能向其写入 null。
            if (initExpr is ILiteralSyntax { Kind: LiteralKind.Null } && fieldBuilder.FieldType.IsValueType)
            {
                continue;
            }
            // 加载 this
            ctorIl.Emit(OpCodes.Ldarg_0);

            // 创建临时 LocalManager 用于字段初始化
            var tempLocal = new LocalManager
            {
                FilePath = "",
                Interpreter = null,
                Globals = local.Globals
            };

            // 加载字段的初始值
            initExpr.Accept(RuntimeVisitors.CreateCompiler(ctorIl, tempLocal));

            var initializerType = initExpr.Accept(new TypeInferenceVisitor(tempLocal)) ?? typeof(object);
            if (fieldBuilder.FieldType == typeof(object) && initializerType.IsValueType)
            {
                ctorIl.Emit(OpCodes.Box, initializerType);
            }
            else if (fieldBuilder.FieldType.IsValueType && initializerType == typeof(object))
            {
                ctorIl.Emit(OpCodes.Unbox_Any, fieldBuilder.FieldType);
            }

            // 存储到字段
            ctorIl.Emit(OpCodes.Stfld, fieldBuilder);
        }

        // 3. 返回
        ctorIl.Emit(OpCodes.Ret);
    }
    /// <summary>
    /// 定义接口的方法
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="memberId">成员ID</param>
    /// <param name="funcValue">函数值</param>
    /// <param name="local">局部变量管理器</param>
    internal static void DefineInterfaceMethod(this ClassInit node, TypeBuilder typeBuilder, ClassMemberId memberId, FuncLangValue funcValue,
        LocalManager local)
    {
        // 获取方法名称
        string methodName = memberId.IdName;

        // 创建一个新的LocalManager实例，继承外部的函数和类定义
        var methodLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder,
            CurrentConstructorBuilder = local.CurrentConstructorBuilder,
            CurrentInitMethodBuilder = local.CurrentInitMethodBuilder,
            Globals = local.Globals
        };

        // 继承外部的函数定义（包括内置函数如 PrintLine）
        foreach (var kvp in local.DelegateVar)
        {
            methodLocal.DelegateVar[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in local.GlobalStaticClasses)
            methodLocal.GlobalStaticClasses[kvp.Key] = kvp.Value;

        // 继承外部的类定义
        foreach (var kvp in local.ClassVar)
        {
            methodLocal.ClassVar[kvp.Key] = kvp.Value;
        }

        // 获取参数类型（不包含 this，this 是隐式的）
        var parameterTypes = new List<Type>();

        // 添加方法参数，并将它们添加到LocalVarTypes中，以便OutputType能正确推断类型
        if (funcValue.Ids is not null)
        {
            foreach (var paramId in funcValue.Ids)
            {
                Type paramType;
                try
                {
                    paramType = paramId.Accept(new TypeInferenceVisitor(methodLocal))!;
                }
                catch
                {
                    paramType = typeof(object);
                }

                parameterTypes.Add(paramType!);
                // 将参数类型添加到LocalVarTypes中，以便在获取返回类型时能正确推断
                methodLocal.LocalVarTypes[paramId.IdName] = paramType;
            }
        }

        // 获取返回类型（现在methodLocal中已经有参数信息了）
        Type returnType;
        try
        {
            returnType = funcValue.Accept(new TypeInferenceVisitor(methodLocal))!;
        }
        catch
        {
            returnType = typeof(void);
        }

        // 定义接口方法（必须是抽象的）
        var methodBuilder = typeBuilder.DefineMethod(
            methodName,
            MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual,
            returnType,
            parameterTypes.ToArray());
    }
    /// <summary>
    /// 定义类的方法
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="memberId">成员ID</param>
    /// <param name="funcValue">函数值</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>方法构建器</returns>
    internal static MethodBuilder DefineMethod(this ClassInit node, TypeBuilder typeBuilder, ClassMemberId memberId, FuncLangValue funcValue,
        LocalManager local)
    {
        return node.DefineMethodInternal(typeBuilder, memberId, funcValue, local, 
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
    }
    /// <summary>
    /// 内部方法，用于定义方法
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="memberId">成员ID</param>
    /// <param name="funcValue">函数值</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="attributes">方法属性</param>
    /// <returns>方法构建器</returns>
    internal static MethodBuilder DefineMethodInternal(this ClassInit node, TypeBuilder typeBuilder, ClassMemberId memberId, FuncLangValue funcValue,
        LocalManager local, MethodAttributes attributes)
    {
        // 获取方法名称
        string methodName = memberId.IdName;

        // 创建一个新的LocalManager实例，继承外部的函数和类定义
        var methodLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder,
            CurrentConstructorBuilder = local.CurrentConstructorBuilder,
            Globals = local.Globals
        };

        // 继承外部的函数定义（包括内置函数如 PrintLine）
        foreach (var kvp in local.DelegateVar)
        {
            methodLocal.DelegateVar[kvp.Key] = kvp.Value;
        }

        // 继承外部的类定义
        foreach (var kvp in local.ClassVar)
        {
            methodLocal.ClassVar[kvp.Key] = kvp.Value;
        }

        // 继承字段定义
        foreach (var kvp in local.FieldVar)
        {
            methodLocal.FieldVar[kvp.Key] = kvp.Value;
        }

        // 获取参数类型（不包含 this，this 是隐式的）
        var parameterTypes = new List<Type>();

        // 添加方法参数，并将它们添加到LocalVarTypes中，以便OutputType能正确推断类型
        if (funcValue.Ids is not null)
        {
            foreach (var paramId in funcValue.Ids)
            {
                Type paramType;
                try
                {
                    paramType = paramId.Accept(new TypeInferenceVisitor(methodLocal))!;
                }
                catch
                {
                    paramType = typeof(object);
                }

                parameterTypes.Add(paramType!);
                // 将参数类型添加到LocalVarTypes中，以便在获取返回类型时能正确推断
                methodLocal.LocalVarTypes[paramId.IdName] = paramType;
            }
        }

        // 获取返回类型（现在methodLocal中已经有参数信息了）
        Type returnType;
        try
        {
            returnType = funcValue.Accept(new TypeInferenceVisitor(methodLocal))!;
        }
        catch
        {
            returnType = typeof(void);
        }

        // 定义方法
        var methodBuilder = typeBuilder.DefineMethod(
            methodName,
            attributes,
            returnType,
            parameterTypes.ToArray());

        // 创建方法的IL生成器
        var methodIl = methodBuilder.GetILGenerator();

        // ReturnStatement 使用此类型校验并转换返回值，包括 object 到值类型的拆箱。
        if (returnType != typeof(void))
        {
            methodLocal.ReturnValueLocal = methodIl.DeclareLocal(returnType!);
        }

        // 处理方法参数
        // 对于实例方法，参数索引从 1 开始（0 是 this）
        // 对于静态方法，参数索引从 0 开始
        int paramIndex = (attributes & MethodAttributes.Static) == 0 ? 1 : 0;
        if (funcValue.Ids is not null)
        {
            int paramTypeIndex = 0;
            foreach (var paramId in funcValue.Ids)
            {
                // 声明局部变量
                var paramType = parameterTypes[paramTypeIndex];
                var localVar = methodIl.DeclareLocal(paramType);

                // 加载参数并存储到局部变量
                methodIl.Emit(OpCodes.Ldarg, paramIndex);
                methodIl.Emit(OpCodes.Stloc, localVar);

                // 将参数添加到新的LocalManager中，完全隔离
                methodLocal.AddLocalVar(paramId.IdName, localVar);

                paramIndex++;
                paramTypeIndex++;
            }
        }

        // 生成方法体的IL代码
        funcValue.BlockStatement.Accept(RuntimeVisitors.CreateCompiler(methodIl, methodLocal));

        // 检查方法体的最后一个语句是否是 ReturnStatement
        var lastStatement = funcValue.BlockStatement.Count > 0
            ? funcValue.BlockStatement[^1]
            : null;

        // 只有当最后一个语句不是 ReturnStatement 时，才添加 Ret 指令
        if (lastStatement is not ReturnStatement)
        {
            // 对于 void 方法，添加 Ret 指令
            // 对于有返回值的方法，如果没有显式 return，这里会导致栈不平衡，但这是用户代码的问题
            methodIl.Emit(OpCodes.Ret);
        }

        // 检查是否是运算符重载方法，如果是，生成桥接方法
        if (OperatorNames.Contains(methodName))
        {
            node.GenerateOperatorBridge(typeBuilder, methodName, methodBuilder, parameterTypes.ToArray(), returnType);
        }

        // 如果是 init 方法，保存到 LocalManager
        if (methodName == "init")
        {
            local.CurrentInitMethodBuilder = methodBuilder;
        }

        return methodBuilder;
    }
    /// <summary>
    /// 生成运算符重载的桥接方法
    /// </summary>
    internal static void GenerateOperatorBridge(this ClassInit node, TypeBuilder typeBuilder, string methodName, MethodBuilder userMethod, Type[] paramTypes, Type returnType)
    {
        // 1. 确定期望的方法签名
        Type expectedReturnType = typeof(object);
        Type[] expectedParamTypes = [typeof(object)];
        
        if (methodName == "_setitem") 
        {
            expectedReturnType = typeof(void);
            expectedParamTypes = [typeof(object), typeof(object)];
        } 
        else if (methodName == "_eq" || methodName == "_lt" || methodName == "_gt" || methodName == "_le" || methodName == "_ge") 
        {
            expectedReturnType = typeof(bool);
            expectedParamTypes = [typeof(object)];
        }

        // 2. 检查用户方法是否已经匹配签名
        bool signatureMatches = returnType == expectedReturnType && 
                                paramTypes.Length == expectedParamTypes.Length &&
                                paramTypes.SequenceEqual(expectedParamTypes);

        if (signatureMatches) 
        {
            // 如果签名已经匹配，不需要生成桥接方法
            // 运行时会自动将其作为虚方法重写
            return; 
        }

        // 3. 定义桥接方法
        // 注意：这里显式定义为重写基类方法
        var bridgeMethod = typeBuilder.DefineMethod(
            methodName,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            expectedReturnType,
            expectedParamTypes);

        var il = bridgeMethod.GetILGenerator();

        // 4. 加载 this
        il.Emit(OpCodes.Ldarg_0);

        // 5. 加载参数并进行转换
        for (int i = 0; i < expectedParamTypes.Length; i++) 
        {
            il.Emit(OpCodes.Ldarg, i + 1);
            if (i < paramTypes.Length) 
            {
                var targetType = paramTypes[i];
                if (targetType != typeof(object))
                {
                    il.Emit(targetType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, targetType);
                }
            }
        }

        // 6. 调用用户方法
        il.Emit(OpCodes.Call, userMethod);

        // 7. 处理返回值
        if (returnType != expectedReturnType) 
        {
             if (expectedReturnType == typeof(object) && returnType.IsValueType) 
             {
                 il.Emit(OpCodes.Box, returnType);
             }
             // 其他类型的转换暂时不处理，假定用户返回类型兼容
        }

        il.Emit(OpCodes.Ret);
    }
}
