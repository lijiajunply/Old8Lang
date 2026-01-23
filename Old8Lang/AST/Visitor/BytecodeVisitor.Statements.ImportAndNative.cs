using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 导入和原生绑定
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitImportStatement(ImportStatement node)
    {
        // 导入语句 - 生成模块加载和符号导入指令

        // 1. 生成LoadModule指令加载模块
        string moduleName = node.GetImportString();
        Emit(OpCode.LoadModule, moduleName);

        // 2. 根据导入类型生成相应的导入指令
        if (node.GetFromClause())
        {
            // import { item1, item2 } from "module"
            var importSpecifiers = node.GetImportSpecifiers();
            if (importSpecifiers != null && importSpecifiers.Count > 0)
            {
                foreach (var specifier in importSpecifiers)
                {
                    if (specifier.Alias != specifier.Name)
                    {
                        // import { item as alias } from "module"
                        Emit(OpCode.ImportSymbolAs, new object[] { moduleName, specifier.Name, specifier.Alias });
                    }
                    else
                    {
                        // import { item } from "module"
                        Emit(OpCode.ImportSymbol, new object[] { moduleName, specifier.Name });
                    }
                }
            }
            else if (importSpecifiers != null && importSpecifiers.Count == 0)
            {
                // import * from "module"
                Emit(OpCode.ImportAll, moduleName);
            }
        }
        else
        {
            // import "module" 或 import "module" as alias
            if (node.GetModuleAlias() != null)
            {
                // 模块别名：将模块对象存储到全局变量
                // 这里我们不生成指令，因为LoadModule已经加载了模块
                // 模块别名的处理在虚拟机中完成
            }
            else
            {
                // 简单导入：导入所有导出符号
                Emit(OpCode.ImportAll, moduleName);
            }
        }

        return null;
    }


    public Instruction? VisitNativeStatement(NativeStatement node)
    {
        // ImportNative 指令：导入原生资源
        // 操作数格式: [dllNameIndex, classNameIndex, mode, p1, p2]
        // mode: 0=Single, 1=All, 2=Class

        int dllNameIndex = _compiler.ConstantPool.AddConstant(node.DllName);
        int classNameIndex = _compiler.ConstantPool.AddConstant(node.ClassName);

        if (node.ImportAll)
        {
            // Mode 1: All Methods
            Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 1, 0, 0 });
        }
        else if (node.MethodList is { Count: > 0 })
        {
            // Method List -> Multiple Single Method Imports
            foreach (var methodName in node.MethodList)
            {
                int methodNameIndex = _compiler.ConstantPool.AddConstant(methodName);
                int aliasIndex = _compiler.ConstantPool.AddConstant(""); // No alias for list import
                Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 0, methodNameIndex, aliasIndex });
            }
        }
        else if (!string.IsNullOrEmpty(node.MethodName))
        {
            // Mode 0: Single Method
            int methodNameIndex = _compiler.ConstantPool.AddConstant(node.MethodName);
            int aliasIndex = _compiler.ConstantPool.AddConstant(node.NativeName ?? "");
            Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 0, methodNameIndex, aliasIndex });
        }
        else
        {
            // Mode 2: Class Import
            string alias = node.Name ?? node.ClassAlias ?? "";
            int aliasIndex = _compiler.ConstantPool.AddConstant(alias);
            Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 2, aliasIndex, 0 });
        }

        return null;
    }

}
