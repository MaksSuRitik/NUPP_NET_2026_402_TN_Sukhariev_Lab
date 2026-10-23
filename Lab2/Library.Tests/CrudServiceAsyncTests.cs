using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Library.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace Library.Tests
{
    [TestClass]
    public class CrudServiceAsyncTests
    {
        private string directory = string.Empty;
        private string filePath = string.Empty;
        private ICrudServiceAsync<PrintedBook> service = null!;

        [TestInitialize]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "LibraryTests_" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            filePath = Path.Combine(directory, "books.json");
            service = new CrudServiceAsync<PrintedBook>(filePath);
        }

        [TestCleanup]
        public void CleanUp()
        {
            Directory.Delete(directory, true);
        }

        [TestMethod]
        public async Task CreateAndRead_ReturnAddedBook()
        {
            PrintedBook book = PrintedBook.CreateNew();
            Assert.IsTrue(await service.CreateAsync(book));
            PrintedBook found = await service.ReadAsync(book.Id);
            Assert.AreEqual(book.Title, found.Title);
            Assert.AreEqual(book.PageCount, found.PageCount);
            Assert.AreEqual(1, (await service.ReadAllAsync()).Count());
        }

        [TestMethod]
        public async Task DuplicateId_IsNotAdded()
        {
            PrintedBook book = PrintedBook.CreateNew();
            await service.CreateAsync(book);
            Assert.IsFalse(await service.CreateAsync(book));
            Assert.AreEqual(1, (await service.ReadAllAsync()).Count());
        }

        [TestMethod]
        public async Task MissingBook_ThrowsAndDoesNotBlockService()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => service.ReadAsync(Guid.NewGuid()));
            Assert.IsTrue(await service.CreateAsync(PrintedBook.CreateNew()));
        }

        [TestMethod]
        public async Task Update_ReplacesBookWithSameId()
        {
            PrintedBook original = PrintedBook.CreateNew();
            await service.CreateAsync(original);
            PrintedBook replacement = PrintedBook.CreateNew();
            replacement.Id = original.Id;
            replacement.ShelfNumber = "Б-3";
            Assert.IsTrue(await service.UpdateAsync(replacement));
            Assert.AreEqual("Б-3", (await service.ReadAsync(original.Id)).ShelfNumber);
            Assert.AreEqual(1, (await service.ReadAllAsync()).Count());
        }

        [TestMethod]
        public async Task UpdateMissingBook_ReturnsFalse()
        {
            Assert.IsFalse(await service.UpdateAsync(PrintedBook.CreateNew()));
            Assert.AreEqual(0, (await service.ReadAllAsync()).Count());
        }

        [TestMethod]
        public async Task Remove_UsesIdAndReturnsFalseAfterRemoval()
        {
            PrintedBook original = PrintedBook.CreateNew();
            await service.CreateAsync(original);
            PrintedBook sameId = new PrintedBook { Id = original.Id };
            Assert.IsTrue(await service.RemoveAsync(sameId));
            Assert.IsFalse(await service.RemoveAsync(sameId));
            Assert.AreEqual(0, (await service.ReadAllAsync()).Count());
        }

        [TestMethod]
        public async Task ReadAll_ReturnsCollectionSnapshot()
        {
            await service.CreateAsync(PrintedBook.CreateNew());
            IEnumerable<PrintedBook> snapshot = await service.ReadAllAsync();
            await service.CreateAsync(PrintedBook.CreateNew());
            Assert.AreEqual(1, snapshot.Count());
            Assert.AreEqual(2, (await service.ReadAllAsync()).Count());
        }

        [TestMethod]
        public async Task Pagination_ReturnsExpectedItemsAndLastPartialPage()
        {
            List<Guid> identifiers = new List<Guid>();
            for (int index = 0; index < 7; index++)
            {
                PrintedBook book = PrintedBook.CreateNew();
                identifiers.Add(book.Id);
                await service.CreateAsync(book);
            }

            CollectionAssert.AreEqual(identifiers.Skip(3).Take(3).ToArray(),
                (await service.ReadAllAsync(2, 3)).Select(book => book.Id).ToArray());
            Assert.AreEqual(1, (await service.ReadAllAsync(3, 3)).Count());
            Assert.AreEqual(0, (await service.ReadAllAsync(4, 3)).Count());
            Assert.AreEqual(0, (await service.ReadAllAsync(int.MaxValue, int.MaxValue)).Count());
        }

        [TestMethod]
        [DataRow(0, 3)]
        [DataRow(-1, 3)]
        [DataRow(1, 0)]
        [DataRow(1, -1)]
        public async Task Pagination_RejectsInvalidArguments(int page, int amount)
        {
            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => service.ReadAllAsync(page, amount));
        }

        [TestMethod]
        public async Task Enumeration_IsStableDuringCollectionChanges()
        {
            await service.CreateAsync(PrintedBook.CreateNew());
            using IEnumerator<PrintedBook> iterator = service.GetEnumerator();
            await service.CreateAsync(PrintedBook.CreateNew());
            int count = 0;
            while (iterator.MoveNext())
            {
                count++;
            }

            Assert.AreEqual(1, count);
            Assert.AreEqual(2, ((IEnumerable)service).Cast<PrintedBook>().Count());
        }

        [TestMethod]
        public async Task ParallelCreation_DoesNotLoseBooksOrCounterUpdates()
        {
            int initialCount = Book.CreatedCount;
            await Parallel.ForEachAsync(Enumerable.Range(0, 1000),
                new ParallelOptions { MaxDegreeOfParallelism = 8 },
                async (index, cancellationToken) =>
                {
                    Assert.IsTrue(await service.CreateAsync(PrintedBook.CreateNew()));
                });

            List<PrintedBook> books = (await service.ReadAllAsync()).ToList();
            Assert.AreEqual(1000, books.Count);
            Assert.AreEqual(1000, books.Select(book => book.Id).Distinct().Count());
            Assert.AreEqual(initialCount + 1000, Book.CreatedCount);
        }

        [TestMethod]
        public async Task ParallelDuplicateCreation_AddsExactlyOnce()
        {
            PrintedBook book = PrintedBook.CreateNew();
            Task<bool>[] tasks = Enumerable.Range(0, 100)
                .Select(index => Task.Run(() => service.CreateAsync(book))).ToArray();
            bool[] results = await Task.WhenAll(tasks);
            Assert.AreEqual(1, results.Count(result => result));
            Assert.AreEqual(1, (await service.ReadAllAsync()).Count());
        }

        [TestMethod]
        public async Task ParallelUpdatesAndRemovals_KeepExpectedCount()
        {
            PrintedBook[] books = Enumerable.Range(0, 100).Select(index => PrintedBook.CreateNew()).ToArray();
            foreach (PrintedBook book in books)
            {
                await service.CreateAsync(book);
            }

            await Parallel.ForEachAsync(Enumerable.Range(0, books.Length),
                new ParallelOptions { MaxDegreeOfParallelism = 8 },
                async (index, cancellationToken) =>
                {
                    if (index % 2 == 0)
                    {
                        Assert.IsTrue(await service.RemoveAsync(books[index]));
                    }
                    else
                    {
                        PrintedBook replacement = PrintedBook.CreateNew();
                        replacement.Id = books[index].Id;
                        replacement.ShelfNumber = "Б-3";
                        Assert.IsTrue(await service.UpdateAsync(replacement));
                    }
                    foreach (PrintedBook book in service)
                    {
                        Assert.AreNotEqual(Guid.Empty, book.Id);
                    }
                });

            List<PrintedBook> remaining = (await service.ReadAllAsync()).ToList();
            Assert.AreEqual(50, remaining.Count);
            Assert.IsTrue(remaining.All(book => book.ShelfNumber == "Б-3"));
        }

        [TestMethod]
        public async Task SaveAsync_PreservesAllProperties()
        {
            PrintedBook book = new PrintedBook("Кобзар", "Тарас Шевченко", 2020, 320, "тверда", "Б-3");
            book.IsAvailable = false;
            await service.CreateAsync(book);
            Assert.IsTrue(await service.SaveAsync());
            List<PrintedBook>? saved = JsonSerializer.Deserialize<List<PrintedBook>>(await File.ReadAllTextAsync(filePath));
            Assert.IsNotNull(saved);
            Assert.AreEqual(1, saved.Count);
            Assert.AreEqual(JsonSerializer.Serialize(book), JsonSerializer.Serialize(saved[0]));
        }

        [TestMethod]
        public async Task ParallelSavesAndCreates_ProduceValidJson()
        {
            Task creation = Parallel.ForEachAsync(Enumerable.Range(0, 1000),
                new ParallelOptions { MaxDegreeOfParallelism = 8 },
                async (index, cancellationToken) =>
                {
                    await service.CreateAsync(PrintedBook.CreateNew());
                });
            Task<bool>[] saves = Enumerable.Range(0, 10)
                .Select(index => Task.Run(() => service.SaveAsync())).ToArray();
            await creation;
            Assert.IsTrue((await Task.WhenAll(saves)).All(result => result));
            Assert.IsTrue(await service.SaveAsync());
            List<PrintedBook>? saved = JsonSerializer.Deserialize<List<PrintedBook>>(await File.ReadAllTextAsync(filePath));
            Assert.IsNotNull(saved);
            Assert.AreEqual(1000, saved.Count);
            Assert.AreEqual(1000, saved.Select(book => book.Id).Distinct().Count());
        }

        [TestMethod]
        public async Task SaveEmptyCollection_WritesEmptyArray()
        {
            Assert.IsTrue(await service.SaveAsync());
            Assert.AreEqual("[]", await File.ReadAllTextAsync(filePath));
        }

        [TestMethod]
        public async Task SaveFailure_ReleasesSemaphoreAndAllowsRetry()
        {
            string missingDirectory = Path.Combine(directory, "missing");
            ICrudServiceAsync<PrintedBook> other = new CrudServiceAsync<PrintedBook>(Path.Combine(missingDirectory, "books.json"));
            Assert.IsFalse(await other.SaveAsync());
            Assert.IsTrue(await other.CreateAsync(PrintedBook.CreateNew()));
            Directory.CreateDirectory(missingDirectory);
            Assert.IsTrue(await other.SaveAsync());
        }

        [TestMethod]
        public async Task NullArguments_AreRejected()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => service.CreateAsync(null!));
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => service.UpdateAsync(null!));
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => service.RemoveAsync(null!));
        }

        [TestMethod]
        public async Task GenericService_WorksWithReaders()
        {
            ICrudServiceAsync<Reader> readers = new CrudServiceAsync<Reader>(filePath);
            Reader reader = Reader.CreateNew();
            Assert.IsTrue(await readers.CreateAsync(reader));
            Assert.AreEqual(reader.CardNumber, (await readers.ReadAsync(reader.Id)).CardNumber);
            Assert.IsTrue(await readers.SaveAsync());
        }

        [TestMethod]
        public void Factories_GenerateValidData()
        {
            Book book = Book.CreateNew();
            PrintedBook printed = PrintedBook.CreateNew();
            EBook electronic = EBook.CreateNew();
            Reader reader = Reader.CreateNew();
            Assert.IsTrue(Book.IsValidYear(book.Year));
            Assert.IsTrue(Book.IsValidYear(printed.Year));
            Assert.IsTrue(Book.IsValidYear(electronic.Year));
            Assert.IsTrue(printed.PageCount >= 50 && printed.PageCount <= 1000);
            Assert.IsTrue(electronic.FileSize >= 1 && electronic.FileSize <= 50);
            Assert.IsFalse(string.IsNullOrWhiteSpace(reader.FullName));
            Assert.AreEqual(4, new[] { book.Id, printed.Id, electronic.Id, reader.Id }.Distinct().Count());
        }
    }
}
