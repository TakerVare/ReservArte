using AwesomeAssertions;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.DTOs.Services;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Tests de los 15 mapeos entidad → DTO (Mapperly, Application/Mapping). Nacieron como
/// tests de caracterización en RA-869f6r7vw: se escribieron y pasaron contra AutoMapper y,
/// al cambiar a Mapperly, solo cambió la región «Punto de acceso», no las aserciones. Cada
/// caso completo rellena todas las propiedades con valores distintos y compara con un DTO
/// esperado escrito entero a mano, de modo que una propiedad mal mapeada sale como
/// diferencia. Una propiedad del DTO sin origen ni siquiera compila (RMG012).
///
/// No se prueban colecciones nulas ni líneas de paquete sin servicio: las entidades
/// inicializan sus colecciones y el repositorio de paquetes carga siempre el servicio
/// (ThenInclude, FK obligatoria), así que ninguno de los dos casos llega al mapeo.
/// </summary>
public class MappingCharacterizationTests
{
    #region Punto de acceso al mapeador

    private static AppointmentDto ToDto(Appointment source) => AppointmentMapper.ToDto(source);

    private static CustomerDto ToDto(Customer source) => CustomerMapper.ToDto(source);

    private static CustomerDetailDto ToDetailDto(Customer source) => CustomerMapper.ToDetailDto(source);

    private static CustomerConsentDto ToDto(CustomerConsent source) => CustomerMapper.ToDto(source);

    private static CustomerAllergyDto ToDto(CustomerAllergy source) => CustomerMapper.ToDto(source);

    private static CustomerNoteDto ToDto(CustomerNote source) => CustomerMapper.ToDto(source);

    private static EmployeeDto ToDto(Employee source) => EmployeeMapper.ToDto(source);

    private static EmployeeAvailabilityDto ToDto(EmployeeAvailability source) => EmployeeMapper.ToDto(source);

    private static EmployeeExceptionDto ToDto(EmployeeException source) => EmployeeMapper.ToDto(source);

    private static ServiceDto ToDto(Service source) => ServiceCatalogMapper.ToDto(source);

    private static ServiceDetailDto ToDetailDto(Service source) => ServiceCatalogMapper.ToDetailDto(source);

    private static ServiceVariationDto ToDto(ServiceVariation source) => ServiceCatalogMapper.ToDto(source);

    private static ServicePricingDto ToDto(ServicePricing source) => ServiceCatalogMapper.ToDto(source);

    private static ServiceCategoryDto ToDto(ServiceCategory source) => ServiceCatalogMapper.ToDto(source);

    private static ServicePackageItemDto ToDto(ServicePackageItem source) => ServiceCatalogMapper.ToDto(source);

    #endregion

