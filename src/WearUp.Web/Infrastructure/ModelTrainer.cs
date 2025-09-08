using Hangfire;

namespace WearUp.Web.Infrastructure;

public static class ModelTrainer
{
    public async static Task Train(IServiceProvider serviceProvider)
    {
        var scope = serviceProvider.CreateScope();
        var trainer = scope.ServiceProvider.GetRequiredService<IProductRecommenderRepository>();
        var ratingsRepo = scope.ServiceProvider.GetRequiredService<IProductRatingRepository>();
        var ratings = await ratingsRepo.GetAllProductRatingsAsync();
        BackgroundJob.Enqueue<IProductRecommenderRepository>(x => x.Train(ratings));
        //RecurringJob.AddOrUpdate("Train", () => trainer.Train(ratings), Cron.Weekly);
    }
}
