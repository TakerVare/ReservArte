namespace ReservArte.Domain.Entities;

public class ProductSaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }

    public ProductSale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
