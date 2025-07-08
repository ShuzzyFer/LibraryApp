namespace Library.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public List<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
    public List<BookTag> BookTags { get; set; } = new List<BookTag>();
    public string CoverImageUrl { get; set; } // Новое поле для URL обложки
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public double AverageRating { get; set; }

    public List<Reservation> Reservations { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Rating> Ratings { get; set; }
    
    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
    }

    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<BookTag> BookTags { get; set; } = new List<BookTag>();
    }

    public class BookGenre
    {
        public int BookId { get; set; }
        public Book Book { get; set; }
        public int GenreId { get; set; }
        public Genre Genre { get; set; }
    }

    public class BookTag
    {
        public int BookId { get; set; }
        public Book Book { get; set; }
        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}