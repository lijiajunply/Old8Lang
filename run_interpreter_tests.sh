#!/bin/bash

# 解释器测试脚本
echo "Running interpreter tests..."

success=0
failed=0
failed_files=()

# 获取所有测试文件
test_files=$(find ./SyntaxTests -name "*.old8")

# 遍历所有测试文件
for file in $test_files; do
    echo "Testing: $file"
    dotnet run --project Old8Lang.App -- -f "$file"
    
    if [ $? -eq 0 ]; then
        echo "✅ PASS"
        ((success++))
    else
        echo "❌ FAIL"
        ((failed++))
        failed_files+=("$file")
    fi
done

# 输出结果
echo "\n\n=== Interpreter Test Results ==="
echo "Total: $(($success + $failed))"
echo "Passed: $success"
echo "Failed: $failed"

# 输出失败的文件
if [ $failed -gt 0 ]; then
    echo "\n❌ Failed files:"
    for file in "${failed_files[@]}"; do
        echo "  - $file"
    done
fi

if [ $failed -eq 0 ]; then
    echo "\n🎉 All interpreter tests passed!"
    exit 0
else
    echo "\n❌ Some interpreter tests failed!"
    exit 1
fi