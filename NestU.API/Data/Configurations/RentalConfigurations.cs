using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NestU.API.Entities;

namespace NestU.API.Data.Configurations;

public class ViewingAppointmentConfiguration : IEntityTypeConfiguration<ViewingAppointment>
{
    public void Configure(EntityTypeBuilder<ViewingAppointment> b)
    {
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.RejectReason).HasMaxLength(500);
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.RoomId }).IsUnique()
            .HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("UX_Appointment_OnePendingPerRoomAndTenant");
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RentalRequestConfiguration : IEntityTypeConfiguration<RentalRequest>
{
    public void Configure(EntityTypeBuilder<RentalRequest> b)
    {
        b.ToTable("RentalRequests", t =>
        {
            t.HasCheckConstraint("CK_RentalRequest_Occupants", "[NumberOfOccupants] >= 1");
            t.HasCheckConstraint("CK_RentalRequest_Duration", "[DurationMonths] >= 1");
        });
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.RejectReason).HasMaxLength(500);
        b.HasIndex(x => new { x.RoomId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.RoomId }).IsUnique()
            .HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("UX_RentalRequest_OnePendingPerRoomAndTenant");
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> b)
    {
        b.ToTable("Contracts", t =>
        {
            t.HasCheckConstraint("CK_Contract_Dates", "[EndDate] > [StartDate]");
            t.HasCheckConstraint("CK_Contract_MonthlyRent", "[MonthlyRent] > 0");
            t.HasCheckConstraint("CK_Contract_Deposit", "[DepositAmount] >= 0");
            t.HasCheckConstraint("CK_Contract_InitialReadings", "[InitialElectricIndex] >= 0 AND [InitialWaterIndex] >= 0");
        });
        // Mỗi người thuê một hợp đồng Active; mỗi phòng một hợp đồng Active
        b.HasIndex(x => x.TenantId).IsUnique().HasFilter("[Status] = 'Active'")
            .HasDatabaseName("UX_Contract_OneActivePerTenant");
        b.HasIndex(x => x.RoomId).IsUnique().HasFilter("[Status] = 'Active'")
            .HasDatabaseName("UX_Contract_OneActivePerRoom");
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RentalRequest).WithMany().HasForeignKey(x => x.RentalRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DepositSettlement).WithOne(s => s.Contract)
            .HasForeignKey<DepositSettlement>(s => s.ContractId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContractMemberConfiguration : IEntityTypeConfiguration<ContractMember>
{
    public void Configure(EntityTypeBuilder<ContractMember> b)
    {
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.CitizenId).HasMaxLength(12).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(10);
        b.HasOne(x => x.Contract).WithMany(c => c.Members).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DepositSettlementConfiguration : IEntityTypeConfiguration<DepositSettlement>
{
    public void Configure(EntityTypeBuilder<DepositSettlement> b)
    {
        b.ToTable("DepositSettlements", t =>
        {
            t.HasCheckConstraint("CK_DepositSettlement_Deductions",
                "[DamageDeduction] >= 0 AND [UnpaidDeduction] >= 0 AND [DamageDeduction] + [UnpaidDeduction] <= [DepositAmount]");
            t.HasCheckConstraint("CK_DepositSettlement_Refund", "[RefundAmount] >= 0");
        });
        b.HasIndex(x => x.ContractId).IsUnique();
        b.Property(x => x.Note).HasMaxLength(500);
    }
}

public class ContractChangeRequestConfiguration : IEntityTypeConfiguration<ContractChangeRequest>
{
    public void Configure(EntityTypeBuilder<ContractChangeRequest> b)
    {
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.DecisionNote).HasMaxLength(500);
        b.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ContractId).IsUnique().HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("UX_ContractChangeRequest_OnePendingPerContract");
    }
}

public class ContractServiceConfiguration : IEntityTypeConfiguration<ContractService>
{
    public void Configure(EntityTypeBuilder<ContractService> b)
    {
        b.HasKey(x => new { x.ContractId, x.ServiceId });
        b.HasOne(x => x.Contract).WithMany(c => c.Services).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
