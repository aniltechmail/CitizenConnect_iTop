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
    public class ComplaintCategoryConfiguration : IEntityTypeConfiguration<ComplaintCategory>
    {
        public void Configure(EntityTypeBuilder<ComplaintCategory> builder)
        {
            builder.ToTable("complaint_categories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.HasOne(x => x.Department)
                   .WithMany()
                   .HasForeignKey(x => x.DepartmentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
