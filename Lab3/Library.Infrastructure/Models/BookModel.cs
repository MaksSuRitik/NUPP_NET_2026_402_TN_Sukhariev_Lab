namespace Library.Infrastructure.Models
{
    // базова модель книги у БД, підхід Table-per-Type
    public class BookModel : BaseModel
    {
        public string Title { get; set; } = "";
        public int Year { get; set; }
        public bool IsAvailable { get; set; } = true;

        // зовнішній ключ на автора (1:N)
        public int AuthorId { get; set; }
        public AuthorModel? Author { get; set; }

        // зв'язок багато до багатьох із читачами
        public List<ReaderModel> Readers { get; set; } = new List<ReaderModel>();
    }
}
