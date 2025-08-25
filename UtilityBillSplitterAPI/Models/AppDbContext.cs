using Microsoft.EntityFrameworkCore;
using UtilityBillSplitter.Models;
using UtilityBillSplitterAPI.Models;

namespace UtilityBillSplitterAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //  Entity sets
        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillShare> BillShares { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  Composite key for group membership
            modelBuilder.Entity<GroupMember>()
                .HasIndex(gm => new { gm.GroupId, gm.UserId })
                .IsUnique();

            //  Precision for monetary values
            modelBuilder.Entity<Bill>()
                .Property(b => b.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bill>()
                .Property(b => b.PaidAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BillShare>()
                .Property(bs => bs.ShareAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.AmountPaid)
                .HasPrecision(18, 2);

            //  Relationships setup

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Group)
                .WithMany(g => g.Bills)
                .HasForeignKey(b => b.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Payer)
                .WithMany()
                .HasForeignKey(b => b.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Group>()
                .HasOne(g => g.CreatedBy)
                .WithMany()
                .HasForeignKey(g => g.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BillShare>()
                .HasOne(bs => bs.Bill)
                .WithMany(b => b.Shares)
                .HasForeignKey(bs => bs.BillId);

            modelBuilder.Entity<BillShare>()
                .HasOne(bs => bs.User)
                .WithMany()
                .HasForeignKey(bs => bs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.BillShare)
                .WithMany()
                .HasForeignKey(p => p.BillShareId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
