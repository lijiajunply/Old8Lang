using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 类定义语句，用于处理Old8Lang中的类声明
/// </summary>
/// <param name="anyLangValue">类模板信息，包含类名、父类、成员变量和方法等</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public class ClassInit(TypeTemplate anyLangValue, SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 类模板信息，包含类的完整定义
    /// </summary>
    private readonly TypeTemplate anyLangValue = anyLangValue;

    /// <summary>
    /// 公共属性，用于访问类模板信息
    /// </summary>
    public TypeTemplate AnyLangValue => anyLangValue;
    
    /// <summary>
    /// 在解释模式下执行类定义
    /// </summary>
    /// <param name="manager">变量管理器，用于管理类的声明和访问</param>
    /// <exception cref="DuplicateNameError">当类名已存在时抛出</exception>
    public override void Run(VariateManager manager)
    {
        // 首先注册所有嵌套类
        RegisterNestedClasses(manager);

        // 检查类是否已存在
        var existingClass = manager.GetAny(new LangId(anyLangValue.ClassName));

        if (existingClass != null)
        {
            throw new DuplicateNameError(this, anyLangValue.ClassName, "类");
        }

        // 立即将类添加到ImportInfos中，以便在类定义内部访问
        manager.AddClassAndFunc(anyLangValue);
    }

    /// <summary>
    /// 递归注册嵌套类
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void RegisterNestedClasses(VariateManager manager)
    {
        // 查找Variates中的嵌套类
        foreach (var (memberId, memberExpr) in anyLangValue.Variates)
        {
            if (memberExpr is TypeTemplate nestedTypeTemplate)
            {
                // 递归注册嵌套类的嵌套类
                var nestedClassInit = new ClassInit(nestedTypeTemplate, nestedTypeTemplate.Position);
                nestedClassInit.RegisterNestedClasses(manager);

                // 注册嵌套类到管理器
                manager.AddClassAndFunc(nestedTypeTemplate);
            }
        }

        // 查找StaticVariates中的嵌套类
        foreach (var (memberId, memberExpr) in anyLangValue.StaticVariates)
        {
            if (memberExpr is TypeTemplate nestedTypeTemplate)
            {
                // 递归注册嵌套类的嵌套类
                var nestedClassInit = new ClassInit(nestedTypeTemplate, nestedTypeTemplate.Position);
                nestedClassInit.RegisterNestedClasses(manager);

                // 注册嵌套类到管理器
                manager.AddClassAndFunc(nestedTypeTemplate);
            }
        }
    }

    /// <summary>
    /// 在编译模式下生成类或接口的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器，用于管理类或接口的声明和访问</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 1. 检查类或接口是否已经存在
        if (local.ClassVar.ContainsKey(anyLangValue.ClassName))
        {
            // 类或接口已经存在，跳过生成
            return;
        }

        // 2. 检查是否已经有动态程序集和模块
        // 如果没有，创建一个全局的动态程序集和模块
        AssemblyBuilder assemblyBuilder;
        ModuleBuilder moduleBuilder;
        
        if (local.DynamicAssembly == null || local.DynamicModule == null)
        {
            var assemblyName = new AssemblyName("Old8LangDynamicAssembly");
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("Old8LangDynamicModule");
            
            // 保存到local中，以便其他类和接口使用
            local.DynamicAssembly = assemblyBuilder;
            local.DynamicModule = moduleBuilder;
        }
        else
        {
            // 使用现有的动态程序集和模块
            assemblyBuilder = local.DynamicAssembly;
            moduleBuilder = local.DynamicModule;
        }

        // 3. 定义基类或接口的基础类型
        Type? baseType = null;
        TypeAttributes typeAttributes;
        
        if (anyLangValue.IsInterface)
        {
            // 接口的处理
            typeAttributes = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract;
        }
        else
        {
            // 类的处理
            typeAttributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;
            
            // 定义基类
            if (anyLangValue.ParentClassName != null &&
                local.ClassVar.TryGetValue(anyLangValue.ParentClassName, out var parentType))
            {
                baseType = parentType;
            }
            else
            {
                // 如果没有指定父类或父类不存在，使用Object作为基类
                baseType = typeof(object);
            }
        }

        // 4. 定义类型（类或接口）
        var typeBuilder = moduleBuilder.DefineType(
            anyLangValue.ClassName,
            typeAttributes,
            baseType);

        // 5. 创建一个新的LocalManager实例，用于生成当前类或接口的IL代码
        // 这样可以避免不同类之间的方法混淆
        var classLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder
        };

        // 6. 定义类或接口的成员
        if (anyLangValue.IsInterface)
        {
            // 接口只能有方法，不能有字段
            DefineInterfaceMembers(typeBuilder, classLocal);
        }
        else
        {
            // 类可以有字段和方法
            var fieldBuilders = DefineClassMembers(typeBuilder, classLocal);

            // 7. 定义类的构造函数
            DefineConstructor(typeBuilder, baseType!, fieldBuilders);
        }

        // 8. 创建类型
        var createdType = typeBuilder.CreateType();

        // 9. 将类型添加到原始LocalManager中，以便其他类可以访问
        local.ClassVar[anyLangValue.ClassName] = createdType;
    }

    /// <summary>
    /// 定义类的成员（字段和方法）
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>字段列表，用于构造函数初始化</returns>
    private List<(FieldBuilder, LangExpression)> DefineClassMembers(TypeBuilder typeBuilder, LocalManager local)
    {
        // 分离字段和方法
        var fields = new List<(ClassMemberId, LangExpression)>();
        var methods = new List<(ClassMemberId, FuncLangValue)>();
        var fieldBuilders = new List<(FieldBuilder, LangExpression)>();

        // 首先，如果有父类，将父类的字段信息复制到当前类的FieldVar中
        if (anyLangValue.ParentClassName != null &&
            local.ClassVar.TryGetValue(anyLangValue.ParentClassName, out var parentType))
        {
            // 获取父类的所有公共字段
            var parentFields = parentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var parentField in parentFields)
            {
                // 将父类字段添加到FieldVar中，这样子类方法就能访问父类字段
                local.FieldVar[parentField.Name] = parentField;
            }
        }

        foreach (var variate in anyLangValue.Variates)
        {
            if (variate.Value is FuncLangValue funcValue)
            {
                methods.Add((variate.Key, funcValue));
            }
            else
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
                fieldType = expr.OutputType(local) ?? typeof(object);
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

        // 定义实例方法
        foreach (var (memberId, funcValue) in methods)
        {
            DefineMethod(typeBuilder, memberId, funcValue, local);
        }

        return fieldBuilders;
    }
    
    /// <summary>
    /// 定义接口的成员（只能有方法，不能有字段）
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="local">局部变量管理器</param>
    private void DefineInterfaceMembers(TypeBuilder typeBuilder, LocalManager local)
    {
        // 接口只能有方法，不能有字段
        foreach (var variate in anyLangValue.Variates)
        {
            if (variate.Value is FuncLangValue funcValue)
            {
                // 接口方法必须是抽象的，不能有实现
                DefineInterfaceMethod(typeBuilder, variate.Key, funcValue, local);
            }
        }
    }
    
    /// <summary>
    /// 定义接口的方法
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="memberId">成员ID</param>
    /// <param name="funcValue">函数值</param>
    /// <param name="local">局部变量管理器</param>
    private void DefineInterfaceMethod(TypeBuilder typeBuilder, ClassMemberId memberId, FuncLangValue funcValue,
        LocalManager local)
    {
        // 获取方法名称
        string methodName = memberId.IdName;

        // 创建一个新的LocalManager实例，继承外部的函数和类定义
        var methodLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder
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

        // 获取参数类型（不包含 this，this 是隐式的）
        var parameterTypes = new List<Type>();

        // 添加方法参数，并将它们添加到LocalVarTypes中，以便OutputType能正确推断类型
        if (funcValue.Ids != null)
        {
            foreach (var paramId in funcValue.Ids)
            {
                Type paramType;
                try
                {
                    paramType = paramId.OutputType(methodLocal);
                }
                catch
                {
                    paramType = typeof(object);
                }

                parameterTypes.Add(paramType);
                // 将参数类型添加到LocalVarTypes中，以便在获取返回类型时能正确推断
                methodLocal.LocalVarTypes[paramId.IdName] = paramType;
            }
        }

        // 获取返回类型（现在methodLocal中已经有参数信息了）
        Type returnType;
        try
        {
            returnType = funcValue.OutputType(methodLocal);
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
    /// 定义类的静态成员（静态字段和静态方法）
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="local">局部变量管理器</param>
    private void DefineStaticMembers(TypeBuilder typeBuilder, LocalManager local)
    {
        foreach (var staticVariate in anyLangValue.StaticVariates)
        {
            if (staticVariate.Value is FuncLangValue funcValue)
            {
                // 直接定义静态方法，不调用DefineMethod
                DefineStaticMethod(typeBuilder, staticVariate.Key, funcValue, local);
            }
            else
            {
                // 定义静态字段
                Type fieldType;
                try
                {
                    fieldType = staticVariate.Value.OutputType(local) ?? typeof(object);
                }
                catch
                {
                    fieldType = typeof(object);
                }

                typeBuilder.DefineField(
                    staticVariate.Key.IdName,
                    fieldType,
                    FieldAttributes.Public | FieldAttributes.Static);
            }
        }
    }

    /// <summary>
    /// 定义类的方法
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="memberId">成员ID</param>
    /// <param name="funcValue">函数值</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>方法构建器</returns>
    private MethodBuilder DefineMethod(TypeBuilder typeBuilder, ClassMemberId memberId, FuncLangValue funcValue,
        LocalManager local)
    {
        return DefineMethodInternal(typeBuilder, memberId, funcValue, local, MethodAttributes.Public);
    }

    /// <summary>
    /// 定义静态方法
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="memberId">成员ID</param>
    /// <param name="funcValue">函数值</param>
    /// <param name="local">局部变量管理器</param>
    private void DefineStaticMethod(TypeBuilder typeBuilder, ClassMemberId memberId, FuncLangValue funcValue,
        LocalManager local)
    {
        DefineMethodInternal(typeBuilder, memberId, funcValue, local,
            MethodAttributes.Public | MethodAttributes.Static);
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
    private MethodBuilder DefineMethodInternal(TypeBuilder typeBuilder, ClassMemberId memberId, FuncLangValue funcValue,
        LocalManager local, MethodAttributes attributes)
    {
        // 获取方法名称
        string methodName = memberId.IdName;

        // 创建一个新的LocalManager实例，继承外部的函数和类定义
        var methodLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder
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
        if (funcValue.Ids != null)
        {
            foreach (var paramId in funcValue.Ids)
            {
                Type paramType;
                try
                {
                    paramType = paramId.OutputType(methodLocal);
                }
                catch
                {
                    paramType = typeof(object);
                }

                parameterTypes.Add(paramType);
                // 将参数类型添加到LocalVarTypes中，以便在获取返回类型时能正确推断
                methodLocal.LocalVarTypes[paramId.IdName] = paramType;
            }
        }

        // 获取返回类型（现在methodLocal中已经有参数信息了）
        Type returnType;
        try
        {
            returnType = funcValue.OutputType(methodLocal);
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

        // 处理方法参数
        // 对于实例方法，参数索引从 1 开始（0 是 this）
        // 对于静态方法，参数索引从 0 开始
        int paramIndex = (attributes & MethodAttributes.Static) == 0 ? 1 : 0;
        if (funcValue.Ids != null)
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
        funcValue.BlockStatement.GenerateIl(methodIl, methodLocal);

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

        return methodBuilder;
    }

    /// <summary>
    /// 定义类的构造函数
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="baseType">基类类型</param>
    /// <param name="fieldBuilders">字段列表</param>
    private void DefineConstructor(TypeBuilder typeBuilder, Type baseType, List<(FieldBuilder, LangExpression)> fieldBuilders)
    {
        // 定义无参数构造函数
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            Type.EmptyTypes);

        var ctorIl = constructorBuilder.GetILGenerator();

        // 1. 调用基类的无参数构造函数
        ctorIl.Emit(OpCodes.Ldarg_0);
        var baseCtor = baseType.GetConstructor(Type.EmptyTypes) ??
                       baseType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           .FirstOrDefault(ctor => ctor.GetParameters().Length == 0);

        if (baseCtor != null)
        {
            ctorIl.Emit(OpCodes.Call, baseCtor);
        }

        // 2. 初始化实例字段
        foreach (var (fieldBuilder, initExpr) in fieldBuilders)
        {
            // 加载 this
            ctorIl.Emit(OpCodes.Ldarg_0);

            // 创建临时 LocalManager 用于字段初始化
            var tempLocal = new LocalManager
            {
                FilePath = "",
                Interpreter = null
            };

            // 加载字段的初始值
            initExpr.LoadIlValue(ctorIl, tempLocal);

            // 存储到字段
            ctorIl.Emit(OpCodes.Stfld, fieldBuilder);
        }

        // 3. 返回
        ctorIl.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为ClassInit是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为ClassInit是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将类定义转换为字符串表示
    /// </summary>
    /// <returns>类定义的字符串表示，包含类名、字段和方法</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        // 根据类型确定前缀
        var typePrefix = anyLangValue.IsInterface ? "interface" :
                        anyLangValue.IsMixin ? "mixin" :
                        anyLangValue.IsAbstract ? "abstract class" : "class";

        sb.AppendLine($"{typePrefix} {anyLangValue.ClassName} {{");
        foreach (var variate in anyLangValue.Variates)
        {
            if (variate.Value is FuncLangValue funcValue)
            {
                // 方法定义
                var paramList = funcValue.Ids != null ? string.Join(", ", funcValue.Ids) : string.Empty;
                sb.AppendLine($"    func {funcValue.Id}({paramList}) {{");
                sb.AppendLine($"        {funcValue.BlockStatement}");
                sb.AppendLine("    }");
            }
            else
            {
                // 字段定义
                sb.AppendLine($"    {variate.Key} <- {variate.Value}");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}