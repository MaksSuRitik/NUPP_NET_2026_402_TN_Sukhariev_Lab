namespace Library.Infrastructure.Models
{
    // читач, 1:1 з LibraryCardModel і M:N з BookModel
    public class ReaderModel : BaseModel
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";

        public LibraryCardModel? LibraryCard { get; set; }
        public List<BookModel> Books { get; set; } = new List<BookModel>();
    }
}
