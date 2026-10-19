using System;
using System.Collections.Generic;
using System.Linq;
using StockMcpServer.Models;

namespace StockMcpServer.Services;

public class CandlestickPatternDetector
{
    public List<CandlestickPattern> DetectPatterns(List<StockCandle> candles)
    {
        var patterns = new List<CandlestickPattern>();
        if (candles == null || candles.Count < 3) return patterns;

        var c1 = candles[^1]; // Nến mới nhất
        var c2 = candles[^2]; // Nến trước đó
        var c3 = candles[^3]; // Nến 2 phiên trước

        decimal body1 = Math.Abs(c1.Close - c1.Open);
        decimal range1 = c1.High - c1.Low;
        decimal upperShadow1 = c1.High - Math.Max(c1.Open, c1.Close);
        decimal lowerShadow1 = Math.Min(c1.Open, c1.Close) - c1.Low;

        decimal body2 = Math.Abs(c2.Close - c2.Open);
        decimal range2 = c2.High - c2.Low;

        // 1. Nhận diện Doji
        if (range1 > 0 && body1 / range1 <= 0.1m)
        {
            if (lowerShadow1 >= range1 * 0.65m && upperShadow1 <= range1 * 0.15m)
            {
                patterns.Add(new CandlestickPattern
                {
                    Name = "Doji Chuồn Chuồn (Dragonfly Doji)",
                    SignalType = PatternSignalType.Bullish,
                    Reliability = "Cao",
                    Description = "Bóng dưới rất dài, bóng trên hầu như không có. Phe mua đã từ chối mức giá thấp và kéo ngược ngoạn mục.",
                    ActionAdvice = "Tín hiệu đảo chiều tăng đáng chú ý nếu xuất hiện ở vùng hỗ trợ hoặc sau nhịp giảm sâu."
                });
            }
            else if (upperShadow1 >= range1 * 0.65m && lowerShadow1 <= range1 * 0.15m)
            {
                patterns.Add(new CandlestickPattern
                {
                    Name = "Doji Bia Mộ (Gravestone Doji)",
                    SignalType = PatternSignalType.Bearish,
                    Reliability = "Rất cao",
                    Description = "Bóng trên rất dài, giá bị phe bán đạp mạnh dội ngược về mức mở cửa cuối phiên.",
                    ActionAdvice = "CẢNH BÁO BẪY ĐỈNH: Phe mua đã kiệt sức. Cân nhắc chốt lời hoặc hạ tỷ trọng ngay lập tức."
                });
            }
            else
            {
                patterns.Add(new CandlestickPattern
                {
                    Name = "Nến Doji Chân Dài (Long-Legged Doji)",
                    SignalType = PatternSignalType.Neutral,
                    Reliability = "Trung bình",
                    Description = "Biên độ dao động lớn nhưng giá đóng cửa gần bằng mở cửa, thể hiện sự giằng co dữ dội giữa phe mua và phe bán.",
                    ActionAdvice = "Chờ đợi cây nến tiếp theo xác nhận xu hướng trước khi giải ngân."
                });
            }
        }

        // 2. Nến Búa Rút Chân (Hammer)
        if (range1 > 0 && lowerShadow1 >= body1 * 2m && upperShadow1 <= body1 * 0.3m && c1.Close >= c1.Low + range1 * 0.6m)
        {
            patterns.Add(new CandlestickPattern
            {
                Name = "Nến Búa Rút Chân (Hammer)",
                SignalType = PatternSignalType.Bullish,
                Reliability = "Cao",
                Description = "Thân nến nhỏ ở phía trên, bóng nến dưới dài ít nhất gấp đôi thân. Lực cầu bắt đáy hấp thụ toàn bộ áp lực bán.",
                ActionAdvice = "Cơ hội giải ngân thăm dò khi cổ phiếu kiểm định thành công vùng hỗ trợ."
            });
        }

        // 3. Nến Búa Ngược / Bắn Sao (Shooting Star)
        if (range1 > 0 && upperShadow1 >= body1 * 2m && lowerShadow1 <= body1 * 0.3m && c1.Close <= c1.High - range1 * 0.6m)
        {
            patterns.Add(new CandlestickPattern
            {
                Name = "Nến Bắn Sao (Shooting Star / Búa Ngược)",
                SignalType = PatternSignalType.Bearish,
                Reliability = "Rất cao",
                Description = "Bóng trên dài gấp đôi thân nến, cho thấy nỗ lực kéo giá tăng trong phiên thất bại hoàn toàn trước lực xả.",
                ActionAdvice = "CẢNH BÁO: Bẫy Bull-trap trong phiên. Tuyệt đối không mua đuổi giá xanh tím."
            });
        }

        // 4. Nhấn Chìm Tăng (Bullish Engulfing)
        bool c2IsRed = c2.Close < c2.Open;
        bool c1IsGreen = c1.Close > c1.Open;
        if (c2IsRed && c1IsGreen && c1.Open <= c2.Close && c1.Close >= c2.Open && body1 > body2)
        {
            patterns.Add(new CandlestickPattern
            {
                Name = "Cụm Nến Nhấn Chìm Tăng (Bullish Engulfing)",
                SignalType = PatternSignalType.Bullish,
                Reliability = "Rất cao",
                Description = "Cây nến xanh lớn nuốt trọn toàn bộ thân nến đỏ trước đó, kèm động lượng áp đảo từ phe mua.",
                ActionAdvice = "Tín hiệu đảo chiều tăng rất mạnh mẽ. Có thể gia tăng tỷ trọng khi có xác nhận thanh khoản."
            });
        }

        // 5. Nhấn Chìm Giảm (Bearish Engulfing)
        bool c2IsGreen = c2.Close > c2.Open;
        bool c1IsRed = c1.Close < c1.Open;
        if (c2IsGreen && c1IsRed && c1.Open >= c2.Close && c1.Close <= c2.Open && body1 > body2)
        {
            patterns.Add(new CandlestickPattern
            {
                Name = "Cụm Nến Nhấn Chìm Giảm (Bearish Engulfing)",
                SignalType = PatternSignalType.Bearish,
                Reliability = "Rất cao",
                Description = "Thân nến đỏ nhấn chìm hoàn toàn nến tăng trước, phản ánh áp lực bán tháo ồ ạt từ phe phân phối.",
                ActionAdvice = "CẢNH BÁO BÁN THÁO: Bán hạ tỷ trọng ngay lập tức để bảo vệ vốn. Tuyệt đối không bắt đáy."
            });
        }

        // 6. Mô hình Sao Mai (Morning Star - 3 nến)
        bool c3IsRed = c3.Close < c3.Open && (c3.Open - c3.Close) / c3.Open > 0.015m;
        bool c2IsSmall = body2 <= body1 * 0.5m;
        if (c3IsRed && c2IsSmall && c1IsGreen && c1.Close >= c3.Close + (c3.Open - c3.Close) * 0.5m)
        {
            patterns.Add(new CandlestickPattern
            {
                Name = "Mô Hình Sao Mai (Morning Star Reversal)",
                SignalType = PatternSignalType.Bullish,
                Reliability = "Rất cao",
                Description = "Bộ 3 nến đảo chiều kinh điển: Nến đỏ lớn -> Nến nhỏ tạo đáy giằng co -> Nến xanh bứt phá lấy lại hơn 50% nhịp giảm.",
                ActionAdvice = "Xác nhận tạo đáy thành công. Điểm vào lệnh chuẩn theo phân tích kỹ thuật."
            });
        }

        // 7. Mô hình Sao Hôm (Evening Star - 3 nến)
        bool c3IsGreen = c3.Close > c3.Open && (c3.Close - c3.Open) / c3.Open > 0.015m;
        if (c3IsGreen && c2IsSmall && c1IsRed && c1.Close <= c3.Open + (c3.Close - c3.Open) * 0.5m)
        {
            patterns.Add(new CandlestickPattern
            {
                Name = "Mô Hình Sao Hôm (Evening Star Reversal)",
                SignalType = PatternSignalType.Bearish,
                Reliability = "Rất cao",
                Description = "Bộ 3 nến tạo đỉnh nguy hiểm: Nến tăng hưng phấn -> Nến chững lại -> Nến đỏ cắm đầu xác nhận đảo chiều.",
                ActionAdvice = "CẢNH BÁO ĐỈNH SÓNG: Kích hoạt kế hoạch chốt lời toàn phần hoặc đặt lệnh Stop-Loss chặt chẽ."
            });
        }

        return patterns;
    }
}
