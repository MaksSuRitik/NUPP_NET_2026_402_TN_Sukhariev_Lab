using System;

namespace Library.Common
{
    public class Reader : IEntity
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string CardNumber { get; set; }

        // Конструктор без параметрів.
        public Reader()
        {
            Id = Guid.NewGuid();
            FullName = string.Empty;
            CardNumber = string.Empty;
        }

        // Конструктор з параметрами.
        public Reader(string fullName, string cardNumber) : this()
        {
            FullName = fullName;
            CardNumber = cardNumber;
        }

        // Метод отримання інформації про читача.
        public string GetInfo()
        {
            return $"{FullName}, читацький квиток: {CardNumber}";
        }
    }
}
