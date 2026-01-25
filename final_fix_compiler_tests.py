#!/usr/bin/env python3
"""
最终修复编译器测试文件 - 清理格式问题
"""

import os
import re
from pathlib import Path

# 基础路径
BASE_DIR = Path(r"C:\Projects\RiderProjects\Old8Lang\Old8Lang.Tests")
COMPILER_DIR = BASE_DIR / "Compiler"

def final_fix_formatting(content):
    """修复格式问题"""

    # 修复 Assert.Null(exception);} 格式问题
    content = re.sub(
        r'Assert\.Null\(exception\);\}',
        'Assert.Null(exception);\n    }',
        content
    )

    # 确保方法之间有适当的空行
    content = re.sub(
        r'\}\s*\n\s*\[Fact\]',
        '}\n\n    [Fact]',
        content
    )

    return content

def fix_compiler_test_file(file_path):
    """修复单个编译器测试文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        original_content = content

        # 修复格式
        fixed_content = final_fix_formatting(content)

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
    print("开始最终修复编译器测试文件格式...\n")

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
