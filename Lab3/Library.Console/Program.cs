using System.Text;
using Library.Infrastructure;
using Library.Infrastructure.Models;
using Library.Infrastructure.Repositories;
using Library.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("Лабораторна робота №3. Бібліотека (EF Core і SQLite)");
Console.WriteLine("Сухарєв, група 402ТН");
Console.WriteLine();

using var context = new LibraryContext();
await context.Database.MigrateAsync();
Console.WriteLine($"База даних SQLite: {Path.GetFileName(context.DbPath)}");
Console.WriteLine();

// очищаємо старі записи, щоб повторні запуски не дублювали дані
context.LibraryCards.RemoveRange(context.LibraryCards);
context.Readers.RemoveRange(context.Readers);
context.Books.RemoveRange(context.Books);
context.Authors.RemoveRange(context.Authors);
await context.SaveChangesAsync();

var authorRepo = new Repository<AuthorModel>(context);
var printedRepo = new Repository<PrintedBookModel>(context);
var ebookRepo = new Repository<EBookModel>(context);
var readerRepo = new Repository<ReaderModel>(context);

var authorService = new DbCrudServiceAsync<AuthorModel>(authorRepo, context);
var printedService = new DbCrudServiceAsync<PrintedBookModel>(printedRepo, context);
var ebookService = new DbCrudServiceAsync<EBookModel>(ebookRepo, context);
var readerService = new DbCrudServiceAsync<ReaderModel>(readerRepo, context);

// 1. Автори (1:N з книгами)
Console.WriteLine("1. CreateAsync: додаємо авторів у БД");
var shevchenko = new AuthorModel { FullName = "Тарас Шевченко", Country = "Україна", BirthYear = 1814 };
var ukrainka = new AuthorModel { FullName = "Леся Українка", Country = "Україна", BirthYear = 1871 };
var franko = new AuthorModel { FullName = "Іван Франко", Country = "Україна", BirthYear = 1856 };

await authorService.CreateAsync(shevchenko);
await authorService.CreateAsync(ukrainka);
await authorService.CreateAsync(franko);
await authorService.SaveAsync();
Console.WriteLine($"Додано авторів: {(await authorService.ReadAllAsync()).Count()}");
Console.WriteLine();

// 2. Паперові та електронні книги (Table-per-Type)
Console.WriteLine("2. CreateAsync: додаємо паперові та електронні книги (Table-per-Type)");
var kobzar = new PrintedBookModel
{
    Title = "Кобзар",
    Year = 2020,
    PageCount = 320,
    CoverType = "тверда",
    ShelfNumber = "А-1",
    AuthorId = shevchenko.Id
};
var lisova = new PrintedBookModel
{
    Title = "Лісова пісня",
    Year = 2021,
    PageCount = 160,
    CoverType = "м'яка",
    ShelfNumber = "А-2",
    AuthorId = ukrainka.Id
};
var tempBook = new PrintedBookModel
{
    Title = "Тестова книга",
    Year = 2024,
    PageCount = 90,
    CoverType = "м'яка",
    ShelfNumber = "В-9",
    AuthorId = franko.Id
};
var zakhar = new EBookModel
{
    Title = "Захар Беркут",
    Year = 2022,
    Format = "PDF",
    FileSizeMb = 2.5,
    FileName = "zakhar_berkut.pdf",
    AuthorId = franko.Id
};
var contraSpem = new EBookModel
{
    Title = "Бояриня",
    Year = 2023,
    Format = "EPUB",
    FileSizeMb = 1.4,
    FileName = "boyarynya.epub",
    AuthorId = ukrainka.Id
};

await printedService.CreateAsync(kobzar);
await printedService.CreateAsync(lisova);
await printedService.CreateAsync(tempBook);
await ebookService.CreateAsync(zakhar);
await ebookService.CreateAsync(contraSpem);
await printedService.SaveAsync();

