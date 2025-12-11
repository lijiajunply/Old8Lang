namespace Old8Lang.App
{
    class DebugImport
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Debug Import Path ===");
                
                // 读取LangInfo
                var langInfo = Apis.ReadJson();
                Console.WriteLine($"ImportPath from ReadJson: {langInfo.ImportPath}");
                
                // 打印LibInfos
                Console.WriteLine($"LibInfos count: {langInfo.LibInfos.Count}");
                foreach (var lib in langInfo.LibInfos)
                {
                    Console.WriteLine($"  - {lib.LibName}, IsDir: {lib.IsDir}");
                }
                
                // 测试Time库路径
                var timeFileName = "Time.old8";
                var timePath = Path.Combine(langInfo.ImportPath, timeFileName);
                Console.WriteLine($"\nTesting Time.old8 path:");
                Console.WriteLine($"  Combined path: {timePath}");
                Console.WriteLine($"  File exists? {File.Exists(timePath)}");
                
                // 测试绝对路径
                var absolutePath = Path.GetFullPath(timePath);
                Console.WriteLine($"  Absolute path: {absolutePath}");
                Console.WriteLine($"  File exists? {File.Exists(absolutePath)}");
                
                // 测试应用程序基目录
                var appPath = Path.Combine(AppContext.BaseDirectory, timePath);
                Console.WriteLine($"  AppContext.BaseDirectory: {AppContext.BaseDirectory}");
                Console.WriteLine($"  App path: {appPath}");
                Console.WriteLine($"  File exists? {File.Exists(appPath)}");
                
                // 直接测试Old8LangLib/OldLib路径
                var directPath = Path.Combine(Directory.GetCurrentDirectory(), "Old8LangLib", "OldLib", timeFileName);
                Console.WriteLine($"  Direct path: {directPath}");
                Console.WriteLine($"  File exists? {File.Exists(directPath)}");
                
                // 列出Old8LangLib/OldLib目录内容
                var oldLibPath = Path.Combine(Directory.GetCurrentDirectory(), "Old8LangLib", "OldLib");
                Console.WriteLine($"\nOld8LangLib/OldLib directory contents:");
                if (Directory.Exists(oldLibPath))
                {
                    foreach (var file in Directory.GetFiles(oldLibPath))
                    {
                        Console.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    Console.WriteLine($"  Directory does not exist: {oldLibPath}");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}