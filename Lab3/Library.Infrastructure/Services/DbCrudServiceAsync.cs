using System.Collections;
using Library.Common;
using Library.Infrastructure.Models;
using Library.Infrastructure.Repositories;

namespace Library.Infrastructure.Services
{
    public class DbCrudServiceAsync<T> : ICrudServiceAsync<T> where T : BaseModel
    {
        private readonly IRepository<T> _repository;
        private readonly LibraryContext _context;

        public DbCrudServiceAsync(IRepository<T> repository, LibraryContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<bool> CreateAsync(T element)
        {
            if (element.ExternalId == Guid.Empty)
            {
                element.ExternalId = Guid.NewGuid();
            }

            await _repository.AddAsync(element);
            return true;
        }

        public async Task<T> ReadAsync(Guid id)
        {
            var all = await _repository.GetAllAsync();
            return all.FirstOrDefault(x => x.ExternalId == id)!;
        }

        public async Task<T> ReadByIntIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<T>> ReadAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<T>> ReadAllAsync(int page, int amount)
        {
            if (page < 1) page = 1;
            if (amount < 1) amount = 10;

            var all = await _repository.GetAllAsync();
            return all.Skip((page - 1) * amount).Take(amount).ToList();
        }

        public async Task<bool> UpdateAsync(T element)
        {
            await _repository.Update(element);
            return true;
        }

        public async Task<bool> RemoveAsync(T element)
        {
            await _repository.Delete(element);
            return true;
        }

        public async Task<bool> SaveAsync()
        {
            await _context.SaveChangesAsync();
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _repository.GetAllAsync().GetAwaiter().GetResult().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
