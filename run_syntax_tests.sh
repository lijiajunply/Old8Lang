#!/bin/bash

# 语法测试脚本
echo "Running syntax tests..."

success=0
failed=0

# 获取所有测试文件
test_files=$(find ./SyntaxTests -name "*.old8")

# 遍历所有测试文件
for file in $test_files; do
    echo "\nTesting: $file"
    dotnet run --project Old8Lang.App -- -s "$file"
    
    if [ $? -eq 0 ]; then
        echo "✅ PASS"
        ((success++))
    else
        echo "❌ FAIL"
        ((failed++))
    fi
done

# 输出结果
echo "\n\n=== Syntax Test Results ==="
echo "Total: $(($success + $failed))"
echo "Passed: $success"
echo "Failed: $failed"

if [ $failed -eq 0 ]; then
    echo "\n🎉 All syntax tests passed!"
    exit 0
else
    echo "\n❌ Some syntax tests failed!"
    exit 1
fi