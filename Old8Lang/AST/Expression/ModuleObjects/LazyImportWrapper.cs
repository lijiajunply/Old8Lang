using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 懒导入包装器，延迟加载模块直到首次使用
/// </summary>
public class LazyImportWrapper(string moduleName, VariateManager manager, SourcePosition position)
    : LangValueType(position)
{
    private bool Loaded;
    private LangValueType? LoadedModule;

    /// <summary>
    /// 当访问模块属性时触发懒加载
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        if (!Loaded)
        {
            LoadModule();
        }

        if (LoadedModule != null)
        {
            return LoadedModule.Dot(dotExpression, currentManager);
        }

        throw new AttributeError(this, dotExpression.ToString(), "LazyModule");
    }

    /// <summary>
    /// 执行实际的模块加载
    /// </summary>
    private void LoadModule()
    {
        if (Loaded) return;

        try
        {
            // 执行实际的导入
            var importStatement = new ImportStatement(moduleName, Position);
            importStatement.Run(manager);

            // 查找导入的模块对象
            var moduleNameVar = Path.GetFileNameWithoutExtension(moduleName);
            if (manager.Scopes.Count > 0 && manager.Scopes[^1].TryGetValue(moduleNameVar, out var moduleObj))
            {
                LoadedModule = moduleObj;
            }
            else
            {
                LoadedModule = new StringLangValue("LazyModule");
            }

            Loaded = true;
        }
        catch (Exception ex)
        {
            throw new ImportError(this, moduleName, ex.Message);
        }
    }

    public override string ToString() => Loaded ? LoadedModule?.ToString() ?? "Loaded" : $"LazyModule({moduleName})";
}