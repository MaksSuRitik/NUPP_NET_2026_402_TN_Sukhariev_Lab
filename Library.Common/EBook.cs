namespace Library.Common
{
    public class EBook : Book
    {
        public string Format { get; set; }
        public double FileSize { get; set; }
        public string FileName { get; set; }

        // Конструктор без параметрів.
        public EBook()
        {
            Format = string.Empty;
            FileName = string.Empty;
        }

        // Конструктор з параметрами.
        public EBook(string title, string author, int year,
            string format, double fileSize, string fileName)
            : base(title, author, year)
        {
            Format = format;
            FileSize = fileSize;
            FileName = fileName;
        }

        // Перевизначений метод.
        public override string GetInfo()
        {
            return base.GetInfo() + $", {Format}, {FileSize} МБ, файл: {FileName}";
        }
    }
}
