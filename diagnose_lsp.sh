#!/bin/bash

# Old8Lang LSP 诊断脚本
# 用于诊断 Language Server Protocol 相关问题

echo "🔍 Old8Lang LSP 诊断工具"
echo "========================"
echo ""

# 检查 1: Language Server 可执行文件
echo "✓ 检查 1: Language Server 可执行文件"
LS_PATH="vscode-old8lang/server/Old8Lang.LanguageServer"
if [ -f "$LS_PATH" ]; then
    echo "  ✅ 文件存在: $LS_PATH"
    file "$LS_PATH"
    if [ -x "$LS_PATH" ]; then
        echo "  ✅ 文件可执行"
    else
        echo "  ❌ 文件不可执行"
        echo "  修复命令: chmod +x $LS_PATH"
    fi
else
    echo "  ❌ Language Server 文件不存在"
    echo "  请运行: ./update_language_server.sh"
fi
echo ""

# 检查 2: VSCode 扩展编译状态
echo "✓ 检查 2: VSCode 扩展编译状态"
if [ -f "vscode-old8lang/out/extension.js" ]; then
    echo "  ✅ 扩展已编译: vscode-old8lang/out/extension.js"
    COMPILE_TIME=$(stat -f "%Sm" -t "%Y-%m-%d %H:%M:%S" vscode-old8lang/out/extension.js 2>/dev/null || stat -c "%y" vscode-old8lang/out/extension.js 2>/dev/null)
    echo "  编译时间: $COMPILE_TIME"
else
    echo "  ❌ 扩展未编译"
    echo "  修复命令: cd vscode-old8lang && npm run compile"
fi
echo ""

# 检查 3: npm 依赖
echo "✓ 检查 3: npm 依赖"
if [ -d "vscode-old8lang/node_modules" ]; then
    echo "  ✅ node_modules 存在"
else
    echo "  ❌ npm 依赖未安装"
    echo "  修复命令: cd vscode-old8lang && npm install"
fi
echo ""

# 检查 4: Language Server 依赖文件
echo "✓ 检查 4: Language Server 依赖文件"
REQUIRED_DLLS=(
    "Old8Lang.dll"
    "OmniSharp.Extensions.LanguageServer.dll"
    "OmniSharp.Extensions.LanguageProtocol.dll"
)

all_dlls_present=true
for dll in "${REQUIRED_DLLS[@]}"; do
    if [ -f "vscode-old8lang/server/$dll" ]; then
        echo "  ✅ $dll"
    else
        echo "  ❌ $dll 缺失"
        all_dlls_present=false
    fi
done

if [ "$all_dlls_present" = false ]; then
    echo "  修复命令: ./update_language_server.sh"
fi
echo ""

# 检查 5: 测试 Language Server 能否启动
echo "✓ 检查 5: Language Server 启动测试"
echo "  正在测试 Language Server 是否能响应（5秒超时）..."
if [ -f "$LS_PATH" ]; then
    # 发送空输入并在 5 秒后超时
    timeout 5 "$LS_PATH" </dev/null >/dev/null 2>&1 &
    LS_PID=$!
    sleep 1

    if ps -p $LS_PID > /dev/null 2>&1; then
        echo "  ✅ Language Server 可以启动"
        kill $LS_PID 2>/dev/null || true
    else
        echo "  ⚠️  Language Server 可能存在问题"
    fi
else
    echo "  ⏭️  跳过（文件不存在）"
fi
echo ""

# 检查 6: 示例测试文件
echo "✓ 检查 6: 测试文件"
TEST_FILES=$(find TestFiles -name "*.old8" 2>/dev/null | head -5)
if [ -n "$TEST_FILES" ]; then
    echo "  ✅ 找到测试文件:"
    echo "$TEST_FILES" | while read -r line; do
        echo "     - $line"
    done
else
    echo "  ⚠️  未找到测试文件"
fi
echo ""

# 总结
echo "========================"
echo "📊 诊断总结"
echo "========================"
echo ""
echo "如果所有检查都通过，请尝试以下步骤："
echo ""
echo "1. 在 VSCode 中打开扩展开发模式："
echo "   cd vscode-old8lang"
echo "   code ."
echo "   按 F5 启动调试"
echo ""
echo "2. 或打包安装扩展："
echo "   cd vscode-old8lang"
echo "   npm install -g @vscode/vsce  # 如果还没安装"
echo "   vsce package"
echo "   # 然后在 VSCode 中: Extensions -> Install from VSIX"
echo ""
echo "3. 启用详细日志查看问题："
echo "   - 在 VSCode 设置中搜索 'old8lang.trace.server'"
echo "   - 设置为 'verbose'"
echo "   - 查看输出面板: 视图 > 输出 > Old8Lang Language Server"
echo ""

# 检查是否有正在运行的 Language Server 进程
RUNNING_LS=$(ps aux | grep -i "Old8Lang.LanguageServer" | grep -v grep | wc -l)
if [ $RUNNING_LS -gt 0 ]; then
    echo "⚠️  警告: 发现 $RUNNING_LS 个正在运行的 Language Server 进程"
    echo "   可能是之前的测试留下的，建议清理："
    echo "   pkill -f Old8Lang.LanguageServer"
    echo ""
fi

echo "如果问题仍然存在，请检查以下内容："
echo "  - TestCoverage 报告显示有 14 个测试失败（93.6% 通过率）"
echo "  - 主要失败在: SignatureHelp (7), SemanticTokens (5) 等功能"
echo "  - 这些失败可能导致部分 LSP 功能不稳定"
echo ""
echo "详细信息请查看:"
echo "  - Old8Lang.Tests/LanguageServer/README_TestCoverage.md"
echo "  - INSTALL_VSCODE_EXTENSION.md"
echo ""
