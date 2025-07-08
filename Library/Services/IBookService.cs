using Library.Models;

namespace Library.Services
{
    public interface IBookService
    {
        Task<Book> GetBookByIdAsync(int id);
        Task<List<Book>> GetAllBooksAsync();
        Task TakeBookAsync(int bookId, int userId);
        Task ReturnBookAsync(int bookId, int userId);
        Task AddRatingAsync(int bookId, int userId, int rating);
        Task AddCommentAsync(int bookId, int userId, string text);
        Task<List<string>> GetBookSuggestionsAsync(string term);
        Task<(List<Book> Books, int TotalCount)> GetFilteredBooksAsync(string searchQuery, string filterType, List<int> genreIds, List<int> tagIds, float? minRating, int? minRatingCount, int page, int pageSize); // Новый метод
    }
}