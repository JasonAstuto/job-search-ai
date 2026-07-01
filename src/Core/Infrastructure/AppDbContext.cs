using JobSearchAi.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAi.Core.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        public DbSet<JobMatch> JobMatches => Set<JobMatch>();
        public DbSet<ApplicationDraft> ApplicationDrafts => Set<ApplicationDraft>();
        public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobPosting>(entity =>
            {
                entity.HasIndex(e => new { e.Source, e.SourceJobId }).IsUnique();
                entity.Property(e => e.Source).HasMaxLength(200);
                entity.Property(e => e.SourceJobId).HasMaxLength(200);
                entity.Property(e => e.RawContent).HasColumnType("text");
                entity.Property(e => e.RawMetadata).HasColumnType("text");
            });

            modelBuilder.Entity<JobMatch>(entity =>
            {
                entity.HasIndex(e => e.JobPostingId);
                entity.Property(e => e.Recommendation).HasMaxLength(1000);
                entity.Property(e => e.MatchSummary).HasColumnType("text");
                entity.Property(e => e.Keywords).HasMaxLength(1000);
            });

            modelBuilder.Entity<ApplicationDraft>(entity =>
            {
                entity.Property(e => e.ResumePath).HasMaxLength(1000);
                entity.Property(e => e.CoverLetterPath).HasMaxLength(1000);
                entity.Property(e => e.ApplicationNotes).HasColumnType("text");
            });

            modelBuilder.Entity<ApprovalDecision>(entity =>
            {
                entity.Property(e => e.Action).HasConversion<string>();
                entity.Property(e => e.Notes).HasColumnType("text");
            });

            modelBuilder.Entity<AuditEvent>(entity =>
            {
                entity.Property(e => e.EntityType).HasMaxLength(200);
                entity.Property(e => e.EntityId).HasMaxLength(200);
                entity.Property(e => e.Action).HasMaxLength(200);
                entity.Property(e => e.Details).HasColumnType("text");
            });
        }
    }
}
