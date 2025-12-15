#!/bin/bash

# Old8Lang 新生成器架构全面测试脚本
# 测试新的状态机架构是否正常工作

echo "=========================================="
echo "Old8Lang 新生成器架构全面测试"
echo "测试时间: $(date '+%Y-%m-%d %H:%M:%S')"
echo "=========================================="
echo ""

# 计数器
TOTAL=0
PASSED=0
FAILED=0

# 测试函数
run_test() {
    local test_file=$1
    local test_name=$2
    local timeout_sec=${3:-10}

    TOTAL=$((TOTAL + 1))
    echo "[$TOTAL] 测试: $test_name"
    echo "    文件: $test_file"

    # 使用 gtimeout (macOS) 或 timeout (Linux)
    if command -v gtimeout &> /dev/null; then
        TIMEOUT_CMD="gtimeout"
    else
        TIMEOUT_CMD="timeout"
    fi

    # 运行测试
    output=$(cd /Users/luckyfish/Documents/Project/RiderProjects/Old8Lang && \
             dotnet run --project Old8Lang.App -- -f "$test_file" 2>&1)
    exit_code=$?

    if [ $exit_code -eq 0 ]; then
        echo "    结果: ✅ PASS"
        PASSED=$((PASSED + 1))
    else
        echo "    结果: ❌ FAIL (exit code: $exit_code)"
        echo "    输出: $output"
        FAILED=$((FAILED + 1))
    fi
    echo ""
}

echo "=== 第一类：生成器核心测试 ==="
echo ""

run_test "InterpreterTests/28_simple_generator.old8" "简单生成器"

echo "=== 第二类：循环语句测试 ==="
echo ""

run_test "InterpreterTests/10_while_statement.old8" "while 循环"
run_test "InterpreterTests/09_for_statement.old8" "for 循环"
run_test "InterpreterTests/11_for_in_statement.old8" "for-in 循环"

echo "=== 第三类：异步和多线程测试 ==="
echo ""

run_test "InterpreterTests/async_basic_execution.old8" "基本异步执行"
run_test "InterpreterTests/async_concurrent.old8" "并发异步任务"
run_test "InterpreterTests/async_sleep_test.old8" "异步延迟测试"
run_test "InterpreterTests/async_mutex.old8" "互斥锁测试"
run_test "InterpreterTests/async_atomic.old8" "原子操作测试"
run_test "InterpreterTests/test_thread_basic.old8" "基础线程测试"

echo "=== 第四类：核心语法测试 ==="
echo ""

run_test "InterpreterTests/01_basic_literals.old8" "基本字面量"
run_test "InterpreterTests/03_arithmetic_expressions.old8" "算术表达式"
run_test "InterpreterTests/08_if_elif_else.old8" "条件语句"
run_test "InterpreterTests/13_function_declaration.old8" "函数声明"

echo "=========================================="
echo "测试结果汇总"
echo "=========================================="
echo "总测试数: $TOTAL"
echo "通过: $PASSED"
echo "失败: $FAILED"
echo "成功率: $(awk "BEGIN {printf \"%.1f%%\", ($PASSED/$TOTAL)*100}")"
echo "=========================================="

if [ $FAILED -eq 0 ]; then
    echo "✅ 所有测试通过！新架构工作正常。"
    exit 0
else
    echo "❌ 有 $FAILED 个测试失败，请检查。"
    exit 1
fi
