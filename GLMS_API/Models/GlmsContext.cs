using System.Reflection.Emit;

namespace GLMS_API.Models
{
    public class GlmsContext : DbContext
    {
        public GlmsContext() { }
        public GlmsContext(DbContextOptions<GlmsContext> options) : base(options) { }

        public virtual DbSet<Client> Clients { get; set; }
        public virtual DbSet<Contract> Contracts { get; set; }
        public virtual DbSet<ServiceRequest> ServiceRequests { get; set; }
        public virtual DbSet<Status> Statuses { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>(e =>
            {
                e.HasKey(x => x.ClientId).HasName("PK__Clients__E67E1A044D1924B5");
                e.Property(x => x.ClientId).HasColumnName("ClientID");
                e.Property(x => x.ClientEmail).HasMaxLength(250);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
                e.Property(x => x.Name).HasMaxLength(150);
                e.Property(x => x.Region).HasMaxLength(100);
            });

            modelBuilder.Entity<Contract>(e =>
            {
                e.HasKey(x => x.ContractId).HasName("PK__Contract__C90D34099DA30E50");
                e.Property(x => x.ContractId).HasColumnName("ContractID");
                e.Property(x => x.ClientId).HasColumnName("ClientID");
                e.Property(x => x.StatusId).HasColumnName("StatusID");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
                e.Property(x => x.ServiceLevel).HasMaxLength(100);
                e.Property(x => x.SignedAgreementFilePath).HasMaxLength(350);
                e.HasOne(d => d.Client).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.ClientId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Contracts__Clien__6B24EA82");
                e.HasOne(d => d.Status).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Contracts__Statu__6C190EBB");
            });

            modelBuilder.Entity<ServiceRequest>(e =>
            {
                e.HasKey(x => x.ServiceRequestId).HasName("PK__ServiceR__790F6CAB95C11B06");
                e.Property(x => x.ServiceRequestId).HasColumnName("ServiceRequestID");
                e.Property(x => x.ContractId).HasColumnName("ContractID");
                e.Property(x => x.StatusId).HasColumnName("StatusID");
                e.Property(x => x.CostUsd).HasColumnType("decimal(18, 2)").HasColumnName("CostUSD");
                e.Property(x => x.CostZar).HasColumnType("decimal(18, 2)").HasColumnName("CostZAR");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
                e.Property(x => x.Description).HasMaxLength(325);
                e.HasOne(d => d.Contract).WithMany(p => p.ServiceRequests)
                    .HasForeignKey(d => d.ContractId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ServiceRe__Contr__6FE99F9F");
                e.HasOne(d => d.Status).WithMany(p => p.ServiceRequests)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ServiceRe__Statu__70DDC3D8");
            });

            modelBuilder.Entity<Status>(e =>
            {
                e.HasKey(x => x.StatusId).HasName("PK__Statuses__C8EE2043922CBDB3");
                e.Property(x => x.StatusId).HasColumnName("StatusID");
                e.Property(x => x.Category).HasMaxLength(50);
                e.Property(x => x.Description).HasMaxLength(250);
                e.Property(x => x.StatusName).HasMaxLength(50);
            });

            modelBuilder.Entity<Role>(e =>
            {
                e.HasKey(x => x.RoleId).HasName("PK__Roles__8AFACE3AC1C70874");
                e.HasIndex(x => x.RoleName, "UQ__Roles__8A2B616060F65813").IsUnique();
                e.Property(x => x.RoleId).HasColumnName("RoleID");
                e.Property(x => x.RoleName).HasMaxLength(75);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.UserId).HasName("PK__Users__1788CCAC33F2B208");
                e.HasIndex(x => x.Email, "UQ__Users__A9D10534A2D81252").IsUnique();
                e.Property(x => x.UserId).HasColumnName("UserID");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
                e.Property(x => x.Email).HasMaxLength(150);
                e.Property(x => x.FullName).HasMaxLength(175);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.Property(x => x.PasswordHash).HasMaxLength(250);
                e.Property(x => x.RoleId).HasColumnName("RoleID");
                e.HasOne(d => d.Role).WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Users__RoleID__628FA481");
            });
        }
    }
}
