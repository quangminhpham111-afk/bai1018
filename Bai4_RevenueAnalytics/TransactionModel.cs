namespace RevenueAnalytics;

public class TransactionModel
{
    public string TransactionId { get; set; } = "";
    public DateTime Date { get; set; }
    public string Branch { get; set; } = "";
    public string Customer { get; set; } = "";
    public decimal Amount { get; set; }
}
