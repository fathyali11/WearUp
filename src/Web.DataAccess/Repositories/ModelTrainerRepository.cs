namespace Web.DataAccess.Repositories;
public class ModelTrainerRepository(IProductRecommenderRepository _productRecommenderRepository,
    IProductRatingRepository _productRatingRepository): IModelTrainerRepository
{
    public async Task ExcuteScheduleAsync()
    {
        var ratings = await _productRatingRepository.GetAllProductRatingsAsync();
        _productRecommenderRepository.Train(ratings);
    }
}
