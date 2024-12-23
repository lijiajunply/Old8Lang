using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Old8LangLib;

// Console.WriteLine(typeof(Math));
// Console.WriteLine(OS.OsInfo());
// Console.WriteLine(OS.Process("neofetch"));
//
//
// ColorfulTerminal.PrintAscii("hello,world");
//
// FileLib.CopyFile("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/bin/Debug/net6.0/Old8LangLib.dll","/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/dll/Old8LangLib.dll");
// FileLib.CopyFile("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/bin/Debug/net6.0/Old8LangLib.dll","/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/Net/dll/Old8LangLib.dll");

// var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
// var ilGenerator = dynamicMethod.GetILGenerator();
// var assemblyName = new AssemblyName("DynamicAssembly");
// var assemblyBuilder =
//     AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
// var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
//
// // 定义一个新的类型
// var typeBuilder = moduleBuilder.DefineType("Test", TypeAttributes.Public);
// var Variates = new[] { "a", "b" };
// var f = Variates.Select(variate => typeBuilder.DefineField(variate, typeof(int), FieldAttributes.Public));
//
// var constructorBuilder =
//     typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);
// var generator = constructorBuilder.GetILGenerator();
// foreach (var variate in f)
// {
//     generator.Emit(OpCodes.Ldarg_0); // 加载当前实例（this）
//     generator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
//     generator.Emit(OpCodes.Stfld, variate); // 将 1 存储到字段 a
// }
// generator.Emit(OpCodes.Ret);
//
// var classType = typeBuilder.CreateType();
// var constructorInfo = classType.GetConstructor(Type.EmptyTypes);
// if (constructorInfo != null)
// {
//     ilGenerator.Emit(OpCodes.Newobj, constructorInfo);
// }
//
// var local_a = ilGenerator.DeclareLocal(classType);
// ilGenerator.Emit(OpCodes.Stloc, local_a.LocalIndex);
// ilGenerator.Emit(OpCodes.Ldloc, local_a.LocalIndex);
// var propertyInfo = classType.GetField("b");
// ilGenerator.Emit(OpCodes.Ldfld, propertyInfo!);
// ilGenerator.Emit(OpCodes.Call,
//     typeof(Console).GetMethod("WriteLine", [typeof(int)])!);
// ilGenerator.Emit(OpCodes.Ret);
// dynamicMethod.Invoke(null, null);

Console.WriteLine(typeof(IDictionary).IsAssignableTo(typeof(IEnumerable)));

class Test
{
    public int a = 1;
    public int b = 2;

    public int Add(int c, int d)
    {
        Console.WriteLine(a);
        Test t = this;
        t.a = 1;
        return c + d;
    }
}