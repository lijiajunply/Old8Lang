#!/bin/bash
# FirstUI.TestApp 启动脚本
# 用于在 macOS 上运行 FirstUI 测试应用

echo "======================================"
echo "  FirstUI.TestApp 启动器"
echo "======================================"
echo ""

# 检测当前shell
if [ -n "$SSH_CONNECTION" ] || [ -n "$SSH_CLIENT" ]; then
    echo "⚠️  警告: 检测到 SSH 连接"
    echo "   GUI 应用无法在 SSH 会话中运行"
    echo "   请在本地Terminal.app中运行此脚本"
    echo ""
    read -p "是否继续尝试运行? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# 检测运行环境
echo "环境信息:"
echo "  操作系统: $(uname -s) $(uname -r)"
echo "  .NET版本: $(dotnet --version)"
echo "  当前用户: $USER"
echo "  工作目录: $(pwd)"
echo ""

# 选择运行模式
echo "选择运行模式:"
echo "  1) 运行 FirstUI 测试 (默认)"
echo "  2) 运行简单 Avalonia 测试"
echo ""
read -p "请选择 (1/2): " -n 1 -r mode
echo ""

case $mode in
    2)
        echo "▶️  启动简单 Avalonia 测试..."
        dotnet run --project FirstUI.TestApp/FirstUI.TestApp.csproj -- --simple
        ;;
    *)
        echo "▶️  启动 FirstUI 测试..."
        dotnet run --project FirstUI.TestApp/FirstUI.TestApp.csproj
        ;;
esac

exit_code=$?

echo ""
if [ $exit_code -eq 0 ]; then
    echo "✅ 应用已正常退出"
else
    echo "❌ 应用退出时出现错误 (退出码: $exit_code)"
fi

exit $exit_code
