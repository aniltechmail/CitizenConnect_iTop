using Core.Entities;
using Core.Entities.Complaint;
using Core.Entities.Identity;
using Core.Entities.Location;
using Core.Entities.Mobile;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Location
        public DbSet<District> Districts => Set<District>();
        public DbSet<Constituency> Constituencies => Set<Constituency>();
        public DbSet<Area> Areas => Set<Area>();
        public DbSet<Block> Blocks => Set<Block>();

        // Identity
        public DbSet<Citizen> Citizens => Set<Citizen>();
        public DbSet<InternalUser> InternalUsers => Set<InternalUser>();

        // Complaint
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ComplaintMedia> ComplaintMedia => Set<ComplaintMedia>();
        public DbSet<ComplaintMessage> ComplaintMessages => Set<ComplaintMessage>();
        public DbSet<ComplaintFeedback> ComplaintFeedbacks => Set<ComplaintFeedback>();
        public DbSet<ComplaintITopMapping> ComplaintITopMappings => Set<ComplaintITopMapping>();

        // Master Data
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
        public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
        public DbSet<EscalationEvent> EscalationEvents => Set<EscalationEvent>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<CitizenRefreshToken> CitizenRefreshTokens => Set<CitizenRefreshToken>();
        public DbSet<CitizenOtp> CitizenOtps => Set<CitizenOtp>();
        public DbSet<MobileDevice> MobileDevices => Set<MobileDevice>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
