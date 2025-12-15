#!/bin/bash
success=0
failed=0
failed_files=()

async_tests=$(find ./InterpreterTests -name "async*.old8" | sort)

for file in $async_tests; do
    echo "测试: $file"
    timeout 10 dotnet run --project Old8Lang.App -- -f "$file" > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        echo "  ✅ PASS"
        ((success++))
    else
        echo "  ❌ FAIL"
        ((failed++))
        failed_files+=("$file")
    fi
done

echo ""
echo "=== 异步测试结果 ==="
echo "总计: $(($success + $failed))"
echo "通过: $success"
echo "失败: $failed"

if [ $failed -gt 0 ]; then
    echo ""
    echo "失败的测试:"
    for file in "${failed_files[@]}"; do
        echo "  - $file"
    done
fi
