using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NestU.API.Entities;

namespace NestU.API.Data.Configurations;

public class TenantProfileConfiguration : IEntityTypeConfiguration<TenantProfile>
{
    public void Configure(EntityTypeBuilder<TenantProfile> b)
    {
        b.ToTable("TenantProfiles", t =>
        {
            t.HasCheckConstraint("CK_TenantProfile_Phone", "LEN([Phone]) = 10");
            t.HasCheckConstraint("CK_TenantProfile_CitizenId", "LEN([CitizenId]) = 12");
            t.HasCheckConstraint("CK_TenantProfile_Student",
                "[IsStudent] = 0 OR ([StudentCode] IS NOT NULL AND [UniversityId] IS NOT NULL)");
        });
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.CitizenId).HasMaxLength(12).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(10).IsRequired();
        b.Property(x => x.StudentCode).HasMaxLength(20);
        b.HasIndex(x => x.StudentCode).IsUnique().HasFilter("[StudentCode] IS NOT NULL");
        b.HasOne(x => x.User).WithOne(u => u.TenantProfile).HasForeignKey<TenantProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LandlordProfileConfiguration : IEntityTypeConfiguration<LandlordProfile>
{
    public void Configure(EntityTypeBuilder<LandlordProfile> b)
    {
        b.ToTable("LandlordProfiles", t =>
        {
            t.HasCheckConstraint("CK_LandlordProfile_Phone", "LEN([Phone]) = 10");
            t.HasCheckConstraint("CK_LandlordProfile_CitizenId", "LEN([CitizenId]) = 12");
        });
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.CitizenId).HasMaxLength(12).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(10).IsRequired();
        b.Property(x => x.ReviewNote).HasMaxLength(500);
        b.HasOne(x => x.User).WithOne(u => u.LandlordProfile).HasForeignKey<LandlordProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
    }
}
