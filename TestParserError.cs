using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Collections.Generic;

namespace Old8Lang.Tests
{
    public class TestParserError
    {
        public static void Main()
        {
            // 创建一个明确的语法错误：缺少右括号
            var code = "func test(x, y { return x + y }";
            var tokens = Old8Lang.LangParser.LangInterpreter.Tokenize(code);
            var parser = new Old8Lang.LangParser.LangParser(tokens, code);
            
            try
            {
                var result = parser.ParseProgram();
                Console.WriteLine("解析成功，结果: " + result);
            }
            catch (SyntaxError ex)
            {
                Console.WriteLine("捕获到SyntaxError: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("捕获到其他异常: " + ex.Message);
            }
        }
    }
}