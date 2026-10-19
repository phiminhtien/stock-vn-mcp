using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using StockMcpServer.Models;
using StockMcpServer.Services;

namespace StockMcpServer.Tools;

[McpServerToolType]
public class StockAnalysisTools
{
    private readonly UnifiedStockService _stockService;
    private readonly TechnicalAnalysisEngine _engine;
    private readonly InvestmentStrategyAdvisor _advisor;
    private readonly CandlestickPatternDetector _patternDetector;
    private readonly VolumeFlowAnalyzer _volumeFlowAnalyzer;
    private readonly RiskAndPositionManager _positionManager;
    private readonly FibonacciCalculator _fibonacciCalculator;
    private readonly MarketOpportunityScanner _opportunityScanner;

    public StockAnalysisTools(
        UnifiedStockService stockService,
        TechnicalAnalysisEngine engine,
        InvestmentStrategyAdvisor advisor,
        CandlestickPatternDetector patternDetector,
        VolumeFlowAnalyzer volumeFlowAnalyzer,
        RiskAndPositionManager positionManager,
        FibonacciCalculator fibonacciCalculator,
        MarketOpportunityScanner opportunityScanner)
    {
        _stockService = stockService;
        _engine = engine;
        _advisor = advisor;
        _patternDetector = patternDetector;
        _volumeFlowAnalyzer = volumeFlowAnalyzer;
        _positionManager = positionManager;
        _fibonacciCalculator = fibonacciCalculator;
        _opportunityScanner = opportunityScanner;
    }