    private static readonly Guid OrgId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateTime Created = new(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
    private static readonly DateTime Updated = new(2026, 4, 5, 6, 7, 8, DateTimeKind.Utc);
    private static readonly DateTime AllergyTest = new(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc);

    // ── Citas ─────────────────────────────────────────────────────────────

    [Fact]
    public void Appointment_se_mapea_campo_a_campo_sin_datos_de_Redsys_ni_de_tenant()
    {
        var dto = ToDto(new Appointment
        {
            Id = 31,
            OrganizationId = OrgId,
            CustomerId = 32,
            EmployeeId = 33,
            AppointmentDate = new DateOnly(2026, 10, 14),
            StartTime = new TimeOnly(10, 15),
            EndTime = new TimeOnly(11, 45),
            Status = AppointmentStatuses.CancelledByCustomer,
            TotalPrice = 85.50m,
            DepositAmount = 20.25m,
            RedsysOrderNumber = "ORDER-NO-DEBE-SALIR",
            RedsysPreAuthToken = "TOKEN-NO-DEBE-SALIR",
            CancellationReason = "Viaje imprevisto",
            CancelledAt = new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc),
            CancelledById = 34,
            CancelledByType = AppointmentCancelledByTypes.Customer,
            Notes = "Primera visita",
            IsActive = true,
            CreatedAt = Created,
            CreatedById = 35,
            UpdatedAt = Updated,
        });

        dto.Should().BeEquivalentTo(new AppointmentDto
        {
            Id = 31,
            CustomerId = 32,
            EmployeeId = 33,
            AppointmentDate = new DateOnly(2026, 10, 14),
            StartTime = new TimeOnly(10, 15),
            EndTime = new TimeOnly(11, 45),
            Status = AppointmentStatuses.CancelledByCustomer,
            TotalPrice = 85.50m,
            DepositAmount = 20.25m,
            CancellationReason = "Viaje imprevisto",
            CancelledAt = new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc),
            CancelledById = 34,
            CancelledByType = AppointmentCancelledByTypes.Customer,
            Notes = "Primera visita",
            IsActive = true,
            CreatedAt = Created,
            CreatedById = 35,
            UpdatedAt = Updated,
        });
    }

    [Fact]
    public void Appointment_en_la_agenda_lleva_los_nombres_de_clienta_y_empleada()
    {
        var dto = AppointmentMapper.ToSummaryDto(AppointmentWithRelations());

        dto.Should().BeEquivalentTo(new AppointmentSummaryDto
        {
            Id = 41,
            CustomerId = 42,
            EmployeeId = 43,
            AppointmentDate = new DateOnly(2026, 11, 2),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            Status = AppointmentStatuses.Confirmed,
            TotalPrice = 70m,
            IsActive = true,
            CreatedAt = Created,
            CreatedById = 44,
            CustomerName = "Carmen López",
            EmployeeName = "María García",
        });
    }

    [Fact]
    public void Appointment_en_la_ficha_lleva_las_lineas_en_su_orden_con_sus_nombres()
    {
        var dto = AppointmentMapper.ToDetailDto(AppointmentWithRelations());

        dto.CustomerName.Should().Be("Carmen López");
        dto.EmployeeName.Should().Be("María García");
        dto.CreatedById.Should().Be(44);
        dto.Items.Should().BeEquivalentTo(
            new[]
            {
                new AppointmentServiceItemDto
                {
                    ServiceId = 51,
                    ServiceName = "Diseño de cejas",
                    ServiceVariationId = 61,
                    ServiceVariationName = "Con hilo",
                    Price = 30m,
                    DurationMinutes = 45,
                    Order = 1,
                },
                new AppointmentServiceItemDto
                {
                    ServiceId = 52,
                    ServiceName = "Tinte",
                    ServiceVariationId = null,
                    ServiceVariationName = null,
                    Price = 40m,
                    DurationMinutes = 45,
                    Order = 2,
                },
            },
            options => options.WithStrictOrdering());
    }

    /// <summary>Cita con clienta, empleada y dos líneas guardadas fuera de orden.</summary>
    private static Appointment AppointmentWithRelations()
    {
        var diseno = new Service { Id = 51, Name = "Diseño de cejas" };
        var tinte = new Service { Id = 52, Name = "Tinte" };

        return new Appointment
        {
            Id = 41,
            OrganizationId = OrgId,
            CustomerId = 42,
            EmployeeId = 43,
            AppointmentDate = new DateOnly(2026, 11, 2),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            Status = AppointmentStatuses.Confirmed,
            TotalPrice = 70m,
            IsActive = true,
            CreatedAt = Created,
            CreatedById = 44,
            Customer = new Customer { Id = 42, FirstName = "Carmen", LastName = "López" },
            Employee = new Employee { Id = 43, FirstName = "María", LastName = "García" },
            ServiceItems =
            {
                new AppointmentServiceItem
                {
                    Id = 2, ServiceId = 52, Service = tinte, Price = 40m, DurationMinutes = 45, Order = 2,
                },
                new AppointmentServiceItem
                {
                    Id = 1, ServiceId = 51, Service = diseno, ServiceVariationId = 61,
                    ServiceVariation = new ServiceVariation { Id = 61, Name = "Con hilo" },
                    Price = 30m, DurationMinutes = 45, Order = 1,
                },
            },
        };
    }

    [Fact]
    public void Appointment_con_los_opcionales_vacios_los_deja_nulos()
    {
        var dto = ToDto(new Appointment { Id = 1, Status = AppointmentStatuses.Pending });

        dto.CancellationReason.Should().BeNull();
        dto.CancelledAt.Should().BeNull();
        dto.CancelledById.Should().BeNull();
        dto.CancelledByType.Should().BeNull();
        dto.Notes.Should().BeNull();
        dto.CreatedById.Should().BeNull();
        dto.UpdatedAt.Should().BeNull();
    }

    // ── Clientes ──────────────────────────────────────────────────────────

    private static Customer FullCustomer() => new()
    {
        Id = 41,
        OrganizationId = OrgId,
        FirstName = "Carmen",
        LastName = "López Ruiz",
        Email = "carmen@example.com",
        Phone = "+34600111222",
        ProfileImageUrl = "https://img.example/carmen.png",
        BirthDate = new DateOnly(1990, 7, 8),
        Category = CustomerCategories.Vip,
        LoyaltyPoints = 120,
        IsBlocked = true,
        BlockedReason = "Tres no-shows",
        PreferredContactMethod = "whatsapp",
        LastAllergyTestAt = AllergyTest,
        IsActive = true,
        CreatedAt = Created,
        UpdatedAt = Updated,
    };

    private static CustomerDto ExpectedCustomerDto() => new()
    {
        Id = 41,
        FirstName = "Carmen",
        LastName = "López Ruiz",
        FullName = "Carmen López Ruiz",
        Email = "carmen@example.com",
        Phone = "+34600111222",
        ProfileImageUrl = "https://img.example/carmen.png",
        BirthDate = new DateOnly(1990, 7, 8),
        Category = CustomerCategories.Vip,
        LoyaltyPoints = 120,
        IsBlocked = true,
        BlockedReason = "Tres no-shows",
        PreferredContactMethod = "whatsapp",
        LastAllergyTestAt = AllergyTest,
        IsActive = true,
        CreatedAt = Created,
        UpdatedAt = Updated,
    };

    [Fact]
    public void Customer_se_mapea_campo_a_campo_con_el_nombre_completo()
    {
        ToDto(FullCustomer()).Should().BeEquivalentTo(ExpectedCustomerDto());
    }

    [Fact]
    public void El_detalle_de_Customer_lleva_la_ficha_y_sus_tres_colecciones_en_orden()
    {
        var customer = FullCustomer();
        customer.Consents.Add(new CustomerConsent
        {
            Id = 51,
            ConsentType = CustomerConsentTypes.DataProcessing,
            IsGranted = true,
            GrantedAt = Created,
        });
        customer.Consents.Add(new CustomerConsent
        {
            Id = 52,
            ConsentType = CustomerConsentTypes.Marketing,
            IsGranted = false,
            RevokedAt = Updated,
        });
        customer.Allergies.Add(new CustomerAllergy
        {
            Id = 53,
            AllergyDescription = "Látex",
            Severity = AllergySeverities.High,
        });
        customer.Allergies.Add(new CustomerAllergy
        {
            Id = 54,
            AllergyDescription = "Níquel",
            Severity = AllergySeverities.Low,
        });
        customer.Notes.Add(new CustomerNote { Id = 55, EmployeeId = 2, Note = "Prefiere tardes", CreatedAt = Created });
        customer.Notes.Add(new CustomerNote { Id = 56, EmployeeId = 3, Note = "Piel sensible", CreatedAt = Updated });

        var expected = new CustomerDetailDto
        {
            Id = 41,
            FirstName = "Carmen",
            LastName = "López Ruiz",
            FullName = "Carmen López Ruiz",
            Email = "carmen@example.com",
            Phone = "+34600111222",
            ProfileImageUrl = "https://img.example/carmen.png",
            BirthDate = new DateOnly(1990, 7, 8),
            Category = CustomerCategories.Vip,
            LoyaltyPoints = 120,
            IsBlocked = true,
            BlockedReason = "Tres no-shows",
            PreferredContactMethod = "whatsapp",
            LastAllergyTestAt = AllergyTest,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
            Consents =
            [
                new() { ConsentType = CustomerConsentTypes.DataProcessing, IsGranted = true, GrantedAt = Created },
                new() { ConsentType = CustomerConsentTypes.Marketing, IsGranted = false, RevokedAt = Updated },
            ],
            Allergies =
            [
                new() { Id = 53, AllergyDescription = "Látex", Severity = AllergySeverities.High },
                new() { Id = 54, AllergyDescription = "Níquel", Severity = AllergySeverities.Low },
            ],
            Notes =
            [
                new() { Id = 55, EmployeeId = 2, Note = "Prefiere tardes", CreatedAt = Created },
                new() { Id = 56, EmployeeId = 3, Note = "Piel sensible", CreatedAt = Updated },
            ],
        };

        ToDetailDto(customer).Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
    }

    [Fact]
    public void El_detalle_de_Customer_sin_colecciones_las_devuelve_vacias_y_no_nulas()
    {
        var dto = ToDetailDto(FullCustomer());

        dto.Consents.Should().NotBeNull().And.BeEmpty();
        dto.Allergies.Should().NotBeNull().And.BeEmpty();
        dto.Notes.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("María", "", "María")]
    [InlineData("", "López", "López")]
    [InlineData("", "", "")]
    [InlineData("Ana María", "de la Fuente", "Ana María de la Fuente")]
    public void El_nombre_completo_de_Customer_no_deja_espacios_sueltos(string first, string last, string full)
    {
        ToDto(new Customer { FirstName = first, LastName = last }).FullName.Should().Be(full);
        ToDetailDto(new Customer { FirstName = first, LastName = last }).FullName.Should().Be(full);
    }

    [Fact]
    public void CustomerConsent_se_mapea_sin_id_ni_datos_de_auditoria()
    {
        ToDto(new CustomerConsent
        {
            Id = 61,
            OrganizationId = OrgId,
            CustomerId = 62,
            ConsentType = CustomerConsentTypes.Photos,
            IsGranted = true,
            GrantedAt = Created,
            RevokedAt = Updated,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new CustomerConsentDto
        {
            ConsentType = CustomerConsentTypes.Photos,
            IsGranted = true,
            GrantedAt = Created,
            RevokedAt = Updated,
        });
    }

    [Fact]
    public void CustomerAllergy_se_mapea_campo_a_campo()
    {
        ToDto(new CustomerAllergy
        {
            Id = 71,
            OrganizationId = OrgId,
            CustomerId = 72,
            AllergyDescription = "Tinte PPD",
            Severity = AllergySeverities.Medium,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new CustomerAllergyDto
        {
            Id = 71,
            AllergyDescription = "Tinte PPD",
            Severity = AllergySeverities.Medium,
        });
    }

    [Fact]
    public void CustomerNote_se_mapea_con_su_autora_y_su_fecha()
    {
        ToDto(new CustomerNote
        {
            Id = 81,
            OrganizationId = OrgId,
            CustomerId = 82,
            EmployeeId = 83,
            Note = "Alergia confirmada en la prueba",
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new CustomerNoteDto
        {
            Id = 81,
            Note = "Alergia confirmada en la prueba",
            EmployeeId = 83,
            CreatedAt = Created,
        });
    }

    // ── Empleados ─────────────────────────────────────────────────────────

    [Fact]
    public void Employee_se_mapea_campo_a_campo_con_el_nombre_completo()
    {
        ToDto(new Employee
        {
            Id = 91,
            OrganizationId = OrgId,
            FirstName = "Lucía",
            LastName = "Martínez",
            Email = "lucia@reservarte.com",
            Phone = "+34600333444",
            Rol = Roles.Manager,
            ProfileImageUrl = "https://img.example/lucia.png",
            HireDate = new DateOnly(2024, 1, 15),
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new EmployeeDto
        {
            Id = 91,
            FirstName = "Lucía",
            LastName = "Martínez",
            FullName = "Lucía Martínez",
            Email = "lucia@reservarte.com",
            Phone = "+34600333444",
            Rol = Roles.Manager,
            ProfileImageUrl = "https://img.example/lucia.png",
            HireDate = new DateOnly(2024, 1, 15),
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        });
    }

    [Theory]
    [InlineData("María", "", "María")]
    [InlineData("", "García", "García")]
    [InlineData("", "", "")]
    public void El_nombre_completo_de_Employee_no_deja_espacios_sueltos(string first, string last, string full)
    {
        ToDto(new Employee { FirstName = first, LastName = last }).FullName.Should().Be(full);
    }

    [Fact]
    public void EmployeeAvailability_se_mapea_campo_a_campo()
    {
        ToDto(new EmployeeAvailability
        {
            Id = 101,
            OrganizationId = OrgId,
            EmployeeId = 102,
            DayOfWeek = 6,
            StartTime = new TimeOnly(9, 30),
            EndTime = new TimeOnly(14, 0),
            IsRecurring = true,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new EmployeeAvailabilityDto
        {
            Id = 101,
            DayOfWeek = 6,
            StartTime = new TimeOnly(9, 30),
            EndTime = new TimeOnly(14, 0),
            IsRecurring = true,
            IsActive = true,
        });
    }

    [Fact]
    public void EmployeeException_se_mapea_campo_a_campo()
    {
        ToDto(new EmployeeException
        {
            Id = 111,
            OrganizationId = OrgId,
            EmployeeId = 112,
            StartDateTime = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            Reason = "Vacaciones de verano",
            Type = EmployeeExceptionTypes.Vacation,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new EmployeeExceptionDto
        {
            Id = 111,
            StartDateTime = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            Type = EmployeeExceptionTypes.Vacation,
            Reason = "Vacaciones de verano",
            IsActive = true,
        });
    }

    [Fact]
    public void EmployeeServiceAssignment_lleva_el_nombre_la_duracion_y_el_estado_del_servicio()
    {
        EmployeeMapper.ToDto(new EmployeeServiceAssignment
        {
            OrganizationId = OrgId,
            EmployeeId = 113,
            ServiceId = 114,
            ProficiencyLevel = 3,
            IsActive = true,
            Service = new Service
            {
                Id = 114,
                OrganizationId = OrgId,
                Name = "Henna de cejas",
                DurationMinutes = 40,
                IsActive = false,
            },
        }).Should().BeEquivalentTo(new EmployeeServiceDto
        {
            ServiceId = 114,
            Name = "Henna de cejas",
            DurationMinutes = 40,
            ProficiencyLevel = 3,
            ServiceIsActive = false,
        });
    }

    // ── Catálogo de servicios ─────────────────────────────────────────────

    private static Service FullService() => new()
    {
        Id = 121,
        OrganizationId = OrgId,
        Name = "Microblading",
        Description = "Técnica pelo a pelo",
        DurationMinutes = 150,
        BasePrice = 250.75m,
        CategoryId = 122,
        Category = new ServiceCategory { Id = 122, Name = "Cejas" },
        ImageUrl = "https://img.example/microblading.png",
        IsActive = true,
        RequiresAllergyTest = true,
        AllergyTestHoursBefore = 48,
        CreatedAt = Created,
        UpdatedAt = Updated,
    };

    [Fact]
    public void Service_se_mapea_campo_a_campo_con_el_nombre_de_su_categoria()
    {
        ToDto(FullService()).Should().BeEquivalentTo(new ServiceDto
        {
            Id = 121,
            Name = "Microblading",
            Description = "Técnica pelo a pelo",
            DurationMinutes = 150,
            BasePrice = 250.75m,
            CategoryId = 122,
            CategoryName = "Cejas",
            ImageUrl = "https://img.example/microblading.png",
            RequiresAllergyTest = true,
            AllergyTestHoursBefore = 48,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        });
    }

    [Fact]
    public void Un_servicio_sin_categoria_cargada_deja_el_nombre_nulo_en_el_listado_y_en_el_detalle()
    {
        var service = FullService();
        service.Category = null;

        ToDto(service).CategoryName.Should().BeNull();
        ToDto(service).CategoryId.Should().Be(122);
        ToDetailDto(service).CategoryName.Should().BeNull();
    }

    [Fact]
    public void El_detalle_de_Service_lleva_variaciones_y_tarifas_en_orden()
    {
        var service = FullService();
        service.Variations.Add(new ServiceVariation { Id = 131, Name = "Retoque", PriceModifier = -50.5m, DurationModifier = -30 });
        service.Variations.Add(new ServiceVariation { Id = 132, Name = "Doble", PriceModifier = 80m, DurationModifier = 45 });
        service.Pricings.Add(new ServicePricing { Id = 133, EmployeeLevel = EmployeeLevels.Junior, Price = 200m });
        service.Pricings.Add(new ServicePricing { Id = 134, EmployeeLevel = EmployeeLevels.Expert, Price = 300.99m });

        ToDetailDto(service).Should().BeEquivalentTo(new ServiceDetailDto
        {
            Id = 121,
            Name = "Microblading",
            Description = "Técnica pelo a pelo",
            DurationMinutes = 150,
            BasePrice = 250.75m,
            CategoryId = 122,
            CategoryName = "Cejas",
            ImageUrl = "https://img.example/microblading.png",
            RequiresAllergyTest = true,
            AllergyTestHoursBefore = 48,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
            Variations =
            [
                new() { Id = 131, Name = "Retoque", PriceModifier = -50.5m, DurationModifier = -30 },
                new() { Id = 132, Name = "Doble", PriceModifier = 80m, DurationModifier = 45 },
            ],
            Pricings =
            [
                new() { Id = 133, EmployeeLevel = EmployeeLevels.Junior, Price = 200m },
                new() { Id = 134, EmployeeLevel = EmployeeLevels.Expert, Price = 300.99m },
            ],
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void El_detalle_de_Service_sin_colecciones_las_devuelve_vacias_y_no_nulas()
    {
        var dto = ToDetailDto(FullService());

        dto.Variations.Should().NotBeNull().And.BeEmpty();
        dto.Pricings.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ServiceVariation_se_mapea_campo_a_campo()
    {
        ToDto(new ServiceVariation
        {
            Id = 141,
            OrganizationId = OrgId,
            ServiceId = 142,
            Name = "Express",
            PriceModifier = 12.34m,
            DurationModifier = -15,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new ServiceVariationDto
        {
            Id = 141,
            Name = "Express",
            PriceModifier = 12.34m,
            DurationModifier = -15,
        });
    }

    [Fact]
    public void ServicePricing_se_mapea_campo_a_campo()
    {
        ToDto(new ServicePricing
        {
            Id = 151,
            OrganizationId = OrgId,
            ServiceId = 152,
            EmployeeLevel = EmployeeLevels.Senior,
            Price = 99.95m,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new ServicePricingDto
        {
            Id = 151,
            EmployeeLevel = EmployeeLevels.Senior,
            Price = 99.95m,
        });
    }

    [Fact]
    public void ServiceCategory_se_mapea_campo_a_campo()
    {
        ToDto(new ServiceCategory
        {
            Id = 161,
            OrganizationId = OrgId,
            Name = "Pestañas",
            Description = "Lifting y extensiones",
            Color = "#E91E63",
            DisplayOrder = 3,
            IsActive = true,
            CreatedAt = Created,
            UpdatedAt = Updated,
        }).Should().BeEquivalentTo(new ServiceCategoryDto
        {
            Id = 161,
            Name = "Pestañas",
            Description = "Lifting y extensiones",
            Color = "#E91E63",
            DisplayOrder = 3,
            IsActive = true,
        });
    }

    [Fact]
    public void ServicePackageItem_trae_del_servicio_su_nombre_precio_base_y_duracion()
    {
        ToDto(new ServicePackageItem
        {
            Id = 171,
            OrganizationId = OrgId,
            ServicePackageId = 172,
            ServiceId = 173,
            Order = 2,
            IsActive = true,
            Service = new Service { Id = 173, Name = "Lifting", BasePrice = 45.5m, DurationMinutes = 60 },
        }).Should().BeEquivalentTo(new ServicePackageItemDto
        {
            Id = 171,
            ServiceId = 173,
            ServiceName = "Lifting",
            BasePrice = 45.5m,
            DurationMinutes = 60,
            Order = 2,
        });
    }
}
