using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NestU.API.Entities;

namespace NestU.API.Data.Configurations;

public class UtilityReadingConfiguration : IEntityTypeConfiguration<UtilityReading>
{
    public void Configure(EntityTypeBuilder<UtilityReading> b)
    {
        b.ToTable("UtilityReadings", t =>
        {
            t.HasCheckConstraint("CK_UtilityReading_Month", "[Month] BETWEEN 1 AND 12");
            t.HasCheckConstraint("CK_UtilityReading_Index", "[ElectricIndex] >= 0 AND [WaterIndex] >= 0");
        });
        // Mỗi hợp đồng một lần ghi chỉ số mỗi tháng
        b.HasIndex(x => new { x.ContractId, x.Year, x.Month }).IsUnique();
        b.HasOne(x => x.Contract).WithMany(c => c.Readings).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices", t =>
        {
            t.HasCheckConstraint("CK_Invoice_Month", "[Month] BETWEEN 1 AND 12");
            t.HasCheckConstraint("CK_Invoice_Total", "[Total] >= 0");
        });
        b.Ignore(x => x.DisplayStatus);
        b.Property(x => x.RejectReason).HasMaxLength(500);
        // Mỗi hợp đồng một hóa đơn mỗi tháng
        b.HasIndex(x => new { x.ContractId, x.Year, x.Month }).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasOne(x => x.Contract).WithMany(c => c.Invoices).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvoiceDetailConfiguration : IEntityTypeConfiguration<InvoiceDetail>
{
    public void Configure(EntityTypeBuilder<InvoiceDetail> b)
    {
        b.Property(x => x.ItemName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(20);
        b.HasOne(x => x.Invoice).WithMany(i => i.Details).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments", t => t.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));
        b.Property(x => x.ProofImageUrl).HasMaxLength(500);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Invoice).WithMany(i => i.Payments).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.ResolveNote).HasMaxLength(500);
        b.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RoomAsset).WithMany().HasForeignKey(x => x.RoomAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("Reviews", t => t.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5"));
        b.Property(x => x.Comment).HasMaxLength(1000);
        // Mỗi hợp đồng đánh giá một lần
        b.HasIndex(x => x.ContractId).IsUnique();
        b.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.ResolveNote).HasMaxLength(500);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.ReporterId, x.TargetType, x.TargetId }).IsUnique().HasFilter("[Status] = 'Open'")
            .HasDatabaseName("UX_Report_OneOpenPerReporterAndTarget");
        b.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
    }
}
