using Hangfire;

var builder = WebApplication.CreateBuilder(args);
builder = DependencyInjection.ConfigureServices(builder);

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseHangfireDashboard("/hangfire");
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    ModelTrainerSchedule.Train(recurringJobManager);
}
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();