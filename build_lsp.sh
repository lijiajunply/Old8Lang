#!/bin/bash

# Old8Lang LSP + VSCode 扩展构建脚本

set -e

echo "=========================================="
echo "Old8Lang LSP + VSCode Extension Builder"
echo "=========================================="
echo ""

# 检测操作系统
OS=$(uname -s)
ARCH=$(uname -m)

if [ "$OS" == "Darwin" ]; then
    if [ "$ARCH" == "arm64" ]; then
        RID="osx-arm64"
        echo "检测到系统: macOS (Apple Silicon)"
    else
        RID="osx-x64"
        echo "检测到系统: macOS (Intel)"
    fi
elif [ "$OS" == "Linux" ]; then
    RID="linux-x64"
    echo "检测到系统: Linux"
else
    RID="win-x64"
    echo "检测到系统: Windows"
fi

echo ""

# 步骤 1: 构建 Language Server
echo "步骤 1/4: 构建 Language Server..."
cd Old8Lang.LanguageServer
dotnet build -c Release
echo "✓ Language Server 构建完成"
echo ""

# 步骤 2: 发布 Language Server
echo "步骤 2/4: 发布 Language Server 到扩展目录..."
dotnet publish -c Release -r "$RID" --self-contained -o ../vscode-old8lang/server
echo "✓ Language Server 发布完成"
echo ""

# 步骤 3: 安装 npm 依赖
echo "步骤 3/4: 安装 VSCode 扩展依赖..."
cd ../vscode-old8lang
if [ ! -d "node_modules" ]; then
    npm install
else
    echo "依赖已存在，跳过安装"
fi
echo "✓ 依赖安装完成"
echo ""

# 步骤 4: 编译 TypeScript
echo "步骤 4/4: 编译 TypeScript..."
npm run compile
echo "✓ TypeScript 编译完成"
echo ""

echo "=========================================="
echo "构建成功！"
echo "=========================================="
echo ""
echo "下一步："
echo "  1. 在 VSCode 中打开 vscode-old8lang 目录"
echo "  2. 按 F5 启动扩展开发宿主"
echo ""
echo "或者打包扩展："
echo "  cd vscode-old8lang"
echo "  npm install -g @vscode/vsce"
echo "  vsce package"
echo ""
