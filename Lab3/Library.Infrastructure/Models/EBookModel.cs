namespace Library.Infrastructure.Models
{
    // електронна книга, окрема таблиця EBooks
    public class EBookModel : BookModel
    {
        public string Format { get; set; } = "";
        public double FileSizeMb { get; set; }
        public string FileName { get; set; } = "";
    }
}
