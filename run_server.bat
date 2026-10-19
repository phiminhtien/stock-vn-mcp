@echo off
title Stock Analysis MCP Server (C#)
cd /d "%~dp0StockMcpServer"

echo =========================================================
echo   CHON GIAO THUC TRANSPORT KHOI CHAY MCP SERVER:
echo =========================================================
echo [1] Chay che do STDIO (Mac dinh cho Claude Desktop, Cursor)
echo [2] Chay che do HTTP / SSE tren port 5000 (Cho Web, Remote)
echo [3] Xem Huong dan chi tiet (--help)
echo =========================================================
set /p choice="Nhap lua chon cua ban (1, 2 hoac 3) [Mac dinh: 1]: "

if "%choice%"=="2" (
    echo Dang khoi chay Stock MCP Server che do HTTP tren port 5000...
    dotnet run -- --transport http --port 5000
) else if "%choice%"=="3" (
    dotnet run -- --help
) else (
    echo Dang khoi chay Stock MCP Server che do STDIO...
    dotnet run -- --transport stdio
)

pause
