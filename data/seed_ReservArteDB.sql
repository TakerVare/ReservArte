-- =============================================================================
-- ReservArte: datos iniciales / pruebas (DML)
-- Requisito: haber ejecutado antes create_ReservArteDB.sql
-- Cambios v2:
--   - Contraseñas en texto plano mantenidas solo para seed de desarrollo.
--     La aplicación siempre guardará hashes BCrypt, nunca texto plano.
--   - INSERT de Configuration sin Id (usa DEFAULT 1 del singleton)
-- =============================================================================

USE ReservArteDB;
GO

-- ============================================
-- DATOS INICIALES (para pruebas)
-- ============================================

-- Configuration: INSERT sin Id, la tabla solo admite una fila (Id DEFAULT 1)
INSERT INTO Configuration (CompanyName, CompanyLogo) VALUES
('More Than Brows', 'https://res.cloudinary.com/dn66z1z2i/image/upload/v1778008245/Logo_Recto_More_Than_Brows_SIN_fondo_styoh8.png');

-- USUARIOS (para autenticación). Id 1=admin, 2-3=empleados, 4-6=clientes.
-- AVISO: contraseñas en texto plano solo válidas para entorno de desarrollo/seed.
--        En producción el campo contendrá siempre un hash BCrypt.
INSERT INTO Users (FirstName, LastName, Email, Password, Rol, Phone) VALUES
('Guillermo', 'Admin', 'guille@svalero.com', 'Admin1234!', 'admin', '+34600000001'),
('María', 'García', 'maria.garcia@reservarte.com', 'Maria123!', 'employee', '+34600000002'),
('Laura', 'Martínez', 'laura.martinez@reservarte.com', 'Laura123!', 'employee', '+34600000003'),
('Ana', 'López', 'ana.lopez@email.com', 'Cliente123!', 'client', '+34600000004'),
('Carmen', 'Rodríguez', 'carmen.rodriguez@email.com', 'Cliente123!', 'client', '+34600000005'),
('Isabel', 'Sánchez', 'isabel.sanchez@email.com', 'Cliente123!', 'client', '+34600000006');

-- EMPLOYEES: Id = User.Id (2=María, 3=Laura)
INSERT INTO Employees (Id, FirstName, LastName, Email, Phone, Rol, HireDate, IsActive) VALUES
(2, 'María', 'García', 'maria.garcia@reservarte.com', '+34600000002', 'employee', '2023-01-15', 1),
(3, 'Laura', 'Martínez', 'laura.martinez@reservarte.com', '+34600000003', 'employee', '2023-03-01', 1);

-- CUSTOMERS: Id = User.Id (4=Ana, 5=Carmen, 6=Isabel)
INSERT INTO Customers (Id, FirstName, LastName, Email, Phone, Rol, Category, LoyaltyPoints, PreferredContactMethod, MarketingConsent) VALUES
(4, 'Ana', 'López', 'ana.lopez@email.com', '+34600000004', 'client', 'regular', 0, 'email', 1),
(5, 'Carmen', 'Rodríguez', 'carmen.rodriguez@email.com', '+34600000005', 'client', 'vip', 150, 'whatsapp', 1),
(6, 'Isabel', 'Sánchez', 'isabel.sanchez@email.com', '+34600000006', 'client', 'regular', 50, 'email', 0);

-- CONSENTIMIENTOS: SavedCards concedido para clientes de prueba (permite guardar tarjetas)
INSERT INTO CustomerConsents (CustomerId, ConsentType, IsGranted, GrantedAt) VALUES
(4, 'SavedCards', 1, GETUTCDATE()),
(5, 'SavedCards', 1, GETUTCDATE()),
(6, 'SavedCards', 1, GETUTCDATE());

