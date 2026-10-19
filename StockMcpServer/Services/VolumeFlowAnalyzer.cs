using System;
using System.Collections.Generic;
using System.Linq;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class VolumeFlowAnalyzer
{
    public VolumeFlowAnalysis AnalyzeFlow(string symbol, List<StockCandle> candles)
    {
        var analysis = new VolumeFlowAnalysis
        {
            Symbol = symbol.ToUpperInvariant()
        };

        if (candles == null || candles.Count < 20)
        {
            analysis.FinalVerdict = "Chưa đủ dữ liệu nến (tối thiểu 20 phiên) để phân tích dòng tiền chuyên sâu.";
            return analysis;
        }

        var latest = candles[^1];
        analysis.CurrentVolume = latest.Volume;

        int lookback = Math.Min(candles.Count, 20);
        var recent20 = candles.TakeLast(lookback).ToList();
        analysis.AverageVolume20 = (decimal)recent20.Average(c => c.Volume);
        analysis.VolumeRatio = analysis.AverageVolume20 > 0 ? Math.Round((decimal)latest.Volume / analysis.AverageVolume20, 2) : 1m;

        // 1. Tính toán On-Balance Volume (OBV)
        decimal obv = 0m;
        var obvHistory = new List<decimal>(candles.Count);

        for (int i = 1; i < candles.Count; i++)
        {
            if (candles[i].Close > candles[i - 1].Close)
                obv += candles[i].Volume;
            else if (candles[i].Close < candles[i - 1].Close)
                obv -= candles[i].Volume;

            obvHistory.Add(obv);
        }

        analysis.ObvCurrent = obv;

        // Phân tích xu hướng OBV 10 phiên gần nhất
        if (obvHistory.Count >= 10)
        {
            decimal obvStart = obvHistory[^10];
            decimal obvEnd = obvHistory[^1];
            decimal priceStart = candles[^10].Close;
            decimal priceEnd = candles[^1].Close;

            if (obvEnd > obvStart && priceEnd >= priceStart)
            {
                analysis.ObvTrend = "DÒNG TIỀN VÀO MẠNH (Dòng tiền thông minh tích lũy)";
                analysis.FlowObservations.Add("OBV tăng đồng thuận cùng đà tăng giá: Dòng tiền lớn đang chủ động gom hàng đẩy giá.");
            }
            else if (obvEnd > obvStart && priceEnd < priceStart)
            {
                analysis.ObvTrend = "PHÂN KỲ DƯƠNG OBV (Gom ngầm tại vùng giảm)";
                analysis.FlowObservations.Add("Tín hiệu tích cực: Giá giảm nhưng OBV lại tăng, cho thấy dòng tiền thông minh đang âm thầm gom hàng giá chiết khấu.");
            }
            else if (obvEnd < obvStart && priceEnd > priceStart)
            {
                analysis.ObvTrend = "PHÂN KỲ ÂM OBV (Kéo xả - CỰC KỲ NGUY HIỂM)";
                analysis.FlowObservations.Add("CẢNH BÁO BẪY KÉO XẢ: Giá tăng rướn nhưng OBV giảm mạnh. Tay to đang bán ròng trong các phiên xanh.");
            }
            else
            {
                analysis.ObvTrend = "DÒNG TIỀN RÚT RA (Áp lực phân phối)";
                analysis.FlowObservations.Add("OBV giảm đồng pha với xu hướng giảm: Dòng tiền lớn chưa sẵn sàng quay trở lại.");
            }
        }

        // 2. Đếm số ngày phân phối (Distribution Days) theo phương pháp William O'Neil / CANSLIM
        // Định nghĩa ngày phân phối: Giá giảm >= 0.2% kèm khối lượng lớn hơn phiên trước đó
        int distCount = 0;
        int accumCount = 0;

        for (int i = 1; i < recent20.Count; i++)
        {
            var cur = recent20[i];
            var prv = recent20[i - 1];

            decimal changePct = prv.Close > 0 ? (cur.Close - prv.Close) / prv.Close : 0m;
            bool volHigher = cur.Volume > prv.Volume;

            if (changePct <= -0.002m && volHigher)
            {
                distCount++;
            }
            else if (changePct >= 0.002m && volHigher)
            {
                accumCount++;
            }
        }

        analysis.DistributionDaysLast20 = distCount;
        analysis.AccumulationDaysLast20 = accumCount;

        // Đánh giá pha dòng tiền của Smart Money
        if (distCount >= 5)
        {
            analysis.SmartMoneyPhase = "GIAI ĐOẠN PHÂN PHỐI NẶNG (Cảnh báo đỉnh)";
            analysis.FlowObservations.Add($"Phát hiện tới {distCount} phiên phân phối trong 20 phiên gần nhất! Dấu hiệu rõ ràng của việc tay to thoái vốn.");
            analysis.FinalVerdict = "RỦI RO CAO: Dòng tiền lớn đang rút ra. Không mở vị thế mua mới; ưu tiên bảo toàn lợi nhuận.";
        }
        else if (accumCount >= 4 && distCount <= 2)
        {
            analysis.SmartMoneyPhase = "GIAI ĐOẠN TÍCH LŨY / ĐẨY GIÁ (Dòng tiền ủng hộ)";
            analysis.FlowObservations.Add($"Áp đảo với {accumCount} phiên tích lũy dòng tiền vào, số ngày phân phối rất ít ({distCount} ngày).");
            analysis.FinalVerdict = "AN TOÀN: Dòng tiền tổ chức đang hỗ trợ mạnh cho đà tăng của cổ phiếu.";
        }
        else
        {
            analysis.SmartMoneyPhase = "GIAI ĐOẠN CÂN BẰNG / GIẰNG CO";
            analysis.FinalVerdict = "TRUNG LẬP: Dòng tiền chưa thể hiện xu hướng gom hay xả rõ rệt. Cần theo dõi thêm.";
        }

        return analysis;
    }
}
