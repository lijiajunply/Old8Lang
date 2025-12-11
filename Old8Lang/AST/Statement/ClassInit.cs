using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class ClassInit(TypeTemplate anyLangValue, SourcePosition position = default) : OldStatement(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override void Run(VariateManager manager)
    {
        // 检查类是否已存在
        var existingClass = manager.GetAny(new LangId(anyLangValue.ClassName));

        if (existingClass != null)
        {
            throw new DuplicateNameError(this, anyLangValue.ClassName, "类");
        }

        // 立即将类添加到ImportInfos中，以便在类定义内部访问
        manager.AddClassAndFunc(anyLangValue);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 1. 检查类是否已经存在
        if (local.ClassVar.ContainsKey(anyLangValue.ClassName))
        {
            // 类已经存在，跳过生成
            return;
        }

        // 2. 创建动态程序集和模块
        var assemblyName = new AssemblyName($"DynamicAssembly_{anyLangValue.ClassName}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule($"DynamicModule_{anyLangValue.ClassName}");

        // 3. 定义基类
        Type? baseType;
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

        // 4. 定义类类型
        var typeBuilder = moduleBuilder.DefineType(
            anyLangValue.ClassName,
            TypeAttributes.Public | TypeAttributes.BeforeFieldInit,
            baseType);

        // 5. 创建一个新的LocalManager实例，用于生成当前类的IL代码
        // 这样可以避免不同类之间的方法混淆
        var classLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder
        };

        // 6. 定义类的字段和方法
        DefineClassMembers(typeBuilder, classLocal);

        // 7. 定义类的构造函数
        DefineConstructor(typeBuilder, baseType);

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
    private void DefineClassMembers(TypeBuilder typeBuilder, LocalManager local)
    {
        // 分离字段和方法
        var fields = new List<(ClassMemberId, LangExpression)>();
        var methods = new List<(ClassMemberId, FuncLangValue)>();

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

            // 定义字段
            typeBuilder.DefineField(
                memberId.IdName,
                fieldType,
                FieldAttributes.Public);
        }

        // 定义实例方法
        foreach (var (memberId, funcValue) in methods)
        {
            DefineMethod(typeBuilder, memberId, funcValue, local);
        }

        // 定义静态成员
        DefineStaticMembers(typeBuilder, local);
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

        // 创建一个全新的LocalManager实例，完全独立于外部环境
        // 这样可以避免不同类之间的方法上下文混淆
        var methodLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            InClassEnv = typeBuilder
        };

        // 获取返回类型
        Type returnType;
        try
        {
            returnType = funcValue.OutputType(methodLocal);
        }
        catch
        {
            returnType = typeof(void);
        }

        // 获取参数类型
        var parameterTypes = new List<Type>();

        // 对于实例方法，第一个参数是this
        if ((attributes & MethodAttributes.Static) == 0)
        {
            parameterTypes.Add(typeBuilder);
        }

        // 添加方法参数
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
            }
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
        int paramIndex = (attributes & MethodAttributes.Static) == 0 ? 1 : 0; // 0是this（实例方法），0是第一个参数（静态方法）
        if (funcValue.Ids != null)
        {
            foreach (var paramId in funcValue.Ids)
            {
                // 声明局部变量
                var paramType = parameterTypes[paramIndex];
                var localVar = methodIl.DeclareLocal(paramType);

                // 加载参数并存储到局部变量
                methodIl.Emit(OpCodes.Ldarg, paramIndex);
                methodIl.Emit(OpCodes.Stloc, localVar);

                // 将参数添加到新的LocalManager中，完全隔离
                methodLocal.AddLocalVar(paramId.IdName, localVar);

                paramIndex++;
            }
        }

        // 生成方法体的IL代码
        funcValue.BlockStatement.GenerateIl(methodIl, methodLocal);

        // 如果方法有返回值，确保最后一个指令是return
        if (returnType != typeof(void))
        {
            // 检查是否已经有return指令，如果没有，添加一个默认的return
            // 这里简化处理，直接添加return指令，实际应该检查最后一个指令
            methodIl.Emit(OpCodes.Ret);
        }
        else
        {
            // 对于void方法，直接添加return指令
            methodIl.Emit(OpCodes.Ret);
        }

        return methodBuilder;
    }

    /// <summary>
    /// 定义类的构造函数
    /// </summary>
    /// <param name="typeBuilder">类型构建器</param>
    /// <param name="baseType">基类类型</param>
    private void DefineConstructor(TypeBuilder typeBuilder, Type baseType)
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
        // 暂时不处理字段的初始化，因为需要处理字段的初始值表达式
        // 实际实现中，应该在这里生成初始化字段的IL代码

        // 3. 返回
        ctorIl.Emit(OpCodes.Ret);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"class {anyLangValue.ClassName} {{");
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