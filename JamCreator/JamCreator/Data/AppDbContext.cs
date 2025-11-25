using JamCreator.Shared.Models;
using Microsoft.EntityFrameworkCore;
using JamCreator.Shared.Interfaces;
using System.Linq.Expressions;   

namespace JamCreator.Data
{
    /// <summary>
    /// Entity Framework Core database context for JamCreator.
    /// Handles all persistence for sessions, participants, and audio tracks.
    /// </summary>
    public class AppDbContext : DbContext
    {
        // === DbSets ===
        public DbSet<JamSessionModel> JamSessions => Set<JamSessionModel>();
        public DbSet<SessionParticipant> Participants => Set<SessionParticipant>();
        public DbSet<AudioTrack> Tracks => Set<AudioTrack>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === JamSessionModel ===
            modelBuilder.Entity<JamSessionModel>(b =>
            {
                b.HasKey(s => s.Id);

                b.Property(s => s.RoomName)
                    .HasMaxLength(100)
                    .IsRequired();

                b.Property(s => s.Genre).HasMaxLength(60);
                b.Property(s => s.Description).HasMaxLength(500);
                b.Property(s => s.Password).HasMaxLength(100);
                b.Property(s => s.Mood).HasMaxLength(60);
                b.Property(s => s.CreatedAtUtc)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Concurrency token
                b.Property(s => s.RowVersion)
                    .IsRowVersion();

                // Indexes
                b.HasIndex(s => s.RoomName);

                // Relationships
                b.HasMany(s => s.Participants)
                    .WithOne(p => p.JamSession)
                    .HasForeignKey(p => p.JamSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(s => s.Tracks)
                    .WithOne(t => t.JamSession)
                    .HasForeignKey(t => t.JamSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === SessionParticipant ===
            modelBuilder.Entity<SessionParticipant>(b =>
            {
                b.HasKey(p => p.Id);

                b.Property(p => p.DisplayName)
                    .HasMaxLength(100)
                    .IsRequired();

                b.Property(p => p.JoinedAtUtc)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Index for faster lookups by session and name
                b.HasIndex(p => new { p.JamSessionId, p.DisplayName });
            });

            // === AudioTrack ===
            modelBuilder.Entity<AudioTrack>(b =>
            {
                b.HasKey(t => t.Id);

                b.Property(t => t.FileName)
                    .HasMaxLength(260)
                    .IsRequired();

                b.Property(t => t.Title).HasMaxLength(120);
                b.Property(t => t.AddedAtUtc)
                    .HasDefaultValueSql("GETUTCDATE()");

                // optional index by filename per session
                b.HasIndex(t => new { t.JamSessionId, t.FileName });
            });
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => m.SentAtUtc);

            modelBuilder.Entity<UserProfile>()
                .HasIndex(p => p.UpdatedAtUtc);
        }
    }
}
