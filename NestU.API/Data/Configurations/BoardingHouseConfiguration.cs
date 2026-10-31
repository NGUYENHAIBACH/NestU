using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NestU.API.Entities;

namespace NestU.API.Data.Configurations;

public class BoardingHouseConfiguration : IEntityTypeConfiguration<BoardingHouse>
{
    public void Configure(EntityTypeBuilder<BoardingHouse> b)
    {
        b.ToTable("BoardingHouses", t =>
        {
            // Quy định 2: giá điện tối đa 4.000đ/kWh
            t.HasCheckConstraint("CK_BoardingHouse_ElectricityPrice", "[ElectricityPrice] > 0 AND [ElectricityPrice] <= 4000");
            t.HasCheckConstraint("CK_BoardingHouse_WaterPrice", "[WaterPrice] > 0");
            t.HasCheckConstraint("CK_BoardingHouse_TotalFloors", "[TotalFloors] >= 1 AND [TotalFloors] <= 50");
        });
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Address).HasMaxLength(300).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.BankCode).HasMaxLength(20);
        b.Property(x => x.BankAccount).HasMaxLength(30);
        b.Property(x => x.AccountName).HasMaxLength(100);
        b.HasIndex(x => x.Status);
        b.HasOne(x => x.Landlord).WithMany().HasForeignKey(x => x.LandlordId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Area).WithMany(a => a.BoardingHouses).HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FireSafetyCertificateConfiguration : IEntityTypeConfiguration<FireSafetyCertificate>
{
    public void Configure(EntityTypeBuilder<FireSafetyCertificate> b)
    {
        b.Property(x => x.CertificateNo).HasMaxLength(50).IsRequired();
        b.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ReviewNote).HasMaxLength(500);
        // Quy định 1: mỗi nhà trọ tối đa một giấy Approved
        b.HasIndex(x => x.BoardingHouseId).IsUnique().HasFilter("[Status] = 'Approved'")
            .HasDatabaseName("UX_Certificate_OneApprovedPerHouse");
        b.HasOne(x => x.BoardingHouse).WithMany(h => h.Certificates).HasForeignKey(x => x.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("Services", t => t.HasCheckConstraint("CK_Service_Price", "[Price] >= 0"));
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasOne(x => x.BoardingHouse).WithMany(h => h.Services).HasForeignKey(x => x.BoardingHouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
