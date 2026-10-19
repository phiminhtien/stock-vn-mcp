using System;
using System.Collections.Generic;
using System.Linq;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class FibonacciCalculator
{
    public FibonacciLevels Calculate(string symbol, List<StockCandle> candles, int lookback = 60)
    {
        var result = new FibonacciLevels
        {
            Symbol = symbol.ToUpperInvariant()
        };

        if (candles == null || candles.Count < 20) return result;

        int count = Math.Min(candles.Count, lookback);
        var slice = candles.TakeLast(count).ToList();

        decimal swingHigh = slice.Max(c => c.High);
        decimal swingLow = slice.Min(c => c.Low);
        decimal diff = swingHigh - swingLow;

        result.SwingHigh = swingHigh;
        result.SwingLow = swingLow;

        var latest = slice.Last();
        // Kiểm tra xu hướng ngắn hạn
        bool isUptrend = latest.Close >= (swingHigh + swingLow) / 2m;
        result.TrendDirection = isUptrend ? "UPTREND (Hồi phục / Tăng trưởng)" : "DOWNTREND (Điều chỉnh / Giảm)";

        if (diff > 0)
        {
            // Fibo Retracements: 0.236, 0.382, 0.500, 0.618 (Tỷ lệ vàng), 0.786
            result.Retracement["Fibo 0.236 (Hỗ trợ nông)"] = Math.Round(swingHigh - diff * 0.236m, 2);
            result.Retracement["Fibo 0.382 (Hỗ trợ tích cực)"] = Math.Round(swingHigh - diff * 0.382m, 2);
            result.Retracement["Fibo 0.500 (Điểm cân bằng)"] = Math.Round(swingHigh - diff * 0.500m, 2);
            result.Retracement["Fibo 0.618 (TỶ LỆ VÀNG REVERSAL)"] = Math.Round(swingHigh - diff * 0.618m, 2);
            result.Retracement["Fibo 0.786 (Hỗ trợ cuối cùng)"] = Math.Round(swingHigh - diff * 0.786m, 2);

            // Fibo Extensions: 1.272, 1.618, 2.000, 2.618
            result.Extensions["Fibo 1.272 (Target ngắn)"] = Math.Round(swingLow + diff * 1.272m, 2);
            result.Extensions["Fibo 1.618 (TARGET VÀNG BÙNG NỔ)"] = Math.Round(swingLow + diff * 1.618m, 2);
            result.Extensions["Fibo 2.618 (Target sóng lớn)"] = Math.Round(swingLow + diff * 2.618m, 2);

            result.KeySupportZone = $"{result.Retracement["Fibo 0.618 (TỶ LỆ VÀNG REVERSAL)"]} - {result.Retracement["Fibo 0.500 (Điểm cân bằng)"]}";
            result.GoldenTargetZone = $"{result.Extensions["Fibo 1.272 (Target ngắn)"]} - {result.Extensions["Fibo 1.618 (TARGET VÀNG BÙNG NỔ)"]}";
        }

        return result;
    }
}
