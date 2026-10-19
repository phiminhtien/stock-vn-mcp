namespace StockMcpServer.Models;

public class TechnicalIndicators
{
    // Moving Averages
    public decimal? Sma20 { get; set; }
    public decimal? Sma50 { get; set; }
    public decimal? Sma200 { get; set; }
    public decimal? Ema20 { get; set; }
    public decimal? Ema50 { get; set; }
    public decimal? Ema200 { get; set; }
    
    // RSI
    public decimal? Rsi14 { get; set; }
    public string RsiSignal { get; set; } = "Trung lập"; // Quá mua, Quá bán, Phân kỳ dương, Phân kỳ âm, Trung lập
    
    // MACD
    public decimal? MacdLine { get; set; }
    public decimal? MacdSignalLine { get; set; }
    public decimal? MacdHistogram { get; set; }
    public string MacdSignal { get; set; } = "Trung lập"; // Cắt lên (Tăng), Cắt xuống (Giảm), Xung lực mạnh, Suy yếu
    
    // Bollinger Bands
    public decimal? BollingerUpper { get; set; }
    public decimal? BollingerMiddle { get; set; }
    public decimal? BollingerLower { get; set; }
    public decimal? BollingerBandwidth { get; set; }
    public bool IsBollingerSqueeze { get; set; }
    
    // Volatility & Levels
    public decimal? Atr14 { get; set; }
    public decimal SupportLevel { get; set; }
    public decimal ResistanceLevel { get; set; }
    
    // Volume Analysis
    public decimal AverageVolume20 { get; set; }
    public decimal VolumeRatio { get; set; } // So với trung bình 20 phiên
    public string VolumeSignal { get; set; } = "Bình thường";
}
