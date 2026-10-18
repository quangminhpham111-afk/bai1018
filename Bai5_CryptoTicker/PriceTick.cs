namespace CryptoTicker;

/// <summary>Một lần quét giá. Là record bất biến nên truyền an toàn giữa các thread.</summary>
public record PriceTick(DateTime Time, decimal Price, decimal Change, decimal ChangePercent);

/// <summary>Dòng hiển thị trong nhật ký lịch sử (dgvHistoryLog).</summary>
public class HistoryRow
{
    public string Time { get; init; } = "";
    public decimal Price { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePercent { get; init; }
    public string Trend { get; init; } = "";
}
