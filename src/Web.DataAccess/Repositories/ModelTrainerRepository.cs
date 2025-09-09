namespace Web.DataAccess.Repositories;
public class ModelTrainerRepository(IProductRecommenderRepository _productRecommenderRepository,
    IProductRatingRepository _productRatingRepository,
    IBackgroundJobsRepository _backgroundJobsRepository,
    HybridCache _hybridCache): IModelTrainerRepository
{
    public async Task ExcuteScheduleAsync()
    {
        var ratings = await _productRatingRepository.GetAllProductRatingsAsync();
        _productRecommenderRepository.Train(ratings);
        await RemoveCacheKeys();
    }
    public async Task EnqueueModelToTrain()
    {
        var ratings = await _productRatingRepository.GetAllProductRatingsAsync();
        _backgroundJobsRepository.Enqueue<IProductRecommenderRepository>(x=>x.Train(ratings));
        await RemoveCacheKeys();
    }
    private async Task RemoveCacheKeys()
    {
        await _hybridCache.RemoveByTagAsync(ProductCacheKeys.RecommendationsTag);
        await _hybridCache.RemoveByTagAsync(ProductRatingCacheKeys.RatingsTag);
    }
}
