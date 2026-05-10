namespace ReservArte.Domain.Entities;

public class ProductSale
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public int? AppointmentId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public int? SoldBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Customer? Customer { get; set; }
    public Appointment? Appointment { get; set; }
    public Employee? SoldByEmployee { get; set; }
    public ICollection<ProductSaleItem> Items { get; set; } = [];
}
