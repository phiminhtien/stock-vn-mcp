# 🛡️ Stock Analysis & Capital Protection MCP Server (.NET / C#)

> **Model Context Protocol (MCP) Server** chuyên dụng phân tích kỹ thuật chứng khoán, tìm kiếm điểm mua/bán tối ưu, nhận diện xu hướng thị trường, tính toán quy mô vị thế và **cảnh báo bẫy giá giúp nhà đầu tư bảo vệ vốn, tránh mất tiền oan uổng**.

Được phát triển hoàn toàn bằng **C# (.NET 10)**, hỗ trợ **ĐA GIAO THỨC TRANSPORT (Multi-Transport Support)**: **STDIO**, **Streamable HTTP (MCP 2025/2026)** và **Server-Sent Events (SSE)**, tích hợp mượt mà vào **Claude Desktop, Cursor IDE, Google Antigravity, VS Code, Web Apps, Docker Swarms, v.v.**

---

## 🌟 Tính Năng Nổi Bật

1. **Hỗ trợ đa giao thức Transport (Multi-Transport)**:
   - 🔌 **STDIO Transport**: Dành cho các ứng dụng Desktop cục bộ (Claude Desktop, Cursor, VS Code extension, Antigravity CLI).
   - 🌐 **Streamable HTTP Transport (`/mcp`)**: Theo đặc tả MCP 2025-11-25 & 2026-07-28 mới nhất, tối ưu cho microservices và cloud.
   - 📡 **Server-Sent Events SSE Transport (`/sse` & `/message`)**: Tương thích ngược với các công cụ/agent kết nối qua SSE từ xa.
   - 🩺 **Health Check & Metadata (`/health`)**: Cung cấp trạng thái máy chủ, danh sách tools và thị trường hỗ trợ qua REST JSON.
2. **Hỗ trợ đa thị trường**:
   - 🇻🇳 **Thị trường Việt Nam (HOSE, HNX, UPCOM)**: Tự động tải nến lịch sử và báo giá trực tiếp (FPT, HPG, VNM, SSI, MWG, VIC, VHM...).
   - 🌐 **Thị trường Quốc tế & Crypto**: Hỗ trợ cổ phiếu Mỹ (AAPL, NVDA, TSLA, MSFT...), chỉ số (SPY, QQQ) và Crypto (BTC-USD, ETH-USD).
3. **Bộ chỉ báo kỹ thuật toàn diện (Technical Indicators)**:
   - Hệ thống đường trung bình: `EMA20`, `EMA50`, `EMA200`, `SMA20`, `SMA50`, `SMA200`.
   - Động lượng (Momentum): `RSI (14)` (Nhận diện Quá mua / Quá bán / Phân kỳ), `MACD (12, 26, 9)`.
   - Biến động & Nén giá: `Bollinger Bands (20, 2)` (Nhận diện vùng co thắt bùng nổ - Squeeze), `ATR (14)`.
   - Kháng cự & Hỗ trợ cứng (Pivot Support/Resistance).
   - Thước đo Fibonacci Retracement (0.382, 0.5, 0.618 Tỷ lệ vàng) & Extension (1.272, 1.618).
4. **Theo dõi dòng tiền Smart Money & Mô hình nến Nhật (VSA & Price Action)**:
   - **On-Balance Volume (OBV)** & Phát hiện phân kỳ âm kéo xả ngầm của tay to.
   - **Đếm số ngày phân phối (Distribution Days)** theo William O'Neil / CANSLIM.
   - Tự động nhận diện nến đảo chiều: Hammer, Shooting Star, Engulfing, Morning/Evening Star, Doji.
