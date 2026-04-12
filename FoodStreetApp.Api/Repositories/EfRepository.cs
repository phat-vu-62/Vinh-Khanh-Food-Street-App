using Microsoft.EntityFrameworkCore;
using FoodStreetApp.Shared.Context;
using FoodStreetApp.Api.Interfaces;

namespace FoodStreetApp.Api.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly CmsDbContext _context;
    private readonly DbSet<T> _dbSet;

    public EfRepository(CmsDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<T> CreateAsync(T item)
    {
        await _dbSet.AddAsync(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateAsync(int id, T item)
    {
        _context.Entry(item).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _dbSet.FindAsync(id);
        if (item == null) return false;

        _dbSet.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }
}
