using Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class LibraryDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Book.Genre> Genres { get; set; }
    public DbSet<Book.Tag> Tags { get; set; }
    public DbSet<Book.BookGenre> BookGenres { get; set; }
    public DbSet<Book.BookTag> BookTags { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    
    public DbSet<Notification> Notifications { get; set; }
    
    public DbSet<FavoriteBook> FavoriteBooks { get; set; }
    public DbSet<DroppedBook> DroppedBooks { get; set; }
    public DbSet<CommentInteraction> CommentInteractions { get; set; }
    
    public DbSet<BookPossession> BookPossessions { get; set; }

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Обязательно для Identity

        // Уникальность email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // CHECK-констрейнт для рейтинга
        modelBuilder.Entity<Rating>()
            .HasCheckConstraint("CK_Rating_Value", "\"Value\" >= 1 AND \"Value\" <= 5");
        
        modelBuilder.Entity<Book.BookGenre>()
            .HasKey(bg => new { bg.BookId, bg.GenreId });

        modelBuilder.Entity<Book.BookGenre>()
            .HasOne(bg => bg.Book)
            .WithMany(b => b.BookGenres)
            .HasForeignKey(bg => bg.BookId);

        modelBuilder.Entity<Book.BookGenre>()
            .HasOne(bg => bg.Genre)
            .WithMany(g => g.BookGenres)
            .HasForeignKey(bg => bg.GenreId);

        modelBuilder.Entity<Book.BookTag>()
            .HasKey(bt => new { bt.BookId, bt.TagId });

        modelBuilder.Entity<Book.BookTag>()
            .HasOne(bt => bt.Book)
            .WithMany(b => b.BookTags)
            .HasForeignKey(bt => bt.BookId);

        modelBuilder.Entity<Book.BookTag>()
            .HasOne(bt => bt.Tag)
            .WithMany(t => t.BookTags)
            .HasForeignKey(bt => bt.TagId);
    }
}