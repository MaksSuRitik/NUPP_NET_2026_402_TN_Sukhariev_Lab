using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Library.Common;

namespace Library.ConsoleApp
{
    internal class Program
    {
        private static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("uk-UA");
            Console.WriteLine("Лабораторна робота №2. Бібліотека");
            Console.WriteLine("Сухарєв, група 402ТН");

            string filePath = Path.Combine(AppContext.BaseDirectory, "books_lab2.json");
            ICrudServiceAsync<PrintedBook> books = new CrudServiceAsync<PrintedBook>(filePath);
            int createdCount = 0;
            object countLock = new object();
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = 4;

            Console.WriteLine("\n1. Parallel.ForEachAsync: створення 1000 книг через CreateNew.");
            await Parallel.ForEachAsync(Enumerable.Range(0, 1000), options,
                async (index, cancellationToken) =>
                {
                    PrintedBook book = PrintedBook.CreateNew();
                    if (await books.CreateAsync(book))
                    {
                        lock (countLock)
                        {
                            createdCount++;
                        }
                    }
                });
            Console.WriteLine("Лічильник під захистом lock: " + createdCount);
            Console.WriteLine("Статичний лічильник через Interlocked: " + Book.CreatedCount);

            Console.WriteLine("\n2. ReadAllAsync: перевірка колекції.");
            List<PrintedBook> allBooks = (await books.ReadAllAsync()).ToList();
            Console.WriteLine("Кількість книг у сервісі: " + allBooks.Count);
            Console.WriteLine("Кількість унікальних Id: " + allBooks.Select(book => book.Id).Distinct().Count());

            Console.WriteLine("\n3. LINQ: мінімальні, максимальні та середні значення.");
            Console.WriteLine($"Рік видання: мін. {allBooks.Min(book => book.Year)}, " +
                $"макс. {allBooks.Max(book => book.Year)}, середнє {allBooks.Average(book => book.Year):F2}");
            Console.WriteLine($"Кількість сторінок: мін. {allBooks.Min(book => book.PageCount)}, " +
                $"макс. {allBooks.Max(book => book.PageCount)}, середнє {allBooks.Average(book => book.PageCount):F2}");

            Console.WriteLine("\n4. Пагінація: сторінка 2, по 3 книги (записи 4-6).");
            foreach (PrintedBook book in await books.ReadAllAsync(2, 3))
            {
                Console.WriteLine($"{book.Title}, {book.Year} р., {book.PageCount} стор.");
            }

            Console.WriteLine("\n5. IEnumerable: перші 3 книги через foreach і Take.");
            foreach (PrintedBook book in books.Take(3))
            {
                Console.WriteLine(book.GetShortInfo());
            }

            Console.WriteLine("\n6. ReadAsync, UpdateAsync, RemoveAsync.");
            PrintedBook firstBook = await books.ReadAsync(allBooks[0].Id);
            Console.WriteLine($"Знайдено: {firstBook.Title}; полиця: {firstBook.ShelfNumber}");
            PrintedBook updatedBook = new PrintedBook(firstBook.Title, firstBook.Author,
                firstBook.Year, firstBook.PageCount, firstBook.CoverType, "Б-3");
            updatedBook.Id = firstBook.Id;
            Console.WriteLine("Оновлено: " + YesNo(await books.UpdateAsync(updatedBook)));
            Console.WriteLine("Нова полиця: " + (await books.ReadAsync(firstBook.Id)).ShelfNumber);
            Console.WriteLine("Видалено: " + YesNo(await books.RemoveAsync(updatedBook)));
            Console.WriteLine("Після видалення: " + (await books.ReadAllAsync()).Count());
            await books.CreateAsync(updatedBook);
            Console.WriteLine("Повернули книгу. Усього: " + (await books.ReadAllAsync()).Count());

            Console.WriteLine("\n7. SemaphoreSlim: два одночасні виклики SaveAsync.");
            bool[] saved = await Task.WhenAll(books.SaveAsync(), books.SaveAsync());
            Console.WriteLine("Обидва збереження успішні: " + YesNo(saved.All(result => result)));
            using (FileStream stream = File.OpenRead(filePath))
            {
                List<PrintedBook>? savedBooks = await JsonSerializer.DeserializeAsync<List<PrintedBook>>(stream);
                Console.WriteLine("Книг у JSON-файлі: " + savedBooks?.Count);
            }
            Console.WriteLine("Файл: books_lab2.json поруч із програмою.");

            Console.WriteLine("\n8. Semaphore: одночасно працюють не більше двох потоків.");
            ShowSemaphore();

            Console.WriteLine("\n9. AutoResetEvent: потік чекає сигналу.");
            ShowAutoResetEvent();

            Console.WriteLine("\nДемонстрацію завершено. Натисніть Enter для виходу.");
            if (!Console.IsInputRedirected)
            {
                Console.ReadLine();
            }
        }

        private static string YesNo(bool value)
        {
            return value ? "так" : "ні";
        }

        private static void ShowSemaphore()
        {
            using Semaphore semaphore = new Semaphore(2, 2);
            Thread[] threads = new Thread[4];
            int active = 0;
            int maximum = 0;
            object syncRoot = new object();

            for (int index = 0; index < threads.Length; index++)
            {
                threads[index] = new Thread(() =>
                {
                    semaphore.WaitOne();
                    try
                    {
                        lock (syncRoot)
                        {
                            active++;
                            maximum = Math.Max(maximum, active);
                        }
                        Thread.Sleep(100);
                        lock (syncRoot)
                        {
                            active--;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                threads[index].Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            Console.WriteLine("Оброблено потоків: " + threads.Length);
            Console.WriteLine("Максимально одночасно всередині: " + maximum);
        }

        private static void ShowAutoResetEvent()
        {
            using AutoResetEvent signal = new AutoResetEvent(false);
            Thread worker = new Thread(() =>
            {
                signal.WaitOne();
                Console.WriteLine("Потік отримав сигнал і продовжив роботу.");
            });
            worker.Start();
            Console.WriteLine("Головний потік надсилає сигнал через Set().");
            signal.Set();
            worker.Join();
            Console.WriteLine("Сигнал автоматично скинуто: " + YesNo(!signal.WaitOne(0)));
        }
    }
}
