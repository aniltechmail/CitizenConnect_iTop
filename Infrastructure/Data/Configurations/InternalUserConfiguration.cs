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
    public class InternalUserConfiguration : IEntityTypeConfiguration<InternalUser>
    {
        public void Configure(EntityTypeBuilder<InternalUser> builder)
        {
            builder.ToTable("internal_users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.Property(x => x.Role).HasConversion<int>();
            builder.HasIndex(x => x.Email).IsUnique();
        }
    }
}
