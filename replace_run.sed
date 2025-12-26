105,410 {
  105 {
    i\    /// <summary>
    i\    /// 在解释模式下执行导入语句
    i\    /// </summary>
    i\    /// <param name="manager">变量管理器，用于管理导入的模块和变量</param>
    i\    /// <exception cref="ImportError">当导入失败时抛出</exception>
    i\    public override void Run(VariateManager manager)
    i\    {
    i\        var moduleName = ImportString;
    i\
    i\        // 处理 module.submodule 语法（排除相对路径和文件扩展名）
    i\        if (moduleName.Contains('.') &&
    i\            !moduleName.StartsWith("./") &&
    i\            !moduleName.StartsWith("../") &&
    i\            !moduleName.EndsWith(".old8") &&
    i\            !moduleName.EndsWith(".ol"))
    i\        {
    i\            HandleSubmoduleImport(moduleName, manager);
    i\            return;
    i\        }
    i\
    i\        // 动态导入处理
    i\        if (IsDynamic)
    i\        {
    i\            HandleDynamicImport(manager);
    i\            return;
    i\        }
    i\
    i\        // 懒导入处理
    i\        if (IsLazy)
    i\        {
    i\            HandleLazyImport(manager);
    i\            return;
    i\        }
    i\
    i\        // 使用新的模块系统服务
    i\        var options = new ImportOptions
    i\        {
    i\            IsFromClause = FromClause,
    i\            ModuleAlias = ModuleAlias,
    i\            ImportSpecifiers = ImportSpecifiers.Select(item => item.Alias).ToList(),
    i\            IsLazy = IsLazy,
    i\            IsSelective = IsSelective
    i\        };
    i\
    i\        var result = ModuleService.ImportModule(moduleName, manager, options);
    i\
    i\        if (!result.IsSuccess)
    i\        {
    i\            if (result.Error != null)
    i\            {
    i\                throw result.Error;
    i\            }
    i\            throw new ImportError(Position, moduleName, "模块导入失败");
    i\        }
    i\    }
  }
  d
}
