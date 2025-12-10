#!/bin/bash

# 批量测试所有编译测试文件

TEST_DIR="CompilerTests"
REPORT_FILE="Reports/20251210-1000-compiler-tests.md"

# 获取所有.old8文件并按文件名排序
files=$(ls $TEST_DIR/*.old8 | sort)

# 初始化计数器
count=1

for file in $files; do
    echo "Testing $file..."
    
    # 运行测试并捕获输出
    output=$(dotnet run --project Old8Lang.App -- -c "$file" 2>&1)
    exit_code=$?
    
    # 提取文件名
    filename=$(basename "$file")
    
    # 检查是否成功
    if [ $exit_code -eq 0 ]; then
        result="✅ 成功"
    else
        result="❌ 失败"
    fi
    
    # 提取时间信息
    parse_time=$(echo "$output" | grep "Parser Build Time" | awk -F: '{print $2}' | tr -d '[:space:]')
    run_time=$(echo "$output" | grep "Process Run Time" | awk -F: '{print $2}' | tr -d '[:space:]')
    total_time=$(echo "$output" | grep "Total" | awk -F: '{print $2}' | tr -d '[:space:]')
    
    # 写入报告
    echo "| $count | $filename | $result | $parse_time | $run_time | $total_time |" >> "$REPORT_FILE"
    
    # 输出结果到控制台
    echo "$result - $filename"
    echo "编译时间: $parse_time, 执行时间: $run_time, 总时间: $total_time"
    echo ""
    
    # 递增计数器
    count=$((count + 1))
done

echo "所有测试完成！"
echo "测试报告已生成: $REPORT_FILE"
