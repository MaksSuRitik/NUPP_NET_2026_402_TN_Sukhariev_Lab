namespace Library.Infrastructure.Models
{
    // паперова книга, окрема таблиця PrintedBooks
    public class PrintedBookModel : BookModel
    {
        public int PageCount { get; set; }
        public string CoverType { get; set; } = "";
        public string ShelfNumber { get; set; } = "";
    }
}
