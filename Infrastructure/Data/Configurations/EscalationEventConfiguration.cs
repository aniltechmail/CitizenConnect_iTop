using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Entities;

namespace Infrastructure.Data.Configurations
{
    public class EscalationEventConfiguration : IEntityTypeConfiguration<EscalationEvent>
    {
        public void Configure(EntityTypeBuilder<EscalationEvent> builder)
        {
            builder.ToTable("escalation_events");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Complaint)
                   .WithMany()
                   .HasForeignKey(x => x.ComplaintId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
