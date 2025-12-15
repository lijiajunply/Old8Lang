#!/bin/bash
# 快速测试关键功能

echo "=== 新生成器架构快速测试 ==="
echo ""

tests=(
    "InterpreterTests/28_simple_generator.old8:生成器"
    "InterpreterTests/10_while_statement.old8:while循环"
    "InterpreterTests/09_for_statement.old8:for循环"
    "InterpreterTests/11_for_in_statement.old8:for-in循环"
    "InterpreterTests/async_basic_execution.old8:异步基础"
    "InterpreterTests/async_mutex.old8:互斥锁"
    "InterpreterTests/async_atomic.old8:原子操作"
)

passed=0
failed=0

for test in "${tests[@]}"; do
    IFS=':' read -r file name <<< "$test"
    echo -n "测试 $name ... "
    if dotnet run --project Old8Lang.App -- -f "$file" > /dev/null 2>&1; then
        echo "✅ PASS"
        ((passed++))
    else
        echo "❌ FAIL"
        ((failed++))
    fi
done

echo ""
echo "=========================================="
echo "通过: $passed, 失败: $failed"
if [ $failed -eq 0 ]; then
    echo "✅ 所有关键测试通过！"
else
    echo "❌ 有 $failed 个测试失败"
fi
