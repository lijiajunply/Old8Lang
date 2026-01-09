#!/bin/bash

# Old8Lang Language Server 更新脚本
# 用于将构建好的 Language Server 复制到 VSCode 扩展目录

set -e

echo "📦 正在构建 Old8Lang.LanguageServer..."
dotnet build Old8Lang.LanguageServer/Old8Lang.LanguageServer.csproj -c Release

echo ""
echo "📋 正在复制文件到 VSCode 扩展目录..."

# 目标目录
TARGET_DIR="vscode-old8lang/server"

# 清空目标目录
rm -rf "$TARGET_DIR"/*

# 源目录
SOURCE_DIR="Old8Lang.LanguageServer/bin/Release/net10.0"

# 复制所有文件
cp -r "$SOURCE_DIR"/* "$TARGET_DIR/"

echo ""
echo "✅ Language Server 更新完成！"
echo ""
echo "📝 下一步操作："
echo "  1. cd vscode-old8lang"
echo "  2. npm run compile"
echo "  3. vsce package (可选，如果要打包安装)"
echo "  4. 在 VSCode 中按 F5 测试，或安装 .vsix 文件"
echo ""
echo "文件位置："
echo "  Server: $TARGET_DIR/Old8Lang.LanguageServer"
echo ""
