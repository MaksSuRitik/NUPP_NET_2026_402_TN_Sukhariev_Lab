namespace Library.Common
{
    public class PrintedBook : Book
    {
        public int PageCount { get; set; }
        public string CoverType { get; set; }
        public string ShelfNumber { get; set; }

        // Конструктор без параметрів потрібний також для завантаження з JSON.
        public PrintedBook()
        {
            CoverType = string.Empty;
            ShelfNumber = string.Empty;
        }

        // Конструктор викликає конструктор базового класу.
        public PrintedBook(string title, string author, int year,
            int pageCount, string coverType, string shelfNumber)
            : base(title, author, year)
        {
            PageCount = pageCount;
            CoverType = coverType;
            ShelfNumber = shelfNumber;
        }

        // Перевизначений метод.
        public override string GetInfo()
        {
            return base.GetInfo() + $", {PageCount} стор., обкладинка: {CoverType}, полиця: {ShelfNumber}";
        }
    }
}
