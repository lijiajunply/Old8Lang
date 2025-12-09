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
        var assemblyName = new AssemblyName("DynamicAssembly");
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // 定义类型时考虑继承关系
        Type? baseType = null;
        if (anyLangValue.ParentClassName != null)
        {
            // 尝试获取父类的Type
            baseType = local.ClassVar.GetValueOrDefault(anyLangValue.ParentClassName);
        }
        
        // 如果没有父类，使用object作为基类
        baseType ??= typeof(object);

        // 定义一个新的类型，指定父类
        var typeBuilder = moduleBuilder.DefineType(anyLangValue.ClassName, TypeAttributes.Public, baseType);

        var fields = new List<FieldBuilder>();
        var fieldValues = new List<LangValueType>();
        var func = new List<FuncLangValue>();
        foreach (var variate in anyLangValue.Variates)
        {
            if (variate.Value is FuncLangValue funcValue)
            {
                func.Add(funcValue);
                continue;
            }

            if (variate.Value is not LangValueType value) continue;

            var fieldBuilder = typeBuilder.DefineField(variate.Key.IdName,
                variate.Value.OutputType(local)!,
                FieldAttributes.Public);
            fields.Add(fieldBuilder);
            fieldValues.Add(value);
        }

        var assemblyNameClone = new AssemblyName("DynamicAssembly");
        var assemblyClone =
            AssemblyBuilder.DefineDynamicAssembly(assemblyNameClone, AssemblyBuilderAccess.Run);
        var moduleClone = assemblyClone.DefineDynamicModule("DynamicModule");
        var typeClone = moduleClone.DefineType(anyLangValue.ClassName, TypeAttributes.Public, baseType);
        foreach (var variate in anyLangValue.Variates.Where(variate => variate.Value is not FuncLangValue))
        {
            typeClone.DefineField(variate.Key.IdName,
                variate.Value.OutputType(local)!,
                FieldAttributes.Public);
        }
        
        foreach (var value in func)
        {
            var funcLocal = new LocalManager(){InClassEnv = typeClone.CreateType()};
            var parameterTypes = value.Ids!.Select(item => item.OutputType(funcLocal)).ToArray();
            var method = typeBuilder.DefineMethod(value.Id!.IdName, MethodAttributes.Public);
            method.SetReturnType(value.OutputType(funcLocal));
            method.SetParameters(parameterTypes);
            value.LoadIl(method, funcLocal);
        }

        var constructorBuilder =
            typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);

        var generator = constructorBuilder.GetILGenerator();
        
        // 调用父类的构造函数
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Call, baseType.GetConstructor(Type.EmptyTypes)!);
        
        // 初始化子类字段
        for (var i = 0; i < fieldValues.Count; i++)
        {
            generator.Emit(OpCodes.Ldarg_0); // 加载当前实例（this）
            fieldValues[i].LoadIlValue(generator, local);
            generator.Emit(OpCodes.Stfld, fields[i]); // 将值存储到字段
        }

        generator.Emit(OpCodes.Ret);

        local.ClassVar.Add(anyLangValue.ClassName, typeBuilder.CreateType());
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