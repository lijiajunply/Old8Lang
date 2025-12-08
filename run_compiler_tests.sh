#!/bin/bash

# 编译测试脚本
echo "Running compiler tests..."

success=0
failed=0

# 获取所有测试文件
test_files=$(find ./CompilerTests -name "*.old8")

# 遍历所有测试文件
for file in $test_files; do
    echo "Testing: $file"
    dotnet run --project Old8Lang.App -- -c "$file"
    
    if [ $? -eq 0 ]; then
        echo "✅ PASS"
        ((success++))
    else
        echo "❌ FAIL"
        ((failed++))
    fi
done

# 输出结果
echo "\n\n=== Compiler Test Results ==="
echo "Total: $(($success + $failed))"
echo "Passed: $success"
echo "Failed: $failed"

if [ $failed -eq 0 ]; then
    echo "\n🎉 All compiler tests passed!"
    exit 0
else
    echo "\n❌ Some compiler tests failed!"
    exit 1
fi