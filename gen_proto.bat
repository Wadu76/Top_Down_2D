@echo off
echo       Start Generating Proto Files


:: 1. 检查目录是否存在，不存在则创建
if not exist "server\pkg\pb" mkdir "server\pkg\pb"
if not exist "client\Assets\Scripts\Network" mkdir "client\Assets\Scripts\Network"

:: 2. 生成 Go 代码
:: --go_out: 输出目录
:: --go_opt: 路径生成选项 (source_relative 表示不按 GOPATH 生成，直接生成在当前目录)
:: --proto_path: proto 文件所在的根目录
echo [1/2] Generating Go code...
protoc --go_out=server/pkg/pb --go_opt=paths=source_relative --proto_path=proto proto/game.proto
if %errorlevel% neq 0 (
    echo [ERROR] Failed to generate Go code!
    pause
    exit /b
)

:: 3. 生成 C# 代码
:: --csharp_out: 输出目录
echo [2/2] Generating C# code...
protoc --csharp_out=client/Assets/Scripts/Network --proto_path=proto proto/game.proto
if %errorlevel% neq 0 (
    echo [ERROR] Failed to generate C# code!
    pause
    exit /b
)

echo ==========================================
echo          Success! All Done.
echo ==========================================
pause