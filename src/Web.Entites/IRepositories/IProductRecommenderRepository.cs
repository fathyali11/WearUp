namespace Web.Entites.IRepositories;
public interface IProductRecommenderRepository
{
    void Train(IEnumerable<ProductRating> ratings);
    float Predict(string userId, int productId);
    List<float> PredictBatch(string userId, IEnumerable<int> productIds);
}
