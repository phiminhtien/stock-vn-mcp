using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class UnifiedStockService
{
    private readonly VnStockProvider _vnProvider;
    private readonly YahooFinanceProvider _yahooProvider;

    public UnifiedStockService(VnStockProvider vnProvider, YahooFinanceProvider yahooProvider)
    {
        _vnProvider = vnProvider;
        _yahooProvider = yahooProvider;
    }

    private bool IsLikelyVnStock(string symbol)
    {
        var clean = symbol.Trim().ToUpperInvariant();
        if (clean.EndsWith(".VN")) return true;
        // Standard Vietnamese stock ticker is 3 uppercase characters
        if (clean.Length == 3 && char.IsLetter(clean[0]) && char.IsLetter(clean[1]) && char.IsLetter(clean[2]))
        {
            return true;
        }
        return false;
    }

    public async Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (IsLikelyVnStock(symbol))
        {
            var quote = await _vnProvider.GetQuoteAsync(symbol, cancellationToken);
            if (quote != null) return quote;
            return await _yahooProvider.GetQuoteAsync(symbol, cancellationToken);
        }
        else
        {
            var quote = await _yahooProvider.GetQuoteAsync(symbol, cancellationToken);
            if (quote != null) return quote;
            return await _vnProvider.GetQuoteAsync(symbol, cancellationToken);
        }
    }

    public async Task<List<StockCandle>> GetHistoricalCandlesAsync(string symbol, int count = 100, CancellationToken cancellationToken = default)
    {
        if (IsLikelyVnStock(symbol))
        {
            var candles = await _vnProvider.GetHistoricalCandlesAsync(symbol, count, cancellationToken);
            if (candles.Count > 0) return candles;
            return await _yahooProvider.GetHistoricalCandlesAsync(symbol, count, cancellationToken);
        }
        else
        {
            var candles = await _yahooProvider.GetHistoricalCandlesAsync(symbol, count, cancellationToken);
            if (candles.Count > 0) return candles;
            return await _vnProvider.GetHistoricalCandlesAsync(symbol, count, cancellationToken);
        }
    }
}
