using Library.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Services
{
    public class BookService : IBookService
    {
        private readonly LibraryDbContext _context;

        public BookService(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<Book> GetBookByIdAsync(int id)
        {
            return await _context.Books
                .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
                .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)
                .Include(b => b.Ratings)
                .Include(b => b.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<Book>> GetAllBooksAsync()
        {
            return await _context.Books
                .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
                .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)
                .Include(b => b.Ratings)
                .ToListAsync();
        }

        public async Task TakeBookAsync(int bookId, int userId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null || book.AvailableCopies <= 0)
            {
                throw new Exception("Книга недоступна для взятия.");
            }

            var possession = new BookPossession
            {
                BookId = bookId,
                UserId = userId,
                TakenDate = DateTime.UtcNow
            };

            book.AvailableCopies--;
            _context.BookPossessions.Add(possession);
            await _context.SaveChangesAsync();
        }

        public async Task ReturnBookAsync(int bookId, int userId)
        {
            var possession = await _context.BookPossessions
                .FirstOrDefaultAsync(bp => bp.BookId == bookId && bp.UserId == userId && bp.ReturnedDate == null);
            if (possession == null)
            {
                throw new Exception("Книга не была взята этим пользователем.");
            }

            possession.ReturnedDate = DateTime.UtcNow;
            var book = await _context.Books.FindAsync(bookId);
            book.AvailableCopies++;
            await _context.SaveChangesAsync();
        }

        public async Task AddRatingAsync(int bookId, int userId, int rating)
        {
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);
            if (existingRating != null)
            {
                existingRating.Value = rating;
            }
            else
            {
                _context.Ratings.Add(new Rating
                {
                    BookId = bookId,
                    UserId = userId,
                    Value = rating
                });
            }

            var book = await _context.Books.Include(b => b.Ratings).FirstOrDefaultAsync(b => b.Id == bookId);
            book.AverageRating = book.Ratings.Any() ? (float)book.Ratings.Average(r => r.Value) : 0;
            await _context.SaveChangesAsync();
        }

        public async Task AddCommentAsync(int bookId, int userId, string text)
        {
            _context.Comments.Add(new Comment
            {
                BookId = bookId,
                UserId = userId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetBookSuggestionsAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new List<string>();
            }

            return await _context.Books
                .Where(b => b.Title.Contains(term) || b.Author.Contains(term))
                .OrderBy(b => b.Title)
                .Take(10)
                .Select(b => $"{b.Title} - {b.Author}")
                .ToListAsync();
        }

        public async Task<(List<Book> Books, int TotalCount)> GetFilteredBooksAsync(string searchQuery, string filterType, List<int> genreIds, List<int> tagIds, float? minRating, int? minRatingCount, int page, int pageSize)
        {
            var query = _context.Books
                .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
                .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)
                .Include(b => b.Ratings)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(b => b.Title.Contains(searchQuery) || b.Author.Contains(searchQuery));
            }

            if (!string.IsNullOrEmpty(filterType))
            {
                switch (filterType)
                {
                    case "genre":
                        if (genreIds != null && genreIds.Any())
                        {
                            query = query.Where(b => b.BookGenres.Any(bg => genreIds.Contains(bg.GenreId)));
                        }
                        break;
                    case "tag":
                        if (tagIds != null && tagIds.Any())
                        {
                            query = query.Where(b => b.BookTags.Any(bt => tagIds.Contains(bt.TagId)));
                        }
                        break;
                    case "rating":
                        if (minRating.HasValue)
                        {
                            query = query.Where(b => b.Ratings.Any() ? b.AverageRating >= minRating.Value : false);
                        }
                        break;
                    case "ratingCount":
                        if (minRatingCount.HasValue)
                        {
                            query = query.Where(b => b.Ratings.Count >= minRatingCount.Value);
                        }
                        break;
                }
            }

            var totalCount = await query.CountAsync();
            var books = await query
                .OrderBy(b => b.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (books, totalCount);
        }
    }
}