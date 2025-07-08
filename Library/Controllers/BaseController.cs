using Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    public class BaseController : Controller
    {
        protected readonly LibraryDbContext _context;
        protected readonly UserManager<User> _userManager;

        public BaseController(LibraryDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
                ViewBag.UnreadNotificationCount = unreadCount;

                var notifications = await _context.Notifications
                    .Include(n => n.Book)
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .ToListAsync();
                ViewBag.Notifications = notifications;
            }
            else
            {
                ViewBag.UnreadNotificationCount = 0;
                ViewBag.Notifications = new List<Notification>();
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}