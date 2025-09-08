namespace Web.DataAccess.Repositories;

public class RecommendationRepository(
    IProductRatingRepository _productRatingRepository,
    IProductRecommenderRepository _productRecommenderRepository,
    HybridCache _hybridCache) : IRecommendationRepository
{

    public async Task<List<(int productId, float score)>> GetTopRecommendationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var allProducts = await _productRatingRepository.GetAllProductIdsForProductRatingsAsync(cancellationToken);
        if (!allProducts.Any())
            return [];

        var userProducts = await _productRatingRepository.GetAllProductIdsForProductRatingsForUserAsync(userId, cancellationToken);

        string cacheKey = $"{ProductCacheKeys.ProductIdsAndScore}_{userId}";

        var recommendations = await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                var candidates = allProducts.Except(userProducts).ToList();
                if (!candidates.Any())
                    return new List<(int, float)>();

                List<(int productId, float score)> result;

                try
                {
                    var scores = _productRecommenderRepository.PredictBatch(userId, candidates);
                    result = candidates.Zip(scores, (id, score) => (id, score))
                    .Where(x => !float.IsNaN(x.Item2) && !float.IsInfinity(x.Item2))
                                       .OrderByDescending(x => x.score)
                                       .Take(5)
                                       .ToList();
                }
                catch (InvalidOperationException)
                {
                    result = candidates.Take(5).Select(p => (p, 0.0f))
                    .ToList();
                }

                return await Task.FromResult(result);
            },
            tags: [$"{ProductCacheKeys.RecommendationsTag}"],
            cancellationToken: cancellationToken
        );

        return recommendations;
    }
}
