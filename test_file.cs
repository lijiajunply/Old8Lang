using System;
using System.IO;
using Old8Lang;

public class TestFileReading
{
    public static void Main(string[] args)
    {
        string filename = "test_clean.old8";
        Console.WriteLine("测试文件读取：");
        Console.WriteLine("文件名：" + filename);
        Console.WriteLine("当前目录：" + Directory.GetCurrentDirectory());
        Console.WriteLine("文件是否存在：" + File.Exists(filename));
        
        if (File.Exists(filename))
        {
            string content = File.ReadAllText(filename);
            Console.WriteLine("\n文件内容：");
            Console.WriteLine("---开始---");
            Console.Write(content);
            Console.WriteLine("\n---结束---");
            Console.WriteLine("\n内容长度：" + content.Length);
            Console.WriteLine("行数：" + content.Split('\n').Length);
            
            foreach (char c in content)
            {
                Console.WriteLine($"字符：'{c}'，ASCII：{Convert.ToInt32(c)}");
            }
        }
        
        // 测试 Apis.FromFile
        Console.WriteLine("\n\n测试 Apis.FromFile：");
        string apiContent = Apis.FromFile(filename);
        Console.WriteLine("---开始---");
        Console.Write(apiContent);
        Console.WriteLine("\n---结束---");
        Console.WriteLine("内容长度：" + apiContent.Length);
        Console.WriteLine("行数：" + apiContent.Split('\n').Length);
    }
}
