namespace Library.Models;

public class EditUserViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public IList<string> Roles { get; set; }
    public List<Book> PossessedBooks { get; set; }
}