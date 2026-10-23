using System;

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

        public new static EBook CreateNew()
        {
            return new EBook("Електронна книга " + Random.Shared.Next(1, 100000),
                "Автор " + Random.Shared.Next(1, 100),
                Random.Shared.Next(1950, DateTime.Now.Year + 1),
                Random.Shared.Next(2) == 0 ? "PDF" : "EPUB",
                Math.Round(1 + Random.Shared.NextDouble() * 49, 2),
                "book_" + Guid.NewGuid().ToString("N"));
        }
    }
}
