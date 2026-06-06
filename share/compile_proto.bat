@echo off
setlocal enabledelayedexpansion

echo Compiling Protobuf files...

:: Thư mục chứa file proto
set PROTO_DIR=proto

:: Thư mục đầu ra cho Go (Server)
set GO_OUT=..\server\pkg\pb

:: Thư mục đầu ra cho C# (Client)
set CSHARP_OUT=..\client\Assets\Scripts\Network\Pb

:: Tạo thư mục nếu chưa tồn tại
if not exist "%GO_OUT%" mkdir "%GO_OUT%"
if not exist "%CSHARP_OUT%" mkdir "%CSHARP_OUT%"

:: Lệnh compile
protoc -I=%PROTO_DIR% --go_out=%GO_OUT% --go-grpc_out=%GO_OUT% --csharp_out=%CSHARP_OUT% %PROTO_DIR%\*.proto

echo Done!
