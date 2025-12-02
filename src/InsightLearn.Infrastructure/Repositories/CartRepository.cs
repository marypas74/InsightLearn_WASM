using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for CartItem entity
/// Handles shopping cart persistence for logged-in users
/// </summary>
public class CartRepository : ICartRepository
{
    private readonly InsightLearnDbContext _context;

    public CartRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CartItem>> GetCartItemsByUserIdAsync(Guid userId)
    {
        return await _context.CartItems
            .Include(c => c.Course)
                .ThenInclude(course => course!.Instructor)
            .Include(c => c.Course)
                .ThenInclude(course => course!.Category)
            .Include(c => c.Course)
                .ThenInclude(course => course!.Reviews)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.AddedAt)
            .ToListAsync();
    }

    public async Task<CartItem?> GetByIdAsync(Guid cartItemId)
    {
        return await _context.CartItems
            .Include(c => c.Course)
                .ThenInclude(course => course!.Instructor)
            .FirstOrDefaultAsync(c => c.Id == cartItemId);
    }

    public async Task<CartItem?> GetByUserAndCourseAsync(Guid userId, Guid courseId)
    {
        return await _context.CartItems
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);
    }

    public async Task<CartItem> AddAsync(CartItem cartItem)
    {
        cartItem.AddedAt = DateTime.UtcNow;
        _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync();
        return cartItem;
    }

    public async Task<CartItem> UpdateAsync(CartItem cartItem)
    {
        cartItem.UpdatedAt = DateTime.UtcNow;
        _context.CartItems.Update(cartItem);
        await _context.SaveChangesAsync();
        return cartItem;
    }

    public async Task<bool> RemoveAsync(Guid cartItemId)
    {
        var cartItem = await _context.CartItems.FindAsync(cartItemId);
        if (cartItem == null) return false;

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveByUserAndCourseAsync(Guid userId, Guid courseId)
    {
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);

        if (cartItem == null) return false;

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> ClearCartAsync(Guid userId)
    {
        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any()) return 0;

        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();
        return cartItems.Count;
    }

    public async Task<int> GetCartItemCountAsync(Guid userId)
    {
        return await _context.CartItems.CountAsync(c => c.UserId == userId);
    }

    public async Task<bool> IsCourseInCartAsync(Guid userId, Guid courseId)
    {
        return await _context.CartItems
            .AnyAsync(c => c.UserId == userId && c.CourseId == courseId);
    }

    public async Task<IEnumerable<CartItem>> GetItemsWithPriceChangesAsync(Guid userId)
    {
        return await _context.CartItems
            .Include(c => c.Course)
            .Where(c => c.UserId == userId && c.Course != null && c.PriceAtAddition != c.Course.CurrentPrice)
            .ToListAsync();
    }

    public async Task<int> RefreshCartPricesAsync(Guid userId)
    {
        var cartItems = await _context.CartItems
            .Include(c => c.Course)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        int updatedCount = 0;
        foreach (var item in cartItems)
        {
            if (item.Course != null && item.PriceAtAddition != item.Course.CurrentPrice)
            {
                item.PriceAtAddition = item.Course.CurrentPrice;
                item.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return updatedCount;
    }

    public async Task<int> RemoveEnrolledCoursesAsync(Guid userId)
    {
        // Get course IDs the user is already enrolled in
        var enrolledCourseIds = await _context.Enrollments
            .Where(e => e.UserId == userId &&
                       (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed))
            .Select(e => e.CourseId)
            .ToListAsync();

        if (!enrolledCourseIds.Any()) return 0;

        // Remove cart items for enrolled courses
        var itemsToRemove = await _context.CartItems
            .Where(c => c.UserId == userId && enrolledCourseIds.Contains(c.CourseId))
            .ToListAsync();

        if (!itemsToRemove.Any()) return 0;

        _context.CartItems.RemoveRange(itemsToRemove);
        await _context.SaveChangesAsync();
        return itemsToRemove.Count;
    }
}