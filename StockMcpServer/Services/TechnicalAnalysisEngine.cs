using System;
using System.Collections.Generic;
using System.Linq;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class TechnicalAnalysisEngine
{
    public TechnicalIndicators CalculateIndicators(List<StockCandle> candles)
    {
        var result = new TechnicalIndicators();
        if (candles == null || candles.Count < 20)
        {
            return result;
        }

        var closes = candles.Select(c => c.Close).ToList();
        var volumes = candles.Select(c => c.Volume).ToList();

        // 1. Moving Averages
        result.Sma20 = CalculateSma(closes, 20);
        result.Sma50 = CalculateSma(closes, 50);
        result.Sma200 = CalculateSma(closes, 200);

        result.Ema20 = CalculateEma(closes, 20);
        result.Ema50 = CalculateEma(closes, 50);
        result.Ema200 = CalculateEma(closes, 200);

        // 2. RSI (14)
        result.Rsi14 = CalculateRsi(closes, 14);
        if (result.Rsi14.HasValue)
        {
            decimal rsi = result.Rsi14.Value;
            if (rsi >= 70)
            {
                result.RsiSignal = $"QUÁ MUA ({rsi:F1}) - Cảnh báo điều chỉnh hoặc rung lắc chốt lời";
            }
            else if (rsi <= 30)
            {
                result.RsiSignal = $"QUÁ BÁN ({rsi:F1}) - Vùng giá quá độ, có thể sắp có nhịp hồi kỹ thuật";
            }
            else if (rsi >= 50)
            {
                result.RsiSignal = $"TÍCH CỰC ({rsi:F1}) - Phe mua đang chiếm ưu thế trên mức cân bằng 50";
            }
            else
            {
                result.RsiSignal = $"TIÊU CỰC ({rsi:F1}) - Phe bán chiếm ưu thế dưới mức cân bằng 50";
            }
        }

        // 3. MACD (12, 26, 9)
        var (macd, signal, hist) = CalculateMacd(closes);
        result.MacdLine = macd;
        result.MacdSignalLine = signal;
        result.MacdHistogram = hist;
        if (macd.HasValue && signal.HasValue && hist.HasValue)
        {
            if (macd.Value > signal.Value && hist.Value > 0)
            {
                result.MacdSignal = "TÍCH CỰC - Đường MACD cắt trên Signal, động lượng giá tăng";
            }
            else if (macd.Value < signal.Value && hist.Value < 0)
            {
                result.MacdSignal = "TIÊU CỰC - Đường MACD cắt dưới Signal, xung lực bán gia tăng";
            }
            else
            {
                result.MacdSignal = "ĐANG THU HẸP - Xung lực phân hóa, chờ tín hiệu cắt rõ ràng";
            }
        }

        // 4. Bollinger Bands (20, 2)
        var (upper, middle, lower, bandwidth, isSqueeze) = CalculateBollingerBands(closes, 20, 2m);
        result.BollingerUpper = upper;
        result.BollingerMiddle = middle;
        result.BollingerLower = lower;
        result.BollingerBandwidth = bandwidth;
        result.IsBollingerSqueeze = isSqueeze;

        // 5. ATR (14)
        result.Atr14 = CalculateAtr(candles, 14);

        // 6. Support & Resistance (Dựa trên 20-30 phiên gần nhất)
        int lookback = Math.Min(candles.Count, 30);
        var recentCandles = candles.TakeLast(lookback).ToList();
        result.SupportLevel = recentCandles.Min(c => c.Low);
        result.ResistanceLevel = recentCandles.Max(c => c.High);

        // 7. Volume Analysis
        int volLookback = Math.Min(volumes.Count, 20);
        var recentVols = volumes.TakeLast(volLookback).ToList();
        result.AverageVolume20 = (decimal)recentVols.Average();
        var latestVol = volumes.Last();

        if (result.AverageVolume20 > 0)
        {
            result.VolumeRatio = Math.Round((decimal)latestVol / result.AverageVolume20, 2);
            if (result.VolumeRatio >= 1.5m)
            {
                result.VolumeSignal = $"ĐỘT BIẾN (+{(result.VolumeRatio - 1m) * 100:F0}% so với TB20) - Dòng tiền lớn hoạt động mạnh";
            }
            else if (result.VolumeRatio <= 0.6m)
            {
                result.VolumeSignal = "CẠN KIỆT (Thanh khoản thấp, thị trường thận trọng hoặc cạn cung)";
            }
            else
            {
                result.VolumeSignal = "ỔN ĐỊNH (Gần mức trung bình 20 phiên)";
            }
        }

        return result;
    }

    public decimal? CalculateSma(List<decimal> values, int period)
    {
        if (values == null || values.Count < period) return null;
        var slice = values.TakeLast(period);
        return Math.Round(slice.Average(), 2);
    }

    public decimal? CalculateEma(List<decimal> values, int period)
    {
        if (values == null || values.Count < period) return null;

        decimal multiplier = 2m / (period + 1m);
        // Start EMA with SMA of first period elements
        decimal currentEma = values.Take(period).Average();

        for (int i = period; i < values.Count; i++)
        {
            currentEma = (values[i] - currentEma) * multiplier + currentEma;
        }

        return Math.Round(currentEma, 2);
    }

    public decimal? CalculateRsi(List<decimal> values, int period = 14)
    {
        if (values == null || values.Count <= period) return null;

        decimal totalGain = 0m;
        decimal totalLoss = 0m;

        for (int i = 1; i <= period; i++)
        {
            decimal diff = values[i] - values[i - 1];
            if (diff >= 0) totalGain += diff;
            else totalLoss += Math.Abs(diff);
        }

        decimal avgGain = totalGain / period;
        decimal avgLoss = totalLoss / period;

        for (int i = period + 1; i < values.Count; i++)
        {
            decimal diff = values[i] - values[i - 1];
            decimal gain = diff >= 0 ? diff : 0m;
            decimal loss = diff < 0 ? Math.Abs(diff) : 0m;

            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
        }

        if (avgLoss == 0m) return 100m;
        decimal rs = avgGain / avgLoss;
        decimal rsi = 100m - (100m / (1m + rs));

        return Math.Round(rsi, 2);
    }

    public (decimal? macd, decimal? signal, decimal? histogram) CalculateMacd(List<decimal> values, int fast = 12, int slow = 26, int signalPeriod = 9)
    {
        if (values == null || values.Count < slow + signalPeriod)
            return (null, null, null);

        var macdSeries = new List<decimal>();
        decimal fastMult = 2m / (fast + 1m);
        decimal slowMult = 2m / (slow + 1m);

        decimal fastEma = values.Take(fast).Average();
        decimal slowEma = values.Take(slow).Average();

        // Bring fastEma to slow index
        for (int i = fast; i < slow; i++)
        {
            fastEma = (values[i] - fastEma) * fastMult + fastEma;
        }

        macdSeries.Add(fastEma - slowEma);

        for (int i = slow; i < values.Count; i++)
        {
            fastEma = (values[i] - fastEma) * fastMult + fastEma;
            slowEma = (values[i] - slowEma) * slowMult + slowEma;
            macdSeries.Add(fastEma - slowEma);
        }

        if (macdSeries.Count < signalPeriod)
            return (null, null, null);

        decimal signalEma = macdSeries.Take(signalPeriod).Average();
        decimal signalMult = 2m / (signalPeriod + 1m);

        for (int i = signalPeriod; i < macdSeries.Count; i++)
        {
            signalEma = (macdSeries[i] - signalEma) * signalMult + signalEma;
        }

        decimal lastMacd = macdSeries.Last();
        decimal histogram = lastMacd - signalEma;

        return (Math.Round(lastMacd, 2), Math.Round(signalEma, 2), Math.Round(histogram, 2));
    }

    public (decimal? upper, decimal? middle, decimal? lower, decimal? bandwidth, bool isSqueeze) CalculateBollingerBands(List<decimal> values, int period = 20, decimal multiplier = 2m)
    {
        if (values == null || values.Count < period)
            return (null, null, null, null, false);

        var slice = values.TakeLast(period).ToList();
        decimal avg = slice.Average();

        decimal sumSquares = slice.Sum(v => (v - avg) * (v - avg));
        decimal stdDev = (decimal)Math.Sqrt((double)(sumSquares / period));

        decimal upper = Math.Round(avg + multiplier * stdDev, 2);
        decimal lower = Math.Round(avg - multiplier * stdDev, 2);
        decimal middle = Math.Round(avg, 2);

        decimal bandwidth = middle > 0 ? Math.Round((upper - lower) / middle, 4) : 0m;
        bool isSqueeze = bandwidth < 0.08m; // Bandwidth co hẹp dưới 8%

        return (upper, middle, lower, bandwidth, isSqueeze);
    }

    public decimal? CalculateAtr(List<StockCandle> candles, int period = 14)
    {
        if (candles == null || candles.Count <= period) return null;

        var trs = new List<decimal>();
        for (int i = 1; i < candles.Count; i++)
        {
            decimal hl = candles[i].High - candles[i].Low;
            decimal hpc = Math.Abs(candles[i].High - candles[i - 1].Close);
            decimal lpc = Math.Abs(candles[i].Low - candles[i - 1].Close);
            decimal tr = Math.Max(hl, Math.Max(hpc, lpc));
            trs.Add(tr);
        }

        if (trs.Count < period) return null;

        decimal currentAtr = trs.Take(period).Average();
        for (int i = period; i < trs.Count; i++)
        {
            currentAtr = (currentAtr * (period - 1) + trs[i]) / period;
        }

        return Math.Round(currentAtr, 2);
    }
}
