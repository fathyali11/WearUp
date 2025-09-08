using Hangfire;

namespace WearUp.Web.Infrastructure;

public static class ModelTrainerSchedule
{
    public static void Train(IRecurringJobManager _recurringJobManager)
    {
        _recurringJobManager.AddOrUpdate<ModelTrainerRepository>("train",
            x => x.ExcuteAsync(),
            Cron.Weekly
            );
    }
}
