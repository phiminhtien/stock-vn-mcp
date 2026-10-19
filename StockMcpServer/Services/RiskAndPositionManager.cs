using System;
using System.Collections.Generic;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class RiskAndPositionManager
{
    /// <summary>
    /// Tính toán kích thước vị thế chuẩn xác theo quy tắc quản trị rủi ro 1% - 2% tài khoản.
    /// </summary>
    public PositionSizeResult CalculatePositionSize(
        string symbol,
        string market,
        decimal totalCapital,
        decimal maxRiskPercent,
        decimal entryPrice,
        decimal stopLossPrice)
    {
        decimal actualEntryPrice = entryPrice;
        decimal actualStopLossPrice = stopLossPrice;

        // Nếu là thị trường VN và giá đang ở đơn vị nghìn đồng (ví dụ 58.2 = 58,200 VND)
        if (market == "VN" && entryPrice < 1000m)
        {
            actualEntryPrice = entryPrice * 1000m;
            actualStopLossPrice = stopLossPrice * 1000m;
        }

        decimal riskPerShare = actualEntryPrice - actualStopLossPrice;
        if (riskPerShare <= 0)
        {
            actualStopLossPrice = Math.Round(actualEntryPrice * 0.94m, 2);
            riskPerShare = actualEntryPrice - actualStopLossPrice;
        }

        decimal maxRiskAmount = Math.Round(totalCapital * (maxRiskPercent / 100m), 2);
        decimal rawShares = maxRiskAmount / riskPerShare;

        // Thị trường Việt Nam quy định lô chẵn 100 cổ phiếu
        long recommendedShares;
        if (market == "VN")
        {
            recommendedShares = ((long)rawShares / 100) * 100;
            if (recommendedShares == 0 && rawShares > 0) recommendedShares = 100;
        }
        else
        {
            recommendedShares = Math.Max(1, (long)rawShares);
        }

        decimal totalPositionValue = Math.Round(recommendedShares * actualEntryPrice, 2);
        decimal portfolioWeight = totalCapital > 0 ? Math.Round((totalPositionValue / totalCapital) * 100m, 1) : 0m;
        decimal riskPerSharePercent = actualEntryPrice > 0 ? Math.Round((riskPerShare / actualEntryPrice) * 100m, 2) : 0m;

        var result = new PositionSizeResult
        {
            Symbol = symbol.ToUpperInvariant(),
            TotalCapital = totalCapital,
            MaxRiskPercent = maxRiskPercent,
            MaxRiskAmount = maxRiskAmount,
            EntryPrice = entryPrice,
            StopLossPrice = stopLossPrice,
            RiskPerShare = riskPerShare,
            RiskPerSharePercent = riskPerSharePercent,
            RecommendedShares = recommendedShares,
            TotalPositionValue = totalPositionValue,
            PortfolioWeightPercent = portfolioWeight,
            Currency = market == "VN" ? "VND" : "USD"
        };

        if (portfolioWeight > 40m)
        {
            result.RiskLevel = "CẢNH BÁO TỶ TRỌNG QUÁ CAO (>40%)";
            result.ManagementAdvice = $"Vị thế này chiếm tới {portfolioWeight}% tổng tài khoản. Nếu cổ phiếu giảm sẽ gây áp lực tâm lý lớn. Khuyên bạn nên chia làm 2 đợt mua (50% tại nền, 50% khi bùng nổ vượt cản).";
        }
        else if (portfolioWeight < 10m)
        {
            result.RiskLevel = "TỶ TRỌNG THẤP (<10%)";
            result.ManagementAdvice = "Vị thế giải ngân an toàn mang tính chất thăm dò, không gây ảnh hưởng lớn đến tổng danh mục.";
        }
        else
        {
            result.RiskLevel = "TỶ TRỌNG LÝ TƯỞNG (15% - 35%)";
            result.ManagementAdvice = "Kích thước vị thế chuẩn theo công thức quản trị vốn của các quỹ đầu tư chuyên nghiệp. Rủi ro tối đa nếu cắt lỗ được khống chế chính xác ở mức đã định.";
        }

        return result;
    }

    /// <summary>
    /// Xây dựng lộ trình chốt lời từng phần và điểm chặn lãi thả nổi (Trailing Stop)
    /// </summary>
    public ExitStrategyPlan BuildExitStrategy(string symbol, decimal entryPrice, decimal currentPrice, decimal atr)
    {
        decimal profitPercent = entryPrice > 0 ? Math.Round(((currentPrice - entryPrice) / entryPrice) * 100m, 2) : 0m;

        var plan = new ExitStrategyPlan
        {
            Symbol = symbol.ToUpperInvariant(),
            EntryPrice = entryPrice,
            CurrentPrice = currentPrice,
            ProfitPercent = profitPercent
        };

        // Mục tiêu chốt lời nấc 1 (+8% đến +12%)
        plan.TakeProfitStep1 = Math.Round(Math.Max(entryPrice * 1.08m, currentPrice + atr * 1.2m), 2);
        plan.TakeProfitStep1Percent = Math.Round(((plan.TakeProfitStep1 - entryPrice) / entryPrice) * 100m, 1);

        // Mục tiêu chốt lời nấc 2 (+18% đến +25%)
        plan.TakeProfitStep2 = Math.Round(entryPrice * 1.20m, 2);
        plan.TakeProfitStep2Percent = Math.Round(((plan.TakeProfitStep2 - entryPrice) / entryPrice) * 100m, 1);

        // Điểm chặn lãi thả nổi (Trailing Stop)
        // Nếu đã có lãi > 5%, nâng Trailing Stop lên trên giá mua (Hòa vốn + phí)
        if (profitPercent >= 10m)
        {
            plan.TrailingStopPrice = Math.Round(currentPrice - atr * 1.5m, 2);
            plan.ActionStage = "GIAI ĐOẠN 3: GỒNG LÃI TỰ DO & NÂNG ĐIỂM CHẶN LÃI";
            plan.ExecutionSteps.Add($"Hiện tại vị thế đang lãi +{profitPercent}% rất tốt.");
            plan.ExecutionSteps.Add($"Chốt lời chủ động 30% - 50% số lượng cổ phiếu quanh vùng {plan.TakeProfitStep1:N2} - {plan.TakeProfitStep2:N2}.");
            plan.ExecutionSteps.Add($"Số lượng cổ phiếu còn lại: Đặt lệnh Stop tự động tại mốc Chặn Lãi: **{plan.TrailingStopPrice:N2}**. Nếu giá chạm mốc này thì bán hết để khóa lợi nhuận.");
        }
        else if (profitPercent >= 4m)
        {
            plan.TrailingStopPrice = Math.Round(entryPrice * 1.01m, 2); // Kéo về hòa vốn + 1%
            plan.ActionStage = "GIAI ĐOẠN 2: KÉO STOP-LOSS VỀ HÒA VỐN (FREE-TRADE)";
            plan.ExecutionSteps.Add($"Vị thế đã có lãi đệm +{profitPercent}%.");
            plan.ExecutionSteps.Add($"Hành động quan trọng nhất lúc này: DỜI NGAY ĐIỂM DỪNG LỖ VỀ GIÁ MUA (**{plan.TrailingStopPrice:N2}**). Kể từ lúc này, thương vụ của bạn đã hoàn toàn loại bỏ rủi ro thua lỗ!");
            plan.ExecutionSteps.Add($"Chờ đợi cổ phiếu tiếp tục tăng để chốt lời nấc 1 tại: **{plan.TakeProfitStep1:N2}** (+{plan.TakeProfitStep1Percent}%).");
        }
        else
        {
            plan.TrailingStopPrice = Math.Round(entryPrice * 0.94m, 2); // Mức cắt lỗ -6%
            plan.ActionStage = "GIAI ĐOẠN 1: QUẢN LÝ RỦI RO NGẶT NGHÈO";
            plan.ExecutionSteps.Add($"Vị thế mới mở hoặc đang dao động quanh nền giá ({profitPercent}%).");
            plan.ExecutionSteps.Add($"Mốc cắt lỗ tối đa bắt buộc: **{plan.TrailingStopPrice:N2}** (-6.0%). Tuyệt đối không được nới lỏng mức dừng lỗ.");
            plan.ExecutionSteps.Add("Kiên nhẫn chờ đợi tín hiệu dòng tiền xác nhận để kích hoạt đợt tăng tốc.");
        }

        plan.TrailingStopPercent = entryPrice > 0 ? Math.Round(((plan.TrailingStopPrice - entryPrice) / entryPrice) * 100m, 1) : 0m;
        return plan;
    }
}
