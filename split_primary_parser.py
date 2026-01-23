#!/usr/bin/env python3
"""
重构 PrimaryParser.cs 文件的脚本
将一个 2052 行的大文件重构为多个 partial class 文件
"""

import os
import re

# 源文件路径
source_file = "/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Old8Lang/LangParser/Parsers/PrimaryParser.cs"
output_dir = "/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Old8Lang/LangParser/Parsers"

# 方法分组定义（组名, 方法列表, 文件名后缀）
method_groups = [
    ("集合类型解析", [
        ("ParseListOrDictionary", 349, 428),
        ("ParseArrayOrRange", 429, 539),
        ("ParseListComprehension", 540, 628),
    ], "Collections"),

    ("Lambda和元组解析", [
        ("ParseLambdaOrTuple", 629, 937),
        ("ParseTupleElementWithOptionalName", 938, 979),
        ("ParseLambdaParameter", 1470, 1533),
    ], "LambdaAndTuple"),

    ("字符串模板解析", [
        ("ParseStringTemplate", 980, 1173),
    ], "StringTemplate"),

    ("字面量解析", [
        ("ParseIdentifier", 1174, 1200),
        ("ParseStringLiteral", 1201, 1213),
        ("ParseCharLiteral", 1214, 1230),
        ("ParseCharValue", 1231, 1285),
        ("ParseIntLiteral", 1286, 1328),
        ("ParseDoubleLiteral", 1329, 1348),
        ("ParseBoolLiteral", 1349, 1357),
        ("ParseNullLiteral", 1358, 1369),
    ], "Literals"),

    ("实例化和切片", [
        ("ParseListInitOrSlice", 1370, 1456),
        ("ParseInstantiate", 1457, 1469),
    ], "InstantiationAndSlice"),

    ("模式匹配解析", [
        ("ParseMatchExpression", 1534, 1657),
        ("ParseTuplePattern", 1658, 1710),
        ("ParseRangePattern", 1711, 1757),
    ], "PatternMatching"),

    ("泛型解析", [
        ("ParseGenericInstantiation", 1758, 1816),
        ("IsLikelyGenericInstantiation", 1817, 1940),
        ("IsBuiltInTypeName", 1941, 1956),
        ("ParseGenericTypeArguments", 1957, 2010),
        ("SplitTypeArguments", 2011, 2051),
    ], "Generics"),
]

# 文件头部
file_header = """using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

"""

def main():
    print("开始重构 PrimaryParser.cs...")

    # 先备份原文件
    backup_file = source_file + ".backup"
    print(f"备份原文件到: {backup_file}")
    with open(source_file, 'r', encoding='utf-8') as src:
        with open(backup_file, 'w', encoding='utf-8') as dst:
            dst.write(src.read())

    # 读取整个源文件
    with open(source_file, 'r', encoding='utf-8') as f:
        all_lines = f.readlines()

    # 创建主文件（包含 ParsePrimary 主入口和构造函数）
    main_file = os.path.join(output_dir, "PrimaryParser.Main.cs")
    print(f"\n创建主文件: {main_file}")

    with open(main_file, 'w', encoding='utf-8') as f:
        f.write(file_header)
        f.write("/// <summary>\n")
        f.write("/// Primary 表达式解析器\n")
        f.write("/// 负责解析主表达式，包括字面量、列表、字典、数组、元组、Lambda、字符串模板等\n")
        f.write("/// </summary>\n")
        f.write("public partial class PrimaryParser(\n")
        f.write("    ParserContext context,\n")
        f.write("    Func<StatementParser> statementParserFactory,\n")
        f.write("    Func<ExpressionParser> expressionParserFactory,\n")
        f.write("    FunctionParser functionParser,\n")
        f.write("    LinqParser linqParser)\n")
        f.write("    : ParserBase(context)\n")
        f.write("{\n")

        # 写入 ParsePrimary 方法（41-348 行）
        f.write("    #region Primary\n\n")
        f.writelines(all_lines[40:348])  # 41-348 行
        f.write("    #endregion\n")

        f.write("}\n")

    print("  ✓ 主文件创建完成")

    # 为每个方法组创建 partial class 文件
    print("\n创建 partial class 文件:")
    for group_name, methods, file_suffix in method_groups:
        output_file = os.path.join(output_dir, f"PrimaryParser.{file_suffix}.cs")

        print(f"  - {file_suffix}: {output_file}")
        print(f"    方法数量: {len(methods)}")

        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(file_header)
            f.write("/// <summary>\n")
            f.write(f"/// Primary 表达式解析器 - {group_name}\n")
            f.write("/// </summary>\n")
            f.write("public partial class PrimaryParser\n")
            f.write("{\n")

            # 写入该组的所有方法
            for method_name, start_line, end_line in methods:
                print(f"      {method_name}: {start_line}-{end_line}")
                f.writelines(all_lines[start_line-1:end_line])
                f.write("\n")

            f.write("}\n")

        print(f"    ✓ 完成")

    # 删除原文件
    print(f"\n删除原文件: {source_file}")
    os.remove(source_file)

    print("\n重构完成！")
    print(f"原文件已备份为: {backup_file}")
    print(f"创建了 1 个主文件 + {len(method_groups)} 个 partial class 文件")
    print("\n建议：")
    print("1. 检查新文件是否正确")
    print("2. 运行测试确保功能正常")
    print("   dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj")
    print("3. 如果一切正常，可以删除备份文件")
    print(f"   rm \"{backup_file}\"")

if __name__ == "__main__":
    main()
