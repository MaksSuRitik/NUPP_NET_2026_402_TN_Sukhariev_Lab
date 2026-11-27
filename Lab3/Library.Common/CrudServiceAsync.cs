using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Common
{
    public class CrudServiceAsync<T> : ICrudServiceAsync<T> where T : class, IEntity
    {
        private readonly List<T> elements = new List<T>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public string FilePath { get; }

        public CrudServiceAsync(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            FilePath = Path.GetFullPath(filePath);
        }

        public async Task<bool> CreateAsync(T element)
        {
            ArgumentNullException.ThrowIfNull(element);
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (elements.Exists(item => item.Id == element.Id))
                {
                    return false;
                }

                elements.Add(element);
                return true;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<T> ReadAsync(Guid id)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (T element in elements)
                {
                    if (element.Id == id)
                    {
                        return element;
                    }
                }

                throw new KeyNotFoundException("Об'єкт не знайдено.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<IEnumerable<T>> ReadAllAsync()
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return new List<T>(elements);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<IEnumerable<T>> ReadAllAsync(int page, int amount)
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Номер сторінки починається з 1.");
            }
            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Кількість має бути додатною.");
            }

            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                long start = (long)(page - 1) * amount;
                if (start >= elements.Count)
                {
                    return new List<T>();
                }

                int count = Math.Min(amount, elements.Count - (int)start);
                return elements.GetRange((int)start, count);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<bool> UpdateAsync(T element)
        {
            ArgumentNullException.ThrowIfNull(element);
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                int index = elements.FindIndex(item => item.Id == element.Id);
                if (index == -1)
                {
                    return false;
                }

                elements[index] = element;
                return true;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<bool> RemoveAsync(T element)
        {
            ArgumentNullException.ThrowIfNull(element);
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                int index = elements.FindIndex(item => item.Id == element.Id);
                if (index == -1)
                {
                    return false;
                }

                elements.RemoveAt(index);
                return true;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<bool> SaveAsync()
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using FileStream stream = new FileStream(FilePath, FileMode.Create,
                    FileAccess.Write, FileShare.None, 4096, true);
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.WriteIndented = true;
                await JsonSerializer.SerializeAsync(stream, elements, options).ConfigureAwait(false);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            semaphore.Wait();
            try
            {
                return new List<T>(elements).GetEnumerator();
            }
            finally
            {
                semaphore.Release();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
