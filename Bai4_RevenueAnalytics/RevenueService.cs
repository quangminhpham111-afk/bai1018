namespace RevenueAnalytics;

/// <summary>
/// Giả lập truy xuất CSDL + tổng hợp giao dịch lớn (4-6 giây).
/// Hàm này chạy trên Background Thread (qua Task.Run) nên KHÔNG được đụng vào Control.
/// Báo tiến độ qua IProgress (tự marshal về UI Thread).
/// </summary>
public static class RevenueService
{
    public static readonly string[] Branches = { "Hà Nội", "Đà Nẵng", "TP.HCM" };

    private static readonly string[] Customers =
    {
        "Nguyễn Văn An", "Trần Thị Bình", "Lê Minh Cường", "Phạm Thu Hà",
        "Hoàng Quốc Dũng", "Vũ Mai Linh", "Đặng Quang Huy", "Bùi Thanh Tâm"
    };

    public static List<TransactionModel> Generate(
        DateTime from, DateTime to, string branch,
        IProgress<(int Percent, string Message)> progress, CancellationToken token)
    {
        const int steps = 50;
        const int rowsPerStep = 100;                       // tổng 5.000 giao dịch
        var rng = new Random();
        int totalMs = rng.Next(4000, 6001);                // 4-6 giây
        int stepMs = totalMs / steps;

        int days = Math.Max(1, (to.Date - from.Date).Days + 1);
        var result = new List<TransactionModel>(steps * rowsPerStep);

        for (int s = 1; s <= steps; s++)
        {
            // Chờ giả lập truy vấn; WaitOne trả về true ngay khi token bị Cancel -> hủy tức thì
            if (token.WaitHandle.WaitOne(stepMs))
                token.ThrowIfCancellationRequested();

            for (int i = 0; i < rowsPerStep; i++)
            {
                token.ThrowIfCancellationRequested();
                int n = result.Count + 1;
                result.Add(new TransactionModel
                {
                    TransactionId = "GD" + n.ToString("D6"),
                    Date = from.Date.AddDays(rng.Next(days)).AddMinutes(rng.Next(480, 1260)),
                    Branch = branch == "Tất cả" ? Branches[rng.Next(Branches.Length)] : branch,
                    Customer = Customers[rng.Next(Customers.Length)],
                    Amount = rng.Next(50, 5000) * 1000m
                });
            }

            int percent = s * 100 / steps;
            progress?.Report((percent, $"Đang tổng hợp dữ liệu... {percent}% ({result.Count:N0} giao dịch)"));
        }

        result.Sort((a, b) => a.Date.CompareTo(b.Date));
        return result;
    }
}
