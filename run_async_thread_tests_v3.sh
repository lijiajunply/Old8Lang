#!/bin/bash

# Old8Lang 异步和多线程功能测试脚本 v3
# 生成时间: 2025-12-16
# 注意：macOS没有timeout命令，此版本不使用超时

echo "======================================"
echo "Old8Lang 异步和多线程功能测试"
echo "======================================"
echo ""

# 测试结果统计
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# 结果文件
TIMESTAMP=$(date "+%Y%m%d-%H%M%S")
RESULT_FILE="Reports/${TIMESTAMP}-异步多线程测试结果.md"

# 创建报告目录
mkdir -p Reports

# 构建项目
echo "正在构建项目..."
if ! dotnet build Old8Lang.App/Old8Lang.App.csproj --configuration Debug > /dev/null 2>&1; then
    echo "✗ 项目构建失败"
    exit 1
fi
echo "✓ 项目构建成功"
echo ""

# 初始化报告文件
cat > "$RESULT_FILE" << 'EOF'
# Old8Lang 异步和多线程功能测试报告

**测试时间**: DATETIME_PLACEHOLDER
**测试模式**: 解释器模式 (-f)

---

## 测试摘要

SUMMARY_PLACEHOLDER

---

## 测试详情

EOF

# 测试函数
run_test() {
    local test_file=$1
    local test_name=$(basename "$test_file")
    local test_type=$2
    local test_category=$3

    TOTAL_TESTS=$((TOTAL_TESTS + 1))

    echo -n "[$test_category] $test_name ... "

    # 运行测试，捕获输出和退出码
    if dotnet run --project Old8Lang.App --no-build --configuration Debug -- -f "$test_file" > /tmp/test_output_$$.txt 2>&1; then
        echo "✓"
        PASSED_TESTS=$((PASSED_TESTS + 1))

        # 记录到报告
        cat >> "$RESULT_FILE" << EOF
### ✓ $test_name

**类型**: $test_type
**分类**: $test_category
**状态**: 通过

<details>
<summary>查看输出</summary>

\`\`\`
$(cat /tmp/test_output_$$.txt)
\`\`\`

</details>

---

EOF
    else
        local exit_code=$?
        echo "✗ (退出码: $exit_code)"
        FAILED_TESTS=$((FAILED_TESTS + 1))

        # 记录到报告
        cat >> "$RESULT_FILE" << EOF
### ✗ $test_name

**类型**: $test_type
**分类**: $test_category
**状态**: 失败 (退出码: $exit_code)

<details>
<summary>查看错误信息</summary>

\`\`\`
$(cat /tmp/test_output_$$.txt)
\`\`\`

</details>

---

EOF
    fi

    rm -f /tmp/test_output_$$.txt
}

# 运行语法测试
echo "======================================"
echo "1. 异步语法测试"
echo "======================================"

run_test "SyntaxTests/async_basic_syntax.old8" "语法测试" "语法验证"
echo ""

# 运行异步功能测试
echo "======================================"
echo "2. 异步功能测试"
echo "======================================"

# 基础异步测试
run_test "InterpreterTests/async_basic_execution.old8" "异步功能" "基础功能"
run_test "InterpreterTests/async_simple_test.old8" "异步功能" "基础功能"
run_test "InterpreterTests/async_pure_test.old8" "异步功能" "基础功能"

# 异步返回值测试
run_test "InterpreterTests/async_return_test.old8" "异步功能" "返回值处理"
run_test "InterpreterTests/async_return_int_test.old8" "异步功能" "返回值处理"
run_test "InterpreterTests/async_simple_return_test.old8" "异步功能" "返回值处理"
run_test "InterpreterTests/async_only_return_test.old8" "异步功能" "返回值处理"
run_test "InterpreterTests/async_return_only_test.old8" "异步功能" "返回值处理"

# 异步高级功能
run_test "InterpreterTests/async_advanced_test.old8" "异步功能" "高级特性"
run_test "InterpreterTests/async_advanced_features.old8" "异步功能" "高级特性"
run_test "InterpreterTests/async_awaitasync_comprehensive_test.old8" "异步功能" "高级特性"
run_test "InterpreterTests/async_task_status_management.old8" "异步功能" "状态管理"

# 异步改进和调试
run_test "InterpreterTests/async_await_improvement.old8" "异步功能" "改进验证"
run_test "InterpreterTests/async_debug_test.old8" "异步功能" "调试测试"
run_test "InterpreterTests/async_debug_return_test.old8" "异步功能" "调试测试"

# 异步睡眠测试
run_test "InterpreterTests/async_sleep_test.old8" "异步功能" "工具函数"
echo ""

# 运行并发控制测试
echo "======================================"
echo "3. 并发控制测试"
echo "======================================"

run_test "InterpreterTests/async_concurrent.old8" "并发控制" "并发执行"
run_test "InterpreterTests/async_mutex.old8" "并发控制" "互斥锁"
run_test "InterpreterTests/async_atomic.old8" "并发控制" "原子操作"
echo ""

# 运行多线程测试
echo "======================================"
echo "4. 多线程测试"
echo "======================================"

run_test "InterpreterTests/test_thread_basic.old8" "多线程" "基础功能"
run_test "InterpreterTests/test_thread_simple.old8" "多线程" "基础功能"
run_test "InterpreterTests/test_thread_comprehensive.old8" "多线程" "全面测试"
echo ""

# 运行Channel测试
echo "======================================"
echo "5. Channel测试"
echo "======================================"

run_test "InterpreterTests/test_channel.old8" "Channel" "通道通信"
echo ""

# 计算成功率
if [ $TOTAL_TESTS -gt 0 ]; then
    SUCCESS_RATE=$(awk "BEGIN {printf \"%.2f\", ($PASSED_TESTS/$TOTAL_TESTS)*100}")
else
    SUCCESS_RATE="0.00"
fi

# 生成测试摘要
echo "======================================"
echo "测试完成"
echo "======================================"
echo ""
echo "总测试数: $TOTAL_TESTS"
echo "通过: $PASSED_TESTS"
echo "失败: $FAILED_TESTS"
echo "成功率: ${SUCCESS_RATE}%"
echo ""

# 更新报告中的占位符
DATETIME=$(date "+%Y-%m-%d %H:%M:%S")
SUMMARY="| 指标 | 数值 |
|------|------|
| 总测试数 | $TOTAL_TESTS |
| 通过 | $PASSED_TESTS |
| 失败 | $FAILED_TESTS |
| 成功率 | ${SUCCESS_RATE}% |"

# 使用perl替换占位符（macOS兼容）
perl -i -pe "s/DATETIME_PLACEHOLDER/$DATETIME/" "$RESULT_FILE"
perl -i -pe "s/SUMMARY_PLACEHOLDER/$(echo "$SUMMARY" | sed 's/\//\\\//g' | sed 's/\n/\\n/g')/" "$RESULT_FILE"

echo "详细报告已保存到: $RESULT_FILE"

# 显示失败测试
if [ $FAILED_TESTS -gt 0 ]; then
    echo ""
    echo "失败的测试:"
    grep "^### ✗" "$RESULT_FILE" | sed 's/^### ✗ /  - /'
fi

echo ""
echo "测试完成！"
