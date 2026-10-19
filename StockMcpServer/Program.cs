using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using StockMcpServer.Services;
using StockMcpServer.Tools;

namespace StockMcpServer;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h") || args.Contains("/?"))
        {
            PrintHelp();
            return;
        }

        string transport = GetSetting(args, "--transport", "MCP_TRANSPORT", "stdio").ToLowerInvariant();

        if (transport == "stdio")
        {
            await RunStdioServerAsync(args);
        }
        else if (transport == "http" || transport == "sse" || transport == "web")
        {
            await RunHttpServerAsync(args);
        }
        else
        {
            Console.Error.WriteLine($"[ERROR] Không nhận diện được giao thức transport '{transport}'.");
            Console.Error.WriteLine("Các giao thức hợp lệ: 'stdio' (mặc định), 'http' (Streamable HTTP & SSE), 'sse'.");
            Console.Error.WriteLine("Chạy với '--help' để xem hướng dẫn chi tiết.");
            Environment.Exit(1);
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("🛡️ STOCK ANALYSIS & CAPITAL PROTECTION MCP SERVER (C# / .NET 10)");
        Console.WriteLine("==========================================================================");
        Console.WriteLine("Cú pháp: dotnet run [options]  hoặc  StockMcpServer.exe [options]");
        Console.WriteLine("\nCác tùy chọn Transport:");
        Console.WriteLine("  --transport <stdio|http|sse>   Giao thức vận chuyển (Mặc định: 'stdio')");
        Console.WriteLine("                                 - stdio : Standard Input/Output (Claude Desktop, Cursor)");
        Console.WriteLine("                                 - http  : Streamable HTTP (MCP 2025/2026) & SSE");
        Console.WriteLine("                                 - sse   : Server-Sent Events qua HTTP");
        Console.WriteLine("  --port <cổng>                  Cổng lắng nghe trong chế độ HTTP/SSE (Mặc định: 5000)");
        Console.WriteLine("  --host <địa chỉ>               Địa chỉ bind IP (Mặc định: 'localhost' hoặc '0.0.0.0')");
        Console.WriteLine("  --help, -h                     Hiển thị hướng dẫn này");
        Console.WriteLine("\nBiến môi trường tương đương:");
        Console.WriteLine("  MCP_TRANSPORT                  stdio | http | sse");
        Console.WriteLine("  MCP_PORT                       Ví dụ: 5000");
        Console.WriteLine("  MCP_HOST                       Ví dụ: 0.0.0.0 hoặc localhost");
        Console.WriteLine("\nCác Endpoint khi chạy HTTP/SSE:");
        Console.WriteLine("  - Streamable HTTP Endpoint     : http://<host>:<port>/mcp");
        Console.WriteLine("  - Server-Sent Events (SSE)     : http://<host>:<port>/sse  hoặc  /mcp/sse");
        Console.WriteLine("  - SSE Message Postback         : http://<host>:<port>/message  hoặc  /mcp/message");
        Console.WriteLine("  - Health Check & Metadata      : http://<host>:<port>/health");
        Console.WriteLine("==========================================================================");
    }

    private static void ConfigureSharedServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<VnStockProvider>();
        services.AddSingleton<YahooFinanceProvider>();
        services.AddSingleton<UnifiedStockService>();
        services.AddSingleton<TechnicalAnalysisEngine>();
        services.AddSingleton<InvestmentStrategyAdvisor>();
        services.AddSingleton<CandlestickPatternDetector>();
        services.AddSingleton<VolumeFlowAnalyzer>();
        services.AddSingleton<RiskAndPositionManager>();
        services.AddSingleton<FibonacciCalculator>();
        services.AddSingleton<MarketOpportunityScanner>();
    }

    private static async Task RunStdioServerAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Đảm bảo stdout sạch hoàn toàn cho MCP JSON-RPC messages
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        ConfigureSharedServices(builder.Services);

        builder.Services.AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "StockAnalysisMcpServer",
                Version = "1.0.0"
            };
        })
        .WithStdioServerTransport()
        .WithTools<StockAnalysisTools>();

        var app = builder.Build();

        Console.Error.WriteLine("=======================================================");
        Console.Error.WriteLine("🚀 Stock Analysis MCP Server (.NET / C#) [STDIO MODE]");
        Console.Error.WriteLine("📈 Giao thức: STDIO JSON-RPC 2.0");
        Console.Error.WriteLine("🛡️ Sẵn sàng kết nối với Claude Desktop, Cursor, Antigravity");
        Console.Error.WriteLine("=======================================================");

        await app.RunAsync();
    }

    private static async Task RunHttpServerAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        string port = GetSetting(args, "--port", "MCP_PORT", "5000");
        string host = GetSetting(args, "--host", "MCP_HOST", "localhost");
        string url = $"http://{host}:{port}";
        builder.WebHost.UseUrls(url);

        ConfigureSharedServices(builder.Services);

        builder.Services.AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "StockAnalysisMcpServer",
                Version = "1.0.0"
            };
        })
        .WithHttpTransport(options =>
        {
#pragma warning disable MCP9004
            options.EnableLegacySse = true; // Kích hoạt /sse và /message cho SSE clients
#pragma warning restore MCP9004
            options.SessionMode = HttpServerSessionMode.StatefulForInitializeClients;
        })
        .WithTools<StockAnalysisTools>();

        var app = builder.Build();

        // Định tuyến MCP đa giao thức:
        // 1. /mcp: Streamable HTTP (POST /mcp) & SSE (/mcp/sse, /mcp/message)
        app.MapMcp("/mcp");
        // 2. Gốc: Hỗ trợ các client SSE tìm kiếm trực tiếp tại /sse và /message
        app.MapMcp("");

        // Trang thông tin & kiểm tra sức khỏe hệ thống
        app.MapGet("/", () => Results.Content(
            "<html><body style='font-family:sans-serif;padding:30px;line-height:1.6;'>" +
            "<h2>🛡️ Stock Analysis MCP Server (C# / .NET 10)</h2>" +
            "<p>Server đang hoạt động và hỗ trợ đa giao thức Transport:</p>" +
            "<ul>" +
            "<li><b>Streamable HTTP Endpoint:</b> <code>" + url + "/mcp</code></li>" +
            "<li><b>Legacy SSE Endpoint:</b> <code>" + url + "/sse</code></li>" +
            "<li><b>SSE Message Endpoint:</b> <code>" + url + "/message</code></li>" +
            "<li><b>Health Check:</b> <code>" + url + "/health</code></li>" +
            "</ul>" +
            "<hr/><p>Hỗ trợ phân tích kỹ thuật chứng khoán Việt Nam & Quốc tế, quản trị rủi ro bảo toàn vốn.</p>" +
            "</body></html>", "text/html"));

        app.MapGet("/health", () => Results.Json(new
        {
            status = "UP",
            service = "StockAnalysisMcpServer",
            version = "1.0.0",
            transports = new[] { "stdio", "http (streamable /mcp)", "sse (/sse)" },
            supportedMarkets = new[] { "VN (HOSE, HNX, UPCOM)", "GLOBAL (US Stocks, Crypto)" },
            tools = new[]
            {
                "get_stock_quote",
                "get_historical_candles",
                "calculate_technical_indicators",
                "analyze_trading_signals",
                "detect_risk_traps",
                "compare_stocks",
                "calculate_position_size",
                "detect_candlestick_patterns",
                "analyze_volume_flow",
                "calculate_exit_strategy",
                "calculate_fibonacci_levels",
                "scan_market_opportunities"
            }
        }));

        Console.WriteLine("=======================================================");
        Console.WriteLine($"🚀 Stock Analysis MCP Server [HTTP/SSE MODE] is RUNNING!");
        Console.WriteLine($"🌐 URL: {url}");
        Console.WriteLine($"📡 Streamable HTTP Endpoint: {url}/mcp");
        Console.WriteLine($"📡 Server-Sent Events (SSE) Endpoint: {url}/sse");
        Console.WriteLine($"🩺 Health Check: {url}/health");
        Console.WriteLine("=======================================================");

        await app.RunAsync();
    }

    private static string GetSetting(string[] args, string flag, string envVar, string defaultValue)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    return args[i + 1];
                }
            }
            if (args[i].StartsWith($"{flag}=", StringComparison.OrdinalIgnoreCase))
            {
                return args[i].Substring(flag.Length + 1);
            }
        }

        var env = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        return defaultValue;
    }
}
