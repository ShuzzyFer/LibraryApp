namespace Library.Models;

public class Rating
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public int Value { get; set; } // 1-5
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
    public Book Book { get; set; }
}