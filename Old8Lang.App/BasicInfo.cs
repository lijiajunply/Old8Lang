namespace Old8Lang.App;

/// <summary>
/// 命令行应用的基本信息类，提供帮助文档、语言信息和命令映射
/// </summary>
public static class BasicInfo
{
    /// <summary>
    /// 获取 Old8Lang 命令行帮助文档
    /// </summary>
    /// <returns>格式化的帮助文档字符串</returns>
    public static string Help => @"Old8Lang 命令行帮助文档

命令格式：
  Old8Lang.App <命令> [参数]

运行模式：
  解释模式   (-f)   直接解释执行 Old8Lang 代码
  IL 模式    (-il)  编译为 IL 中间代码后执行
  虚拟机模式 (-vm)  编译为字节码后由虚拟机执行

可用命令：
  执行命令：
    -f <文件路径>          解释执行指定的 .old8 或 .ol 文件
    -il <文件路径>         以 IL 模式编译并执行指定的 .old8 或 .ol 文件
    -c <文件路径>          -il 的别名，二者等价
    -vm <文件路径>         以虚拟机模式编译并执行指定的 .old8 或 .ol 文件
    -s <文件路径>          对指定的 .old8 或 .ol 文件进行语法测试
    run [脚本名|-s|-f|-il] [文件]   智能运行文件或项目（自动检测模式）

  字节码命令：
    -compile <输入.old8> <输出.o8c> 将源码编译为字节码文件
    -execute <文件.o8c>             执行字节码文件

  项目管理命令：
    init                   初始化 Old8Lang 项目
    install                安装项目的所有依赖
    restore                恢复项目的所有依赖（基于 o8package.json）
    remove <包名>          从项目移除依赖包
    list, ls               列出已安装的依赖包

  打包与发布命令：
    pack                   将包文件夹打包成 .o8pkg 文件
    unpack                 解包 .o8pkg 文件到指定目录
    sign                   对包文件进行数字签名
    verify                 验证包文件的数字签名
    cert                   证书管理工具
    publish                打包、签名并发布包
    env                    管理项目环境配置

  调试与性能分析命令：
    debug-start <文件路径> 启动调试会话（挂上调试器运行文件）
    debug-bp <子命令>      断点管理：add / func / list / remove / clear
    debug <命令>           调试控制：continue / step / stepinto / stepover / stepout / pause / stop
    profile <子命令>       性能分析会话：start / stop / status / clear

  信息命令：
    -h                     显示此帮助信息
    -var                   显示当前版本号

通用选项：
  -d, --debug            启用调试输出，显示详细的编译过程信息
  -l, --log-level <级别> 设置日志输出级别 (error, warning, info, debug)
  -D <符号>              定义预编译符号（必须写在文件名之后）
  --perf                 输出性能报告（必须写在文件名之后）

使用示例：
  执行文件：
    Old8Lang.App -f example.old8                    # 解释执行
    Old8Lang.App -il example.old8                   # IL 模式执行
    Old8Lang.App -vm example.old8 --debug           # 虚拟机模式 + 调试输出
    Old8Lang.App -f example.old8 --perf             # 解释执行并输出性能报告
    Old8Lang.App run example.old8                   # 智能运行（自动选择模式）

  字节码：
    Old8Lang.App -compile example.old8 example.o8c  # 编译为字节码
    Old8Lang.App -execute example.o8c               # 执行字节码

  项目与发布：
    Old8Lang.App init                               # 初始化项目
    Old8Lang.App install                            # 安装依赖
    Old8Lang.App list                               # 列出已安装包
    Old8Lang.App publish --auto-cert -o ./release   # 打包、签名并发布

注意事项：
  - 仅支持 .old8 和 .ol 扩展名的文件
  - IL 模式会显示执行时间统计
  - 语法测试会显示解析时间和生成的代码结构
  - -D、--perf、--env 等选项必须写在文件名之后
  - 项目管理命令需要在项目目录中执行（init 除外）
  - debug-bp / debug / profile 目前的命令行工作流尚不完整，
    调试与性能测量的可用做法见 Docs/CLI_GUIDE.md 与 Docs/PERFORMANCE_GUIDE.md";
}
