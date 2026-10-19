using System;
using System.Collections.Generic;

namespace StockMcpServer.Models;

public class TradingSignalReport
{
    public string Symbol { get; set; } = string.Empty;
    public string Market { get; set; } = "VN";
    public decimal CurrentPrice { get; set; }
    public decimal PriceChangePercent { get; set; }
    public string Currency { get; set; } = "VND";
    
    // Trend & General status
    public string TrendStatus { get; set; } = string.Empty; // UPTREND MẠNH, UPTREND, TÍCH LŨY (SIDEWAY), DOWNTREND, RƠI TỰ DO
    public string PrimaryAction { get; set; } = string.Empty; // MUA MẠNH, MUA THĂM DÒ, NẮM GIỮ, HẠ TỶ TRỌNG, BÁN CHỐT LỜI, CẮT LỖ KHẨN CẤP, KHÔNG THAM GIA
    public int SafetyScore { get; set; } // Điểm an toàn vốn từ 1 -> 10 (10 = Cực kỳ an toàn, 1 = Rủi ro mất vốn cực cao)
    
    // Trade Setup & Price Zones
    public decimal SafeBuyMin { get; set; }
    public decimal SafeBuyMax { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal StopLossPercent { get; set; }
    public decimal TakeProfit1 { get; set; }
    public decimal TakeProfit1Percent { get; set; }
    public decimal TakeProfit2 { get; set; }
    public decimal TakeProfit2Percent { get; set; }
    public decimal RiskRewardRatio { get; set; }
    
    // Technical Breakdown
    public TechnicalIndicators Indicators { get; set; } = new();
    
    // Safety & Capital Protection ("Tránh mất tiền oan uổng")
    public List<string> PositiveDrivers { get; set; } = new();
    public List<string> DangerousTraps { get; set; } = new();
    public List<string> GoldenRules { get; set; } = new();
    public string ExecutiveSummary { get; set; } = string.Empty;
}
