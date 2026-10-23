using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Library.Common
{
    // Дженерік-сервіс працює з об'єктами, які мають Id.
    public class CrudService<T> : ICrudService<T> where T : IEntity
    {
        private List<T> elements = new List<T>();

        public void Create(T element)
        {
            if (elements.Exists(item => item.Id == element.Id))
            {
                throw new ArgumentException("Об'єкт із таким Id уже існує.");
            }

            elements.Add(element);
        }

        public T Read(Guid id)
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

        public IEnumerable<T> ReadAll()
        {
            return elements.AsReadOnly();
        }

        public void Update(T element)
        {
            int index = elements.FindIndex(item => item.Id == element.Id);
            if (index == -1)
            {
                throw new KeyNotFoundException("Об'єкт для оновлення не знайдено.");
            }

            elements[index] = element;
        }

        public void Remove(T element)
        {
            T foundElement = Read(element.Id);
            elements.Remove(foundElement);
        }

        // Серіалізація колекції у JSON-файл.
        public void Save(string filePath)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;
            string json = JsonSerializer.Serialize(elements, options);
            File.WriteAllText(filePath, json);
        }

        // Десеріалізація колекції з JSON-файлу.
        public void Load(string filePath)
        {
            string json = File.ReadAllText(filePath);
            List<T>? loadedElements = JsonSerializer.Deserialize<List<T>>(json);
            if (loadedElements == null)
            {
                throw new InvalidDataException("Файл не містить колекції об'єктів.");
            }

            HashSet<Guid> identifiers = new HashSet<Guid>();
            foreach (T element in loadedElements)
            {
                if (element is null || !identifiers.Add(element.Id))
                {
                    throw new InvalidDataException("Файл містить порожні об'єкти або повторні Id.");
                }
            }

            elements = loadedElements;
        }
    }
}
