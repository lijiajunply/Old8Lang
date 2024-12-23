using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ClassInit(AnyValue anyValue) : OldStatement
{
    public override void Run(VariateManager Manager) => Manager.AddClassAndFunc(anyValue);

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
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


        foreach (var value in func)
        {
            var funcLocal = new LocalManager();
            var parameterTypes = value.Ids!.Select(item => item.OutputType(funcLocal)).ToArray();
            var method = typeBuilder.DefineMethod(value.Id!.IdName, MethodAttributes.Public);
            method.SetReturnType(value.OutputType(funcLocal));
            method.SetParameters(parameterTypes);
            value.LoadIL(method, funcLocal);
        }

        var constructorBuilder =
            typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);

        var generator = constructorBuilder.GetILGenerator();
        for (var i = 0; i < fieldValues.Count; i++)
        {
            generator.Emit(OpCodes.Ldarg_0); // 加载当前实例（this）
            fieldValues[i].LoadILValue(generator, local);
            generator.Emit(OpCodes.Stfld, fields[i]); // 将 1 存储到字段 a
        }

        generator.Emit(OpCodes.Ret);

        local.ClassVar.Add(anyValue.Id.IdName, typeBuilder.CreateType());
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => anyValue.ToString();
}