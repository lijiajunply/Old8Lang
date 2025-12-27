@echo off
REM Old8Lang LSP + VSCode 扩展构建脚本 (Windows)

echo ==========================================
echo Old8Lang LSP + VSCode Extension Builder
echo ==========================================
echo.

echo 检测到系统: Windows
echo.

REM 步骤 1: 构建 Language Server
echo 步骤 1/4: 构建 Language Server...
cd Old8Lang.LanguageServer
dotnet build -c Release
if %ERRORLEVEL% NEQ 0 (
    echo 错误: Language Server 构建失败
    exit /b %ERRORLEVEL%
)
echo √ Language Server 构建完成
echo.

REM 步骤 2: 发布 Language Server
echo 步骤 2/4: 发布 Language Server 到扩展目录...
dotnet publish -c Release -r win-x64 --self-contained -o ..\vscode-old8lang\server
if %ERRORLEVEL% NEQ 0 (
    echo 错误: Language Server 发布失败
    exit /b %ERRORLEVEL%
)
echo √ Language Server 发布完成
echo.

REM 步骤 3: 安装 npm 依赖
echo 步骤 3/4: 安装 VSCode 扩展依赖...
cd ..\vscode-old8lang
if not exist "node_modules" (
    call npm install
    if %ERRORLEVEL% NEQ 0 (
        echo 错误: npm 安装失败
        exit /b %ERRORLEVEL%
    )
) else (
    echo 依赖已存在，跳过安装
)
echo √ 依赖安装完成
echo.

REM 步骤 4: 编译 TypeScript
echo 步骤 4/4: 编译 TypeScript...
call npm run compile
if %ERRORLEVEL% NEQ 0 (
    echo 错误: TypeScript 编译失败
    exit /b %ERRORLEVEL%
)
echo √ TypeScript 编译完成
echo.

echo ==========================================
echo 构建成功！
echo ==========================================
echo.
echo 下一步：
echo   1. 在 VSCode 中打开 vscode-old8lang 目录
echo   2. 按 F5 启动扩展开发宿主
echo.
echo 或者打包扩展：
echo   cd vscode-old8lang
echo   npm install -g @vscode/vsce
echo   vsce package
echo.

cd ..
