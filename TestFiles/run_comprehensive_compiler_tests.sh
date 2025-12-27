#!/bin/bash

# 编译器测试脚本
# 运行所有 CompilerTests 目录下的 .old8 文件并生成报告

TIMESTAMP=$(date +"%Y%m%d-%H%M")
REPORT_FILE="Reports/${TIMESTAMP}-编译器全面测试.md"
COMPILER_TESTS_DIR="CompilerTests"

# 创建 Reports 目录（如果不存在）
mkdir -p Reports

# 初始化计数器
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# 创建报告文件
cat > "$REPORT_FILE" << 'EOF'
# Old8Lang 编译器全面测试报告

## 测试概述

EOF

echo "测试日期: $(date '+%Y-%m-%d %H:%M:%S')" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# 收集所有测试文件
TEST_FILES=$(find "$COMPILER_TESTS_DIR" -name "*.old8" | sort)

# 计算总测试数
TOTAL_TESTS=$(echo "$TEST_FILES" | wc -l | tr -d ' ')

echo "## 测试结果" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "| 测试文件 | 状态 | 说明 |" >> "$REPORT_FILE"
echo "|---------|------|------|" >> "$REPORT_FILE"

# 运行每个测试
for TEST_FILE in $TEST_FILES; do
    echo "正在测试: $TEST_FILE"

    # 运行测试并捕获输出
    OUTPUT=$(dotnet run --project Old8Lang.App -- -c "$TEST_FILE" 2>&1)
    EXIT_CODE=$?

    # 检查是否成功
    if echo "$OUTPUT" | grep -q "\[编译错误\]"; then
        STATUS="❌ 失败"
        FAILED_TESTS=$((FAILED_TESTS + 1))

        # 提取错误信息
        ERROR_TYPE=$(echo "$OUTPUT" | grep "\[错误类型\]" | head -1)
        ERROR_MSG=$(echo "$OUTPUT" | grep "\[错误信息\]" | head -1)
        DESCRIPTION="$ERROR_TYPE $ERROR_MSG"
    elif echo "$OUTPUT" | grep -q "编译成功"; then
        STATUS="✅ 通过"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        DESCRIPTION="编译并执行成功"
    else
        STATUS="⚠️ 未知"
        DESCRIPTION="未能确定测试结果"
    fi

    echo "| $TEST_FILE | $STATUS | $DESCRIPTION |" >> "$REPORT_FILE"
done

# 添加统计信息
echo "" >> "$REPORT_FILE"
echo "## 测试统计" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "- **总测试数**: $TOTAL_TESTS" >> "$REPORT_FILE"
echo "- **通过**: $PASSED_TESTS" >> "$REPORT_FILE"
echo "- **失败**: $FAILED_TESTS" >> "$REPORT_FILE"
echo "- **成功率**: $(awk "BEGIN {printf \"%.2f%%\", ($PASSED_TESTS/$TOTAL_TESTS)*100}")" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# 添加测试分类统计
echo "## 测试分类" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### 性能测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "- 27_deep_recursion.old8 - 深度递归测试" >> "$REPORT_FILE"
echo "- 28_large_loops.old8 - 大规模循环测试" >> "$REPORT_FILE"
echo "- 29_large_data_structures.old8 - 大数据结构测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### 库引用测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "- 30_time_library.old8 - Time库测试" >> "$REPORT_FILE"
echo "- 31_math_library.old8 - Math库测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### 边界条件测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "- 32_boundary_conditions.old8 - 边界条件测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### 错误处理测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "- 33_error_handling.old8 - 错误处理测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### 数据结构测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "- 34_complex_data_structures.old8 - 复杂数据结构测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### 控制流测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "- 35_advanced_control_flow.old8 - 高级控制流测试" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

echo ""
echo "============================================"
echo "测试完成！"
echo "总测试数: $TOTAL_TESTS"
echo "通过: $PASSED_TESTS"
echo "失败: $FAILED_TESTS"
echo "报告已生成: $REPORT_FILE"
echo "============================================"
