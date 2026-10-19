using System.Collections.Generic;

namespace StockMcpServer.Models;

public class VolumeFlowAnalysis
{
    public string Symbol { get; set; } = string.Empty;
    public long CurrentVolume { get; set; }
    public decimal AverageVolume20 { get; set; }
    public decimal VolumeRatio { get; set; }
    
    public decimal ObvCurrent { get; set; }
    public string ObvTrend { get; set; } = "Trung lập"; // Dòng tiền vào (Tích lũy), Dòng tiền rút (Phân phối)
    
    public int DistributionDaysLast20 { get; set; } // Số phiên phân phối
    public int AccumulationDaysLast20 { get; set; } // Số phiên tích lũy
    
    public string SmartMoneyPhase { get; set; } = string.Empty; // Gom hàng, Đẩy giá, Phân phối, Đè giá
    public List<string> FlowObservations { get; set; } = new();
    public string FinalVerdict { get; set; } = string.Empty;
}
