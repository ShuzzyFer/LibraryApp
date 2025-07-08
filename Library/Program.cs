using Library.Models;
using Library.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка Identity
builder.Services.AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<LibraryDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
});

builder.Services.AddScoped<IBookService, BookService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Инициализация ролей и начальных данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var dbContext = services.GetRequiredService<LibraryDbContext>();

    // Создание ролей
    string[] roleNames = { "Reader", "Admin", "Moderator" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = roleName });
        }
    }

    // Создание начального администратора
    const string adminEmail = "admin@example.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            Name = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(adminUser, "Admin123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            throw new Exception("Failed to create admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Создание жанров
    if (!dbContext.Genres.Any())
    {
        dbContext.Genres.AddRange(
            new Book.Genre { Name = "Фэнтези" },
            new Book.Genre { Name = "Антиутопия" },
            new Book.Genre { Name = "Драма" },
            new Book.Genre { Name = "Романтика" },
            new Book.Genre { Name = "Фантастика" },
            new Book.Genre { Name = "Мистика" },
            new Book.Genre { Name = "Исторический роман" },
            new Book.Genre { Name = "Сатира" },
            new Book.Genre { Name = "Фикшн" },
            new Book.Genre { Name = "Триллер" },
            new Book.Genre { Name = "Ужасы" },
            new Book.Genre { Name = "Приключения" },
            new Book.Genre { Name = "Мемуары" },
            new Book.Genre { Name = "Философия" },
            new Book.Genre { Name = "Магический реализм" },
            new Book.Genre { Name = "Сказка" },
            new Book.Genre { Name = "Военная проза" }
        );
        await dbContext.SaveChangesAsync();
    }

    // Создание тегов
    if (!dbContext.Tags.Any())
    {
        dbContext.Tags.AddRange(
            new Book.Tag { Name = "Приключения" },
            new Book.Tag { Name = "Магия" },
            new Book.Tag { Name = "Любовь" },
            new Book.Tag { Name = "Технологии" },
            new Book.Tag { Name = "История" },
            new Book.Tag { Name = "Сатира" },
            new Book.Tag { Name = "Тайна" },
            new Book.Tag { Name = "Дружба" },
            new Book.Tag { Name = "Выживание" }
        );
        await dbContext.SaveChangesAsync();
    }

    // Добавление начальных книг
    // Внутри using (var scope = app.Services.CreateScope()) { ... }
if (!dbContext.Books.Any())
{
    var genres = await dbContext.Genres.ToListAsync();
    var tags = await dbContext.Tags.ToListAsync();

    dbContext.Books.AddRange(
        new Book { Title = "Гарри Поттер и Философский Камень", Author = "Дж. К. Роулинг", Description = "Начало волшебного пути Гарри", TotalCopies = 10, AvailableCopies = 8, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фэнтези").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Магия").Id }, new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id } } },
        new Book { Title = "1984", Author = "Джордж Оруэлл", Description = "Мрачное будущее под контролем", TotalCopies = 7, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Антиутопия").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Технологии").Id } } },
        new Book { Title = "Хоббит", Author = "Дж. Р. Р. Толкин", Description = "Приключения Бильбо в Средиземье", TotalCopies = 6, AvailableCopies = 6, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фэнтези").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id }, new Book.BookTag { TagId = tags.First(t => t.Name == "Магия").Id } } },
        new Book { Title = "Убить пересмешника", Author = "Харпер Ли", Description = "Справедливость в маленьком городе", TotalCopies = 5, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Гордость и предубеждение", Author = "Джейн Остин", Description = "Любовь через предрассудки", TotalCopies = 4, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Романтика").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Любовь").Id } } },
        new Book { Title = "Великий Гэтсби", Author = "Ф. Скотт Фицджеральд", Description = "Трагедия американской мечты", TotalCopies = 3, AvailableCopies = 2, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фикшн").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Дюна", Author = "Фрэнк Герберт", Description = "Эпопея о пустынной планете", TotalCopies = 8, AvailableCopies = 7, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фантастика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id } } },
        new Book { Title = "Над пропастью во ржи", Author = "Дж. Д. Сэлинджер", Description = "Бунт молодого Холдена", TotalCopies = 5, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фикшн").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Властелин колец: Братство кольца", Author = "Дж. Р. Р. Толкин", Description = "Начало великого похода", TotalCopies = 6, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фэнтези").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id }, new Book.BookTag { TagId = tags.First(t => t.Name == "Магия").Id } } },
        new Book { Title = "О дивный новый мир", Author = "Олдос Хаксли", Description = "Искусственный рай будущего", TotalCopies = 4, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Антиутопия").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фантастика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Технологии").Id } } },
        new Book { Title = "Мастер и Маргарита", Author = "Михаил Булгаков", Description = "Любовь и дьявол в Москве", TotalCopies = 8, AvailableCopies = 6, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Романтика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Любовь").Id }, new Book.BookTag { TagId = tags.First(t => t.Name == "Магия").Id } } },
        new Book { Title = "Преступление и наказание", Author = "Фёдор Достоевский", Description = "Раскольников и его терзания", TotalCopies = 7, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Война и мир", Author = "Лев Толстой", Description = "Эпопея о судьбах России", TotalCopies = 10, AvailableCopies = 8, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Исторический роман").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Анна Каренина", Author = "Лев Толстой", Description = "Любовь и трагедия Анны", TotalCopies = 6, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Романтика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Любовь").Id } } },
        new Book { Title = "Идиот", Author = "Фёдор Достоевский", Description = "Доброта князя Мышкина", TotalCopies = 5, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Дружба").Id } } },
        new Book { Title = "Братья Карамазовы", Author = "Фёдор Достоевский", Description = "Семейная трагедия и вера", TotalCopies = 7, AvailableCopies = 6, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Собачье сердце", Author = "Михаил Булгаков", Description = "Эксперимент профессора", TotalCopies = 4, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Сатира").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фантастика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Сатира").Id } } },
        new Book { Title = "Тихий Дон", Author = "Михаил Шолохов", Description = "Казаки в эпоху перемен", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Исторический роман").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Отцы и дети", Author = "Иван Тургенев", Description = "Конфликт поколений", TotalCopies = 5, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Герой нашего времени", Author = "Михаил Лермонтов", Description = "Жизнь Печорина", TotalCopies = 4, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Романтика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Любовь").Id } } },
        new Book { Title = "Евгений Онегин", Author = "Александр Пушкин", Description = "Любовь и скука Онегина", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Романтика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Любовь").Id } } },
        new Book { Title = "Мёртвые души", Author = "Николай Гоголь", Description = "Афера Чичикова", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Сатира").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Сатира").Id } } },
        new Book { Title = "Шинель", Author = "Николай Гоголь", Description = "Судьба маленького человека", TotalCopies = 3, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Три товарища", Author = "Эрих Мария Ремарк", Description = "Дружба в трудные времена", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Дружба").Id } } },
        new Book { Title = "На Западном фронте без перемен", Author = "Эрих Мария Ремарк", Description = "Ужасы войны глазами солдата", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Военная проза").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Алхимик", Author = "Пауло Коэльо", Description = "Поиск своего пути", TotalCopies = 4, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Философия").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id } } },
        new Book { Title = "Сто лет одиночества", Author = "Габриэль Гарсиа Маркес", Description = "Сага семьи Буэндиа", TotalCopies = 5, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Магический реализм").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Парфюмер", Author = "Патрик Зюскинд", Description = "История убийцы и ароматов", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Триллер").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Маленький принц", Author = "Антуан де Сент-Экзюпери", Description = "Философия дружбы", TotalCopies = 8, AvailableCopies = 7, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Сказка").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Философия").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Дружба").Id } } },
        new Book { Title = "Портрет Дориана Грея", Author = "Оскар Уайльд", Description = "Цена вечной молодости", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фикшн").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Старик и море", Author = "Эрнест Хемингуэй", Description = "Борьба человека с природой", TotalCopies = 4, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Выживание").Id } } },
        new Book { Title = "По ком звонит колокол", Author = "Эрнест Хемингуэй", Description = "Любовь на войне", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Военная проза").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Романтика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Любовь").Id } } },
        new Book { Title = "Зов Ктулху", Author = "Говард Ф. Лавкрафт", Description = "Кошмар из глубин", TotalCopies = 3, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Ужасы").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Вино из одуванчиков", Author = "Рэй Брэдбери", Description = "Лето детства", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фикшн").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Дружба").Id } } },
        new Book { Title = "451 градус по Фаренгейту", Author = "Рэй Брэдбери", Description = "Мир без книг", TotalCopies = 7, AvailableCopies = 6, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Антиутопия").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фантастика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Технологии").Id } } },
        new Book { Title = "Тёмные начала: Северное сияние", Author = "Филип Пулман", Description = "Путешествие Лиры", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фэнтези").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id }, new Book.BookTag { TagId = tags.First(t => t.Name == "Магия").Id } } },
        new Book { Title = "Имя ветра", Author = "Патрик Ротфусс", Description = "История Квоута", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фэнтези").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Магия").Id }, new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id } } },
        new Book { Title = "Песнь льда и пламени: Игра престолов", Author = "Джордж Р. Р. Мартин", Description = "Борьба за трон", TotalCopies = 8, AvailableCopies = 7, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фэнтези").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id } } },
        new Book { Title = "Шантарам", Author = "Грегори Дэвид Робертс", Description = "Жизнь в Индии", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Приключения").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id } } },
        new Book { Title = "Дневник Анны Франк", Author = "Анна Франк", Description = "Жизнь в укрытии", TotalCopies = 4, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мемуары").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Бойцовский клуб", Author = "Чак Паланик", Description = "Анархия и хаос", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Триллер").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Тень ветра", Author = "Карлос Руис Сафон", Description = "Тайны Барселоны", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Триллер").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Код да Винчи", Author = "Дэн Браун", Description = "Загадки истории", TotalCopies = 7, AvailableCopies = 6, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Триллер").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id }, new Book.BookTag { TagId = tags.First(t => t.Name == "История").Id } } },
        new Book { Title = "Голодные игры", Author = "Сьюзен Коллинз", Description = "Борьба за выживание", TotalCopies = 8, AvailableCopies = 7, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Фантастика").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Приключения").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Выживание").Id } } },
        new Book { Title = "Дракула", Author = "Брэм Стокер", Description = "Легенда о вампире", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Ужасы").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Остров сокровищ", Author = "Роберт Льюис Стивенсон", Description = "Пираты и сокровища", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Приключения").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Приключения").Id } } },
        new Book { Title = "Зелёная миля", Author = "Стивен Кинг", Description = "Чудеса в тюрьме", TotalCopies = 7, AvailableCopies = 6, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Драма").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Оно", Author = "Стивен Кинг", Description = "Страх в Дерри", TotalCopies = 6, AvailableCopies = 5, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Ужасы").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Мистика").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Тайна").Id } } },
        new Book { Title = "Скотный двор", Author = "Джордж Оруэлл", Description = "Революция животных", TotalCopies = 4, AvailableCopies = 3, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Сатира").Id }, new Book.BookGenre { GenreId = genres.First(g => g.Name == "Антиутопия").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Сатира").Id } } },
        new Book { Title = "Робинзон Крузо", Author = "Даниэль Дефо", Description = "Выживание на острове", TotalCopies = 5, AvailableCopies = 4, CoverImageUrl = "/images/book.jpg", BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = genres.First(g => g.Name == "Приключения").Id } }, BookTags = new List<Book.BookTag> { new Book.BookTag { TagId = tags.First(t => t.Name == "Выживание").Id } } }
    );
    await dbContext.SaveChangesAsync();
}
}

app.Run();