    // ==========================================
    // 1. LẤY BÁO GIÁ HIỆN TẠI
    // ==========================================
    [McpServerTool(Name = "get_stock_quote")]
    [Description("Lấy dữ liệu giá mới nhất, biên độ dao động trong ngày, khối lượng giao dịch và thông tin cơ bản của một mã cổ phiếu (Việt Nam như FPT, VNM, HPG, SSI... hoặc Quốc tế như AAPL, TSLA, NVDA...).")]
    public async Task<string> GetStockQuote(
        [Description("Mã cổ phiếu cần tra cứu (ví dụ: FPT, VNM, HPG, AAPL, MSFT)")] string symbol,
        CancellationToken cancellationToken = default)
    {
        var quote = await _stockService.GetQuoteAsync(symbol, cancellationToken);
        if (quote == null)
        {
            return $"Không tìm thấy dữ liệu giá cho mã cổ phiếu '{symbol}'. Vui lòng kiểm tra lại mã.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"### 📈 BÁO CÁO GIÁ CỔ PHIẾU: **{quote.Symbol}**");
        sb.AppendLine($"- **Thị trường**: {quote.Market} ({quote.Currency})");
        sb.AppendLine($"- **Giá hiện tại**: **{quote.CurrentPrice:N2}**");
        sb.AppendLine($"- **Biến động phiên**: {(quote.Change >= 0 ? "+" : "")}{quote.Change:N2} ({(quote.ChangePercent >= 0 ? "+" : "")}{quote.ChangePercent:F2}%)");
        sb.AppendLine($"- **Cao nhất ngày (High)**: {quote.DayHigh:N2}");
        sb.AppendLine($"- **Thấp nhất ngày (Low)**: {quote.DayLow:N2}");
        sb.AppendLine($"- **Khối lượng giao dịch**: {quote.Volume:N0} cp");
        sb.AppendLine($"- **Cập nhật ngày**: {quote.LastUpdated:yyyy-MM-dd}");

        return sb.ToString();
    }

    // ==========================================
    // 2. LẤY LỊCH SỬ NẾN OHLCV
    // ==========================================
    [McpServerTool(Name = "get_historical_candles")]
    [Description("Lấy danh sách các cây nến lịch sử (OHLCV: Ngày, Giá Mở, Cao nhất, Thấp nhất, Đóng cửa, Khối lượng) của mã cổ phiếu trong số phiên yêu cầu.")]
    public async Task<string> GetHistoricalCandles(
        [Description("Mã cổ phiếu (ví dụ: FPT, VNM, AAPL)")] string symbol,
        [Description("Số phiên giao dịch cần lấy (mặc định: 30)")] int count = 30,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, count, cancellationToken);
        if (candles.Count == 0)
        {
            return $"Không có dữ liệu lịch sử nến cho mã '{symbol}'.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"### 📊 DỮ LIỆU LỊCH SỬ NẾN CỔ PHIẾU: **{symbol.ToUpperInvariant()}** ({candles.Count} phiên gần nhất)");
        sb.AppendLine("| Ngày | Mở cửa (Open) | Cao nhất (High) | Thấp nhất (Low) | Đóng cửa (Close) | Khối lượng (Vol) |");
        sb.AppendLine("| :--- | :---: | :---: | :---: | :---: | :---: |");

        foreach (var c in candles.TakeLast(30))
        {
            sb.AppendLine($"| {c.Date:yyyy-MM-dd} | {c.Open:N2} | {c.High:N2} | {c.Low:N2} | {c.Close:N2} | {c.Volume:N0} |");
        }

        return sb.ToString();
    }

    // ==========================================
    // 3. TÍNH CHỈ BÁO KỸ THUẬT CỐT LÕI
    // ==========================================
    [McpServerTool(Name = "calculate_technical_indicators")]
    [Description("Tính toán chi tiết toàn bộ chỉ báo kỹ thuật cốt lõi: SMA/EMA (20, 50, 200), RSI (14), MACD (12, 26, 9), Dải Bollinger Bands, ATR (14), Hỗ trợ/Kháng cự và phân tích thanh khoản.")]
    public async Task<string> CalculateIndicators(
        [Description("Mã cổ phiếu (ví dụ: FPT, VNM, HPG, AAPL)")] string symbol,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, 120, cancellationToken);
        if (candles.Count < 20)
        {
            return $"Không đủ dữ liệu lịch sử để tính toán chỉ báo cho mã '{symbol}' (cần tối thiểu 20 phiên).";
        }

        var ind = _engine.CalculateIndicators(candles);
        var latest = candles[^1];

        var sb = new StringBuilder();
        sb.AppendLine($"### 📐 BẢNG CHỈ BÁO KỸ THUẬT: **{symbol.ToUpperInvariant()}** (Giá hiện tại: **{latest.Close:N2}**)");
        sb.AppendLine("#### 1. Hệ Thống Đường Trung Bình (Trend MAs):");
        sb.AppendLine($"- **EMA 20**: {ind.Ema20?.ToString("N2") ?? "N/A"} | **SMA 20**: {ind.Sma20?.ToString("N2") ?? "N/A"}");
        sb.AppendLine($"- **EMA 50**: {ind.Ema50?.ToString("N2") ?? "N/A"} | **SMA 50**: {ind.Sma50?.ToString("N2") ?? "N/A"}");
        sb.AppendLine($"- **EMA 200**: {ind.Ema200?.ToString("N2") ?? "N/A"} | **SMA 200**: {ind.Sma200?.ToString("N2") ?? "N/A"}");

        sb.AppendLine("\n#### 2. Động Lượng & Xung Lực (Momentum):");
        sb.AppendLine($"- **RSI (14)**: **{ind.Rsi14?.ToString("F1") ?? "N/A"}** => *{ind.RsiSignal}*");
        sb.AppendLine($"- **MACD Line**: {ind.MacdLine?.ToString("N2") ?? "N/A"} | **Signal**: {ind.MacdSignalLine?.ToString("N2") ?? "N/A"} | **Hist**: {ind.MacdHistogram?.ToString("N2") ?? "N/A"}");
        sb.AppendLine($"- **Trạng thái MACD**: *{ind.MacdSignal}*");

        sb.AppendLine("\n#### 3. Dải Bollinger Bands & Biến Động:");
        sb.AppendLine($"- **Bollinger Upper (Cản trên)**: {ind.BollingerUpper?.ToString("N2") ?? "N/A"}");
        sb.AppendLine($"- **Bollinger Middle (Trục giữa)**: {ind.BollingerMiddle?.ToString("N2") ?? "N/A"}");
        sb.AppendLine($"- **Bollinger Lower (Đáy dưới)**: {ind.BollingerLower?.ToString("N2") ?? "N/A"}");
        sb.AppendLine($"- **Độ nén dải băng (Squeeze)**: {(ind.IsBollingerSqueeze ? "🔥 ĐANG CO THẮT CHẶT - Sắp bùng nổ biến động lớn!" : "Bình thường")}");
        sb.AppendLine($"- **ATR 14 (Biên độ dao động trung bình)**: {ind.Atr14?.ToString("N2") ?? "N/A"}");

        sb.AppendLine("\n#### 4. Vùng Cản & Thanh Khoản:");
        sb.AppendLine($"- **Hỗ trợ cứng (Support)**: **{ind.SupportLevel:N2}**");
        sb.AppendLine($"- **Kháng cự gần nhất (Resistance)**: **{ind.ResistanceLevel:N2}**");
        sb.AppendLine($"- **Khối lượng phiên cuối**: {latest.Volume:N0} cp ({ind.VolumeSignal})");

        return sb.ToString();
    }

