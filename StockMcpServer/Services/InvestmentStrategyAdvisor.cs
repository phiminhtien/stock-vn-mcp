using System;
using System.Collections.Generic;
using System.Linq;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class InvestmentStrategyAdvisor
{
    public TradingSignalReport GenerateReport(string symbol, string market, List<StockCandle> candles, TechnicalIndicators indicators)
    {
        var latest = candles[^1];
        var prev = candles.Count > 1 ? candles[^2] : latest;
        decimal price = latest.Close;
        decimal priceChangePercent = prev.Close > 0 ? Math.Round(((price - prev.Close) / prev.Close) * 100m, 2) : 0m;

        var report = new TradingSignalReport
        {
            Symbol = symbol.ToUpperInvariant(),
            Market = market,
            CurrentPrice = price,
            PriceChangePercent = priceChangePercent,
            Currency = market == "VN" ? "VND (nghìn đồng)" : "USD",
            Indicators = indicators
        };

        // 1. Xác định Xu Hướng Thị Trường (Trend Analysis)
        DetermineTrend(report, price, indicators);

        // 2. Kiểm tra các Bẫy Rủi Ro ("Tránh mất tiền oan uổng")
        DetectRiskTraps(report, candles, indicators);

        // 3. Tính toán Vùng Mua, Stop-loss và Chốt Lời chuẩn R:R
        CalculatePriceZonesAndRR(report, price, indicators);

        // 4. Quyết định Hành Động & Điểm An Toàn Vốn
        DetermineActionAndSafetyScore(report, indicators);

        // 5. Tổng hợp Lời Khuyên Quản Trị Vốn Vàng
        AssembleGoldenRules(report);

        return report;
    }

    private void DetermineTrend(TradingSignalReport report, decimal price, TechnicalIndicators ind)
    {
        bool aboveEma20 = ind.Ema20.HasValue && price >= ind.Ema20.Value;
        bool aboveEma50 = ind.Ema50.HasValue && price >= ind.Ema50.Value;
        bool aboveEma200 = ind.Ema200.HasValue && price >= ind.Ema200.Value;

        bool emaAlignmentBull = ind.Ema20.HasValue && ind.Ema50.HasValue && ind.Ema20.Value >= ind.Ema50.Value;
        bool emaAlignmentBear = ind.Ema20.HasValue && ind.Ema50.HasValue && ind.Ema20.Value < ind.Ema50.Value;

        if (aboveEma20 && aboveEma50 && emaAlignmentBull && (ind.Ema200 == null || aboveEma200))
        {
            report.TrendStatus = "UPTREND MẠNH (Xu hướng tăng đồng thuận trên các đường MA)";
            report.PositiveDrivers.Add("Giá nằm trên toàn bộ hệ thống đường trung bình ngắn và dài hạn (EMA20, EMA50, EMA200).");
        }
        else if (aboveEma20 && emaAlignmentBull)
        {
            report.TrendStatus = "UPTREND NGẮN HẠN (Đang trong nhịp tăng nhưng cần theo dõi cản trung hạn)";
            report.PositiveDrivers.Add("Giá giữ vững trên EMA20 ngắn hạn, cấu trúc đỉnh đáy nâng dần.");
        }
        else if (!aboveEma20 && !aboveEma50 && emaAlignmentBear && (ind.Ema200.HasValue && !aboveEma200))
        {
            report.TrendStatus = "DOWNTREND MẠNH (Rơi tự do - CỰC KỲ NGUY HIỂM)";
            report.DangerousTraps.Add("CẢNH BÁO: Cổ phiếu nằm dưới tất cả các đường MA lớn. Mọi nhịp hồi đều có thể là bẫy Bull-trap.");
        }
        else if (!aboveEma20 && emaAlignmentBear)
        {
            report.TrendStatus = "DOWNTREND (Đang trong kênh điều chỉnh giảm)";
            report.DangerousTraps.Add("Giá gãy đường EMA20, xu hướng ngắn hạn nghiêng về phe bán.");
        }
        else
        {
            report.TrendStatus = "TÍCH LŨY (SIDEWAY - Thị trường đang giằng co tìm điểm cân bằng)";
        }
    }

    private void DetectRiskTraps(TradingSignalReport report, List<StockCandle> candles, TechnicalIndicators ind)
    {
        var latest = candles[^1];

        // Trap 1: Quá mua cực độ (FOMO trap)
        if (ind.Rsi14.HasValue && ind.Rsi14.Value >= 75)
        {
            report.DangerousTraps.Add($"BẪY FOMO ĐỈNH: RSI đạt {ind.Rsi14.Value:F1} (Quá mua cực độ). Rủi ro đảo chiều giảm sâu lên tới 75%. KHÔNG ĐƯỢC ĐUA LỆNH GIÁ CAO.");
        }

        // Trap 2: Giá xa nền EMA20 (> 8-10%)
        if (ind.Ema20.HasValue && latest.Close > ind.Ema20.Value * 1.08m)
        {
            decimal stretch = Math.Round(((latest.Close - ind.Ema20.Value) / ind.Ema20.Value) * 100m, 1);
            report.DangerousTraps.Add($"GIÁ KÉO RƯỚN: Giá hiện tại cách xa EMA20 đến +{stretch}%. Xu hướng sẽ sớm hút ngược về đường trung bình.");
        }

        // Trap 3: Bán xả khối lượng lớn (Distribution Day)
        if (candles.Count >= 2)
        {
            var prev = candles[^2];
            bool isBigRed = latest.Close < latest.Open && (latest.Open - latest.Close) / latest.Open > 0.02m;
            bool isHighVol = ind.AverageVolume20 > 0 && latest.Volume >= (long)(ind.AverageVolume20 * 1.5m);

            if (isBigRed && isHighVol)
            {
                report.DangerousTraps.Add("TÍN HIỆU PHÂN PHỐI: Nến đỏ thân dài kèm khối lượng lớn gấp >1.5 lần trung bình. Phe tổ chức / Smart money có dấu hiệu thoát hàng.");
            }
        }

        // Trap 4: Chạm cản trên kháng cự mạnh
        if (ind.ResistanceLevel > 0 && latest.Close >= ind.ResistanceLevel * 0.985m)
        {
            report.DangerousTraps.Add($"VÙNG KHÁNG CỰ MẠNH ({ind.ResistanceLevel}): Giá đang tiếp cận đỉnh cũ, áp lực bán chốt lời tại đây sẽ rất lớn.");
        }

        // Positive 1: Bollinger Squeeze (Nén chặt sắp bùng nổ)
        if (ind.IsBollingerSqueeze)
        {
            report.PositiveDrivers.Add("DẢI BOLLINGER CO THẮT (Squeeze): Biến động giá nén rất chặt, sắp có một nhịp bung nổ xu hướng mạnh mẽ.");
        }

        // Positive 2: Chạm hỗ trợ bật lên (Test Support)
        if (ind.SupportLevel > 0 && Math.Abs(latest.Low - ind.SupportLevel) / latest.Low <= 0.015m && latest.Close > latest.Open)
        {
            report.PositiveDrivers.Add($"GIỮ VỮNG HỖ TRỢ ({ind.SupportLevel}): Lực cầu bắt đáy hấp thụ tốt ngay tại vùng hỗ trợ cứng.");
        }
    }

    private void CalculatePriceZonesAndRR(TradingSignalReport report, decimal price, TechnicalIndicators ind)
    {
        decimal atr = ind.Atr14 ?? (price * 0.03m);

        // Vùng mua an toàn: Quanh EMA20 hoặc ngay trên Hỗ Trợ
        if (ind.Ema20.HasValue && price >= ind.Ema20.Value)
        {
            report.SafeBuyMin = Math.Round(Math.Max(ind.SupportLevel, ind.Ema20.Value * 0.99m), 2);
            report.SafeBuyMax = Math.Round(ind.Ema20.Value * 1.015m, 2);
        }
        else
        {
            report.SafeBuyMin = Math.Round(ind.SupportLevel, 2);
            report.SafeBuyMax = Math.Round(ind.SupportLevel + atr * 0.5m, 2);
        }

        // Stop-loss kỷ luật: Dưới vùng hỗ trợ cứng hoặc giá hiện tại trừ 1.5 ATR
        decimal rawStopLoss = Math.Min(price - atr * 1.5m, ind.SupportLevel * 0.98m);
        // Giới hạn stop loss không xa quá 7% để bảo vệ tài khoản
        decimal maxStopLoss = price * 0.93m;
        report.StopLossPrice = Math.Round(Math.Max(rawStopLoss, maxStopLoss), 2);
        report.StopLossPercent = price > 0 ? Math.Round(((report.StopLossPrice - price) / price) * 100m, 2) : 0m;

        // Take Profit: Mục tiêu 1 (Kháng cự gần nhất hoặc R:R 1.5)
        decimal riskPerShare = price - report.StopLossPrice;
        if (riskPerShare <= 0) riskPerShare = price * 0.05m;

        report.TakeProfit1 = Math.Round(Math.Max(ind.ResistanceLevel, price + riskPerShare * 1.5m), 2);
        report.TakeProfit1Percent = price > 0 ? Math.Round(((report.TakeProfit1 - price) / price) * 100m, 2) : 0m;

        // Take Profit 2 (R:R >= 2.5 - 3.0)
        report.TakeProfit2 = Math.Round(price + riskPerShare * 2.5m, 2);
        report.TakeProfit2Percent = price > 0 ? Math.Round(((report.TakeProfit2 - price) / price) * 100m, 2) : 0m;

        // Tính tỷ lệ Risk / Reward
        decimal potentialProfit = report.TakeProfit1 - price;
        decimal potentialRisk = price - report.StopLossPrice;
        report.RiskRewardRatio = potentialRisk > 0 ? Math.Round(potentialProfit / potentialRisk, 2) : 0m;
    }

    private void DetermineActionAndSafetyScore(TradingSignalReport report, TechnicalIndicators ind)
    {
        int score = 5; // Base

        if (report.TrendStatus.Contains("UPTREND MẠNH")) score += 3;
        else if (report.TrendStatus.Contains("UPTREND")) score += 1;
        else if (report.TrendStatus.Contains("DOWNTREND MẠNH")) score -= 4;
        else if (report.TrendStatus.Contains("DOWNTREND")) score -= 2;

        if (ind.Rsi14.HasValue)
        {
            if (ind.Rsi14.Value >= 45 && ind.Rsi14.Value <= 65) score += 1;
            else if (ind.Rsi14.Value > 70) score -= 2; // Rủi ro đu đỉnh
            else if (ind.Rsi14.Value < 30) score += 1; // Vùng oversold tích lũy
        }

        if (report.DangerousTraps.Count > 0) score -= report.DangerousTraps.Count;
        if (report.PositiveDrivers.Count > 0) score += report.PositiveDrivers.Count;

        score = Math.Clamp(score, 1, 10);
        report.SafetyScore = score;

        // Quyết định hành động chính
        if (report.TrendStatus.Contains("DOWNTREND MẠNH"))
        {
            report.PrimaryAction = "TUYỆT ĐỐI ĐỨNG NGOÀI (CẤM BẮT ĐÁY DAO RƠI)";
            report.ExecutiveSummary = "Cổ phiếu đang trong chu kỳ rơi tự do. Xác suất thua lỗ cực cao nếu bắt dao rơi. Hãy chờ cổ phiếu tạo mô hình 2 đáy hoặc vượt EMA20.";
        }
        else if (ind.Rsi14.HasValue && ind.Rsi14.Value >= 75)
        {
            report.PrimaryAction = "HẠ TỶ TRỌNG / CHỐT LỜI TỪNG PHẦN (CẤM MUA MỚI)";
            report.ExecutiveSummary = "Cổ phiếu rơi vào vùng hưng phấn cực độ (Quá Mua). Nhà đầu tư thông minh nên hiện thực hóa lợi nhuận, tuyệt đối không mua đuổi.";
        }
        else if (report.CurrentPrice <= report.StopLossPrice)
        {
            report.PrimaryAction = "BÁN CẮT LỖ KỶ LUẬT (QUẢN TRỊ RỦI RO NGAY)";
            report.ExecutiveSummary = "Giá đã xuyên thủng ngưỡng hỗ trợ và chạm mốc cắt lỗ. Phải hạ tỷ trọng ngay để bảo vệ nguồn vốn còn lại, tránh khoản lỗ phình to.";
        }
        else if (report.SafetyScore >= 7 && report.RiskRewardRatio >= 1.5m)
        {
            if (report.CurrentPrice <= report.SafeBuyMax && report.CurrentPrice >= report.SafeBuyMin)
            {
                report.PrimaryAction = "MUA ĐẸP TẠI VÙNG GIÁ AN TOÀN (BUY ON DIP)";
                report.ExecutiveSummary = $"Điểm vào lệnh rất đẹp: Nằm ngay trong vùng giá an toàn ({report.SafeBuyMin} - {report.SafeBuyMax}) với tỷ lệ R:R đạt {report.RiskRewardRatio}:1.";
            }
            else
            {
                report.PrimaryAction = "CHỜ CHỈNH VỀ VÙNG MUA AN TOÀN (CANH MUA)";
                report.ExecutiveSummary = $"Xu hướng tốt nhưng giá đang cao hơn vùng mua an toàn. Hãy kiên nhẫn chờ nhịp rung lắc về quanh {report.SafeBuyMin} - {report.SafeBuyMax} để giải ngân.";
            }
        }
        else if (report.TrendStatus.Contains("UPTREND") && report.CurrentPrice > report.SafeBuyMax)
        {
            report.PrimaryAction = "TIẾP TỤC NẮM GIỮ (CHẶN LÃI THẢ NỔI)";
            report.ExecutiveSummary = "Vị thế đang có lãi nên tiếp tục gồng lãi, nâng điểm chặn lỗ (trailing stop) lên đường EMA20 để bảo toàn thành quả.";
        }
        else
        {
            report.PrimaryAction = "QUAN SÁT THẬN TRỌNG (CHỜ XÁC NHẬN DÒNG TIỀN)";
            report.ExecutiveSummary = "Chưa có tín hiệu vào lệnh đủ an toàn. Cần quan sát thêm sự phản ứng của dòng tiền tại các vùng hỗ trợ.";
        }
    }

    private void AssembleGoldenRules(TradingSignalReport report)
    {
        report.GoldenRules.Add("QUY TẮC SỐ 1: Bảo toàn vốn là ưu tiên số 1. Luôn đặt mức cắt lỗ tự động tối đa -5% đến -7%, không bao giờ trung bình giá xuống khi cổ phiếu đang rơi.");
        report.GoldenRules.Add($"QUY TẮC SỐ 2: Tỷ lệ Lợi nhuận / Rủi ro (R:R) phải từ 1.5:1 đến 2:1 trở lên (Hiện tại: {report.RiskRewardRatio}:1). Nếu R:R < 1.5 thì KHÔNG VÀO LỆNH.");
        report.GoldenRules.Add("QUY TẮC SỐ 3: Chia vốn đi lệnh làm 2-3 phần (30% thăm dò nền giá, 50% khi bùng nổ vượt cản có vol, 20% khi test lại thành công).");
        report.GoldenRules.Add("QUY TẮC SỐ 4: Tuyệt đối không FOMO mua xanh tím khi cổ phiếu đã tăng 3-4 phiên liên tiếp hoặc RSI > 70.");
    }
}
