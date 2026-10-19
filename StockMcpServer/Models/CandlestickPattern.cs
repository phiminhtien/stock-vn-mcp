namespace StockMcpServer.Models;

public enum PatternSignalType
{
    Bullish,
    Bearish,
    Neutral
}

public class CandlestickPattern
{
    public string Name { get; set; } = string.Empty;
    public PatternSignalType SignalType { get; set; }
    public string Reliability { get; set; } = "Trung bình"; // Cao, Rất cao, Trung bình
    public string Description { get; set; } = string.Empty;
    public string ActionAdvice { get; set; } = string.Empty;
}
