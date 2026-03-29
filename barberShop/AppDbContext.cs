using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace barberShop
{
    public class AppDbContext : IdentityDbContext<Felhasznalo>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Fodrasz> Fodraszok { get; set; }
        public DbSet<Szolgaltatas> Szolgaltatasok { get; set; }
        public DbSet<Idopont> Idopontok { get; set; }

        public DbSet<FodraszMunkaIdo> FodraszMunkaidok { get; set; }
        public DbSet<FodraszSzunet> FodraszSzunetek { get; set; }

        public DbSet<ToroltIdopont> ToroltIdopontok { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Szolgaltatas>()
                .Property(s => s.Ar)
                .HasPrecision(18, 1);

            modelBuilder.Entity<Idopont>()
                .HasOne(i => i.Fodrasz)
                .WithMany(f => f.Idopontok)
                .HasForeignKey(i => i.FodraszId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Idopont>()
                .HasOne(i => i.Szolgaltatas)
                .WithMany()
                .HasForeignKey(i => i.SzolgaltatasId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Idopont>()
                .HasIndex(i => new { i.FodraszId, i.EsedekessegiIdopont })
                .IsUnique();

            modelBuilder.Entity<Fodrasz>()
                .HasMany(f => f.VallaltSzolgaltatasok)
                .WithMany()
                .UsingEntity(j => j.ToTable("FodraszSzolgaltatas"));

            modelBuilder.Entity<FodraszMunkaIdo>()
                .HasOne(f => f.Fodrasz)
                .WithMany(m =>m.FodraszMunkaidok)
                .HasForeignKey( f=>f.FodraszId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FodraszSzunet>()
                .HasOne(f => f.Fodrasz)
                .WithMany(sz => sz.FodraszSzunetek)
                .HasForeignKey(f => f.FodraszId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FodraszMunkaIdo>()
                .HasIndex(m => new { m.FodraszId, m.Datum })
                .IsUnique();

            modelBuilder.Entity<ToroltIdopont>()
                .HasOne(t => t.Szolgaltatas)
                .WithMany()
                .HasForeignKey(t => t.SzolgaltatasId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
