using System;
using System.IO;
using System.Text;
using Library.Common;

namespace Library.ConsoleApp
{
    internal class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Лабораторна робота №1. Бібліотека");
            Console.WriteLine("Сухарєв, група 402ТН");

            ICrudService<PrintedBook> books = new CrudService<PrintedBook>();
            PrintedBook firstBook = new PrintedBook("Кобзар", "Тарас Шевченко", 2020,
                320, "тверда", "А-1");
            PrintedBook secondBook = new PrintedBook("Лісова пісня", "Леся Українка", 2021,
                160, "м'яка", "А-2");
            EBook electronicBook = new EBook("Захар Беркут", "Іван Франко", 2022,
                "PDF", 2.5, "zakhar_berkut.pdf");
            Reader reader = new Reader("Сухарєв", "402ТН-01");

            Console.WriteLine("\n1. Create: додаємо дві паперові книги.");
            books.Create(firstBook);
            books.Create(secondBook);

            Console.WriteLine("\n2. ReadAll: усі додані книги.");
            PrintBooks(books);

            Console.WriteLine("\n3. Read: пошук першої книги за Id.");
            Console.WriteLine(books.Read(firstBook.Id).GetInfo());

            Console.WriteLine("\n4. Update: змінюємо полицю першої книги на Б-3.");
            PrintedBook updatedBook = new PrintedBook(firstBook.Title, firstBook.Author,
                firstBook.Year, firstBook.PageCount, firstBook.CoverType, "Б-3");
            updatedBook.Id = firstBook.Id;
            books.Update(updatedBook);
            Console.WriteLine(books.Read(firstBook.Id).GetInfo());

            Console.WriteLine("\n5. Remove: видаляємо другу книгу.");
            books.Remove(secondBook);
            PrintBooks(books);

            Console.WriteLine("\n6. Той самий CRUD-сервіс для іншого типу: Reader.");
            ICrudService<Reader> readers = new CrudService<Reader>();
            readers.Create(reader);
            Console.WriteLine(readers.Read(reader.Id).GetInfo());

            Console.WriteLine("\n7. Успадкування: електронна книга як Book.");
            Book book = electronicBook;
            Console.WriteLine(book.GetInfo());

            Console.WriteLine("\n8. Делегат і подія: видача паперової книги.");
            updatedBook.Borrowed += ShowMessage;
            updatedBook.Borrow(reader);
            Console.WriteLine("Книга доступна: " + (updatedBook.IsAvailable ? "так" : "ні"));

            Console.WriteLine("\n9. Статичне поле, статичний метод і метод розширення.");
            Console.WriteLine("Створено об'єктів книг: " + Book.CreatedCount);
            Console.WriteLine("Рік 2020 коректний: " + (Book.IsValidYear(2020) ? "так" : "ні"));
            Console.WriteLine(updatedBook.GetShortInfo());

            Console.WriteLine("\n10. Save: зберігаємо книги у books.json поруч із програмою.");
            string filePath = Path.Combine(AppContext.BaseDirectory, "books.json");
            books.Save(filePath);
            Console.WriteLine("Дані збережено.");

            Console.WriteLine("\n11. Load: завантажуємо файл у новий порожній сервіс.");
            ICrudService<PrintedBook> loadedBooks = new CrudService<PrintedBook>();
            loadedBooks.Load(filePath);
            PrintBooks(loadedBooks);
            PrintedBook loadedBook = loadedBooks.Read(firstBook.Id);
            Console.WriteLine("Стан видачі збережено: " + (!loadedBook.IsAvailable ? "так" : "ні"));

            Console.WriteLine("\nДемонстрацію завершено. Натисніть Enter для виходу.");
            if (!Console.IsInputRedirected)
            {
                Console.ReadLine();
            }
        }

        private static void PrintBooks(ICrudService<PrintedBook> books)
        {
            foreach (PrintedBook book in books.ReadAll())
            {
                Console.WriteLine(book.GetInfo());
            }
        }

        private static void ShowMessage(string message)
        {
            Console.WriteLine("Подія: " + message);
        }
    }
}
