using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configurations
{
    public class CitizenConfiguration : IEntityTypeConfiguration<Citizen>
    {
        public void Configure(EntityTypeBuilder<Citizen> builder)
        {
            builder.ToTable("citizens");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(15);
            builder.Property(x => x.Email).HasMaxLength(200);
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.HasIndex(x => x.Phone).IsUnique();
            builder.HasIndex(x => x.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            builder.HasOne(x => x.Block)
                   .WithMany(x => x.Citizens)
                   .HasForeignKey(x => x.BlockId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
