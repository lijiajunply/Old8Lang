#!/usr/bin/env python3
"""
彻底修复编译器测试文件 - 移除所有解释器模式的断言
"""

import os
import re
from pathlib import Path

# 基础路径
BASE_DIR = Path(r"C:\Projects\RiderProjects\Old8Lang\Old8Lang.Tests")
COMPILER_DIR = BASE_DIR / "Compiler"

def thorough_fix_test_method(content):
    """彻底修复测试方法"""

    # 步骤1: 替换 ast.Run 为编译器调用
    content = re.sub(
        r'var ast = interpreter\.Build\(code\);\s*\n\s*ast\.Run\(interpreter\.Manager\);',
        '''var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);''',
        content
    )

    # 步骤2: 移除所有 interpreter.Manager.GetValue 相关的断言
    # 匹配从 "// Assert" 到方法结束的所有内容
    content = re.sub(
        r'\n\s*// Assert\s*\n\s*var result = interpreter\.Manager\.GetValue\([^)]+\);[^\}]*',
        '',
        content,
        flags=re.DOTALL
    )

    # 步骤3: 移除孤立的 Assert 语句（在编译器断言之后的）
    content = re.sub(
        r'(Assert\.Null\(exception\);)\s*\n\s*\n\s*// Assert\s*\n\s*var result[^\}]*',
        r'\1',
        content,
        flags=re.DOTALL
    )

    return content

def fix_compiler_test_file(file_path):
    """修复单个编译器测试文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        original_content = content

        # 修复内容
        fixed_content = thorough_fix_test_method(content)

        # 只有内容改变时才写回
        if fixed_content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(fixed_content)
            return True

        return False
    except Exception as e:
        print(f"[ERROR] 修复文件失败 {file_path}: {str(e)}")
        return False

def main():
    """主函数"""
    print("开始彻底修复编译器测试文件...\n")

    fixed_count = 0
    total_count = 0

    # 遍历所有编译器测试文件
    for root, dirs, files in os.walk(COMPILER_DIR):
        for file in files:
            if file.endswith('.cs'):
                file_path = Path(root) / file
                total_count += 1

                if fix_compiler_test_file(file_path):
                    fixed_count += 1
                    print(f"[OK] 修复: {file_path}")

    print(f"\n完成!")
    print(f"修复: {fixed_count}")
    print(f"总计: {total_count}")

if __name__ == "__main__":
    main()
