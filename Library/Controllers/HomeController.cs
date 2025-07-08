using Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(LibraryDbContext context, UserManager<User> userManager)
            : base(context, userManager)
        {
        }

        public async Task<IActionResult> Index()
        {
            // Персональные рекомендации (для авторизованных)
            var recommendedBooks = new List<Book>();
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                // Жанры из оценённых или забронированных книг
                var userGenres = await _context.Ratings
                    .Where(r => r.UserId == userId)
                    .Select(r => r.Book)
                    .Union(_context.Reservations
                        .Where(r => r.UserId == userId && r.Status == "Active")
                        .Select(r => r.Book))
                    .SelectMany(b => b.BookGenres.Select(bg => bg.GenreId))
                    .Distinct()
                    .ToListAsync();

                recommendedBooks = await _context.Books
                    .Include(b => b.Ratings)
                    .Include(b => b.BookGenres)
                    .Where(b => b.BookGenres.Any(bg => userGenres.Contains(bg.GenreId)) && b.AvailableCopies > 0)
                    .OrderByDescending(b => b.AverageRating)
                    .Take(3)
                    .ToListAsync();
            }

            // Популярные за месяц (по бронированиям за последние 30 дней)
            var recentPopularBooks = await _context.Reservations
                .Where(r => r.StartDate >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(r => r.BookId)
                .Select(g => new { BookId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(3)
                .Join(_context.Books.Include(b => b.Ratings),
                    x => x.BookId,
                    b => b.Id,
                    (x, b) => b)
                .ToListAsync();

            // Самые популярные (по рейтингу)
            var topRatedBooks = await _context.Books
                .Include(b => b.Ratings)
                .Where(b => b.Ratings.Any())
                .OrderByDescending(b => b.AverageRating)
                .Take(3)
                .ToListAsync();

            // Инициализация коллекций для избежания null
            foreach (var book in recommendedBooks.Concat(recentPopularBooks).Concat(topRatedBooks))
            {
                book.Ratings ??= new List<Rating>();
                book.BookGenres ??= new List<Book.BookGenre>();
                book.BookTags ??= new List<Book.BookTag>();
            }

            ViewBag.RecommendedBooks = recommendedBooks;
            ViewBag.RecentPopularBooks = recentPopularBooks;
            ViewBag.TopRatedBooks = topRatedBooks;

            return View();
        }

        public IActionResult Guide()
        {
            return View();
        }
    }
}