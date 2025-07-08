namespace Library.Models;

public class CommentInteraction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int CommentId { get; set; }
    public Comment Comment { get; set; }
    public string Action { get; set; } // "Like", "Dislike", "Report"
}