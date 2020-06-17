﻿using Microsoft.EntityFrameworkCore;
using Okurdostu.Data.Model;

namespace Okurdostu.Data
{
    public partial class OkurdostuContext : DbContext
    {
        public OkurdostuContext()
        {
        }

        public OkurdostuContext(DbContextOptions<OkurdostuContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Need> Need { get; set; }
        public virtual DbSet<NeedItem> NeedItem { get; set; }
        public virtual DbSet<NeedLike> NeedLike { get; set; }
        public virtual DbSet<University> University { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<UserEducation> UserEducation { get; set; }
        public virtual DbSet<UserEducationDoc> UserEducationDoc { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=okurdostu;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}