using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class ClassInit(AnyValue anyValue, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        // 检查类是否已存在
        var existingClass = manager.AnyInfo.FirstOrDefault(info => 
            info is AnyValue any && any.Id.IdName == anyValue.Id.IdName);
        
        if (existingClass != null)
        {
            throw new DuplicateNameError(this, anyValue.Id.IdName, "类");
        }
        
        manager.AddClassAndFunc(anyValue);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var assemblyName = new AssemblyName("DynamicAssembly");
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // 定义一个新的类型
        var typeBuilder = moduleBuilder.DefineType(anyValue.Id.IdName, TypeAttributes.Public);

        var fields = new List<FieldBuilder>();
        var fieldValues = new List<Old8Lang.AST.Expression.ValueType>();
        var func = new List<FuncValue>();
        foreach (var variate in anyValue.Variates)
        {
            if (variate.Value is FuncValue funcValue)
            {
                func.Add(funcValue);
                continue;
            }

            if (variate.Value is not Old8Lang.AST.Expression.ValueType value) continue;

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
        var typeClone = moduleClone.DefineType(anyValue.Id.IdName, TypeAttributes.Public);
        foreach (var variate in anyValue.Variates.Where(variate => variate.Value is not FuncValue))
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
        for (var i = 0; i < fieldValues.Count; i++)
        {
            generator.Emit(OpCodes.Ldarg_0); // 加载当前实例（this）
            fieldValues[i].LoadIlValue(generator, local);
            generator.Emit(OpCodes.Stfld, fields[i]); // 将 1 存储到字段 a
        }

        generator.Emit(OpCodes.Ret);

        local.ClassVar.Add(anyValue.Id.IdName, typeBuilder.CreateType());
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"class {anyValue.Id.IdName} {{");
        foreach (var variate in anyValue.Variates)
        {
            if (variate.Value is FuncValue funcValue)
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