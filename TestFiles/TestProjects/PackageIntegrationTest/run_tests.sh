#!/bin/bash

# Old8Lang 包管理集成测试脚本

echo "======================================"
echo "  Old8Lang 包管理集成测试"
echo "======================================"
echo ""

# 设置颜色
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 测试目录
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$TEST_DIR/../.." && pwd)"
TESTS_DIR="$TEST_DIR/tests"

# Old8Lang 可执行文件
OLD8LANG="dotnet run --project $PROJECT_ROOT/Old8Lang.App --"

# 统计变量
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# 测试结果数组
declare -a TEST_RESULTS

# 运行单个测试
run_test() {
    local test_file=$1
    local test_name=$(basename "$test_file" .old8)

    TOTAL_TESTS=$((TOTAL_TESTS + 1))

    echo -e "${YELLOW}[$TOTAL_TESTS]${NC} 运行测试: $test_name"
    echo "   文件: $test_file"

    # 运行测试
    if $OLD8LANG -f "$test_file" > /tmp/old8lang_test_output.txt 2>&1; then
        echo -e "   ${GREEN}✓ 通过${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        TEST_RESULTS+=("✓ $test_name")

        # 显示输出的前几行
        echo "   输出预览:"
        head -n 5 /tmp/old8lang_test_output.txt | sed 's/^/     /'
    else
        echo -e "   ${RED}✗ 失败${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        TEST_RESULTS+=("✗ $test_name")

        # 显示错误信息
        echo "   错误信息:"
        cat /tmp/old8lang_test_output.txt | sed 's/^/     /'
    fi

    echo ""
}

# 检查 Old8Lang 是否可用
echo "检查 Old8Lang 环境..."
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}错误: dotnet 命令未找到${NC}"
    exit 1
fi

if [ ! -d "$PROJECT_ROOT/Old8Lang.App" ]; then
    echo -e "${RED}错误: Old8Lang.App 项目未找到${NC}"
    exit 1
fi

echo -e "${GREEN}✓ 环境检查通过${NC}"
echo ""

# 检查包是否存在
echo "检查测试包..."
if [ ! -d "$TEST_DIR/packages/Logger" ]; then
    echo -e "${RED}错误: Logger 包未找到${NC}"
    exit 1
fi

if [ ! -d "$TEST_DIR/packages/HttpClient" ]; then
    echo -e "${RED}错误: HttpClient 包未找到${NC}"
    exit 1
fi

echo -e "${GREEN}✓ 测试包检查通过${NC}"
echo "   - Logger"
echo "   - HttpClient"
echo ""

# 构建项目
echo "构建 Old8Lang 项目..."
cd "$PROJECT_ROOT"
if ! dotnet build Old8Lang.sln > /dev/null 2>&1; then
    echo -e "${RED}错误: 项目构建失败${NC}"
    exit 1
fi
echo -e "${GREEN}✓ 构建成功${NC}"
echo ""

# 运行所有测试
echo "======================================"
echo "  开始运行测试"
echo "======================================"
echo ""

# 测试 1: 基本包导入
run_test "$TESTS_DIR/test_basic_package_import.old8"

# 测试 2: 包依赖
run_test "$TESTS_DIR/test_package_dependency.old8"

# 测试 3: 包别名
run_test "$TESTS_DIR/test_package_alias.old8"

# 显示测试总结
echo "======================================"
echo "  测试总结"
echo "======================================"
echo ""
echo "总测试数: $TOTAL_TESTS"
echo -e "通过: ${GREEN}$PASSED_TESTS${NC}"
echo -e "失败: ${RED}$FAILED_TESTS${NC}"
echo ""

# 显示详细结果
echo "详细结果:"
for result in "${TEST_RESULTS[@]}"; do
    echo "  $result"
done
echo ""

# 生成测试报告
REPORT_DIR="$PROJECT_ROOT/Reports"
mkdir -p "$REPORT_DIR"
REPORT_FILE="$REPORT_DIR/包管理集成测试-$(date +%Y%m%d-%H%M%S).md"

cat > "$REPORT_FILE" << EOF
# Old8Lang 包管理集成测试报告

**生成时间**: $(date "+%Y-%m-%d %H:%M:%S")
**测试目录**: $TEST_DIR

## 测试概况

- **总测试数**: $TOTAL_TESTS
- **通过**: $PASSED_TESTS
- **失败**: $FAILED_TESTS
- **通过率**: $(awk "BEGIN {printf \"%.1f\", ($PASSED_TESTS/$TOTAL_TESTS)*100}")%

## 测试结果

EOF

# 添加详细结果到报告
for result in "${TEST_RESULTS[@]}"; do
    echo "- $result" >> "$REPORT_FILE"
done

cat >> "$REPORT_FILE" << EOF

## 测试包信息

### Logger 包
- **版本**: 1.2.0
- **描述**: Simple logging library for Old8Lang
- **主文件**: Logger.old8

### HttpClient 包
- **版本**: 2.0.0
- **描述**: HTTP client library for Old8Lang
- **主文件**: HttpClient.old8
- **依赖**: Logger ^1.0.0

## 测试用例

### 1. 基本包导入测试
- **文件**: test_basic_package_import.old8
- **目的**: 测试基本的包导入和使用功能
- **测试内容**:
  - 导入 Logger 包
  - 创建 Logger 实例
  - 调用不同级别的日志方法

### 2. 包依赖测试
- **文件**: test_package_dependency.old8
- **目的**: 测试包的依赖解析和加载
- **测试内容**:
  - 导入 HttpClient 包（依赖 Logger）
  - 验证依赖自动加载
  - 测试 HTTP 客户端功能

### 3. 包别名测试
- **文件**: test_package_alias.old8
- **目的**: 测试包的别名导入功能
- **测试内容**:
  - 使用别名导入包
  - 通过别名使用包功能

## 结论

EOF

if [ $FAILED_TESTS -eq 0 ]; then
    echo "✅ **所有测试通过！** Old8Lang 包管理集成功能正常。" >> "$REPORT_FILE"
else
    echo "⚠️ **有测试失败。** 请检查失败的测试用例。" >> "$REPORT_FILE"
fi

echo -e "${GREEN}✓ 测试报告已生成${NC}: $REPORT_FILE"
echo ""

# 返回退出码
if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}======================================"
    echo -e "  所有测试通过！"
    echo -e "======================================${NC}"
    exit 0
else
    echo -e "${RED}======================================"
    echo -e "  有测试失败"
    echo -e "======================================${NC}"
    exit 1
fi
