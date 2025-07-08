namespace Library.Models;

public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public int ReportCount { get; set; } // Количество жалоб
}