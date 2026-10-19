using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class VnStockProvider : IStockDataProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "VNDirect Vietnam Market";

    public VnStockProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var candles = await GetHistoricalCandlesAsync(symbol, 2, cancellationToken);
        if (candles.Count == 0) return null;

        var latest = candles[^1];
        var prev = candles.Count > 1 ? candles[^2] : latest;

        decimal change = latest.Close - prev.Close;
        decimal changePercent = prev.Close > 0 ? (change / prev.Close) * 100m : 0m;

        return new StockQuote
        {
            Symbol = symbol.ToUpperInvariant(),
            Market = "VN",
            CurrentPrice = latest.Close,
            PreviousClose = prev.Close,
            Change = change,
            ChangePercent = Math.Round(changePercent, 2),
            DayHigh = latest.High,
            DayLow = latest.Low,
            Volume = latest.Volume,
            LastUpdated = latest.Date,
            Currency = "VND (nghìn đồng)"
        };
    }

    public async Task<List<StockCandle>> GetHistoricalCandlesAsync(string symbol, int count = 100, CancellationToken cancellationToken = default)
    {
        var cleanSymbol = symbol.ToUpperInvariant().Replace(".VN", "").Trim();
        var toTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Request ample calendar days to get desired trading sessions (accounting for weekends/holidays)
        var fromTime = DateTimeOffset.UtcNow.AddDays(-Math.Max(count * 2, 60)).ToUnixTimeSeconds();

        string url = $"https://dchart-api.vndirect.com.vn/dchart/history?resolution=D&symbol={cleanSymbol}&from={fromTime}&to={toTime}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new List<StockCandle>();
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("s", out var status) || status.GetString() != "ok")
        {
            return new List<StockCandle>();
        }

        var tArray = root.GetProperty("t");
        var cArray = root.GetProperty("c");
        var oArray = root.GetProperty("o");
        var hArray = root.GetProperty("h");
        var lArray = root.GetProperty("l");
        var vArray = root.GetProperty("v");

        int len = tArray.GetArrayLength();
        var candles = new List<StockCandle>(len);

        for (int i = 0; i < len; i++)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(tArray[i].GetInt64()).LocalDateTime;
            decimal open = oArray[i].GetDecimal();
            decimal high = hArray[i].GetDecimal();
            decimal low = lArray[i].GetDecimal();
            decimal close = cArray[i].GetDecimal();
            long volume = vArray[i].GetInt64();

            candles.Add(new StockCandle(date, open, high, low, close, volume));
        }

        if (candles.Count > count)
        {
            candles = candles.GetRange(candles.Count - count, count);
        }

        return candles;
    }
}
