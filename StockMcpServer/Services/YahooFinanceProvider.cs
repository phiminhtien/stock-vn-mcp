using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class YahooFinanceProvider : IStockDataProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "Yahoo Finance";

    public YahooFinanceProvider(HttpClient httpClient)
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
            Market = "GLOBAL",
            CurrentPrice = latest.Close,
            PreviousClose = prev.Close,
            Change = change,
            ChangePercent = Math.Round(changePercent, 2),
            DayHigh = latest.High,
            DayLow = latest.Low,
            Volume = latest.Volume,
            LastUpdated = latest.Date,
            Currency = "USD"
        };
    }

    public async Task<List<StockCandle>> GetHistoricalCandlesAsync(string symbol, int count = 100, CancellationToken cancellationToken = default)
    {
        string range = count switch
        {
            <= 5 => "5d",
            <= 30 => "1mo",
            <= 90 => "3mo",
            <= 180 => "6mo",
            <= 365 => "1y",
            _ => "2y"
        };

        string url = $"https://query2.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range={range}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new List<StockCandle>();
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("chart", out var chart) ||
            !chart.TryGetProperty("result", out var results) ||
            results.GetArrayLength() == 0)
        {
            return new List<StockCandle>();
        }

        var result0 = results[0];
        if (!result0.TryGetProperty("timestamp", out var timestamps) ||
            !result0.TryGetProperty("indicators", out var indicators) ||
            !indicators.TryGetProperty("quote", out var quoteArray) ||
            quoteArray.GetArrayLength() == 0)
        {
            return new List<StockCandle>();
        }

        var quote0 = quoteArray[0];
        var opens = quote0.GetProperty("open");
        var highs = quote0.GetProperty("high");
        var lows = quote0.GetProperty("low");
        var closes = quote0.GetProperty("close");
        var volumes = quote0.GetProperty("volume");

        int len = timestamps.GetArrayLength();
        var candles = new List<StockCandle>(len);

        for (int i = 0; i < len; i++)
        {
            if (closes[i].ValueKind == JsonValueKind.Null ||
                opens[i].ValueKind == JsonValueKind.Null ||
                highs[i].ValueKind == JsonValueKind.Null ||
                lows[i].ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).LocalDateTime;
            decimal open = Math.Round(opens[i].GetDecimal(), 2);
            decimal high = Math.Round(highs[i].GetDecimal(), 2);
            decimal low = Math.Round(lows[i].GetDecimal(), 2);
            decimal close = Math.Round(closes[i].GetDecimal(), 2);
            long vol = volumes[i].ValueKind != JsonValueKind.Null ? volumes[i].GetInt64() : 0;

            candles.Add(new StockCandle(date, open, high, low, close, vol));
        }

        if (candles.Count > count)
        {
            candles = candles.GetRange(candles.Count - count, count);
        }

        return candles;
    }
}
