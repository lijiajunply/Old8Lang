#!/bin/bash

# 解释器测试脚本
echo "Running interpreter tests..."

success=0
failed=0
failed_files=()

# 获取所有测试文件
test_files=$(find ./InterpreterTests -name "*.old8")

# 遍历所有测试文件
for file in $test_files; do
    echo ""
    echo "Testing: $file"
    
    # 检查文件末尾是否包含"error"标记
    has_error_marker=$(tail -n 1 "$file" | grep -i "error")
    
    # 运行测试
    dotnet run --project Old8Lang.App -- -f "$file"
    test_exit_code=$?
    
    if [ -n "$has_error_marker" ]; then
        # 期望测试失败
        if [ $test_exit_code -ne 0 ]; then
            echo "✅ PASS (expected failure)"
            ((success++))
        else
            echo "❌ FAIL (expected failure but passed)"
            ((failed++))
            failed_files+=("$file")
        fi
    else
        # 期望测试成功
        if [ $test_exit_code -eq 0 ]; then
            echo "✅ PASS"
            ((success++))
        else
            echo "❌ FAIL"
            ((failed++))
            failed_files+=($file)
        fi
    fi
done

# 输出结果
echo ""
echo "------------------------------"
echo "=== Interpreter Test Results ==="
echo "Total: $(($success + $failed))"
echo "Passed: $success"
echo "Failed: $failed"

# 输出失败的文件
if [ $failed -gt 0 ]; then
    echo "❌ Failed files:"
    for file in "${failed_files[@]}"; do
        echo "  - $file"
    done
fi

if [ $failed -eq 0 ]; then
    echo "🎉 All interpreter tests passed!"
    exit 0
else
    echo "❌ Some interpreter tests failed!"
    exit 1
fi