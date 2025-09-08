
namespace Web.DataAccess.Repositories;
public class ProductRatingRepository(ApplicationDbContext _context
    ,HybridCache _hybridCache) : IProductRatingRepository
{
    public async Task AddOrUpdateRatingAsync(string userId, int productId, int rating, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ProductRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId, cancellationToken);

        if (existing is null)
        {
            await _context.ProductRatings.AddAsync(new ProductRating
            {
                UserId = userId,
                ProductId = productId,
                Rating = rating
            }, cancellationToken);
        }
        else
        {
            if(rating==RatingNumbers.RemoveFromWishlist)
            {
                var isInCart = await _context.CartItems.AsNoTracking()
                .AnyAsync(x => x.ProductId == productId && x.Cart.UserId == userId, cancellationToken);

                if (isInCart)
                    return;
            }
            


            await _context.ProductRatings
                .Where(r => r.ProductId == productId && r.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Rating, rating)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await RemoveKeys(cancellationToken);
    }

    public async Task<bool> UpdateRatingsForPurchaseAsync(string userId, List<int> productIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || productIds == null || productIds.Count == 0)
            return false;

        var affectedRows = await _context.ProductRatings
            .Where(r => r.UserId == userId && productIds.Contains(r.ProductId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Rating, RatingNumbers.BuyItem)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
        await RemoveKeys(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<IEnumerable<int>> GetAllProductIdsForProductRatingsAsync(CancellationToken cancellationToken=default)
    {
        var cacheKey = ProductRatingCacheKeys.AllRatings;
        var ratings = await _hybridCache.GetOrCreateAsync(cacheKey,
            async _=> await _context.ProductRatings
            .AsNoTracking()
            .Select(x=>x.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken),
            tags: [ProductRatingCacheKeys.RatingsTag],
            cancellationToken:cancellationToken);

        return ratings is not null ? ratings : [];
    }

    public async Task<IEnumerable<int>> GetAllProductIdsForProductRatingsForUserAsync(string userId,CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ProductRatingCacheKeys.AllRatings}_{userId}";
        var ratings = await _hybridCache.GetOrCreateAsync(cacheKey,
            async _ => await _context.ProductRatings
            .AsNoTracking()
            .Where(x=>x.UserId==userId)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken),
            tags: [ProductRatingCacheKeys.RatingsTag],
            cancellationToken: cancellationToken);

        return ratings is not null ? ratings : [];
    }

    public async Task<IEnumerable<ProductRating>> GetAllProductRatingsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = ProductRatingCacheKeys.AllRatings;
        var ratings = await _hybridCache.GetOrCreateAsync(cacheKey,
            async _ => await _context.ProductRatings
            .AsNoTracking()
            .Distinct()
            .ToListAsync(cancellationToken),
            tags: [ProductRatingCacheKeys.RatingsTag],
            cancellationToken: cancellationToken);

        return ratings is not null ? ratings : [];
    }

    private async Task RemoveKeys(CancellationToken cancellationToken)
    {
        await _hybridCache.RemoveByTagAsync(ProductRatingCacheKeys.RatingsTag, cancellationToken);
    }
}
