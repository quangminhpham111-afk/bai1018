namespace BookManager;

public class BookModel
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    /// <summary>Đường dẫn file ảnh bìa (lưu vào CSV).</summary>
    public string CoverPath { get; set; } = "";

    /// <summary>Chỉ hiển thị tên file trên lưới cho gọn.</summary>
    public string CoverFileName => string.IsNullOrWhiteSpace(CoverPath) ? "" : Path.GetFileName(CoverPath);

    /// <summary>Giá trị tồn kho của đầu sách = đơn giá x số lượng.</summary>
    public decimal TotalValue => Price * Quantity;
}
