using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities.Complaint;

namespace Infrastructure.Data.Configurations
{
    public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
    {
        public void Configure(EntityTypeBuilder<Complaint> builder)
        {
            builder.ToTable("complaints");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RefNumber).IsRequired().HasMaxLength(20);
            builder.HasIndex(x => x.RefNumber).IsUnique();
            builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Description).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasOne(x => x.Citizen)
                   .WithMany()
                   .HasForeignKey(x => x.CitizenId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Block)
                   .WithMany()
                   .HasForeignKey(x => x.BlockId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Category)
                   .WithMany()
                   .HasForeignKey(x => x.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AssignedDepartment)
                   .WithMany()
                   .HasForeignKey(x => x.AssignedDepartmentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AssignedAgent)
                   .WithMany()
                   .HasForeignKey(x => x.AssignedAgentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Media)
                   .WithOne(x => x.Complaint)
                   .HasForeignKey(x => x.ComplaintId);

            builder.HasMany(x => x.Messages)
                   .WithOne(x => x.Complaint)
                   .HasForeignKey(x => x.ComplaintId);

            builder.HasOne(x => x.Feedback)
                   .WithOne(x => x.Complaint)
                   .HasForeignKey<ComplaintFeedback>(x => x.ComplaintId);

            builder.HasOne(x => x.ITopMapping)
                   .WithOne(x => x.Complaint)
                   .HasForeignKey<ComplaintITopMapping>(x => x.ComplaintId);
        }
    }
}
