using Library.Models;
using Microsoft.AspNetCore.Authorization;

namespace Library.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class AccountController : BaseController
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly LibraryDbContext _context; // Добавляем контекст для доступа к Reservations

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole<int>> roleManager, LibraryDbContext context) : base(context, userManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
    }

    // ... (Register, Login, Logout)

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var reservations = await _context.Reservations.Include(r => r.Book).Where(r => r.UserId == user.Id).ToListAsync();
        var comments = await _context.Comments.Include(c => c.Book).Where(c => c.UserId == user.Id).ToListAsync();
        var ratings = await _context.Ratings.Include(r => r.Book).Where(r => r.UserId == user.Id).ToListAsync();
        var favoriteBooks = await _context.FavoriteBooks.Include(f => f.Book).Where(f => f.UserId == user.Id).ToListAsync();
        var droppedBooks = await _context.DroppedBooks.Include(d => d.Book).Where(d => d.UserId == user.Id).ToListAsync();
        var possessedBooks = await _context.BookPossessions
            .Include(bp => bp.Book)
            .Where(bp => bp.UserId == user.Id && bp.ReturnedDate == null) // Только не возвращённые
            .ToListAsync();

        ViewBag.UserName = user.Name;
        ViewBag.Bio = user.Bio;
        ViewBag.FavoriteGenres = user.FavoriteGenres;
        ViewBag.Reservations = reservations;
        ViewBag.Comments = comments;
        ViewBag.Ratings = ratings;
        ViewBag.FavoriteBooks = favoriteBooks;
        ViewBag.DroppedBooks = droppedBooks;
        ViewBag.PossessedBooks = possessedBooks;

        return View();
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    public async Task<IActionResult> Register(string name, string email, string password)
    {
        if (ModelState.IsValid)
        {
            var user = new User { UserName = email, Email = email, Name = name, CreatedAt = DateTime.UtcNow };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Reader");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Books"); // Изменено на Books
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View();
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Books"); // Изменено на Books
            }
            ModelState.AddModelError("", "Неверная попытка входа.");
        }
        return View();
    }

    // POST: /Account/Logout
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Books"); // Изменено на Books
    }
    
    [HttpGet]
    public async Task<IActionResult> UserProfile(int userId)
    {
        var user = await _context.Users
            .Include(u => u.FavoriteBooks).ThenInclude(f => f.Book)
            .Include(u => u.DroppedBooks).ThenInclude(d => d.Book)
            .Include(u => u.Comments).ThenInclude(c => c.Book)
            .Include(u => u.Reservations).ThenInclude(r => r.Book)
            .Include(u => u.Ratings).ThenInclude(r => r.Book)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        ViewBag.UserName = user.Name;
        ViewBag.Bio = user.Bio;
        ViewBag.FavoriteGenres = user.FavoriteGenres;
        ViewBag.FavoriteBooks = user.FavoriteBooks;
        ViewBag.DroppedBooks = user.DroppedBooks;
        ViewBag.Comments = user.Comments;
        ViewBag.Reservations = user.Reservations;
        ViewBag.Ratings = user.Ratings;

        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        return View(new EditProfileViewModel
        {
            Bio = user.Bio,
            FavoriteGenres = user.FavoriteGenres
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        user.Bio = model.Bio;
        user.FavoriteGenres = model.FavoriteGenres;
        await _context.SaveChangesAsync();

        return RedirectToAction("Profile");
    }
    
    
}