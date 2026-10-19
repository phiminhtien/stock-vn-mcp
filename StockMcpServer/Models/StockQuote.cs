using System;

namespace StockMcpServer.Models;

public class StockQuote
{
    public string Symbol { get; set; } = string.Empty;
    public string Market { get; set; } = "VN";
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Currency { get; set; } = "VND";
}
