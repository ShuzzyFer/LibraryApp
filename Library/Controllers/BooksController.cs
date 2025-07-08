using Library.Models;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Library.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class BooksController : BaseController
{
    private readonly IBookService _bookService;
    private readonly LibraryDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public BooksController(IBookService bookService, LibraryDbContext context, UserManager<User> userManager, IConfiguration configuration):base(context, userManager)
    {
        _bookService = bookService;
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    // GET: /Books
    public async Task<IActionResult> Index(string searchQuery = null, string filterType = null, List<int> genreIds = null, List<int> tagIds = null, float? minRating = null, int? minRatingCount = null, int page = 1, int pageSize = 10)
    {
        var (books, totalBooks) = await _bookService.GetFilteredBooksAsync(searchQuery, filterType, genreIds, tagIds, minRating, minRatingCount, page, pageSize);

        ViewBag.SearchQuery = searchQuery;
        ViewBag.FilterType = filterType;
        ViewBag.GenreIds = genreIds ?? new List<int>();
        ViewBag.TagIds = tagIds ?? new List<int>();
        ViewBag.MinRating = minRating;
        ViewBag.MinRatingCount = minRatingCount;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalBooks = totalBooks;
        ViewBag.TotalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);
        ViewBag.AllGenres = await _context.Genres.ToListAsync();
        ViewBag.AllTags = await _context.Tags.ToListAsync();

        return View(books);
    }
    
    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var suggestions = await _bookService.GetBookSuggestionsAsync(term);
        return Json(suggestions);
    }
    
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var book = await _context.Books
            .Include(b => b.Comments).ThenInclude(c => c.User)
            .Include(b => b.Reservations)
            .Include(b => b.Ratings)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound();

        if (User.Identity.IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.IsFavorite = await _context.FavoriteBooks
                .AnyAsync(f => f.UserId == user.Id && f.BookId == id);
            ViewBag.IsDropped = await _context.DroppedBooks
                .AnyAsync(d => d.UserId == user.Id && d.BookId == id);
            ViewBag.UserRating = await _context.Ratings
                .Where(r => r.UserId == user.Id && r.BookId == id)
                .Select(r => r.Value)
                .FirstOrDefaultAsync();

            // Проверяем активную бронь и получаем её ID
            var activeReservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.BookId == id && r.Status == "Active");
            ViewBag.IsReservedByUser = activeReservation != null;
            ViewBag.ReservationId = activeReservation?.Id; // Передаём ID брони, если она есть

            // Проверяем, отправлял ли пользователь жалобу на каждый комментарий
            ViewBag.ReportedComments = await _context.CommentInteractions
                .Where(ci => ci.UserId == user.Id && ci.Action == "Report" && ci.Comment.BookId == id)
                .Select(ci => ci.CommentId)
                .ToListAsync();
        }
        else
        {
            ViewBag.IsFavorite = false;
            ViewBag.IsDropped = false;
            ViewBag.UserRating = 0;
            ViewBag.IsReservedByUser = false;
            ViewBag.ReservationId = null; // Для неавторизованных пользователей
            ViewBag.ReportedComments = new List<int>();
        }

        ViewBag.RatingsCount = book.Ratings?.Count ?? 0;
        return View(book);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment(int bookId, string text)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var comment = new Comment
        {
            BookId = bookId,
            UserId = user.Id,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = bookId });
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Reserve(int bookId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var book = await _context.Books.FindAsync(bookId);
        if (book == null || book.AvailableCopies <= 0)
            return BadRequest("Книга недоступна для бронирования");

        var reservation = new Reservation
        {
            UserId = user.Id,
            BookId = bookId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14), // Бронь на 2 недели
            Status = "Active"
        };

        book.AvailableCopies--;
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = bookId });
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Rate(int bookId, int value)
    {
        if (value < 1 || value > 5)
            return BadRequest("Оценка должна быть от 1 до 5");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var book = await _context.Books.FindAsync(bookId);
        if (book == null)
            return NotFound();

        // Проверяем, есть ли уже оценка от этого пользователя
        var existingRating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.BookId == bookId);

        if (existingRating != null)
        {
            // Обновляем существующую оценку
            existingRating.Value = value;
        }
        else
        {
            // Добавляем новую оценку
            var newRating = new Rating
            {
                UserId = user.Id,
                BookId = bookId,
                Value = value
            };
            _context.Ratings.Add(newRating);
        }

        // Сохраняем изменения (новую оценку или обновление)
        await _context.SaveChangesAsync();

        // Пересчитываем средний рейтинг на основе всех оценок в базе
        var ratings = await _context.Ratings
            .Where(r => r.BookId == bookId)
            .Select(r => r.Value)
            .ToListAsync();

        book.AverageRating = ratings.Any() ? (float)ratings.Average() : 0;
        await _context.SaveChangesAsync(); // Сохраняем обновленный рейтинг

        return RedirectToAction("Details", new { id = bookId });
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CancelReservation(int reservationId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var reservation = await _context.Reservations
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == user.Id);

        if (reservation == null || reservation.Status != "Active") return NotFound();

        reservation.Status = "Cancelled";
        reservation.Book.AvailableCopies++;

        await _context.SaveChangesAsync();

        // Проверяем подписчиков на уведомления
        await SendNotificationsForBook(reservation.Book.Id);

        return RedirectToAction("Profile", "Account");
    }
    
    private async Task SendNotificationsForBook(int bookId)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book == null || book.AvailableCopies <= 0) return; // Отправляем только если книга доступна

        var notifications = await _context.Notifications
            .Include(n => n.User)
            .Where(n => n.BookId == bookId && !n.IsSent)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            // Обновляем сообщение перед отправкой
            notification.Message = $"Книга '{book.Title}' теперь доступна в библиотеке!";
            notification.IsSent = true;

            if (!string.IsNullOrEmpty(notification.User.Email))
            {
                await SendEmailAsync(notification.User.Email, "Книга доступна",
                    notification.Message);
            }
        }

        await _context.SaveChangesAsync();
    }
    
    private async Task SendEmailAsync(string email, string subject, string message)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
        try
        {
            var smtpClient = new System.Net.Mail.SmtpClient(smtpSettings.Server)
            {
                Port = smtpSettings.Port,
                Credentials = new System.Net.NetworkCredential(smtpSettings.Username, smtpSettings.Password),
                EnableSsl = true,
            };

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(smtpSettings.Username),
                Subject = subject,
                Body = message,
                IsBodyHtml = false,
            };
            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки email: {ex.Message}");
        }
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToFavorites(int bookId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var isFavorite = await _context.FavoriteBooks
            .AnyAsync(f => f.UserId == user.Id && f.BookId == bookId);
        var isDropped = await _context.DroppedBooks
            .AnyAsync(d => d.UserId == user.Id && d.BookId == bookId);

        if (isFavorite)
        {
            var favorite = await _context.FavoriteBooks
                .FirstAsync(f => f.UserId == user.Id && f.BookId == bookId);
            _context.FavoriteBooks.Remove(favorite);
        }
        else if (!isDropped) // Нельзя добавить в Любимое, если уже в Брошенных
        {
            _context.FavoriteBooks.Add(new FavoriteBook { UserId = user.Id, BookId = bookId });
        }
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", new { id = bookId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToDropped(int bookId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var isDropped = await _context.DroppedBooks
            .AnyAsync(d => d.UserId == user.Id && d.BookId == bookId);
        var isFavorite = await _context.FavoriteBooks
            .AnyAsync(f => f.UserId == user.Id && f.BookId == bookId);

        if (isDropped)
        {
            var dropped = await _context.DroppedBooks
                .FirstAsync(d => d.UserId == user.Id && d.BookId == bookId);
            _context.DroppedBooks.Remove(dropped);
        }
        else if (!isFavorite) // Нельзя добавить в Брошенные, если уже в Любимых
        {
            _context.DroppedBooks.Add(new DroppedBook { UserId = user.Id, BookId = bookId });
        }
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", new { id = bookId });
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> LikeComment(int commentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var interaction = await _context.CommentInteractions
            .FirstOrDefaultAsync(ci => ci.UserId == user.Id && ci.CommentId == commentId);
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null) return NotFound();

        if (interaction == null)
        {
            // Новый лайк
            _context.CommentInteractions.Add(new CommentInteraction
            {
                UserId = user.Id,
                CommentId = commentId,
                Action = "Like"
            });
            comment.Likes++;
        }
        else if (interaction.Action == "Like")
        {
            // Отмена лайка
            _context.CommentInteractions.Remove(interaction);
            comment.Likes--;
        }
        else if (interaction.Action == "Dislike")
        {
            // Переключение с дизлайка на лайк
            interaction.Action = "Like";
            comment.Dislikes--;
            comment.Likes++;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Details", new { id = comment.BookId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DislikeComment(int commentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var interaction = await _context.CommentInteractions
            .FirstOrDefaultAsync(ci => ci.UserId == user.Id && ci.CommentId == commentId);
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null) return NotFound();

        if (interaction == null)
        {
            // Новый дизлайк
            _context.CommentInteractions.Add(new CommentInteraction
            {
                UserId = user.Id,
                CommentId = commentId,
                Action = "Dislike"
            });
            comment.Dislikes++;
        }
        else if (interaction.Action == "Dislike")
        {
            // Отмена дизлайка
            _context.CommentInteractions.Remove(interaction);
            comment.Dislikes--;
        }
        else if (interaction.Action == "Like")
        {
            // Переключение с лайка на дизлайк
            interaction.Action = "Dislike";
            comment.Likes--;
            comment.Dislikes++;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Details", new { id = comment.BookId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ReportComment(int commentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null) return NotFound();

        var interaction = await _context.CommentInteractions
            .FirstOrDefaultAsync(ci => ci.UserId == user.Id && ci.CommentId == commentId);

        if (interaction == null)
        {
            Console.WriteLine($"Adding report for comment {commentId} by user {user.Id}");
            var newInteraction = new CommentInteraction
            {
                UserId = user.Id,
                CommentId = commentId,
                Action = "Report"
            };
            _context.CommentInteractions.Add(newInteraction);

            // Попробуем сохранить только добавление взаимодействия
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Interaction added: ID = {newInteraction.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving interaction: {ex.Message}");
                throw;
            }

            // Теперь обновляем ReportCount
            comment.ReportCount++;
            _context.Entry(comment).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"ReportCount updated to {comment.ReportCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating ReportCount: {ex.Message}");
                throw;
            }
        }
        else
        {
            Console.WriteLine($"User {user.Id} already reported comment {commentId}");
        }

        return RedirectToAction("Details", new { id = comment.BookId });
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> TakeBook(int bookId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var book = await _context.Books.FindAsync(bookId);
        if (book == null || book.AvailableCopies <= 0) return NotFound();

        // Добавляем запись о владении
        _context.BookPossessions.Add(new BookPossession
        {
            UserId = user.Id,
            BookId = bookId,
            TakenDate = DateTime.UtcNow
        });

        // Уменьшаем количество доступных экземпляров
        book.AvailableCopies--;
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = bookId });
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> NotifyWhenAvailable(int bookId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var book = await _context.Books.FindAsync(bookId);
        if (book == null) return NotFound();

        var existingNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == user.Id && n.BookId == bookId && !n.IsSent);
        if (existingNotification != null)
        {
            return RedirectToAction("Details", new { id = bookId });
        }

        var notification = new Notification
        {
            UserId = user.Id,
            BookId = bookId,
            Message = $"Вы получите уведомление, когда книга '{book.Title}' станет доступна.", // Нейтральный текст
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            IsSent = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = bookId });
    }

}