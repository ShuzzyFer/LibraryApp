namespace Library.Models;

public class BookPossession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; }
    public DateTime TakenDate { get; set; }
    public DateTime? ReturnedDate { get; set; } // NULL, если книга ещё не возвращена
}