5. **Triết Lý "Tránh Mất Tiền Oan Uổng" (Capital Protection First)**:
   - 🚫 **Cấm bắt dao rơi**: Cảnh báo tức thì khi cổ phiếu đang trong chu kỳ rơi tự do dưới các đường MA lớn.
   - 🚫 **Bẫy FOMO đu đỉnh**: Phát hiện trạng thái quá mua cực độ (RSI > 75) hoặc giá kéo rướn xa EMA20 (>8-10%).
   - 🚫 **Bẫy Bull-trap & Phân phối**: Nhận diện cây nến xả hàng có khối lượng đột biến của tay to (Smart Money).
   - 💼 **Quản lý quy mô vị thế (Position Sizing)**: Tính chính xác số lượng cổ phiếu được mua theo quy tắc 1% - 2% rủi ro tài khoản (Chống cháy tài khoản).
   - 🛑 **Cắt lỗ kỷ luật (Stop-Loss)**: Luôn đề xuất mốc dừng lỗ tối đa -5% đến -7% từ điểm vào lệnh. Cấm gồng lỗ.
   - 🏹 **Chặn lãi thả nổi (Trailing Stop)**: Kế hoạch chốt lời từng nấc và dời Stop-Loss về giá mua để chuyển sang vị thế "Free-Trade".
   - ⚖️ **Bộ lọc tỷ lệ Risk / Reward**: Chỉ khuyến nghị vào lệnh khi R:R tối thiểu từ 1.5:1 đến 2:1 trở lên.

---

## 🛠️ Danh Sách 12 Tools Cung Cấp

### 📊 Nhóm 1: Dữ Liệu Giá & Nến Nhật (Price Action)
| Tên Tool | Mô Tả | Tham Số |
| :--- | :--- | :--- |
| `get_stock_quote` | Lấy giá hiện tại, biến động trong phiên, khối lượng, biên độ giá ngày (High/Low). | `symbol` (ví dụ: `FPT`, `HPG`, `AAPL`) |
| `get_historical_candles` | Lấy dữ liệu nến lịch sử (OHLCV) theo số phiên mong muốn. | `symbol`, `count` (mặc định: 30) |
| `detect_candlestick_patterns` | Tự động nhận diện mô hình nến đảo chiều: Hammer (Búa rút chân), Shooting Star (Búa ngược), Engulfing (Nhấn chìm), Morning/Evening Star (Sao Mai/Sao Hôm), Doji. | `symbol` |

### 📐 Nhóm 2: Chỉ Báo Kỹ Thuật & Dòng Tiền Smart Money
| Tên Tool | Mô Tả | Tham Số |
| :--- | :--- | :--- |
| `calculate_technical_indicators` | Tính toán chi tiết hệ thống SMA/EMA, RSI, MACD, Bollinger Bands, ATR, Hỗ trợ/Kháng cự cứng. | `symbol` |
| `analyze_volume_flow` | Phân tích dòng tiền ròng OBV, Volume Spread Analysis (VSA) và đếm số phiên phân phối (Distribution Days) theo William O'Neil / CANSLIM. | `symbol` |
| `calculate_fibonacci_levels` | Tự động đo đạc các mốc thoái lui Fibo Retracement (0.382, 0.5, 0.618 Tỷ lệ vàng) và mục tiêu mở rộng Fibo Extension (1.272, 1.618). | `symbol`, `lookback` (mặc định: 60) |

### 🎯 Nhóm 3: Chiến Lược Điểm Mua/Bán & Quản Trị Rủi Ro
| Tên Tool | Mô Tả | Tham Số |
| :--- | :--- | :--- |
| `analyze_trading_signals` | **[TOOL ĐỈNH CAO]** Phân tích xu hướng, Vùng Mua An Toàn, Điểm Cắt Lỗ, Target Chốt Lời, Tỷ lệ R:R, Điểm An Toàn Vốn (1-10) và Cảnh Báo Nguy Hiểm. | `symbol` |
| `detect_risk_traps` | Quét nhanh bẫy rủi ro: Đu đỉnh FOMO, xả hàng phân phối, gãy hỗ trợ, rơi tự do không có cầu. | `symbol` |
| `calculate_position_size` | Tính toán chính xác số lượng cổ phiếu nên mua theo Quy Tắc 1%-2% rủi ro tài khoản (Ngăn ngừa cháy tài khoản, tự động làm tròn lô chẵn 100 cp tại sàn VN). | `symbol`, `totalCapital`, `maxRiskPercent`, `entryPrice`, `stopLossPrice` |
| `calculate_exit_strategy` | Lên lộ trình chốt lời từng phần (nấc 1, nấc 2) và điểm chặn lãi thả nổi (Trailing Stop) bảo vệ thành quả. | `symbol`, `entryPrice` |

