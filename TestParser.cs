using System; using Old8Lang.LangParser;

namespace TestParser
{
    class Program
    {
        static void Main(string[] args)
        {
            // 读取测试文件内容
            string testContent = System.IO.File.ReadAllText("/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/test.old8");
            
            try
            {
                // 使用解析器解析测试内容
                var interpreter = new LangInterpreter();
                var result = interpreter.Build(testContent);
                
                Console.WriteLine("解析成功！");
                Console.WriteLine("生成的代码：");
                Console.WriteLine(result.ToCode());
            }
            catch (Exception ex)
            {
                Console.WriteLine("解析失败！");
                Console.WriteLine("错误信息：");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}