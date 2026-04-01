namespace FoodStreetApp.Api.Repositories;

public interface IRepository<T>
{
    Task<IReadOnlyCollection<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> CreateAsync(T item);
    Task<bool> UpdateAsync(int id, T item);
    Task<bool> DeleteAsync(int id);
}
