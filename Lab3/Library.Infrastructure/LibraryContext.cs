using Library.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure
{
    public class LibraryContext : DbContext
    {
        public DbSet<AuthorModel> Authors { get; set; }
        public DbSet<BookModel> Books { get; set; }
        public DbSet<PrintedBookModel> PrintedBooks { get; set; }
        public DbSet<EBookModel> EBooks { get; set; }
        public DbSet<ReaderModel> Readers { get; set; }
        public DbSet<LibraryCardModel> LibraryCards { get; set; }

        private readonly string _dbPath;

        public LibraryContext()
        {
            _dbPath = FindSolutionDbPath();
        }

        public LibraryContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
            _dbPath = FindSolutionDbPath();
        }

        public string DbPath => _dbPath;

        private static string FindSolutionDbPath()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Library.sln")))
                {
                    return Path.Combine(dir.FullName, "library.db");
                }
                dir = dir.Parent;
            }

            DirectoryInfo? cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (cwd != null)
            {
                if (File.Exists(Path.Combine(cwd.FullName, "Library.sln")))
                {
                    return Path.Combine(cwd.FullName, "library.db");
                }
                cwd = cwd.Parent;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "library.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // наслідування Table-per-Type (окрема таблиця для кожного типу книги)
            modelBuilder.Entity<BookModel>(entity =>
            {
                entity.ToTable("Books");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Title).IsRequired().HasMaxLength(150);
            });

            modelBuilder.Entity<PrintedBookModel>(entity =>
            {
                entity.ToTable("PrintedBooks");
                entity.Property(p => p.CoverType).HasMaxLength(50);
                entity.Property(p => p.ShelfNumber).HasMaxLength(30);
            });

            modelBuilder.Entity<EBookModel>(entity =>
            {
                entity.ToTable("EBooks");
                entity.Property(e => e.Format).HasMaxLength(30);
                entity.Property(e => e.FileName).HasMaxLength(150);
            });

            // один до багатьох (1:N): AuthorModel -> BookModel
            modelBuilder.Entity<AuthorModel>(entity =>
            {
                entity.ToTable("Authors");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.FullName).IsRequired().HasMaxLength(120);
                entity.Property(a => a.Country).HasMaxLength(80);

                entity.HasMany(a => a.Books)
                    .WithOne(b => b.Author)
                    .HasForeignKey(b => b.AuthorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // один до одного (1:1): ReaderModel <-> LibraryCardModel
            modelBuilder.Entity<ReaderModel>(entity =>
            {
                entity.ToTable("Readers");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.FullName).IsRequired().HasMaxLength(120);
                entity.Property(r => r.Phone).HasMaxLength(30);

                entity.HasOne(r => r.LibraryCard)
                    .WithOne(c => c.Reader)
                    .HasForeignKey<LibraryCardModel>(c => c.ReaderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // багато до багатьох (M:N): ReaderModel <-> BookModel
                entity.HasMany(r => r.Books)
                    .WithMany(b => b.Readers)
                    .UsingEntity(j => j.ToTable("ReaderBooks"));
            });

            modelBuilder.Entity<LibraryCardModel>(entity =>
            {
                entity.ToTable("LibraryCards");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.CardNumber).IsRequired().HasMaxLength(40);
            });
        }
    }
}
