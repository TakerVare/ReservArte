namespace ReservArte.Domain.Entities;

public class ServicePackageItem
{
    public int Id { get; set; }
    public int ServicePackageId { get; set; }
    public int ServiceId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public ServicePackage ServicePackage { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
