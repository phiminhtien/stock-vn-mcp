using System;

namespace StockMcpServer.Models;

public record StockCandle(
    DateTime Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);
