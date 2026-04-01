using System.Collections.Concurrent;

namespace FoodStreetApp.Api.Repositories;

public class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly ConcurrentDictionary<int, T> _store = new();
    private readonly Func<T, int> _getId;
    private readonly Action<T, int> _setId;
    private int _idCounter;

    public InMemoryRepository(Func<T, int> getId, Action<T, int> setId)
    {
        _getId = getId;
        _setId = setId;
    }

    public Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        IReadOnlyCollection<T> items = _store.Values.ToList();
        return Task.FromResult(items);
    }

    public Task<T?> GetByIdAsync(int id)
    {
        _store.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<T> CreateAsync(T item)
    {
        var id = Interlocked.Increment(ref _idCounter);
        _setId(item, id);
        _store[id] = item;
        return Task.FromResult(item);
    }

    public Task<bool> UpdateAsync(int id, T item)
    {
        if (!_store.ContainsKey(id))
        {
            return Task.FromResult(false);
        }

        _setId(item, id);
        _store[id] = item;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var removed = _store.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}
