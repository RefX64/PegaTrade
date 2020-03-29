using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PegaTrade.Core.StaticLogic;

namespace PegaTrade.Core.EntityFramework
{
    public partial class PegasunDBContext
    {
        public virtual DbSet<ApiDetails> ApiDetails { get; set; }
        public virtual DbSet<BBComments> BBComments { get; set; }
        public virtual DbSet<BBCommentVotes> BBCommentVotes { get; set; }
        public virtual DbSet<BBThreads> BBThreads { get; set; }
        public virtual DbSet<BBThreadVotes> BBThreadVotes { get; set; }
        public virtual DbSet<Coins> Coins { get; set; }
        public virtual DbSet<Exceptions> Exceptions { get; set; }
        public virtual DbSet<OfficialCoins> OfficialCoins { get; set; }
        public virtual DbSet<Portfolios> Portfolios { get; set; }
        public virtual DbSet<PTUserInfo> PTUserInfo { get; set; }
        public virtual DbSet<UserInfo> UserInfo { get; set; }
        public virtual DbSet<Users> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(AppSettingsProvider.PegasunDBConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            CustomModelBuildingGeneration(modelBuilder);

            modelBuilder.Entity<ApiDetails>(entity =>
            {
                entity.Property(e => e.ApiPrivate).IsUnicode(false);

                entity.Property(e => e.ApiPublic).IsUnicode(false);

                entity.Property(e => e.ApiThirdKey).IsUnicode(false);

                entity.Property(e => e.Name).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ApiDetails)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_ApiDetails_Users");
            });

            modelBuilder.Entity<BBComments>(entity =>
            {
                entity.Property(e => e.Message).IsUnicode(false);

                entity.HasOne(d => d.Thread)
                    .WithMany(p => p.BBComments)
                    .HasForeignKey(d => d.ThreadId)
                    .HasConstraintName("FK_BBComments_BBThreads");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.BBComments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BBComments_Users");
            });

            modelBuilder.Entity<BBCommentVotes>(entity =>
            {
                entity.HasOne(d => d.Comment)
                    .WithMany(p => p.BBCommentVotes)
                    .HasForeignKey(d => d.CommentId)
                    .HasConstraintName("FK_BBCommentVotes_BBComments");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.BBCommentVotes)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_BBCommentVotes_Users");
            });

            modelBuilder.Entity<BBThreads>(entity =>
            {
                entity.Property(e => e.Message).IsUnicode(false);

                entity.Property(e => e.Title).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.BBThreads)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BBThreads_Users");
            });

            modelBuilder.Entity<BBThreadVotes>(entity =>
            {
                entity.HasOne(d => d.Thread)
                    .WithMany(p => p.BBThreadVotes)
                    .HasForeignKey(d => d.ThreadId)
                    .HasConstraintName("FK_BBThreadVotes_BBThreads");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.BBThreadVotes)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_BBThreadVotes_Users");
            });

            modelBuilder.Entity<Coins>(entity =>
            {
                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.Symbol).IsUnicode(false);

                entity.HasOne(d => d.Portfolio)
                    .WithMany(p => p.Coins)
                    .HasForeignKey(d => d.PortfolioId)
                    .HasConstraintName("FK_Coins_Portfolios");
            });

            modelBuilder.Entity<Exceptions>(entity =>
            {
                entity.Property(e => e.ExtraData).IsUnicode(false);

                entity.Property(e => e.InnerMessage).IsUnicode(false);

                entity.Property(e => e.Message).IsUnicode(false);

                entity.Property(e => e.Source).IsUnicode(false);
            });

            modelBuilder.Entity<OfficialCoins>(entity =>
            {
                entity.Property(e => e.Name).IsUnicode(false);

                entity.Property(e => e.Symbol).IsUnicode(false);
            });

            modelBuilder.Entity<Portfolios>(entity =>
            {
                entity.Property(e => e.Name).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Portfolios)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Portfolios_Users");
            });

            modelBuilder.Entity<PTUserInfo>(entity =>
            {
                entity.Property(e => e.UserId).ValueGeneratedNever();

                entity.Property(e => e.Bio).IsUnicode(false);

                entity.Property(e => e.FavoriteCoins).IsUnicode(false);

                entity.Property(e => e.Location).IsUnicode(false);

                entity.Property(e => e.Website).IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithOne(p => p.PTUserInfo)
                    .HasForeignKey<PTUserInfo>(d => d.UserId)
                    .HasConstraintName("FK_PTUserInfo_Users");
            });

            modelBuilder.Entity<UserInfo>(entity =>
            {
                entity.Property(e => e.UserInfoId).ValueGeneratedNever();

                entity.Property(e => e.AboutMe).IsUnicode(false);

                entity.Property(e => e.ConfirmationAuthCode).IsUnicode(false);

                entity.HasOne(d => d.UserInfoNavigation)
                    .WithOne(p => p.UserInfo)
                    .HasForeignKey<UserInfo>(d => d.UserInfoId)
                    .HasConstraintName("FK_UserInfo_Users");
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.Email).IsUnicode(false);

                entity.Property(e => e.FullName).IsUnicode(false);

                entity.Property(e => e.Password).IsUnicode(false);

                entity.Property(e => e.Salt).IsUnicode(false);

                entity.Property(e => e.Username).IsUnicode(false);
            });
        }

        private void CustomModelBuildingGeneration(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.Ignore(e => e.UserInfo);
                entity.Ignore(e => e.Password);
                entity.Ignore(e => e.Salt);
            });
        }
    }
}
