namespace Library.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } // Прочитано ли уведомление
        public bool IsSent { get; set; } // Отправлено ли на email

        public User User { get; set; }
        public Book Book { get; set; }
    }
}