    // ==========================================
    // 4. PHÂN TÍCH TÍN HIỆU MUA/BÁN TỔNG THỂ
    // ==========================================
    [McpServerTool(Name = "analyze_trading_signals")]
    [Description("PHÂN TÍCH TOÀN DIỆN ĐIỂM MUA/BÁN VÀ QUẢN TRỊ RỦI RO: Đánh giá xu hướng, xác định chính xác VÙNG MUA AN TOÀN, ĐIỂM CẮT LỖ BẢO TOÀN VỐN, MỤC TIÊU CHỐT LỜI (T1/T2), Tỷ lệ Risk/Reward, và cảnh báo các bẫy giảm giá giúp nhà đầu tư KHÔNG MẤT TIỀN OAN UỔNG.")]
    public async Task<string> AnalyzeTradingSignals(
        [Description("Mã cổ phiếu cần phân tích (ví dụ: FPT, HPG, VNM, SSI, AAPL, NVDA)")] string symbol,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, 120, cancellationToken);
        if (candles.Count < 20)
        {
            return $"Không đủ dữ liệu lịch sử để phân tích chiến lược cho mã '{symbol}'.";
        }

        var indicators = _engine.CalculateIndicators(candles);
        string market = symbol.Trim().Length == 3 && char.IsLetter(symbol.Trim()[0]) ? "VN" : "GLOBAL";
        var report = _advisor.GenerateReport(symbol, market, candles, indicators);

        var sb = new StringBuilder();
        sb.AppendLine($"# 🛡️ CHIẾN LƯỢC ĐẦU TƯ & QUẢN TRỊ RỦI RO: **{report.Symbol}**");
        sb.AppendLine($"**Giá hiện tại**: `{report.CurrentPrice:N2} {report.Currency}` (Biến động: {(report.PriceChangePercent >= 0 ? "+" : "")}{report.PriceChangePercent}%)");
        sb.AppendLine($"**Xu hướng chủ đạo**: **{report.TrendStatus}**");
        sb.AppendLine($"**Điểm An Toàn Vốn**: **{report.SafetyScore}/10** {(report.SafetyScore >= 7 ? "🟢 (An toàn cao)" : report.SafetyScore >= 5 ? "🟡 (Trung bình)" : "🔴 (Cực kỳ rủi ro)")}");
        sb.AppendLine($"**HÀNH ĐỘNG KHUYẾN NGHỊ**: 🎯 **`{report.PrimaryAction}`**");
        sb.AppendLine($"\n> 💡 **Nhận định trọng tâm**: {report.ExecutiveSummary}");

        sb.AppendLine("\n---");
        sb.AppendLine("### 🎯 KẾ HOẠCH ĐI LỆNH (TRADE SETUP) TỐI ƯU LỢI NHUẬN:");
        sb.AppendLine($"- 🟢 **Vùng mua an toàn (Safe Entry Zone)**: **{report.SafeBuyMin:N2} - {report.SafeBuyMax:N2}**");
        sb.AppendLine($"- 🛑 **Mức cắt lỗ kỷ luật (Stop Loss)**: **{report.StopLossPrice:N2}** ({report.StopLossPercent:F1}%) *(Bảo vệ tài khoản, cấm gồng lỗ nếu vi phạm)*");
        sb.AppendLine($"- 🎯 **Mục tiêu chốt lời 1 (Target 1)**: **{report.TakeProfit1:N2}** (+{report.TakeProfit1Percent:F1}%)");
        sb.AppendLine($"- 🚀 **Mục tiêu chốt lời 2 (Target 2)**: **{report.TakeProfit2:N2}** (+{report.TakeProfit2Percent:F1}%)");
        sb.AppendLine($"- ⚖️ **Tỷ lệ Lợi nhuận / Rủi ro (Risk : Reward)**: **{report.RiskRewardRatio:F1} : 1** {(report.RiskRewardRatio >= 2.0m ? "✅ (Rất đẹp > 2:1)" : report.RiskRewardRatio >= 1.5m ? "⚠️ (Tạm chấp nhận)" : "❌ (Kém, rủi ro cao hơn lợi nhuận)")}");

        if (report.DangerousTraps.Count > 0)
        {
            sb.AppendLine("\n---");
            sb.AppendLine("### ⚠️ CẢNH BÁO NGUY HIỂM - TRÁNH MẤT TIỀN OAN UỔNG:");
            foreach (var trap in report.DangerousTraps)
            {
                sb.AppendLine($"- ❌ **{trap}**");
            }
        }

        if (report.PositiveDrivers.Count > 0)
        {
            sb.AppendLine("\n---");
            sb.AppendLine("### 🌟 ĐỘNG LỰC TĂNG GIÁ TÍCH CỰC:");
            foreach (var driver in report.PositiveDrivers)
            {
                sb.AppendLine($"- ✅ {driver}");
            }
        }

        sb.AppendLine("\n---");
        sb.AppendLine("### 📜 4 NGUYÊN TẮC VÀNG BẢO TOÀN VỐN CỦA PHÙ THỦY CHỨNG KHOÁN:");
        foreach (var rule in report.GoldenRules)
        {
            sb.AppendLine($"- 🔑 {rule}");
        }

