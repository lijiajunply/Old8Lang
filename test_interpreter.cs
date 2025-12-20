using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

class Program
{
    static void Main()
    {
        try
        {
            var interpreter = new LangInterpreter();
            var code = @"
PrintLine(""Hello, Old8Lang!"")
x <- 42
PrintLine(""x = "" + x)
";
            var result = interpreter.Run(code, "test.old8");
            Console.WriteLine("程序执行完成");
            if (result != null) {
                Console.WriteLine("返回值: " + result.ToStr());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("错误: " + ex.Message);
            Console.WriteLine("堆栈跟踪: " + ex.StackTrace);
        }
    }
}