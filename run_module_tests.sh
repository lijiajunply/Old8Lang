#!/bin/bash

# Old8Lang 模块系统测试运行脚本

set -e

echo "=========================================="
echo "Old8Lang 模块系统测试运行器"
echo "=========================================="

# 获取脚本所在目录
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$SCRIPT_DIR"

# 测试结果目录
RESULTS_DIR="Reports"
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
REPORT_FILE="$RESULTS_DIR/module-tests-$TIMESTAMP.md"

# 创建结果目录
mkdir -p "$RESULTS_DIR"

# 初始化报告
cat > "$REPORT_FILE" << EOF
# Old8Lang 模块系统测试报告

**运行时间**: $(date)
**测试目录**: Old8Lang.Tests/Interpreter/Modules

## 测试结果概览

| 测试类别 | 状态 | 通过 | 失败 | 总数 | 耗时 |
|---------|------|------|------|------|------|
EOF

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 打印带颜色的消息
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 运行测试并收集结果
run_test_category() {
    local category_name="$1"
    local filter_pattern="$2"
    local description="$3"

    print_info "运行 $description 测试..."

    # 运行测试并捕获输出
    local test_output
    local test_exit_code

    test_output=$(dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
        --filter "FullyQualifiedName~$filter_pattern" \
        --logger "console;verbosity=normal" \
        --no-build \
        2>&1) || test_exit_code=$?

    # 解析测试结果
    local passed=0
    local failed=0
    local total=0
    local duration="N/A"

    if echo "$test_output" | grep -q "Passed:"; then
        passed=$(echo "$test_output" | grep -o "Passed: [0-9]*" | head -1 | cut -d' ' -f2)
        failed=$(echo "$test_output" | grep -o "Failed: [0-9]*" | head -1 | cut -d' ' -f2)
        total=$(echo "$test_output" | grep -o "Total: [0-9]*" | head -1 | cut -d' ' -f2)
        duration=$(echo "$test_output" | grep -o "Test Run Successful\." -A5 | grep "Time:" | cut -d' ' -f2 || echo "N/A")
    fi

    # 更新报告
    local status="✅ 通过"
    if [ "$test_exit_code" -ne 0 ]; then
        status="❌ 失败"
    fi

    echo "| $description | $status | $passed | $failed | $total | $duration |" >> "$REPORT_FILE"

    # 显示结果
    if [ "$test_exit_code" -eq 0 ]; then
        print_success "$description 测试完成: $passed/$total 通过"
    else
        print_error "$description 测试失败: $failed/$total 失败"
        echo "$test_output" | tail -20
    fi

    echo "----------------------------------------"

    return $test_exit_code
}

# 总体统计
total_passed=0
total_failed=0
total_categories=0
failed_categories=0

# 运行各类测试
print_info "开始运行模块系统测试..."

echo "" >> "$REPORT_FILE"

# 基础导入测试
if run_test_category "basic-import" "Old8Lang.Tests.Interpreter.Modules.BasicImport" "基础导入"; then
    ((total_categories++))
else
    ((total_categories++))
    ((failed_categories++))
fi

# 错误处理测试
if run_test_category "error-handling" "Old8Lang.Tests.Interpreter.Modules.ErrorHandling" "错误处理"; then
    ((total_categories++))
else
    ((total_categories++))
    ((failed_categories++))
fi

# 性能测试
if run_test_category "performance" "Old8Lang.Tests.Interpreter.Modules.Performance" "性能测试"; then
    ((total_categories++))
else
    ((total_categories++))
    ((failed_categories++))
fi

# 集成测试
if run_test_category "integration" "Old8Lang.Tests.Interpreter.Modules.Integration" "集成测试"; then
    ((total_categories++))
else
    ((total_categories++))
    ((failed_categories++))
fi

# 运行所有模块测试（用于生成总体统计）
print_info "生成测试总体统计..."

all_tests_output=$(dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
    --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules" \
    --logger "trx;LogFileName=$RESULTS_DIR/module-tests-$TIMESTAMP.trx" \
    --no-build \
    2>&1 || true)

if echo "$all_tests_output" | grep -q "Passed:"; then
    total_passed=$(echo "$all_tests_output" | grep -o "Passed: [0-9]*" | head -1 | cut -d' ' -f2)
    total_failed=$(echo "$all_tests_output" | grep -o "Failed: [0-9]*" | head -1 | cut -d' ' -f2)
fi

# 完成报告
cat >> "$REPORT_FILE" << EOF

## 测试统计

- **总测试数**: $((total_passed + total_failed))
- **通过测试**: $total_passed
- **失败测试**: $total_failed
- **成功率**: $(echo "scale=2; $total_passed * 100 / ($total_passed + $total_failed)" | bc -l 2>/dev/null || echo "N/A")%
- **测试类别**: $total_categories
- **失败类别**: $failed_categories

## 测试文件说明

### Core/
- \`ModuleImportTestBase.cs\`: 测试基类，提供通用功能
- \`TestFileSystemHelper.cs\`: 文件系统测试助手

### BasicImport/
- \`SimpleImportTests.cs\`: 基本导入功能测试
- \`AliasImportTests.cs\`: 别名导入功能测试

### ErrorHandling/
- \`ImportErrorTests.cs\`: 错误处理和异常情况测试

### Performance/
- \`ImportPerformanceTests.cs\`: 性能和内存使用测试

### Integration/
- \`RealWorldUsageTests.cs\`: 真实使用场景集成测试

## 建议

EOF

if [ "$total_failed" -gt 0 ]; then
    cat >> "$REPORT_FILE" << EOF
⚠️ **发现 $total_failed 个失败测试**，请查看详细的测试输出并修复相关问题。

EOF
else
    cat >> "$REPORT_FILE" << EOF
✅ **所有测试通过！** 模块系统功能正常。

EOF
fi

cat >> "$REPORT_FILE" << EOF
## 运行命令

\`\`\`bash
# 运行所有模块测试
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules"

# 运行特定类别测试
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.BasicImport"
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.ErrorHandling"
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.Performance"
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.Integration"
\`\`\`

---

**报告生成时间**: $(date)
**脚本版本**: 1.0
EOF

# 输出总结
echo ""
echo "=========================================="
echo "测试完成！"
echo "=========================================="

if [ "$total_failed" -gt 0 ]; then
    print_error "发现 $total_failed 个失败测试"
    print_warning "请查看报告文件: $REPORT_FILE"
else
    print_success "所有测试通过！"
    print_info "测试报告: $REPORT_FILE"
fi

echo ""
echo "测试结果文件:"
echo "- Markdown 报告: $REPORT_FILE"
echo "- TRX 结果: $RESULTS_DIR/module-tests-$TIMESTAMP.trx"

echo ""
echo "详细测试日志:"
echo "----------------------------------------"
echo "$all_tests_output" | tail -50

exit $total_failed