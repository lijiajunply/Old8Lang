#!/bin/bash

# 语法测试脚本
echo "Running syntax tests..."

success=0
failed=0
failed_files=()

# 获取所有测试文件
test_files=$(find ./SyntaxTests -name "*.old8")

# 遍历所有测试文件
for file in $test_files; do
    echo ""
    echo "Testing: $file"
    
    # 检查文件末尾是否包含"error"标记
    has_error_marker=$(tail -n 1 "$file" | grep -i "error")
    
    # 运行测试，添加10秒超时限制
    # 使用兼容bash 3.2的方式实现超时
    dotnet run --project Old8Lang.App -- -s "$file" &
    DOTNET_PID=$!
    
    # 设置超时时间（秒）
    TIMEOUT=10
    
    # 等待进程结束或超时
    for ((i=0; i<$TIMEOUT; i++)); do
        if kill -0 $DOTNET_PID 2>/dev/null; then
            sleep 1
        else
            break
        fi
    done
    
    # 检查进程是否还在运行
    if kill -0 $DOTNET_PID 2>/dev/null; then
        # 超时，杀死进程
        echo "⏱️  Test timed out after $TIMEOUT seconds, killing process..."
        kill -9 $DOTNET_PID 2>/dev/null
        wait $DOTNET_PID 2>/dev/null
        test_exit_code=124  # 使用timeout命令的标准退出码
    else
        # 进程正常结束，获取退出码
        wait $DOTNET_PID 2>/dev/null
        test_exit_code=$?
    fi
    
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
echo "=== Syntax Test Results ==="
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
    echo "🎉 All syntax tests passed!"
    exit 0
else
    echo "❌ Some syntax tests failed!"
    exit 1
fi