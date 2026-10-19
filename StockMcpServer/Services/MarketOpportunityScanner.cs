using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class ScannedOpportunity
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal ChangePercent { get; set; }
    public string Tier { get; set; } = "THEO DÕI"; // TIER A, TIER B, THEO DÕI
    public string SetupPattern { get; set; } = string.Empty; // Breakout Vol lớn, Siết nền Squeeze, Test hỗ trợ cạn vol
    public int SafetyScore { get; set; }
    public decimal Rsi14 { get; set; }
    public decimal VolumeRatio { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class MarketOpportunityScanner
{
    private readonly UnifiedStockService _stockService;
    private readonly TechnicalAnalysisEngine _engine;
    private readonly InvestmentStrategyAdvisor _advisor;

    // Danh sách mặc định các mã cổ phiếu trụ / thanh khoản cao trên sàn chứng khoán Việt Nam
    public static readonly string[] DefaultVnWatchlist = new[]
    {
        "FPT", "HPG", "VNM", "SSI", "TCB", "MWG", "VHM", "MBB", "STB", "VIC", "DGC", "PVD", "REE", "KDH"
    };

    public MarketOpportunityScanner(
        UnifiedStockService stockService,
        TechnicalAnalysisEngine engine,
        InvestmentStrategyAdvisor advisor)
    {
        _stockService = stockService;
        _engine = engine;
        _advisor = advisor;
    }

    public async Task<List<ScannedOpportunity>> ScanOpportunitiesAsync(
        IEnumerable<string>? customSymbols = null,
        CancellationToken cancellationToken = default)
    {
        var symbolsToScan = customSymbols?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        if (symbolsToScan == null || symbolsToScan.Count == 0)
        {
            symbolsToScan = DefaultVnWatchlist.ToList();
        }

        var results = new List<ScannedOpportunity>();

        foreach (var sym in symbolsToScan)
        {
            try
            {
                var candles = await _stockService.GetHistoricalCandlesAsync(sym, 80, cancellationToken);
                if (candles.Count < 20) continue;

                var ind = _engine.CalculateIndicators(candles);
                string market = sym.Trim().Length == 3 ? "VN" : "GLOBAL";
                var report = _advisor.GenerateReport(sym, market, candles, ind);
                var latest = candles[^1];

                string pattern = "Vận động bình thường";
                string tier = "THEO DÕI";

                bool isBreakout = ind.VolumeRatio >= 1.5m && latest.Close > latest.Open && ind.Ema20.HasValue && latest.Close > ind.Ema20.Value;
                bool isSqueeze = ind.IsBollingerSqueeze && ind.VolumeRatio <= 0.85m;
                bool isOversoldBounce = ind.Rsi14.HasValue && ind.Rsi14.Value <= 35 && latest.Close >= latest.Open;
                bool isHealthyTrend = report.TrendStatus.Contains("UPTREND") && ind.Rsi14 >= 50 && ind.Rsi14 <= 65;

                if (isBreakout && report.SafetyScore >= 7)
                {
                    pattern = "🔥 BÙNG NỔ VƯỢT CẢN (Breakout Vol lớn)";
                    tier = "TIER A (Cơ hội Vàng)";
                }
                else if (isSqueeze && report.SafetyScore >= 6)
                {
                    pattern = "💎 SIẾT NỀN CO THẮT (Squeeze cạn vol - Chuẩn bị bùng nổ)";
                    tier = "TIER A (Cơ hội Vàng)";
                }
                else if (isHealthyTrend)
                {
                    pattern = "🟢 UPTREND ĐỀU ĐẶN (Canh mua rung lắc quanh MA20)";
                    tier = "TIER B (Cơ hội Tốt)";
                }
                else if (isOversoldBounce)
                {
                    pattern = "⚡ QUÁ BÁN RÚT CHÂN (Bắt nhịp hồi phục kỹ thuật)";
                    tier = "TIER B (Cơ hội Tốt)";
                }

                results.Add(new ScannedOpportunity
                {
                    Symbol = sym.ToUpperInvariant(),
                    CurrentPrice = report.CurrentPrice,
                    ChangePercent = report.PriceChangePercent,
                    Tier = tier,
                    SetupPattern = pattern,
                    SafetyScore = report.SafetyScore,
                    Rsi14 = ind.Rsi14 ?? 50m,
                    VolumeRatio = ind.VolumeRatio,
                    Recommendation = report.PrimaryAction
                });
            }
            catch
            {
                // Bỏ qua mã lỗi để scan liên tục không gián đoạn
            }
        }

        // Sắp xếp: Ưu tiên TIER A -> TIER B -> Điểm an toàn cao nhất
        return results
            .OrderByDescending(r => r.Tier.StartsWith("TIER A") ? 2 : r.Tier.StartsWith("TIER B") ? 1 : 0)
            .ThenByDescending(r => r.SafetyScore)
            .ToList();
    }
}
