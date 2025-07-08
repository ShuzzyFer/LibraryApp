using Library.Models;
using Microsoft.AspNetCore.Identity;

namespace Library.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

[Authorize(Roles = "Admin,Moderator")]
public class AdminController : BaseController
{
    private readonly LibraryDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly IConfiguration _configuration;

    public AdminController(LibraryDbContext context, UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager, IConfiguration configuration) : base(context, userManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    // Админ-панель
    [Authorize(Roles = "Admin")]
    public IActionResult Index()
    {
        return View();
    }

    // Список пользователей (только для Admin)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users(string searchQuery = null, int page = 1, int pageSize = 10)
    {
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(u => u.Name.Contains(searchQuery) || u.Email.Contains(searchQuery));
        }

        var totalUsers = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var comments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Book)
            .Where(c => c.ReportCount > 0)
            .ToListAsync();

        ViewBag.SearchQuery = searchQuery;
        ViewBag.Comments = comments;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
        return View(users);
    }

    // Список книг (только для Admin)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Books(string searchQuery = null, int page = 1, int pageSize = 10)
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

        var totalBooks = await query.CountAsync();
        var books = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Инициализируем коллекции, если они null
        foreach (var book in books)
        {
            book.BookGenres ??= new List<Book.BookGenre>();
            book.BookTags ??= new List<Book.BookTag>();
            book.Ratings ??= new List<Rating>();
        }

        ViewBag.SearchQuery = searchQuery;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalBooks = totalBooks;
        ViewBag.TotalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);
        return View(books);
    }

    // Добавление книги (только для Admin)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> AddBook()
    {
        ViewBag.AllGenres = await _context.Genres.ToListAsync();
        ViewBag.AllTags = await _context.Tags.ToListAsync();
        return View(new Book()); // Передаём пустую модель для формы
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddBook(Book model, int[] genreIds, int[] tagIds)
    {
        if (ModelState.IsValid)
        {
            // Устанавливаем начальные значения
            model.AvailableCopies = model.TotalCopies;
            model.BookGenres = new List<Book.BookGenre>();
            model.BookTags = new List<Book.BookTag>();

            // Добавляем жанры
            if (genreIds != null && genreIds.Any())
            {
                model.BookGenres = genreIds.Select(gid => new Book.BookGenre { GenreId = gid }).ToList();
            }

            // Добавляем теги
            if (tagIds != null && tagIds.Any())
            {
                model.BookTags = tagIds.Select(tid => new Book.BookTag { TagId = tid }).ToList();
            }

            // Сохраняем книгу
            _context.Books.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Books");
        }

        // Если валидация не прошла, возвращаем форму с данными
        ViewBag.AllGenres = await _context.Genres.ToListAsync();
        ViewBag.AllTags = await _context.Tags.ToListAsync();
        return View(model);
    }

    // Редактирование книги (только для Admin)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> EditBook(int id)
    {
        var book = await _context.Books
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound();

        ViewBag.AllGenres = await _context.Genres.ToListAsync();
        ViewBag.AllTags = await _context.Tags.ToListAsync();
        return View(book);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> EditBook(Book model, int[] genreIds, int[] tagIds)
    {
        if (ModelState.IsValid)
        {
            var book = await _context.Books
                .Include(b => b.BookGenres)
                .Include(b => b.BookTags)
                .FirstOrDefaultAsync(b => b.Id == model.Id);
            if (book == null) return NotFound();

            // Обновляем основные поля
            book.Title = model.Title;
            book.Author = model.Author;
            book.Description = model.Description;
            book.CoverImageUrl = model.CoverImageUrl;
            book.TotalCopies = model.TotalCopies;

            // Проверяем, чтобы AvailableCopies не превышал TotalCopies
            if (book.AvailableCopies > book.TotalCopies)
            {
                book.AvailableCopies = book.TotalCopies;
            }

            // Обновляем жанры
            book.BookGenres.Clear();
            if (genreIds != null && genreIds.Any())
            {
                book.BookGenres = genreIds.Select(gid => new Book.BookGenre { BookId = book.Id, GenreId = gid })
                    .ToList();
            }

            // Обновляем теги
            book.BookTags.Clear();
            if (tagIds != null && tagIds.Any())
            {
                book.BookTags = tagIds.Select(tid => new Book.BookTag { BookId = book.Id, TagId = tid }).ToList();
            }

            // Сохраняем изменения
            _context.Update(book);
            await _context.SaveChangesAsync();
            return RedirectToAction("Books");
        }

        // Если валидация не прошла, возвращаем форму с данными
        ViewBag.AllGenres = await _context.Genres.ToListAsync();
        ViewBag.AllTags = await _context.Tags.ToListAsync();
        return View(model);
    }

    // Удаление книги (только для Admin)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Books");
    }

    // Редактирование пользователя (Admin и Moderator)
    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> EditUser(int id, string bookSearchQuery = null)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Roles = await _userManager.GetRolesAsync(user),
            PossessedBooks = await _context.BookPossessions
                .Include(bp => bp.Book)
                .Where(bp => bp.UserId == user.Id && bp.ReturnedDate == null)
                .Select(bp => bp.Book)
                .ToListAsync()
        };

        // Загружаем все книги для выбора с учётом поиска
        var booksQuery = _context.Books.AsQueryable();
        if (!string.IsNullOrEmpty(bookSearchQuery))
        {
            booksQuery = booksQuery.Where(b => b.Title.Contains(bookSearchQuery) || b.Author.Contains(bookSearchQuery));
        }

        var allBooks = await booksQuery.ToListAsync();

        if (User.IsInRole("Admin"))
        {
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        }

        ViewBag.AllBooks = allBooks;
        ViewBag.BookSearchQuery = bookSearchQuery;
        return View(model);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    public async Task<IActionResult> EditUser(EditUserViewModel model, string[] selectedRoles, int[] selectedBooks,
        string bookSearchQuery = null)
    {
        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null) return NotFound();

        user.Name = model.Name;
        user.Email = model.Email;

        if (User.IsInRole("Admin"))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRolesAsync(user, selectedRoles);
        }

        var currentPossessions = await _context.BookPossessions
            .Where(bp => bp.UserId == user.Id && bp.ReturnedDate == null)
            .ToListAsync();

        // Удаляем книги, которые больше не выбраны
        foreach (var possession in currentPossessions)
        {
            if (selectedBooks == null || !selectedBooks.Contains(possession.BookId))
            {
                possession.ReturnedDate = DateTime.UtcNow;
                var book = await _context.Books.FindAsync(possession.BookId);
                if (book != null)
                {
                    book.AvailableCopies++;
                    await SendNotificationsForBook(book.Id); // Отправляем уведомления
                }
            }
        }

        // Добавляем новые книги
        if (selectedBooks != null)
        {
            foreach (var bookId in selectedBooks)
            {
                if (!currentPossessions.Any(p => p.BookId == bookId))
                {
                    var book = await _context.Books.FindAsync(bookId);
                    if (book != null && book.AvailableCopies > 0)
                    {
                        _context.BookPossessions.Add(new BookPossession
                        {
                            UserId = user.Id,
                            BookId = bookId,
                            TakenDate = DateTime.UtcNow
                        });
                        book.AvailableCopies--;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(User.IsInRole("Admin") ? "Users" : "ModeratorPanel");
    }

    private async Task SendNotificationsForBook(int bookId)
    {
        var notifications = await _context.Notifications
            .Include(n => n.User)
            .Include(n => n.Book)
            .Where(n => n.BookId == bookId && !n.IsSent)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsSent = true;
            // Отправка на email
            if (!string.IsNullOrEmpty(notification.User.Email))
            {
                await SendEmailAsync(notification.User.Email, "Книга доступна",
                    $"Книга '{notification.Book.Title}' теперь доступна в библиотеке!");
            }
        }

        await _context.SaveChangesAsync();
    }

// Метод отправки email (пример с SMTP)
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

    // Панель модератора
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> ModeratorPanel(string searchQuery = null, int page = 1, int pageSize = 10)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Book)
            .Where(c => c.ReportCount > 0)
            .ToListAsync();

        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(u => u.Name.Contains(searchQuery) || u.Email.Contains(searchQuery));
        }

        var totalUsers = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchQuery = searchQuery;
        ViewBag.Users = users;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
        return View(comments);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("ModeratorPanel");
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    public async Task<IActionResult> ClearReports(int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment != null)
        {
            comment.ReportCount = 0;
            _context.CommentInteractions.RemoveRange(
                _context.CommentInteractions.Where(ci => ci.CommentId == commentId && ci.Action == "Report"));
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("ModeratorPanel");
    }
}