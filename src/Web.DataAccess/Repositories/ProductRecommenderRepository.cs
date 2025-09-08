using Microsoft.ML;
using Microsoft.ML.Trainers;
using Web.Entites.ViewModels.RecommendationSystem;

namespace Web.DataAccess.Repositories;

public class ProductRecommenderRepository : IProductRecommenderRepository
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private readonly string _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "mlmodels", "productRecommender.zip");

    public ProductRecommenderRepository()
    {
        _mlContext = new MLContext();
        if (File.Exists(_modelPath))
            _model = _mlContext.Model.Load(_modelPath, out var _);
    }

    public void Train(IEnumerable<ProductRating> ratings)
    {
        var data = ratings.Select(r => new ProductRatingInput
        {
            UserId = r.UserId,
            ProductId = r.ProductId,
            Label = r.Rating
        });

        var trainData = _mlContext.Data.LoadFromEnumerable(data);

        var options = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = "UserKey",
            MatrixRowIndexColumnName = "ProductKey",
            LabelColumnName = "Label",
            NumberOfIterations = 20,
            ApproximationRank = 100
        };

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "UserKey", inputColumnName: nameof(ProductRatingInput.UserId))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "ProductKey", inputColumnName: nameof(ProductRatingInput.ProductId)))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(options));

        _model = pipeline.Fit(trainData);

        Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
        _mlContext.Model.Save(_model, trainData.Schema, _modelPath);
    }
    public float Predict(string userId, int productId)
    {
        if (_model == null)
            throw new InvalidOperationException("Model has not been trained.");

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<ProductRatingInput, ProductPrediction>(_model);

        var prediction = predictionEngine.Predict(new ProductRatingInput
        {
            UserId = userId,
            ProductId = productId
        });

        return prediction.Score;
    }

    public List<float> PredictBatch(string userId, IEnumerable<int> productIds)
    {
        if (_model == null)
            throw new InvalidOperationException("Model has not been trained.");

        var inputs = productIds.Select(p => new ProductRatingInput { UserId = userId, ProductId = p }).ToList();
        var data = _mlContext.Data.LoadFromEnumerable(inputs);
        var predictions = _model.Transform(data);
        var result= _mlContext.Data.CreateEnumerable<ProductPrediction>(predictions, reuseRowObject: false)
                              .Select(x => x.Score)
                              .ToList();
        return result;
    }
}

