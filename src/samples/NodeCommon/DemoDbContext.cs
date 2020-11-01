using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NodeCommon
{
    public class DemoDbContext : DbContext
    {
        public DbSet<Users> Users { get; set; }

        public DemoDbContext(DbContextOptions<DemoDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>()
                .Property(r => r.Name).HasMaxLength(8).IsRequired();
            modelBuilder.Entity<Users>()
                .HasKey(r => r.Id);
        }
    }
}