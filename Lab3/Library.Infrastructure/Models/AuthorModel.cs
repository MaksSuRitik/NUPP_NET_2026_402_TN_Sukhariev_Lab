namespace Library.Infrastructure.Models
{
    // автор книг, зв'язок один до багатьох з BookModel
    public class AuthorModel : BaseModel
    {
        public string FullName { get; set; } = "";
        public string Country { get; set; } = "";
        public int BirthYear { get; set; }

        public List<BookModel> Books { get; set; } = new List<BookModel>();
    }
}
