using Old8Lang.Bytecode.Metadata;
using Old8Lang.Bytecode.ModuleSystem;
using Old8Lang.Error;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.StandardLibrary;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 模块和对象
/// </summary>
public partial class VirtualMachine
{
    private void LoadModule(string moduleName)
    {
        // 检查模块是否已加载
        if (_moduleRegistry.IsModuleLoaded(moduleName))
        {
            return; // 模块已加载，直接返回
        }

        // 检测循环依赖
        if (!_moduleRegistry.MarkModuleLoading(moduleName))
        {
            throw new ImportError(new SourcePosition(), moduleName, $"检测到循环依赖：模块 '{moduleName}' 正在加载中");
        }

        try
        {
            if (_standardLibraries.IsStandardLibrary(moduleName))
            {
                var manager = new VariateManager { StandardLibraries = _standardLibraries };
                if (!StandardLibraryLoader.TryLoadStandardLibrary(moduleName, manager, out var loaded) ||
                    loaded is not UnifiedModule standardModule)
                    throw new InvalidOperationException($"无法加载标准库 '{moduleName}'");

                var symbols = standardModule.GetExportedSymbols().ToDictionary(
                    name => name, name => (object?)standardModule.GetSymbol(name));
                symbols[ModuleRegistry.ModuleObjectSymbol] = standardModule;
                var bytecode = new BytecodeFile
                {
                    Exports = symbols.Keys.ToDictionary(name => name,
                        name => new ExportedSymbol(name, ExportedSymbolType.Variable))
                };
                _moduleRegistry.RegisterModule(moduleName, bytecode, symbols);
                return;
            }

            // 加载并编译模块
            var moduleBytecode = _moduleLoader.LoadModule(moduleName);

            // 创建模块的全局变量空间
            var moduleGlobals = new Dictionary<string, object?>();
            foreach (var globalVar in moduleBytecode.GlobalVariables)
            {
                moduleGlobals[globalVar] = null;
            }

            // 执行模块的初始化代码（如果有入口点）
            if (moduleBytecode.EntryPointIndex >= 0)
            {
                // 创建临时虚拟机执行模块初始化
                var moduleVM = new VirtualMachine(moduleBytecode, _baseDirectory, _standardLibraries);

                // 复制模块注册表（避免重复加载依赖）
                foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                {
                    var loadedModule = _moduleRegistry.GetModule(loadedModuleName);
                    if (loadedModule != null)
                    {
                        moduleVM._moduleRegistry.RegisterModule(
                            loadedModuleName,
                            loadedModule.BytecodeFile,
                            loadedModule.Globals
                        );
                    }
                }

                // 执行模块初始化
                moduleVM.Execute();

                // 获取模块的全局变量（从 ConcurrentDictionary 转换为 Dictionary）
                moduleGlobals = moduleVM.SnapshotGlobalValues();

                // 传递性导入：将模块VM加载的所有依赖模块也注册到当前VM的模块注册表中
                foreach (var depModuleName in moduleVM._moduleRegistry.GetLoadedModuleNames())
                {
                    // 跳过当前正在加载的模块自己
                    if (depModuleName == moduleName)
                    {
                        continue;
                    }

                    // 如果当前VM还没有加载这个依赖模块，则注册它
                    if (!_moduleRegistry.IsModuleLoaded(depModuleName))
                    {
                        var depModule = moduleVM._moduleRegistry.GetModule(depModuleName);
                        if (depModule != null)
                        {
                            _moduleRegistry.RegisterModule(
                                depModuleName,
                                depModule.BytecodeFile,
                                depModule.Globals
                            );
                        }
                    }
                }
            }

            // 注册模块
            _moduleRegistry.RegisterModule(moduleName, moduleBytecode, moduleGlobals);
        }
        catch (Exception ex)
        {
            throw new ImportError(new SourcePosition(), moduleName, $"加载模块 '{moduleName}' 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建对象实例（用于从模块导入的类的实例化）
    /// </summary>

    private BytecodeObjectInstance CreateObjectInstance(ClassMetadata classMetadata, object?[] constructorArgs)
    {
        // 创建对象实例
        var obj = new BytecodeObjectInstance(classMetadata.Name);

        // 初始化接口信息（包括父类实现的接口）
        var allInterfaces = new HashSet<string>();
        var currentClassForInterfaces = classMetadata;
        while (currentClassForInterfaces != null)
        {
            // 添加当前类实现的接口
            foreach (var interfaceName in currentClassForInterfaces.InterfaceNames)
                allInterfaces.Add(interfaceName);
            foreach (var interfaceName in currentClassForInterfaces.ImplementsInterfaces)
                allInterfaces.Add(interfaceName);

            // 查找父类
            if (!string.IsNullOrEmpty(currentClassForInterfaces.BaseClassName))
            {
                currentClassForInterfaces = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClassForInterfaces.BaseClassName);
                if (currentClassForInterfaces == null)
                {
                    // 从模块中查找父类
                    foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                    {
                        try
                        {
                            var symbol = _moduleRegistry.GetModuleSymbol(loadedModuleName, currentClassForInterfaces?.BaseClassName ?? "");
                            if (symbol is ClassMetadata baseClass)
                            {
                                currentClassForInterfaces = baseClass;
                                break;
                            }
                        }
                        catch
                        {
                            // 继续查找
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }
        obj.Interfaces = allInterfaces.ToList();

        // 初始化所有字段为默认值（包括父类字段）
        var allFields = new List<FieldMetadata>();
        var currentClass = classMetadata;
        while (currentClass != null)
        {
            allFields.AddRange(currentClass.Fields);

            // 查找父类（首先从当前字节码文件，然后从模块）
            if (!string.IsNullOrEmpty(currentClass.BaseClassName))
            {
                currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                if (currentClass == null)
                {
                    // 从模块中查找父类
                    foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                    {
                        try
                        {
                            var symbol =
                                _moduleRegistry.GetModuleSymbol(loadedModuleName, currentClass?.BaseClassName ?? "");
                            if (symbol is ClassMetadata baseClass)
                            {
                                currentClass = baseClass;
                                break;
                            }
                        }
                        catch
                        {
                            // 继续查找
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }

        // 初始化所有字段
        foreach (var field in allFields)
        {
            if (!obj.Fields.ContainsKey(field.Name))
            {
                obj.Fields[field.Name] = null;
            }
        }

        // 查找并调用构造函数（init方法）
        var initMethod = classMetadata.Methods.FirstOrDefault(m => m.Name == "init");
        if (initMethod != null)
        {
            // 准备方法调用参数：第一个参数是 this（对象本身）
            var methodArgs = new object?[constructorArgs.Length + 1];
            methodArgs[0] = obj;
            Array.Copy(constructorArgs, 0, methodArgs, 1, constructorArgs.Length);

            // 检查参数类型
            ValidateConstructorParameterTypes(initMethod.Function, methodArgs, classMetadata.Name);

            // 调用构造函数
            CallFunction(initMethod.Function, methodArgs);
        }

        return obj;
    }

    /// <summary>
    /// 验证构造函数参数类型
    /// </summary>

}