        return sb.ToString();
    }

    // ==========================================
    // 5. QUÉT BẪY RỦI RO
    // ==========================================
    [McpServerTool(Name = "detect_risk_traps")]
    [Description("QUÉT BẪY RỦI RO & BẢO VỆ TÀI KHOẢN: Kiểm tra tức thời mã cổ phiếu có đang vướng vào: Bẫy Bull-trap đỉnh, Phân kỳ âm RSI/MACD, Phân phối bán xả của tổ chức, Đua lệnh vùng quá mua FOMO, hay đang rơi tự do không có hỗ trợ hay không.")]
    public async Task<string> DetectRiskTraps(
        [Description("Mã cổ phiếu cần quét rủi ro (ví dụ: FPT, HPG, VNM, NVDA)")] string symbol,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, 100, cancellationToken);
        if (candles.Count < 20)
        {
            return $"Không đủ dữ liệu để quét bẫy rủi ro cho mã '{symbol}'.";
        }

        var ind = _engine.CalculateIndicators(candles);
        string market = symbol.Trim().Length == 3 ? "VN" : "GLOBAL";
        var report = _advisor.GenerateReport(symbol, market, candles, ind);

        var sb = new StringBuilder();
        sb.AppendLine($"### 🚨 BẢNG QUÉT BẪY RỦI RO CỔ PHIẾU: **{symbol.ToUpperInvariant()}**");
        sb.AppendLine($"*Giá hiện tại: {report.CurrentPrice:N2} | Xu hướng: {report.TrendStatus}*");

        if (report.DangerousTraps.Count == 0)
        {
            sb.AppendLine("\n🎉 **TỐT! Không phát hiện bẫy rủi ro nghiêm trọng.**");
            sb.AppendLine("- Cổ phiếu không bị phân phối xả hàng lớn.");
            sb.AppendLine("- RSI không trong vùng quá mua đỉnh.");
            sb.AppendLine("- Cấu trúc giá không rơi tự do dưới các đường MA lớn.");
            sb.AppendLine($"- Vẫn cần tuân thủ mốc cắt lỗ kỷ luật tại: **{report.StopLossPrice:N2}**.");
        }
        else
        {
            sb.AppendLine($"\n⚠️ **PHÁT HIỆN {report.DangerousTraps.Count} YẾU TỐ RỦI RO CAO CẦN ĐẶC BIỆT CẢNH GIÁC:**");
            for (int i = 0; i < report.DangerousTraps.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {report.DangerousTraps[i]}");
            }
            sb.AppendLine("\n💡 **LỜI KHUYÊN**: Nếu đang nắm giữ mã này, cân nhắc hạ tỷ trọng ngay lập tức hoặc nâng sát ngưỡng chặn lãi. TUYỆT ĐỐI KHÔNG BẮT ĐÁY HOẶC MUA GIA TĂNG.");
        }

        return sb.ToString();
    }

    // ==========================================
    // 6. SO SÁNH SỨC MẠNH CỔ PHIẾU
    // ==========================================
    [McpServerTool(Name = "compare_stocks")]
    [Description("So sánh sức mạnh kỹ thuật, xu hướng và tỷ lệ Risk/Reward giữa 2 hoặc nhiều mã cổ phiếu để tìm ra mã an toàn và có tiềm năng sinh lời tốt nhất.")]
    public async Task<string> CompareStocks(
        [Description("Danh sách các mã cổ phiếu phân cách bằng dấu phẩy (ví dụ: 'FPT, VNM, HPG' hoặc 'AAPL, NVDA, MSFT')")] string symbols,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (symbolList.Length == 0) return "Vui lòng nhập ít nhất một mã cổ phiếu để so sánh.";

        var sb = new StringBuilder();
        sb.AppendLine("### ⚖️ BẢNG SO SÁNH SỨC MẠNH CỔ PHIẾU & RỦI RO");
        sb.AppendLine("| Mã CP | Giá hiện tại | Xu hướng | RSI (14) | Điểm An Toàn | Khuyến Nghị | Tỷ lệ R:R | Mốc Cắt Lỗ |");
        sb.AppendLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |");

        foreach (var sym in symbolList)
        {
            try
            {
                var candles = await _stockService.GetHistoricalCandlesAsync(sym, 100, cancellationToken);
                if (candles.Count < 20)
                {
                    sb.AppendLine($"| {sym.ToUpperInvariant()} | N/A | Không đủ dữ liệu | - | - | - | - | - |");
                    continue;
                }

                var ind = _engine.CalculateIndicators(candles);
                string market = sym.Trim().Length == 3 ? "VN" : "GLOBAL";
                var report = _advisor.GenerateReport(sym, market, candles, ind);

                string trendShort = report.TrendStatus.Contains("UPTREND") ? "🟢 Tăng" :
                                   report.TrendStatus.Contains("DOWNTREND") ? "🔴 Giảm" : "🟡 Đi ngang";

                sb.AppendLine($"| **{report.Symbol}** | {report.CurrentPrice:N2} | {trendShort} | {ind.Rsi14?.ToString("F1") ?? "N/A"} | **{report.SafetyScore}/10** | `{report.PrimaryAction}` | {report.RiskRewardRatio:F1}:1 | {report.StopLossPrice:N2} |");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"| {sym.ToUpperInvariant()} | Lỗi: {ex.Message} | - | - | - | - | - | - |");
            }
        }

        sb.AppendLine("\n💡 **Quy tắc chọn mã**: Ưu tiên chọn mã có **Xu hướng Tăng 🟢**, **Điểm an toàn >= 7**, **RSI từ 45-65** và **Tỷ lệ R:R >= 2:1**.");
        return sb.ToString();
    }

    // ==========================================
    // 7. [MỚI] TÍNH TOÁN KÍCH THƯỚC VỊ THẾ & PHÂN BỔ VỐN
    // ==========================================
    [McpServerTool(Name = "calculate_position_size")]
    [Description("QUẢN TRỊ RỦI RO DANH MỤC: Tính toán chính xác số lượng cổ phiếu được mua theo Quy Tắc 1% - 2% rủi ro tài khoản. Giúp nhà đầu tư KHÔNG BAO GIỜ CHÁY TÀI KHOẢN, khống chế mức lỗ tối đa trong tầm kiểm soát.")]
    public async Task<string> CalculatePositionSize(
        [Description("Mã cổ phiếu định mua (ví dụ: FPT, HPG, AAPL)")] string symbol,
        [Description("Tổng vốn đầu tư của tài khoản (ví dụ: 100000000 cho 100 triệu VND hoặc 50000 cho 50k USD)")] decimal totalCapital,
        [Description("Mức rủi ro tối đa chấp nhận mất trên tổng vốn (mặc định: 2.0%)")] decimal maxRiskPercent = 2.0m,
        [Description("Giá dự kiến mua (nếu để 0 hệ thống sẽ lấy giá thị trường mới nhất)")] decimal entryPrice = 0m,
        [Description("Mức cắt lỗ dự kiến (nếu để 0 hệ thống sẽ lấy mức cắt lỗ kỷ luật chuẩn)")] decimal stopLossPrice = 0m,
        CancellationToken cancellationToken = default)
    {
        var quote = await _stockService.GetQuoteAsync(symbol, cancellationToken);
        if (quote == null) return $"Không tìm thấy dữ liệu giá cho mã '{symbol}'.";

        if (entryPrice <= 0) entryPrice = quote.CurrentPrice;

        if (stopLossPrice <= 0)
        {
            var candles = await _stockService.GetHistoricalCandlesAsync(symbol, 60, cancellationToken);
            if (candles.Count >= 20)
            {
                var ind = _engine.CalculateIndicators(candles);
                var report = _advisor.GenerateReport(symbol, quote.Market, candles, ind);
                stopLossPrice = report.StopLossPrice;
            }
            else
            {
                stopLossPrice = Math.Round(entryPrice * 0.94m, 2);
            }
        }

        var result = _positionManager.CalculatePositionSize(
            symbol,
            quote.Market,
            totalCapital,
            maxRiskPercent,
            entryPrice,
            stopLossPrice);

        var sb = new StringBuilder();
        sb.AppendLine($"# 💼 BẢNG TÍNH TOÁN PHÂN BỔ VỐN & VỊ THẾ: **{result.Symbol}**");
        sb.AppendLine($"- **Tổng vốn tài khoản**: **{result.TotalCapital:N0} {result.Currency}**");
        sb.AppendLine($"- **Mức rủi ro tối đa cho phép**: **{result.MaxRiskPercent}%** = **{result.MaxRiskAmount:N0} {result.Currency}**");
        sb.AppendLine($"- **Giá vào lệnh (Entry)**: **{result.EntryPrice:N2}** | **Mốc cắt lỗ (Stop-Loss)**: **{result.StopLossPrice:N2}** ({-result.RiskPerSharePercent:F1}%)");

        sb.AppendLine("\n---");
        sb.AppendLine("### 🎯 KẾT QUẢ ĐI LỆNH TỐI ƯU:");
        sb.AppendLine($"- 📌 **SỐ LƯỢNG NÊN MUA**: **{result.RecommendedShares:N0} cổ phiếu** {(quote.Market == "VN" ? "(Lô chẵn 100 cp)" : "")}");
        sb.AppendLine($"- 💰 **Tổng giá trị giải ngân**: **{result.TotalPositionValue:N0} {result.Currency}**");
        sb.AppendLine($"- 📊 **Tỷ trọng trong danh mục**: **{result.PortfolioWeightPercent}%** => *{result.RiskLevel}*");
        sb.AppendLine($"- 🛡️ **Số tiền mất tối đa nếu chạm Stop-Loss**: **{(result.RiskPerShare * result.RecommendedShares):N0} {result.Currency}** (Đúng giới hạn {result.MaxRiskPercent}% tài khoản)");

        sb.AppendLine("\n---");
        sb.AppendLine($"💡 **Lời khuyên phân bổ vốn**: {result.ManagementAdvice}");
        return sb.ToString();
    }

    // ==========================================
    // 8. [MỚI] NHẬN DIỆN MÔ HÌNH NẾN ĐẢO CHIỀU
    // ==========================================
    [McpServerTool(Name = "detect_candlestick_patterns")]
    [Description("NHẬN DIỆN MÔ HÌNH NẾN NHẬT: Tự động quét và phát hiện các mẫu nến đảo chiều kinh điển (Nến Búa Hammer, Nhấn chìm Engulfing, Sao Mai Morning Star, Búa ngược Shooting Star, Doji Bia Mộ...) để nhận biết bước đi của phe mua và phe bán.")]
    public async Task<string> DetectCandlestickPatterns(
        [Description("Mã cổ phiếu cần kiểm tra mô hình nến (ví dụ: FPT, HPG, VNM, AAPL)")] string symbol,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, 30, cancellationToken);
        if (candles.Count < 5) return $"Không đủ dữ liệu nến để nhận diện mô hình cho mã '{symbol}'.";

        var patterns = _patternDetector.DetectPatterns(candles);
        var latest = candles[^1];

        var sb = new StringBuilder();
        sb.AppendLine($"### 🕯️ PHÂN TÍCH MÔ HÌNH NẾN NHẬT (PRICE ACTION): **{symbol.ToUpperInvariant()}**");
        sb.AppendLine($"*Phiên gần nhất: {latest.Date:yyyy-MM-dd} | Mở: {latest.Open:N2} | Cao: {latest.High:N2} | Thấp: {latest.Low:N2} | Đóng: {latest.Close:N2}*");

        if (patterns.Count == 0)
        {
            sb.AppendLine("\nChưa phát hiện mẫu hình nến đảo chiều đặc biệt trong 2-3 phiên gần nhất. Nến hiện tại đang vận động theo xu hướng tự nhiên.");
        }
        else
        {
            sb.AppendLine($"\n🔍 **Phát hiện {patterns.Count} mẫu hình nến đáng chú ý:**\n");
            foreach (var p in patterns)
            {
                string icon = p.SignalType == PatternSignalType.Bullish ? "🟢 [TÍN HIỆU TĂNG]" :
                              p.SignalType == PatternSignalType.Bearish ? "🔴 [CẢNH BÁO GIẢM / BẪY ĐỈNH]" : "🟡 [LƯỠNG LỰ]";

                sb.AppendLine($"#### {icon} **{p.Name}** (Độ tin cậy: {p.Reliability})");
                sb.AppendLine($"- **Mô tả hành vi**: {p.Description}");
                sb.AppendLine($"- **Hành động khuyến nghị**: *{p.ActionAdvice}*\n");
            }
        }

        return sb.ToString();
    }

    // ==========================================
    // 9. [MỚI] PHÂN TÍCH DÒNG TIỀN VSA & OBV
    // ==========================================
    [McpServerTool(Name = "analyze_volume_flow")]
    [Description("PHÂN TÍCH DÒNG TIỀN THÔNG MINH (SMART MONEY & VSA): Phân tích chỉ báo On-Balance Volume (OBV), đếm số phiên phân phối (Distribution Days) theo William O'Neil / CANSLIM, xác định cổ phiếu đang ở pha Gom hàng, Đẩy giá hay Phân phối xả hàng.")]
    public async Task<string> AnalyzeVolumeFlow(
        [Description("Mã cổ phiếu (ví dụ: FPT, HPG, SSI, NVDA)")] string symbol,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, 60, cancellationToken);
        if (candles.Count < 20) return $"Không đủ dữ liệu để phân tích dòng tiền cho mã '{symbol}'.";

        var flow = _volumeFlowAnalyzer.AnalyzeFlow(symbol, candles);

        var sb = new StringBuilder();
        sb.AppendLine($"# 🌊 PHÂN TÍCH DÒNG TIỀN & KHỐI LƯỢNG VSA: **{flow.Symbol}**");
        sb.AppendLine($"- **Khối lượng phiên cuối**: {flow.CurrentVolume:N0} cp ({flow.VolumeRatio:F2}x so với TB20)");
        sb.AppendLine($"- **Pha hành động của Smart Money**: **`{flow.SmartMoneyPhase}`**");
        sb.AppendLine($"- **Xu hướng dòng tiền ròng (OBV Trend)**: **{flow.ObvTrend}**");

        sb.AppendLine("\n---");
        sb.AppendLine("### 📊 THỐNG KÊ 20 PHIÊN GẦN NHẤT:");
        sb.AppendLine($"- 🟢 **Số phiên Tích Lũy dòng tiền vào**: **{flow.AccumulationDaysLast20} phiên**");
        sb.AppendLine($"- 🔴 **Số phiên Phân Phối xả hàng**: **{flow.DistributionDaysLast20} phiên** {(flow.DistributionDaysLast20 >= 4 ? "⚠️ (CẢNH BÁO NGUY CƠ TẠO ĐỈNH)" : "✅ (An toàn)")}");

        if (flow.FlowObservations.Count > 0)
        {
            sb.AppendLine("\n### 🔍 GHI NHẬN HÀNH VI DÒNG TIỀN:");
            foreach (var obs in flow.FlowObservations)
            {
                sb.AppendLine($"- {obs}");
            }
        }

        sb.AppendLine($"\n> 🎯 **KẾT LUẬN CUỐI CÙNG**: {flow.FinalVerdict}");
        return sb.ToString();
    }

    // ==========================================
    // 10. [MỚI] CHIẾN LƯỢC CHỐT LỜI & CHẶN LÃI THẢ NỔI
    // ==========================================
    [McpServerTool(Name = "calculate_exit_strategy")]
    [Description("CHIẾN LƯỢC CHỐT LỜI & CHẶN LÃI THẢ NỔI (TRAILING STOP): Lên kế hoạch chốt lời từng phần (nấc 1, nấc 2) và tự động tính toán điểm Trailing Stop để gồng lãi tối đa, biến vị thế thành 'Free-Trade' không thể thua lỗ.")]
    public async Task<string> CalculateExitStrategy(
        [Description("Mã cổ phiếu (ví dụ: FPT, HPG, AAPL)")] string symbol,
        [Description("Giá vốn thực tế bạn đã mua")] decimal entryPrice,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, 60, cancellationToken);
        if (candles.Count < 20) return $"Không đủ dữ liệu cho mã '{symbol}'.";

        var latest = candles[^1];
        var ind = _engine.CalculateIndicators(candles);
        decimal atr = ind.Atr14 ?? (latest.Close * 0.03m);

        var plan = _positionManager.BuildExitStrategy(symbol, entryPrice, latest.Close, atr);

        var sb = new StringBuilder();
        sb.AppendLine($"# 🏹 KẾ HOẠCH CHỐT LỜI & CHẶN LÃI THẢ NỔI: **{plan.Symbol}**");
        sb.AppendLine($"- **Giá vốn mua**: `{plan.EntryPrice:N2}` | **Giá hiện tại**: `{plan.CurrentPrice:N2}`");
        sb.AppendLine($"- **Tỷ suất sinh lời hiện tại**: **{(plan.ProfitPercent >= 0 ? "+" : "")}{plan.ProfitPercent:F2}%**");
        sb.AppendLine($"- **Trạng thái chiến lược**: **`{plan.ActionStage}`**");

        sb.AppendLine("\n---");
        sb.AppendLine("### 🎯 CÁC MỐC HÀNH ĐỘNG CỤ THỂ:");
        sb.AppendLine($"- 🟢 **Mục tiêu Chốt Lời Nấc 1 (Bán 30-40%)**: **{plan.TakeProfitStep1:N2}** (+{plan.TakeProfitStep1Percent}%)");
        sb.AppendLine($"- 🚀 **Mục tiêu Chốt Lời Nấc 2 (Bán 30% tiếp theo)**: **{plan.TakeProfitStep2:N2}** (+{plan.TakeProfitStep2Percent}%)");
        sb.AppendLine($"- 🛑 **MỐC CHẶN LÃI THẢ NỔI (TRAILING STOP)**: **{plan.TrailingStopPrice:N2}** ({(plan.TrailingStopPercent >= 0 ? "+" : "")}{plan.TrailingStopPercent}%)");

        sb.AppendLine("\n### 📋 BƯỚC THỰC HIỆN KỶ LUẬT:");
        foreach (var step in plan.ExecutionSteps)
        {
            sb.AppendLine($"- {step}");
        }

        return sb.ToString();
    }

    // ==========================================
    // 11. [MỚI] TÍNH TOÁN CÁC MỐC FIBONACCI
    // ==========================================
    [McpServerTool(Name = "calculate_fibonacci_levels")]
    [Description("TÍNH TOÁN CÁC MỐC FIBONACCI: Tự động đo đạc các mốc thoái lui Fibo Retracement (0.382, 0.5, 0.618 Tỷ lệ vàng) và các mốc mục tiêu mở rộng Fibo Extension (1.272, 1.618) cho nhịp sóng hiện tại.")]
    public async Task<string> CalculateFibonacciLevels(
        [Description("Mã cổ phiếu (ví dụ: FPT, HPG, SSI, AAPL)")] string symbol,
        [Description("Số phiên nhìn lại để tìm đỉnh đáy sóng (mặc định: 60 phiên)")] int lookback = 60,
        CancellationToken cancellationToken = default)
    {
        var candles = await _stockService.GetHistoricalCandlesAsync(symbol, lookback, cancellationToken);
        if (candles.Count < 20) return $"Không đủ dữ liệu tính Fibonacci cho mã '{symbol}'.";

        var fibo = _fibonacciCalculator.Calculate(symbol, candles, lookback);
        var latest = candles[^1];

        var sb = new StringBuilder();
        sb.AppendLine($"# 🌀 THƯỚC ĐO FIBONACCI SÓNG GIÁ: **{fibo.Symbol}**");
        sb.AppendLine($"- **Giá hiện tại**: `{latest.Close:N2}` | **Xu hướng**: **{fibo.TrendDirection}**");
        sb.AppendLine($"- **Đỉnh sóng gần nhất (Swing High)**: **{fibo.SwingHigh:N2}**");
        sb.AppendLine($"- **Đáy sóng gần nhất (Swing Low)**: **{fibo.SwingLow:N2}**");

        sb.AppendLine("\n---");
        sb.AppendLine("### 📉 CÁC MỐC HỖ TRỢ THOÁI LUI (FIBONACCI RETRACEMENT):");
        foreach (var kvp in fibo.Retracement)
        {
            bool isCurrentNear = Math.Abs(latest.Close - kvp.Value) / latest.Close <= 0.02m;
            sb.AppendLine($"- **{kvp.Key}**: **{kvp.Value:N2}** {(isCurrentNear ? "👈 (GIÁ ĐANG Ở ĐÂY)" : "")}");
        }

        sb.AppendLine("\n### 🚀 CÁC MỤC TIÊU BÙNG NỔ (FIBONACCI EXTENSION):");
        foreach (var kvp in fibo.Extensions)
        {
            sb.AppendLine($"- **{kvp.Key}**: **{kvp.Value:N2}**");
        }

        sb.AppendLine($"\n💡 **Vùng hỗ trợ then chốt (Reversal Golden Zone)**: **{fibo.KeySupportZone}**");
        sb.AppendLine($"🎯 **Vùng mục tiêu sóng lớn**: **{fibo.GoldenTargetZone}**");
        return sb.ToString();
    }

    // ==========================================
    // 12. [MỚI] BỘ SÀNG LỌC CƠ HỘI TOÀN THỊ TRƯỜNG
    // ==========================================
    [McpServerTool(Name = "scan_market_opportunities")]
    [Description("BỘ SÀNG LỌC CƠ HỘI TOÀN THỊ TRƯỜNG: Tự động quét danh sách cổ phiếu hàng đầu (mặc định VN30/Bluechips hoặc danh sách tùy biến) để tìm kiếm các mã: Vừa Breakout vol lớn, Đang siết nền Bollinger Squeeze cạn vol, hoặc Đang quá bán rút chân an toàn.")]
    public async Task<string> ScanMarketOpportunities(
        [Description("Danh sách mã cổ phiếu phân cách bằng dấu phẩy cần quét (để trống sẽ tự động quét danh sách VN30/Bluechips chọn lọc)")] string customSymbols = "",
        CancellationToken cancellationToken = default)
    {
        IEnumerable<string>? list = null;
        if (!string.IsNullOrWhiteSpace(customSymbols))
        {
            list = customSymbols.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var opportunities = await _opportunityScanner.ScanOpportunitiesAsync(list, cancellationToken);
        if (opportunities.Count == 0) return "Không tìm thấy cơ hội thỏa mãn điều kiện lọc lúc này.";

        var sb = new StringBuilder();
        sb.AppendLine("# 🏆 BẢNG XẾP HẠNG CƠ HỘI ĐẦU TƯ TỐT NHẤT THỊ TRƯỜNG");
        sb.AppendLine("| Xếp Hạng | Mã CP | Giá hiện tại | Mẫu Hình Kỹ Thuật | Điểm An Toàn | RSI | Khối Lượng | Khuyến Nghị |");
        sb.AppendLine("| :--- | :---: | :---: | :--- | :---: | :---: | :---: | :--- |");

        foreach (var opp in opportunities)
        {
            sb.AppendLine($"| **{opp.Tier}** | **{opp.Symbol}** | {opp.CurrentPrice:N2} | {opp.SetupPattern} | **{opp.SafetyScore}/10** | {opp.Rsi14:F1} | {opp.VolumeRatio:F1}x TB20 | `{opp.Recommendation}` |");
        }

        sb.AppendLine("\n---");
        sb.AppendLine("💡 **HƯỚNG DẪN GIẢI NGÂN HIỆU QUẢ CAO**:");
        sb.AppendLine("1. **Ưu tiên TIER A (Cơ hội Vàng)**: Đây là các mã hội tụ đủ điều kiện: Dòng tiền bùng nổ hoặc tích lũy siết nền cạn vol, điểm an toàn cao.");
        sb.AppendLine("2. Luôn kết hợp gọi tool `calculate_position_size` để tính số lượng cổ phiếu được mua tương ứng với mức vốn của bạn.");
        sb.AppendLine("3. Tuyệt đối tuân thủ mốc cắt lỗ kỷ luật để bảo vệ thành quả.");

        return sb.ToString();
    }
}
