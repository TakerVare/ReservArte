namespace ReservArte.Domain.Entities;

public class EmployeeService
{
    public int EmployeeId { get; set; }
    public int ServiceId { get; set; }
    public int ProficiencyLevel { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public Employee Employee { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
