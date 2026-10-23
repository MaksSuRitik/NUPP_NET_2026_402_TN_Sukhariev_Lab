namespace Library.Common
{
    public static class BookExtensions
    {
        // Метод розширення для класу Book та його нащадків.
        public static string GetShortInfo(this Book book)
        {
            return $"{book.Author} - «{book.Title}»";
        }
    }
}
