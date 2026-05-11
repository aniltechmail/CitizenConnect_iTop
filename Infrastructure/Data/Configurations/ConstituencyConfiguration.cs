using Core.Entities.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configurations
{
    public class ConstituencyConfiguration : IEntityTypeConfiguration<Constituency>
    {
        public void Configure(EntityTypeBuilder<Constituency> builder)
        {
            builder.ToTable("constituencies");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(20);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasOne(x => x.District)
                   .WithMany(x => x.Constituencies)
                   .HasForeignKey(x => x.DistrictId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
