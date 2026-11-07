using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NestU.API.Entities;

namespace NestU.API.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms", t =>
        {
            t.HasCheckConstraint("CK_Room_Floor", "[Floor] >= 1");
            t.HasCheckConstraint("CK_Room_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Room_DepositAmount", "[DepositAmount] >= 0");
            t.HasCheckConstraint("CK_Room_Area", "[Area] > 0");
            t.HasCheckConstraint("CK_Room_Capacity", "[Capacity] >= 1 AND [Capacity] <= 10");
        });
        b.Property(x => x.RoomNumber).HasMaxLength(10).IsRequired();
        b.Property(x => x.Address).HasMaxLength(255);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion();
        // Số phòng không trùng trong một nhà trọ
        b.HasIndex(x => new { x.BoardingHouseId, x.RoomNumber }).IsUnique();
        b.HasIndex(x => new { x.BoardingHouseId, x.Floor });
        b.HasOne(x => x.BoardingHouse).WithMany(h => h.Rooms).HasForeignKey(x => x.BoardingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoomImageConfiguration : IEntityTypeConfiguration<RoomImage>
{
    public void Configure(EntityTypeBuilder<RoomImage> b)
    {
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Room).WithMany(r => r.Images).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoomAmenityConfiguration : IEntityTypeConfiguration<RoomAmenity>
{
    public void Configure(EntityTypeBuilder<RoomAmenity> b)
    {
        b.HasKey(x => new { x.RoomId, x.AmenityId });
        b.HasOne(x => x.Room).WithMany(r => r.RoomAmenities).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Amenity).WithMany(a => a.RoomAmenities).HasForeignKey(x => x.AmenityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoomAssetConfiguration : IEntityTypeConfiguration<RoomAsset>
{
    public void Configure(EntityTypeBuilder<RoomAsset> b)
    {
        b.ToTable("RoomAssets", t =>
        {
            t.HasCheckConstraint("CK_RoomAsset_Quantity", "[Quantity] >= 1");
            t.HasCheckConstraint("CK_RoomAsset_EstimatedValue", "[EstimatedValue] >= 0");
        });
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Room).WithMany(r => r.Assets).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AssetType).WithMany().HasForeignKey(x => x.AssetTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContractAssetConfiguration : IEntityTypeConfiguration<ContractAsset>
{
    public void Configure(EntityTypeBuilder<ContractAsset> b)
    {
        b.ToTable("ContractAssets", t =>
        {
            t.HasCheckConstraint("CK_ContractAsset_Quantity", "[Quantity] >= 0");
            t.HasCheckConstraint("CK_ContractAsset_Deduction", "[DeductionAmount] >= 0");
        });
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);
        // Mỗi hợp đồng một dòng tài sản mỗi giai đoạn
        b.HasIndex(x => new { x.ContractId, x.RoomAssetId, x.Stage }).IsUnique();
        b.HasOne(x => x.Contract).WithMany(c => c.Assets).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.RoomAsset).WithMany().HasForeignKey(x => x.RoomAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
