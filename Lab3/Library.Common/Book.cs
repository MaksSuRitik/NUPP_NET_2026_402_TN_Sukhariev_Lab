using System;
using System.Threading;

namespace Library.Common
{
    // Делегат для повідомлення про видачу книги.
    public delegate void BookBorrowedHandler(string message);

    public class Book : IEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int Year { get; set; }
        public bool IsAvailable { get; set; }

        // Статичне поле спільне для всіх книг.
        public static int CreatedCount;

        // Подія викликається під час видачі книги.
        public event BookBorrowedHandler? Borrowed;

        // Статичний конструктор.
        static Book()
        {
            CreatedCount = 0;
        }

        // Конструктор без параметрів.
        public Book()
        {
            Id = Guid.NewGuid();
            Title = string.Empty;
            Author = string.Empty;
            IsAvailable = true;
            Interlocked.Increment(ref CreatedCount);
        }

        // Конструктор з параметрами.
        public Book(string title, string author, int year) : this()
        {
            Title = title;
            Author = author;
            Year = year;
        }

        // Віртуальний метод, який перевизначають дочірні класи.
        public virtual string GetInfo()
        {
            return $"{Title}, {Author}, {Year} р.";
        }

        // Метод видачі книги читачу.
        public void Borrow(Reader reader)
        {
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Книгу вже видано.");
            }

            IsAvailable = false;
            Borrowed?.Invoke($"Книгу «{Title}» отримав читач {reader.FullName}.");
        }

        // Статичний метод перевірки року видання.
        public static bool IsValidYear(int year)
        {
            return year > 0 && year <= DateTime.Now.Year;
        }

        public static Book CreateNew()
        {
            return new Book("Книга " + Random.Shared.Next(1, 100000),
                "Автор " + Random.Shared.Next(1, 100),
                Random.Shared.Next(1950, DateTime.Now.Year + 1));
        }
    }
}
