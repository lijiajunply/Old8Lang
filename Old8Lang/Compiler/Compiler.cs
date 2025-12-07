using System.Reflection.Emit;
using Old8Lang.AST.Statement;

namespace Old8Lang.Compiler;

public static class Compiler
{
    public static Action Compile(BlockStatement statement, string path, IMiniInterpreter i)
    {
        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path ,Interpreter = i};
        statement.GenerateIl(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ret);
        var oldLangRun = (Action)dynamicMethod.CreateDelegate(typeof(Action));
        return oldLangRun;
    }

    public static Action Compile(string path, IMiniInterpreter i)
    {
        var statement = i.Build(Apis.FromFile(path));
        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path ,Interpreter = i};
        statement.GenerateIl(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ret);
        var oldLangRun = (Action)dynamicMethod.CreateDelegate(typeof(Action));
        return oldLangRun;
    }


}