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
    public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
    {
        public void Configure(EntityTypeBuilder<SlaPolicy> builder)
        {
            builder.ToTable("sla_policies");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Category)
                   .WithMany()
                   .HasForeignKey(x => x.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