### 🏆 Nhóm 4: So Sánh & Sàng Lọc Cơ Hội Thị Trường
| Tên Tool | Mô Tả | Tham Số |
| :--- | :--- | :--- |
| `compare_stocks` | So sánh sức mạnh kỹ thuật và xếp hạng an toàn giữa 2 hoặc nhiều mã cổ phiếu (ví dụ: `FPT, HPG, VNM`). | `symbols` |
| `scan_market_opportunities` | Tự động quét VN30/Bluechips hoặc danh sách tùy biến để tìm mã: Breakout vol lớn, Siết nền Bollinger Squeeze, hoặc Quá bán rút chân. | `customSymbols` (để trống quét mặc định) |

---

## 🚦 Tùy Chọn Khởi Chạy Các Giao Thức (Transports)

Bạn có thể chọn giao thức hoạt động thông qua **Command Line Arguments** hoặc **Biến Môi Trường**:

| Tùy chọn Flag | Biến môi trường | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `--transport <stdio\|http\|sse>` | `MCP_TRANSPORT` | `stdio` | Chọn giao thức: `stdio`, `http` hoặc `sse` |
| `--port <cổng>` | `MCP_PORT` hoặc `PORT` | `5000` | Cổng HTTP lắng nghe khi chạy chế độ web |
| `--host <địa chỉ>` | `MCP_HOST` | `localhost` | Địa chỉ IP/host bind (`localhost` hoặc `0.0.0.0`) |
| `--help`, `-h` | - | - | Hiển thị trợ giúp lệnh |

### 1. Khởi chạy chế độ STDIO (Mặc định cho Desktop)
```powershell
# Chạy trực tiếp từ mã nguồn
dotnet run

# Hoặc chỉ định rõ transport
dotnet run -- --transport stdio

# Hoặc chạy file đã publish
./publish/StockMcpServer.exe --transport stdio
```

### 2. Khởi chạy chế độ HTTP / SSE (Web Server & Remote MCP)
```powershell
# Khởi chạy trên cổng 5000
dotnet run -- --transport http --port 5000

# Hoặc mở cho mạng ngoài (bind 0.0.0.0)
dotnet run -- --transport http --host 0.0.0.0 --port 5000
```

Khi chạy ở chế độ HTTP, server cung cấp đầy đủ các endpoint:
- **Streamable HTTP Endpoint**: `http://localhost:5000/mcp`
- **Server-Sent Events (SSE)**: `http://localhost:5000/sse` hoặc `http://localhost:5000/mcp/sse`
- **SSE Message Postback**: `http://localhost:5000/message` hoặc `http://localhost:5000/mcp/message`
- **Trang Dashboard & Health Check**: `http://localhost:5000/` và `http://localhost:5000/health`

