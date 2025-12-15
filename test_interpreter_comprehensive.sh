#!/bin/bash

echo "=== Old8Lang 解释器模式综合测试 ==="
echo "开始时间: $(date '+%Y-%m-%d %H:%M:%S')"
echo ""

success=0
failed=0
timeout_count=0
failed_files=()
timeout_files=()

# 获取所有测试文件
test_files=$(find ./InterpreterTests -name "*.old8" | sort)
total=$(echo "$test_files" | wc -l | tr -d ' ')

echo "找到 $total 个测试文件"
echo ""

# 遍历所有测试文件
current=0
for file in $test_files; do
    ((current++))
    basename=$(basename "$file")
    printf "[%3d/%3d] 测试: %-50s " "$current" "$total" "$basename"

    # 检查文件末尾是否包含"error"标记
    has_error_marker=$(tail -n 1 "$file" | grep -i "error")

    # 运行测试，10秒超时
    timeout 10 dotnet run --project Old8Lang.App -- -f "$file" >/dev/null 2>&1
    exit_code=$?

    if [ $exit_code -eq 124 ]; then
        # 超时
        echo "⏱️  TIMEOUT"
        ((timeout_count++))
        timeout_files+=("$file")
    elif [ -n "$has_error_marker" ]; then
        # 期望测试失败
        if [ $exit_code -ne 0 ]; then
            echo "✅ PASS (expected error)"
            ((success++))
        else
            echo "❌ FAIL (expected error but passed)"
            ((failed++))
            failed_files+=("$file")
        fi
    else
        # 期望测试成功
        if [ $exit_code -eq 0 ]; then
            echo "✅ PASS"
            ((success++))
        else
            echo "❌ FAIL (exit: $exit_code)"
            ((failed++))
            failed_files+=("$file")
        fi
    fi
done

echo ""
echo "================================"
echo "=== 测试结果汇总 ==="
echo "================================"
echo "总计: $total"
echo "通过: $success"
echo "失败: $failed"
echo "超时: $timeout_count"
echo "成功率: $(awk "BEGIN {printf \"%.1f%%\", ($success/$total)*100}")"

if [ $failed -gt 0 ]; then
    echo ""
    echo "❌ 失败的测试 ($failed):"
    for file in "${failed_files[@]}"; do
        echo "  - $file"
    done
fi

if [ $timeout_count -gt 0 ]; then
    echo ""
    echo "⏱️  超时的测试 ($timeout_count):"
    for file in "${timeout_files[@]}"; do
        echo "  - $file"
    done
fi

echo ""
echo "结束时间: $(date '+%Y-%m-%d %H:%M:%S')"

if [ $failed -eq 0 ] && [ $timeout_count -eq 0 ]; then
    echo "🎉 所有测试通过！"
    exit 0
else
    echo "❌ 部分测试失败或超时"
    exit 1
fi
