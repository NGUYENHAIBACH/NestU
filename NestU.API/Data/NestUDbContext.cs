using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NestU.API.Entities;

namespace NestU.API.Data;

public class NestUDbContext : IdentityDbContext<ApplicationUser>
{
    public NestUDbContext(DbContextOptions<NestUDbContext> options) : base(options) { }

    public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();
    public DbSet<LandlordProfile> LandlordProfiles => Set<LandlordProfile>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<BoardingHouse> BoardingHouses => Set<BoardingHouse>();
    public DbSet<FireSafetyCertificate> FireSafetyCertificates => Set<FireSafetyCertificate>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();
    public DbSet<RoomAsset> RoomAssets => Set<RoomAsset>();
    public DbSet<ContractAsset> ContractAssets => Set<ContractAsset>();
    public DbSet<ViewingAppointment> ViewingAppointments => Set<ViewingAppointment>();
    public DbSet<RentalRequest> RentalRequests => Set<RentalRequest>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractMember> ContractMembers => Set<ContractMember>();
    public DbSet<DepositSettlement> DepositSettlements => Set<DepositSettlement>();
    public DbSet<ContractChangeRequest> ContractChangeRequests => Set<ContractChangeRequest>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ContractService> ContractServices => Set<ContractService>();
    public DbSet<UtilityReading> UtilityReadings => Set<UtilityReading>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Mọi decimal là tiền hoặc diện tích: 18,2 là đủ
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        // Enum lưu dạng chuỗi để filtered index "WHERE Status = 'Active'" đọc được
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(NestUDbContext).Assembly);

        // SQL Server không cho nhiều đường cascade tới cùng một bảng.
        // Mặc định mọi FK là Restrict; các bảng con thuần tuý bật Cascade riêng trong Configuration.
        foreach (var fk in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            var ns = fk.DeclaringEntityType.ClrType.Namespace ?? string.Empty;
            if (ns.StartsWith("Microsoft.AspNetCore.Identity")) continue; // giữ nguyên bảng Identity
            if (fk.DeleteBehavior == DeleteBehavior.Cascade && !fk.IsOwnership && !IsChildCascade(fk))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static bool IsChildCascade(Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey fk)
    {
        var dependent = fk.DeclaringEntityType.ClrType;
        var principal = fk.PrincipalEntityType.ClrType;
        return (dependent, principal) switch
        {
            (var d, var p) when d == typeof(TenantProfile) && p == typeof(ApplicationUser) => true,
            (var d, var p) when d == typeof(LandlordProfile) && p == typeof(ApplicationUser) => true,
            (var d, var p) when d == typeof(DepositSettlement) && p == typeof(Contract) => true,
            (var d, var p) when d == typeof(UtilityReading) && p == typeof(Contract) => true,
            (var d, var p) when d == typeof(Invoice) && p == typeof(Contract) => true,
            (var d, var p) when d == typeof(RoomImage) && p == typeof(Room) => true,
            (var d, var p) when d == typeof(RoomAmenity) && p == typeof(Room) => true,
            (var d, var p) when d == typeof(RoomAsset) && p == typeof(Room) => true,
            (var d, var p) when d == typeof(ContractMember) && p == typeof(Contract) => true,
            (var d, var p) when d == typeof(ContractAsset) && p == typeof(Contract) => true,
            (var d, var p) when d == typeof(ContractService) && p == typeof(Contract) => true,
            (var d, var p) when d == typeof(InvoiceDetail) && p == typeof(Invoice) => true,
            (var d, var p) when d == typeof(Payment) && p == typeof(Invoice) => true,
            (var d, var p) when d == typeof(FireSafetyCertificate) && p == typeof(BoardingHouse) => true,
            (var d, var p) when d == typeof(Service) && p == typeof(BoardingHouse) => true,
            _ => false
        };
    }
}
