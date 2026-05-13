using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.UserType).HasConversion<int>();
            builder.Property(x => x.Type).HasConversion<int>();
            builder.HasOne(x => x.Complaint)
                   .WithMany()
                   .HasForeignKey(x => x.ComplaintId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
