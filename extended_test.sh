#!/bin/bash
echo "=== 扩展功能测试 ==="
echo ""

# 异步测试
echo "【异步功能测试】"
async_tests=(
  "async_mutex"
  "async_atomic"
  "async_basic_execution"
  "async_concurrent"
  "async_sleep_test"
  "async_return_test"
  "async_simple_test"
)

async_pass=0
async_fail=0
for test in "${async_tests[@]}"; do
  printf "  %-40s " "$test:"
  if dotnet run --project Old8Lang.App -- -f InterpreterTests/${test}.old8 >/dev/null 2>&1; then
    echo "✅"
    ((async_pass++))
  else
    echo "❌"
    ((async_fail++))
  fi
done
echo "  异步测试: $async_pass/$((async_pass + async_fail)) 通过"
echo ""

# 线程和生成器测试
echo "【线程和生成器测试】"
thread_tests=(
  "test_thread_basic"
  "test_thread_simple"
  "28_simple_generator"
  "28_generator"
)

thread_pass=0
thread_fail=0
for test in "${thread_tests[@]}"; do
  printf "  %-40s " "$test:"
  if dotnet run --project Old8Lang.App -- -f InterpreterTests/${test}.old8 >/dev/null 2>&1; then
    echo "✅"
    ((thread_pass++))
  else
    echo "❌"
    ((thread_fail++))
  fi
done
echo "  线程/生成器测试: $thread_pass/$((thread_pass + thread_fail)) 通过"
echo ""

# 核心语法测试
echo "【核心语法测试】"
core_tests=(
  "01_basic_literals"
  "03_arithmetic_expressions"
  "08_if_elif_else"
  "09_for_statement"
  "10_while_statement"
  "11_for_in_statement"
  "12_in_expression"
  "12_switch_statement"
  "13_function_declaration"
  "14_lambda_expressions"
  "15_class_declaration"
  "16_class_methods"
  "17_exception_handling"
  "18_string_templates"
  "27_ternary_expressions"
)

core_pass=0
core_fail=0
for test in "${core_tests[@]}"; do
  printf "  %-40s " "$test:"
  if dotnet run --project Old8Lang.App -- -f InterpreterTests/${test}.old8 >/dev/null 2>&1; then
    echo "✅"
    ((core_pass++))
  else
    echo "❌"
    ((core_fail++))
  fi
done
echo "  核心语法测试: $core_pass/$((core_pass + core_fail)) 通过"
echo ""

total_pass=$((async_pass + thread_pass + core_pass))
total=$((async_pass + async_fail + thread_pass + thread_fail + core_pass + core_fail))

echo "================================"
echo "总计: $total_pass/$total 通过"
echo "================================"
