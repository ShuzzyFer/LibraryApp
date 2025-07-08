namespace Library.Models;

public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } // "Active", "Completed", "Cancelled"

    public User User { get; set; }
    public Book Book { get; set; }
}