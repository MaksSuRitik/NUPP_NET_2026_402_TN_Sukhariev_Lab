using System;
using System.Collections.Generic;

namespace Library.Common
{
    public interface ICrudService<T>
    {
        void Create(T element);
        T Read(Guid id);
        IEnumerable<T> ReadAll();
        void Update(T element);
        void Remove(T element);
        void Load(string filePath);
        void Save(string filePath);
    }
}