### 3. Menu chọn nhanh trực quan
Bạn có thể nhấp đúp file [`run_server.bat`](file:///C:/Users/ADMIN/Desktop/agent-orchestration-tracking/run_server.bat) để chọn chế độ STDIO, HTTP hoặc xem hướng dẫn qua menu số.

---

## ⚙️ Cấu Hình MCP Cho Các Ứng Dụng AI

### Cách 1: Kết nối dạng STDIO (Khuyên dùng khi chạy local)

#### Cho **Claude Desktop**
File cấu hình: `%APPDATA%\Claude\claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "stock-analysis": {
      "command": "C:\\Users\\ADMIN\\Desktop\\agent-orchestration-tracking\\StockMcpServer\\publish\\StockMcpServer.exe",
      "args": ["--transport", "stdio"]
    }
  }
}
```

#### Cho **Cursor IDE**
Trong Cursor: `Settings` -> `Features` -> `MCP Servers` -> `Add New MCP Server`:
- **Name**: `stock-analysis`
- **Type**: `stdio`
- **Command**: `dotnet run --project C:\Users\ADMIN\Desktop\agent-orchestration-tracking\StockMcpServer\StockMcpServer.csproj -- --transport stdio`

---

### Cách 2: Kết nối dạng HTTP / SSE (Dành cho Remote Server / Microservices)

Đầu tiên khởi chạy server nền:
```powershell
dotnet run --project StockMcpServer -- --transport http --port 5000
```

#### Cho các client hỗ trợ **Streamable HTTP (MCP 2025/2026)**
```json
{
  "mcpServers": {
    "stock-analysis-http": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

#### Cho các client hỗ trợ **SSE Transport**
```json
{
  "mcpServers": {
    "stock-analysis-sse": {
      "url": "http://localhost:5000/sse"
    }
  }
}
```

---

## 💡 Quy Trình 3 Bước Đầu Tư Hiệu Quả Cao Cùng AI

### Bước 1: Sàng lọc cổ phiếu hàng đầu (`scan_market_opportunities`)
> **Câu hỏi gợi ý:** *"Hôm nay thị trường có mã nào thuộc TIER A bùng nổ vượt cản hoặc siết nền an toàn không?"*

```markdown
# 🏆 BẢNG XẾP HẠNG CƠ HỘI ĐẦU TƯ TỐT NHẤT THỊ TRƯỜNG
| Xếp Hạng | Mã CP | Giá hiện tại | Mẫu Hình Kỹ Thuật | Điểm An Toàn | RSI | Khối Lượng | Khuyến Nghị |
| :--- | :---: | :---: | :--- | :---: | :---: | :---: | :--- |
| TIER A (Cơ hội Vàng) | HPG | 20.90 | 💎 SIẾT NỀN CO THẮT (Squeeze cạn vol - Chuẩn bị bùng nổ) | 7/10 | 52.2 | 0.6x TB20 | MUA ĐẸP TẠI VÙNG GIÁ AN TOÀN |
| TIER B (Cơ hội Tốt) | MWG | 73.90 | 🟢 UPTREND ĐỀU ĐẶN (Canh mua rung lắc quanh MA20) | 10/10 | 53.0 | 0.5x TB20 | MUA ĐẸP TẠI VÙNG GIÁ AN TOÀN |
| THEO DÕI | VNM | 57.80 | Vận động bình thường | 3/10 | 41.9 | 0.8x TB20 | QUAN SÁT THẬN TRỌNG |
| THEO DÕI | FPT | 58.20 | Vận động bình thường | 2/10 | 33.6 | 0.7x TB20 | QUAN SÁT THẬN TRỌNG |
| THEO DÕI | SSI | 19.45 | Vận động bình thường | 2/10 | 42.8 | 0.7x TB20 | QUAN SÁT THẬN TRỌNG |
```

### Bước 2: Kiểm tra bẫy phân phối Smart Money & Mô hình nến
> **Câu hỏi gợi ý:** *"Kiểm tra dòng tiền Smart Money và mô hình nến của mã HPG xem có an toàn không?"*

```markdown
# 🌊 PHÂN TÍCH DÒNG TIỀN & KHỐI LƯỢNG VSA: HPG
- Khối lượng phiên cuối: 10,189,400 cp (0.58x so với TB20)
- Pha hành động của Smart Money: GIAI ĐOẠN PHÂN PHỐI NẶNG (Cảnh báo đỉnh)
- Xu hướng dòng tiền ròng (OBV Trend): PHÂN KỲ ÂM OBV (Kéo xả - CỰC KỲ NGUY HIỂM)
- Thống kê 20 phiên: 2 phiên Tích lũy | 5 phiên Phân phối xả hàng ⚠️
> 🎯 KẾT LUẬN: RỦI RO CAO: Dòng tiền lớn đang rút ra. Không mở vị thế mua mới; ưu tiên bảo toàn lợi nhuận.
```

### Bước 3: Tính toán quy mô vị thế & Lên kế hoạch chốt lời
> **Câu hỏi gợi ý:** *"Vốn của tôi là 200 triệu, tôi chấp nhận rủi ro 2%, hãy tính số lượng cổ phiếu FPT tôi nên mua và điểm dừng lỗ/chốt lời?"*

```markdown
# 💼 BẢNG TÍNH TOÁN PHÂN BỔ VỐN & VỊ THẾ: FPT
- Tổng vốn tài khoản: 200,000,000 VND
- Mức rủi ro tối đa cho phép: 2.0% = 4,000,000 VND
- Giá vào lệnh (Entry): 58.20 | Mốc cắt lỗ (Stop-Loss): 54.68 (-6.1%)
- 📌 SỐ LƯỢNG NÊN MUA: 1,100 cổ phiếu (Lô chẵn 100 cp)
- 💰 Tổng giá trị giải ngân: 64,020,000 VND (32.0% danh mục => Tỷ trọng lý tưởng)
- 🛡️ Số tiền mất tối đa nếu chạm Stop-Loss: 3,872,000 VND (Đúng giới hạn 2% tài khoản)
```

---

## 📁 Cấu Trúc Dự Án C#
```
StockMcpServer/
├── Models/
│   ├── StockCandle.cs             # Nến OHLCV
│   ├── StockQuote.cs              # Báo giá phiên hiện tại
│   ├── TechnicalIndicators.cs     # Các chỉ báo SMA/EMA, RSI, MACD, Bollinger, ATR
│   ├── TradingSignalReport.cs     # Báo cáo chiến lược mua/bán & bẫy rủi ro
│   ├── CandlestickPattern.cs      # Mô hình nến Nhật (Hammer, Doji, Engulfing...)
│   ├── PositionSizeResult.cs      # Kết quả phân bổ vốn & quy mô vị thế
│   ├── VolumeFlowAnalysis.cs      # Dòng tiền VSA, OBV & ngày phân phối
│   └── TradingStrategyModels.cs   # Fibonacci & Chiến lược Trailing Stop
├── Services/
│   ├── IStockDataProvider.cs      # Interface nạp dữ liệu thị trường
│   ├── VnStockProvider.cs         # Kết nối API thị trường Việt Nam (VNDirect)
│   ├── YahooFinanceProvider.cs    # Kết nối API chứng khoán Quốc tế & Crypto
│   ├── UnifiedStockService.cs     # Tự động định tuyến ticker & nạp dữ liệu
│   ├── TechnicalAnalysisEngine.cs # Bộ máy toán học tính toán chỉ báo kỹ thuật
│   ├── InvestmentStrategyAdvisor.cs # Bộ quy tắc quản trị rủi ro & tỷ lệ R:R
│   ├── CandlestickPatternDetector.cs # Nhận diện mô hình nến đảo chiều
│   ├── VolumeFlowAnalyzer.cs      # Phân tích dòng tiền Smart Money & OBV
│   ├── RiskAndPositionManager.cs  # Quản lý rủi ro danh mục & Position Sizing
│   ├── FibonacciCalculator.cs     # Tính toán thước đo Fibonacci
│   └── MarketOpportunityScanner.cs # Bộ sàng lọc cơ hội bùng nổ toàn thị trường
├── Tools/
│   └── StockAnalysisTools.cs      # Khai báo 12 MCP Tools theo chuẩn ModelContextProtocol
├── Program.cs                     # Điều phối đa giao thức Transport (STDIO, HTTP, SSE)
├── StockMcpServer.csproj          # Cấu hình project .NET 10 (Web SDK)
└── README.md                      # Hướng dẫn chi tiết
```
