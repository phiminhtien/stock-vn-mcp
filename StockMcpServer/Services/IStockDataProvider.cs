using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public interface IStockDataProvider
{
    string ProviderName { get; }
    Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<StockCandle>> GetHistoricalCandlesAsync(string symbol, int count = 100, CancellationToken cancellationToken = default);
}