Console.WriteLine($"Паперових книг у таблиці PrintedBooks: {(await printedService.ReadAllAsync()).Count()}");
Console.WriteLine($"Електронних книг у таблиці EBooks: {(await ebookService.ReadAllAsync()).Count()}");
Console.WriteLine();

// 3. Читачі, читацькі квитки (1:1) та взяті книги (M:N)
Console.WriteLine("3. CreateAsync: додаємо читачів з квитками (1:1) і книгами (M:N)");
var reader1 = new ReaderModel
{
    FullName = "Сухарєв Максим",
    Phone = "+380671112233",
    LibraryCard = new LibraryCardModel
    {
        CardNumber = "402TN-01",
        IssuedAt = DateTime.Now,
        IsActive = true
    },
    Books = new List<BookModel> { kobzar, zakhar }
};

var reader2 = new ReaderModel
{
    FullName = "Коваленко Олег",
    Phone = "+380504445566",
    LibraryCard = new LibraryCardModel
    {
        CardNumber = "402TN-02",
        IssuedAt = DateTime.Now,
        IsActive = true
    },
    Books = new List<BookModel> { lisova, contraSpem }
};

await readerService.CreateAsync(reader1);
await readerService.CreateAsync(reader2);
await readerService.SaveAsync();
Console.WriteLine($"Додано читачів: {(await readerService.ReadAllAsync()).Count()}");
Console.WriteLine();

// 4. Пошук по Guid і по int Id
Console.WriteLine("4. ReadAsync (Guid) і GetByIdAsync (int): пошук у БД");
var byGuid = await printedService.ReadAsync(kobzar.ExternalId);
var byInt = await printedService.ReadByIntIdAsync(lisova.Id);
Console.WriteLine($"За Guid: {byGuid.Title}, полиця {byGuid.ShelfNumber}, {byGuid.PageCount} стор.");
Console.WriteLine($"За int Id={lisova.Id}: {byInt.Title}, полиця {byInt.ShelfNumber}, {byInt.PageCount} стор.");
Console.WriteLine();

// 5. UpdateAsync і RemoveAsync
Console.WriteLine("5. UpdateAsync і RemoveAsync: оновлення та видалення");
byGuid.ShelfNumber = "Б-3";
await printedService.UpdateAsync(byGuid);
await printedService.RemoveAsync(tempBook);
await printedService.SaveAsync();
Console.WriteLine($"Нова полиця для {byGuid.Title}: {(await printedService.ReadByIntIdAsync(byGuid.Id)).ShelfNumber}");
Console.WriteLine($"Після видалення тестової книги залишилось паперових книг: {(await printedService.ReadAllAsync()).Count()}");
Console.WriteLine();

// 6. Пагінація
Console.WriteLine("6. Пагінація ReadAllAsync(page: 1, amount: 2) для PrintedBooks");
var firstPage = await printedService.ReadAllAsync(1, 2);
foreach (var b in firstPage)
{
    Console.WriteLine($"  [{b.Id}] {b.Title}, {b.Year} р., {b.PageCount} стор., полиця {b.ShelfNumber}");
}
Console.WriteLine();

// 7. Перевірка зв'язків 1:N, 1:1 та M:N
Console.WriteLine("7. Перевірка зв'язків у БД (1:N, 1:1, M:N)");
var authorsWithBooks = await context.Authors.Include(a => a.Books).ToListAsync();
foreach (var a in authorsWithBooks)
{
    Console.WriteLine($"  [1:N] {a.FullName}: {string.Join(", ", a.Books.Select(b => b.Title))}");
}

var readersWithDetails = await context.Readers
    .Include(r => r.LibraryCard)
    .Include(r => r.Books)
    .ToListAsync();
foreach (var r in readersWithDetails)
{
    Console.WriteLine($"  [1:1 + M:N] {r.FullName}, квиток {r.LibraryCard?.CardNumber}, книги: {string.Join(", ", r.Books.Select(b => b.Title))}");
}
