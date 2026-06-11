using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Puzzle> Puzzles { get; set; }
        public DbSet<BundlePuzzle> BundlePuzzles { get; set; }
        public DbSet<MiniBlock> MiniBlocks { get; set; }
        public DbSet<PuzzleBlock> PuzzleBlocks { get; set; }
        public DbSet<PuzzleBlockLine> PuzzleBlockLines { get; set; }
        public DbSet<StudentAttempt> StudentAttempts { get; set; }
        public DbSet<StudentAttemptBlock> StudentAttemptBlocks { get; set; }
        public DbSet<StudentAttemptBlockLine> StudentAttemptBlockLines { get; set; }
        public DbSet<StudentAttemptMiniBlock> StudentAttemptMiniBlocks { get; set; }

        // LTI 1.3 entities
        public DbSet<LtiPlatform> LtiPlatforms { get; set; }
        public DbSet<LtiDeployment> LtiDeployments { get; set; }
        public DbSet<LtiState> LtiStates { get; set; }
        public DbSet<LtiSession> LtiSessions { get; set; }
        public DbSet<LtiResourceLink> LtiResourceLinks { get; set; }
        public DbSet<LtiAccessToken> LtiAccessTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BundlePuzzle>()
                .HasKey(bp => new { bp.BundleId, bp.PuzzleId });

            modelBuilder.Entity<BundlePuzzle>()
                .HasOne(bp => bp.Bundle)
                .WithMany(b => b.BundlePuzzles)
                .HasForeignKey(bp => bp.BundleId);

            modelBuilder.Entity<BundlePuzzle>()
                .HasOne(bp => bp.Puzzle)
                .WithMany(p => p.BundlePuzzles)
                .HasForeignKey(bp => bp.PuzzleId);

            modelBuilder.Entity<MiniBlock>()
                .HasOne(mb => mb.PuzzleBlockLine)
                .WithMany(p => p.MiniBlocks)
                .HasForeignKey(mb => mb.PuzzleBlockLineId);

            modelBuilder.Entity<StudentAttempt>()
                .HasOne(sa => sa.Bundle)
                .WithMany()
                .HasForeignKey(sa => sa.BundleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAttempt>()
                .HasOne(sa => sa.Puzzle)
                .WithMany()
                .HasForeignKey(sa => sa.PuzzleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAttemptBlock>()
                .HasOne(sab => sab.Attempt)
                .WithMany(a => a.Blocks)
                .HasForeignKey(sab => sab.StudentAttemptId);

            modelBuilder.Entity<StudentAttemptBlockLine>()
                .HasOne(sabl => sabl.StudentAttemptBlock)
                .WithMany(sab => sab.Lines)
                .HasForeignKey(sabl => sabl.StudentAttemptBlockId);

            modelBuilder.Entity<StudentAttemptMiniBlock>()
                .HasOne(samb => samb.AttemptBlockLine)
                .WithMany(sabl => sabl.MiniBlocks)
                .HasForeignKey(samb => samb.StudentAttemptBlockLineId);

            modelBuilder.Entity<PuzzleBlock>()
                .HasOne(pb => pb.Puzzle)
                .WithMany(p => p.PuzzleBlocks)
                .HasForeignKey(pb => pb.PuzzleId);

            modelBuilder.Entity<PuzzleBlockLine>()
                .HasOne(pbl => pbl.PuzzleBlock)
                .WithMany(pb => pb.Lines)
                .HasForeignKey(pbl => pbl.PuzzleBlockId);

            // Language configuration
            modelBuilder.Entity<Language>()
                .HasIndex(l => l.Name)
                .IsUnique();

            modelBuilder.Entity<Puzzle>()
                .HasOne(p => p.Language)
                .WithMany(l => l.Puzzles)
                .HasForeignKey(p => p.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);

            // New configurations for instructor ownership
            modelBuilder.Entity<Bundle>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(b => b.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Puzzle>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(p => p.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure ShareableLink is unique
            modelBuilder.Entity<Bundle>()
                .HasIndex(b => b.ShareableLink)
                .IsUnique();

            // LTI 1.3 configurations
            modelBuilder.Entity<LtiPlatform>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(p => p.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LtiPlatform>()
                .HasIndex(p => new { p.Issuer, p.ClientId })
                .IsUnique();

            modelBuilder.Entity<LtiDeployment>()
                .HasOne(d => d.Platform)
                .WithMany(p => p.Deployments)
                .HasForeignKey(d => d.LtiPlatformId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LtiDeployment>()
                .HasOne(d => d.Bundle)
                .WithMany()
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LtiDeployment>()
                .HasIndex(d => new { d.LtiPlatformId, d.DeploymentId })
                .IsUnique();

            modelBuilder.Entity<LtiState>()
                .HasIndex(s => s.State)
                .IsUnique();

            modelBuilder.Entity<LtiState>()
                .HasIndex(s => s.ExpiresAt);

            modelBuilder.Entity<LtiSession>()
                .HasOne(s => s.Platform)
                .WithMany()
                .HasForeignKey(s => s.LtiPlatformId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LtiSession>()
                .HasOne(s => s.Bundle)
                .WithMany()
                .HasForeignKey(s => s.BundleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LtiResourceLink>()
                .HasOne(r => r.Platform)
                .WithMany()
                .HasForeignKey(r => r.LtiPlatformId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LtiResourceLink>()
                .HasIndex(r => new { r.LtiPlatformId, r.DeploymentId, r.ResourceLinkId })
                .IsUnique();

            modelBuilder.Entity<LtiAccessToken>()
                .HasOne(t => t.Platform)
                .WithMany()
                .HasForeignKey(t => t.LtiPlatformId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LtiAccessToken>()
                .HasIndex(t => t.ExpiresAt);
        }
    }
}