-- CATEGORÍA DE SERVICIOS: solo Diseño de cejas (todos los servicios ofertados son de este tipo)
INSERT INTO ServiceCategories (Name, Description, Color, DisplayOrder, IsActive) VALUES
('Diseño de cejas', 'Servicios de diseño, perfilado y tratamiento de cejas', '#E8B4BC', 1, 1);

-- SERVICIOS: todos de diseño de cejas, duración 45 minutos
INSERT INTO Services (Name, Description, DurationMinutes, BasePrice, CategoryId, IsActive, RequiresAllergyTest) VALUES
('Diseño de cejas clásico', 'Perfilado y diseño de cejas con pinza y cera', 45, 18.00, 1, 1, 0),
('Diseño de cejas con tinte', 'Diseño y tinte de cejas para definir y dar color', 45, 22.00, 1, 1, 1),
('Laminado de cejas', 'Tratamiento para fijar y dar forma al vello de las cejas', 45, 25.00, 1, 1, 0),
('Diseño + henna cejas', 'Diseño de cejas con aplicación de henna', 45, 20.00, 1, 1, 0),
('Microblading simulación', 'Diseño y relleno con efecto microblading (no permanente)', 45, 28.00, 1, 1, 0),
('Diseño de cejas premium', 'Diseño completo con limpieza y acabado profesional', 45, 30.00, 1, 1, 0);

-- VARIACIONES DE SERVICIOS (ServiceVariations) — sin modificar duración para mantener 45 min
-- ServiceId: 1=Diseño clásico, 2=Con tinte, 3=Laminado, 4=Henna, 5=Microblading simulación, 6=Premium
INSERT INTO ServiceVariations (ServiceId, Name, PriceModifier, DurationModifier, IsActive) VALUES
(1, N'Estándar', 0, 0, 1),
(1, N'Con limpieza previa', 3.00, 0, 1),
(2, N'Tinte solo', 0, 0, 1),
(2, N'Tinte + diseño', 2.00, 0, 1),
(3, N'Laminado básico', 0, 0, 1),
(3, N'Laminado + diseño', 3.00, 0, 1),
(4, N'Henna natural', 0, 0, 1),
(4, N'Henna + diseño', 2.00, 0, 1),
(5, N'Simulación estándar', 0, 0, 1),
(5, N'Simulación + corrección', 5.00, 0, 1),
(6, N'Premium completo', 0, 0, 1),
(6, N'Premium + mascarilla', 4.00, 0, 1);

-- EMPLEADO-SERVICIO: una fila por cada combinación empleado × servicio (2 empleados × 6 servicios = 12 registros)
INSERT INTO EmployeeServices (EmployeeId, ServiceId, ProficiencyLevel, IsActive) VALUES
(2, 1, 1, 1), (2, 2, 1, 1), (2, 3, 1, 1), (2, 4, 1, 1), (2, 5, 1, 1), (2, 6, 1, 1),
(3, 1, 1, 1), (3, 2, 1, 1), (3, 3, 1, 1), (3, 4, 1, 1), (3, 5, 1, 1), (3, 6, 1, 1);

-- Sustituye el segundo INSERT del script monolítico (mismas claves PK): elevar nivel de competencia
UPDATE EmployeeServices
SET ProficiencyLevel = 3
WHERE EmployeeId IN (2, 3) AND ServiceId BETWEEN 1 AND 6;

-- DISPONIBILIDAD EMPLEADOS (EmployeeId 2=María, 3=Laura)
INSERT INTO EmployeeAvailabilities (EmployeeId, DayOfWeek, StartTime, EndTime, IsRecurring) VALUES
(2, 1, '09:00', '18:00', 1), -- María Lunes
(2, 2, '09:00', '18:00', 1), -- María Martes
(2, 3, '09:00', '18:00', 1), -- María Miércoles
(2, 4, '09:00', '18:00', 1), -- María Jueves
(2, 5, '09:00', '14:00', 1), -- María Viernes
(3, 1, '10:00', '19:00', 1), -- Laura Lunes
(3, 2, '10:00', '19:00', 1), -- Laura Martes
(3, 3, '10:00', '19:00', 1), -- Laura Miércoles
(3, 4, '10:00', '19:00', 1), -- Laura Jueves
(3, 5, '10:00', '15:00', 1); -- Laura Viernes

