# Библиотека

Библиотека — это веб-приложение на ASP.NET Core для управления библиотечным каталогом, бронирования книг, оставления оценок и комментариев, а также получения персонализированных рекомендаций. Проект включает интерактивную главную страницу с Яндекс.Картой, показывающей местоположение библиотеки, и страницу с инструкциями по использованию.

## Функционал

- **Главная страница**:
  - Персонализированные рекомендации книг (на основе жанров, оценённых или забронированных пользователем).
  - Популярные книги за последний месяц (по бронированиям).
  - Самые популярные книги (по среднему рейтингу).
  - Описание библиотеки.
  - Яндекс.Карта с меткой по адресу: г. Минск, Слободская 95.
- **Каталог книг**:
  - Поиск книг по названию или автору.
  - Просмотр деталей книги, включая описание, жанры, теги и отзывы.
- **Бронирование**:
  - Возможность бронировать книги, если они доступны.
  - Уведомления о доступности книги (на сайте и по email).
- **Оценки и комментарии**:
  - Пользователи могут ставить оценки (1–5) и оставлять комментарии.
  - Поддержка лайков, дизлайков и жалоб на комментарии.
- **Страница "Инструкция"**:
  - Подробное руководство по использованию сайта (регистрация, поиск, бронирование, уведомления).
- **Админ-панель**:
  - Для администраторов и модераторов: управление книгами, комментариями и пользователями.
- **Уведомления**:
  - Отображение уведомлений в шапке сайта для авторизованных пользователей.

## Технологии

- **Backend**: ASP.NET Core, Entity Framework Core
- **Frontend**: Bootstrap, jQuery, Razor
- **База данных**: SQL Server (или другая, в зависимости от конфигурации)
- **API**: Яндекс.Карты JavaScript API
- **Аутентификация**: ASP.NET Core Identity

## Установка

1. **Клонируйте репозиторий**:
   ```bash
   git clone <repository-url>
   cd Library
   ```

2. **Настройте зависимости**:
   Убедитесь, что установлен .NET SDK (версия 6.0 или выше):
   ```bash
   dotnet --version
   ```
   Установите зависимости:
   ```bash
   dotnet restore
   ```

3. **Настройте базу данных**:
   - Укажите строку подключения в `appsettings.json`:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LibraryDb;Trusted_Connection=True;"
       }
     }
     ```
   - Выполните миграции:
     ```bash
     dotnet ef migrations add InitialCreate
     dotnet ef database update
     ```

4. **Настройте Яндекс.Карты API**:
   - Получите бесплатный API-ключ на [Яндекс.Девелопер](https://developer.tech.yandex.com/services/).
   - Вставьте ключ в `Views/Home/Index.cshtml`:
     ```html
     <script src="https://api-maps.yandex.ru/2.1/?apikey=YOUR_API_KEY&lang=ru_RU"></script>
     ```

5. **Добавьте тестовые данные** (опционально):
   В `Program.cs` добавьте инициализацию:
   ```csharp
   if (!context.Books.Any())
   {
       context.Books.AddRange(
           new Book { Title = "Гарри Поттер", Author = "Дж. К. Роулинг", TotalCopies = 5, AvailableCopies = 2, CoverImageUrl = "https://via.placeholder.com/200", AverageRating = 5.0, Ratings = new List<Rating> { new Rating { Value = 5, UserId = 1 } }, BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = 1 } } },
           new Book { Title = "Властелин колец", Author = "Дж. Р. Р. Толкин", TotalCopies = 3, AvailableCopies = 1, CoverImageUrl = "https://via.placeholder.com/200", AverageRating = 4.0, Ratings = new List<Rating> { new Rating { Value = 4, UserId = 1 } }, BookGenres = new List<Book.BookGenre> { new Book.BookGenre { GenreId = 1 } } }
       );
       context.Genres.Add(new Book.Genre { Id = 1, Name = "Фэнтези" });
       context.Reservations.Add(new Reservation { UserId = 1, BookId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Status = "Active" });
       context.SaveChanges();
   }
   ```

## Запуск

1. **Соберите проект**:
   ```bash
   dotnet build
   ```

2. **Запустите приложение**:
   ```bash
   dotnet run
   ```

3. **Откройте сайт**:
   Перейдите по адресу `http://localhost:5000` (или порт, указанный в консоли).

## Тестирование

1. **Главная страница**:
   - Откройте `http://localhost:5000`.
   - Проверьте отображение:
     - Описания библиотеки.
     - Яндекс.Карты с меткой на Слободской 95.
     - Секций рекомендаций, популярных книг за месяц и самых популярных.
   - Убедитесь, что карта загружается (требуется API-ключ).

2. **Страница "Инструкция"**:
   - Перейдите по ссылке "Инструкция" в шапке.
   - Проверьте аккордеон с руководством.

3. **Каталог книг**:
   - Перейдите в раздел "Книги" и протестируйте поиск, бронирование и оценки.

4. **Админ-панель**:
   - Войдите как администратор и проверьте функционал управления.

## Проблемы и решения

- **Карта не загружается**:
  - Проверьте API-ключ в `Views/Home/Index.cshtml`.
  - Откройте консоль разработчика (F12 → Console) и проверьте ошибки.
  - Убедитесь, что ключ активирован для **JavaScript API и HTTP Геокодер** на [Яндекс.Девелопер](https://developer.tech.yandex.com/services/).

- **Пустые секции рекомендаций**:
  - Проверьте наличие данных в таблицах `Books`, `Genres`, `BookGenres`, `Reservations`, `Ratings`:
    ```sql
    SELECT * FROM Books;
    SELECT * FROM Genres;
    SELECT * FROM BookGenres;
    SELECT * FROM Reservations;
    SELECT * FROM Ratings;
    ```
  - Добавьте тестовые данные (см. выше).

- **Ошибка `UnexpectedToken` в `Index.cshtml`**:
  - Проверьте синтаксис в `Views/Home/Index.cshtml`.
  - Убедитесь, что все теги и блоки `{}` закрыты.

## Внесение вклада

1. Форкните репозиторий.
2. Создайте ветку: `git checkout -b feature/your-feature`.
3. Внесите изменения и закоммитьте: `git commit -m "Добавлена новая функция"`.
4. Отправьте в репозиторий: `git push origin feature/your-feature`.
5. Создайте Pull Request.

## Контакты

Для вопросов и предложений: [вставьте ваш email или контакты].

© 2025 Library