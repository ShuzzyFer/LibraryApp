namespace Library.Models;

using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<int>
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Bio { get; set; }
    public string? FavoriteGenres { get; set; }
    public List<FavoriteBook> FavoriteBooks { get; set; }
    public List<DroppedBook> DroppedBooks { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Reservation> Reservations { get; set; }
    public List<Rating> Ratings { get; set; }
}