-- POLÍTICA DE CANCELACIÓN
INSERT INTO CancellationPolicies (MinHoursBeforeCancel, PenaltyPercentage, MaxNoShowsBeforeBlock, VipMinHoursBeforeCancel, VipPenaltyPercentage, IsActive) VALUES
(24, 50, 3, 12, 25, 1);

-- CITAS (Appointments) - datos de ejemplo
-- Clientes: 4=Ana López, 5=Carmen Rodríguez, 6=Isabel Sánchez
-- Empleadas: 2=María García, 3=Laura Martínez
INSERT INTO Appointments (CustomerId, EmployeeId, AppointmentDate, StartTime, EndTime, Status, TotalPrice, DepositAmount, Notes) VALUES
(4, 2, '2026-03-10', '10:00', '10:30', 'confirmed', 25.00, 0, N'Corte mujer - primera visita'),
(5, 2, '2026-03-10', '11:00', '13:00', 'confirmed', 65.00, 20.00, N'Tinte completo'),
(6, 3, '2026-03-10', '10:00', '10:20', 'pending', 15.00, 0, NULL),
(4, 3, '2026-03-11', '12:00', '12:45', 'pending', 35.00, 0, N'Hidratación profunda'),
(5, 2, '2026-03-11', '09:30', '10:00', 'pending', 25.00, 0, NULL),
(6, 3, '2026-03-12', '16:00', '16:30', 'completed', 25.00, 0, N'Corte realizado'),
(4, 2, '2026-03-12', '11:00', '13:30', 'completed', 85.00, 30.00, N'Mechas - cliente satisfecha'),
(6, 2, '2026-03-13', '12:00', '12:20', 'cancelled', 15.00, 0, NULL);

UPDATE Appointments
SET CancellationReason = N'Cliente no pudo asistir',
    CancelledAt = GETUTCDATE(),
    CancelledByType = 'customer',
    UpdatedAt = GETUTCDATE()
WHERE Id = 8;

-- Detalle de servicios por cita (AppointmentServiceItems). ServiceVariationId NULL = variación estándar.
INSERT INTO AppointmentServiceItems (AppointmentId, ServiceId, ServiceVariationId, Price, DurationMinutes, [Order]) VALUES
(1, 1, NULL, 25.00, 30, 1),
(2, 3, NULL, 65.00, 120, 1),
(3, 2, NULL, 15.00, 20, 1),
(4, 6, NULL, 35.00, 45, 1),
(5, 1, NULL, 25.00, 30, 1),
(6, 1, NULL, 25.00, 30, 1),
(7, 4, NULL, 85.00, 150, 1),
(8, 2, NULL, 15.00, 20, 1);

-- PAGOS (Payments) - datos de ejemplo (CustomerId 4=Ana, 5=Carmen, 6=Isabel)
INSERT INTO Payments (AppointmentId, CustomerId, Amount, Currency, PaymentMethodType, Status, RedsysOrderNumber, ProcessedAt) VALUES
(1, 4, 25.00, 'EUR', 'card', 'captured', 'DEMO-PAY-001', GETUTCDATE()),
(2, 5, 65.00, 'EUR', 'card', 'captured', 'DEMO-PAY-002', GETUTCDATE()),
(6, 6, 25.00, 'EUR', 'cash', 'captured', 'DEMO-PAY-003', GETUTCDATE()),
(7, 4, 85.00, 'EUR', 'card', 'captured', 'DEMO-PAY-004', GETUTCDATE());

PRINT 'Verificando tablas creadas:';
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;

PRINT 'Datos iniciales insertados correctamente (v2)';
GO