using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PROG7311GLMS.Models;

public partial class GlmsContext : DbContext
{
    public GlmsContext()
    {
    }

    public GlmsContext(DbContextOptions<GlmsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ServiceRequest> ServiceRequests { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=GLMS;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__Clients__E67E1A044D1924B5");

            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.ClientEmail).HasMaxLength(250);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Region).HasMaxLength(100);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contract__C90D34099DA30E50");

            entity.Property(e => e.ContractId).HasColumnName("ContractID");
            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ServiceLevel).HasMaxLength(100);
            entity.Property(e => e.SignedAgreementFilePath).HasMaxLength(350);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");

            entity.HasOne(d => d.Client).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Contracts__Clien__6B24EA82");

            entity.HasOne(d => d.Status).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Contracts__Statu__6C190EBB");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AC1C70874");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616060F65813").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(75);
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(e => e.ServiceRequestId).HasName("PK__ServiceR__790F6CAB95C11B06");

            entity.Property(e => e.ServiceRequestId).HasColumnName("ServiceRequestID");
            entity.Property(e => e.ContractId).HasColumnName("ContractID");
            entity.Property(e => e.CostUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("CostUSD");
            entity.Property(e => e.CostZar)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("CostZAR");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(325);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");

            entity.HasOne(d => d.Contract).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceRe__Contr__6FE99F9F");

            entity.HasOne(d => d.Status).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceRe__Statu__70DDC3D8");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Statuses__C8EE2043922CBDB3");

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC33F2B208");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534A2D81252").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(175);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(250);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleID__628FA481");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
