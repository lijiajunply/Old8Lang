using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.Interpreter;

namespace Old8Lang.Bytecode.ModuleSystem;

/// <summary>
/// 模块加载器 - 负责加载和编译模块
/// </summary>
public class ModuleLoader
{
    private readonly ModuleResolver _moduleResolver;
    private readonly string? _baseDirectory;

    public ModuleLoader(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory;
        _moduleResolver = new ModuleResolver();
    }

    /// <summary>
    /// 加载模块并编译为字节码
    /// </summary>
    public BytecodeFile LoadModule(string moduleName)
    {
        // 解析模块路径
        var resolveResult = _moduleResolver.ResolveModule(moduleName, _baseDirectory);

        if (!resolveResult.IsSuccess)
        {
            var attemptedPaths = string.Join(", ", resolveResult.AttemptedPaths);
            throw new Exception($"无法解析模块 '{moduleName}': 尝试过的路径: {attemptedPaths}");
        }

        // 读取模块文件
        string moduleCode;
        try
        {
            moduleCode = File.ReadAllText(resolveResult.ResolvedPath!);
        }
        catch (Exception ex)
        {
            throw new Exception($"无法读取模块文件 '{resolveResult.ResolvedPath}': {ex.Message}");
        }

        // 解析模块代码
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(moduleCode);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 设置模块名称
        bytecodeFile.ModuleName = moduleName;

        // 自动导出所有顶层函数和类（如果没有显式导出）
        if (bytecodeFile.Exports == null || bytecodeFile.Exports.Count == 0)
        {
            bytecodeFile.Exports = new Dictionary<string, ExportedSymbol>();

            // 导出所有函数
            for (int i = 0; i < bytecodeFile.Functions.Count; i++)
            {
                var func = bytecodeFile.Functions[i];
                bytecodeFile.Exports[func.Name] = new ExportedSymbol(
                    func.Name,
                    ExportedSymbolType.Function,
                    func,
                    i
                );
            }

            // 导出所有类
            for (int i = 0; i < bytecodeFile.Classes.Count; i++)
            {
                var cls = bytecodeFile.Classes[i];
                bytecodeFile.Exports[cls.Name] = new ExportedSymbol(
                    cls.Name,
                    ExportedSymbolType.Class,
                    cls,
                    i
                );
            }

            // 导出所有接口
            for (int i = 0; i < bytecodeFile.Interfaces.Count; i++)
            {
                var iface = bytecodeFile.Interfaces[i];
                bytecodeFile.Exports[iface.Name] = new ExportedSymbol(
                    iface.Name,
                    ExportedSymbolType.Interface,
                    iface,
                    i
                );
            }

            // 导出所有Mixin
            for (int i = 0; i < bytecodeFile.Mixins.Count; i++)
            {
                var mixin = bytecodeFile.Mixins[i];
                bytecodeFile.Exports[mixin.Name] = new ExportedSymbol(
                    mixin.Name,
                    ExportedSymbolType.Mixin,
                    mixin,
                    i
                );
            }
        }

        return bytecodeFile;
    }

    /// <summary>
    /// 获取模块的绝对路径
    /// </summary>
    public string? ResolveModulePath(string moduleName)
    {
        var result = _moduleResolver.ResolveModule(moduleName, _baseDirectory);
        return result.IsSuccess ? result.ResolvedPath : null;
    }
}
