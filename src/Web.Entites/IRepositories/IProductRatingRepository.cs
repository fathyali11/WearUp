namespace Web.Entites.IRepositories;
public interface IProductRatingRepository
{
    Task AddOrUpdateRatingAsync(string userId, int productId, int rating, CancellationToken cancellationToken = default);
    Task<bool> UpdateRatingsForPurchaseAsync(string userId, List<int> productIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<int>> GetAllProductIdsForProductRatingsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<int>> GetAllProductIdsForProductRatingsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductRating>> GetAllProductRatingsAsync(CancellationToken cancellationToken = default);
}
