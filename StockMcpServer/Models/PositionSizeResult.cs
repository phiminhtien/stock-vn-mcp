using System;

namespace StockMcpServer.Models;

public class PositionSizeResult
{
    public string Symbol { get; set; } = string.Empty;
    public decimal TotalCapital { get; set; }
    public decimal MaxRiskPercent { get; set; }
    public decimal MaxRiskAmount { get; set; }
    
    public decimal EntryPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal RiskPerShare { get; set; }
    public decimal RiskPerSharePercent { get; set; }
    
    public long RecommendedShares { get; set; }
    public decimal TotalPositionValue { get; set; }
    public decimal PortfolioWeightPercent { get; set; }
    
    public string Currency { get; set; } = "VND";
    public string RiskLevel { get; set; } = "Hợp lý";
    public string ManagementAdvice { get; set; } = string.Empty;
}
