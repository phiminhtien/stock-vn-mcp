using System.Collections.Generic;

namespace StockMcpServer.Models;

public class FibonacciLevels
{
    public string Symbol { get; set; } = string.Empty;
    public decimal SwingHigh { get; set; }
    public decimal SwingLow { get; set; }
    public string TrendDirection { get; set; } = "UPTREND";
    
    // Retracement levels: key = Ratio (e.g. 0.382), value = Price
    public Dictionary<string, decimal> Retracement { get; set; } = new();
    
    // Extension levels: key = Ratio (e.g. 1.618), value = Price
    public Dictionary<string, decimal> Extensions { get; set; } = new();
    
    public string KeySupportZone { get; set; } = string.Empty;
    public string GoldenTargetZone { get; set; } = string.Empty;
}

public class ExitStrategyPlan
{
    public string Symbol { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ProfitPercent { get; set; }
    
    public decimal TakeProfitStep1 { get; set; } // Chốt 30-40%
    public decimal TakeProfitStep1Percent { get; set; }
    
    public decimal TakeProfitStep2 { get; set; } // Chốt 30% tiếp theo
    public decimal TakeProfitStep2Percent { get; set; }
    
    public decimal TrailingStopPrice { get; set; } // Mốc chặn lãi thả nổi bảo vệ thành quả
    public decimal TrailingStopPercent { get; set; }
    
    public string ActionStage { get; set; } = string.Empty;
    public List<string> ExecutionSteps { get; set; } = new();
}
