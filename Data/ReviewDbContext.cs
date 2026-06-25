using Microsoft.EntityFrameworkCore;
using DBCheckAI.Models;

namespace DBCheckAI.Data
{
    public class ReviewDbContext : DbContext
    {
        public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options)
        {
        }

        public DbSet<AiReviewRecord> AiReviewRecords { get; set; }
        public DbSet<AiReviewIssue> AiReviewIssues { get; set; }
        public DbSet<AiReviewRepoStat> AiReviewRepoStats { get; set; }
        public DbSet<AiReviewUserStat> AiReviewUserStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AiReviewRecord 索引
            modelBuilder.Entity<AiReviewRecord>(entity =>
            {
                entity.HasIndex(e => new { e.Repo, e.CreatedAt }).HasDatabaseName("idx_repo_time");
                entity.HasIndex(e => new { e.Repo, e.PrNumber }).HasDatabaseName("idx_pr");
                entity.HasIndex(e => new { e.Reviewer, e.CreatedAt }).HasDatabaseName("idx_reviewer");
            });

            // AiReviewIssue 索引
            modelBuilder.Entity<AiReviewIssue>(entity =>
            {
                entity.HasIndex(e => e.ReviewId).HasDatabaseName("idx_review_id");
            });

            // AiReviewRepoStat 唯一索引
            modelBuilder.Entity<AiReviewRepoStat>(entity =>
            {
                entity.HasIndex(e => new { e.Repo, e.StatDate }).IsUnique().HasDatabaseName("uk_repo_date");
            });

            // AiReviewUserStat 唯一索引
            modelBuilder.Entity<AiReviewUserStat>(entity =>
            {
                entity.HasIndex(e => new { e.Username, e.Repo, e.StatDate }).IsUnique().HasDatabaseName("uk_user_repo_date");
            });
        }
    }
}
