using Hangfire;

namespace WearUp.Web.Infrastructure;
public static class DependencyInjection
{
    public static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        ConfigureSerilog(builder);
        ConfigureCache(builder);
        ConfigureOptions(builder);
        ConfigureMappings();
        ConfigureMvc(builder);
        ConfigureDatabase(builder);
        ConfigureRateLimiting(builder);
        ConfigureIdentity(builder);
        ConfigureAuthentication(builder);
        ConfigureStripe(builder);
        ConfigureSession(builder);
        ConfigureValidators(builder);
        ConfigureRepositories(builder);
        ConfigureHangfire(builder);
        return builder;
    }

    private static void ConfigureHangfire(WebApplicationBuilder builder)
    {
        builder.Services.AddHangfireServer();
        builder.Services.AddHangfire(config =>
            config.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
    }
    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();
        builder.Host.UseSerilog();
    }

    private static void ConfigureCache(WebApplicationBuilder builder)
    {
        builder.Services.AddHybridCache();
    }

    private static void ConfigureOptions(WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<EmailSettings>()
            .Bind(builder.Configuration.GetSection(nameof(EmailSettings)))
            .ValidateOnStart();

        builder.Services.AddOptions<GoogleSettings>()
            .Bind(builder.Configuration.GetSection(nameof(GoogleSettings)))
            .ValidateOnStart();

        builder.Services.AddOptions<StripeSettings>()
            .Bind(builder.Configuration.GetSection(nameof(StripeSettings)))
            .ValidateOnStart();

        builder.Services.AddOptions<CloudinarySettings>()
            .Bind(builder.Configuration.GetSection(nameof(CloudinarySettings)))
            .ValidateOnStart();
    }

    private static void ConfigureMappings()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(CategoryMapping).Assembly);
    }

    private static void ConfigureMvc(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<ApplicationDbContext>(
            options => options.UseSqlServer(connectionString)
        );
    }

    private static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RatelimitingHelpers.GetPartitionKey(context, "userName"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy("register", context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RatelimitingHelpers.GetPartitionKey(context, "userName"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy("forgotPassword", context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RatelimitingHelpers.GetPartitionKey(context, "email"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy("resendEmailConfirmation", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RatelimitingHelpers.GetPartitionKey(context, "email"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var actionName = context.HttpContext.GetEndpoint()?
                                    .Metadata
                                    .GetMetadata<ControllerActionDescriptor>()?
                                    .ActionName;

                int retryAfterSeconds = actionName?.ToLower() switch
                {
                    "login" => 60,
                    "register" => 60,
                    "forgotpassword" => 300,
                    "resendemailconfirmation" => 900,
                    _ => 60
                };

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Redirect($"/Home/TooManyRequests?retryAfterSeconds={retryAfterSeconds}");
                await Task.CompletedTask;
            };
        });
    }

    private static void ConfigureIdentity(WebApplicationBuilder builder)
    {
        // make password simple for testing only
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(op =>
        {
            op.Password.RequiredUniqueChars = 0;
            op.Password.RequireNonAlphanumeric = false;
            op.Password.RequireUppercase = false;
            op.Password.RequireDigit = false;
            op.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "Cookies";
        })
        .AddCookie("Cookies")
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            var googleSettings = builder.Configuration.GetSection(nameof(GoogleSettings)).Get<GoogleSettings>();
            options.ClientId = googleSettings!.ClientId;
            options.ClientSecret = googleSettings.ClientSecret;
        });

        builder.Services.Configure<CookieAuthenticationOptions>("Cookies", options =>
        {
            options.LoginPath = "/Auths/Login";
            options.LogoutPath = "/Auths/Logout";
            options.AccessDeniedPath = "/Auths/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(2);
        });
    }

    private static void ConfigureStripe(WebApplicationBuilder builder)
    {
        var stripeSettings = builder.Configuration.GetSection(nameof(StripeSettings)).Get<StripeSettings>();
        StripeConfiguration.ApiKey = stripeSettings!.Secretkey;
    }

    private static void ConfigureSession(WebApplicationBuilder builder)
    {
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession();
        builder.Services.AddHttpContextAccessor();
    }

    private static void ConfigureValidators(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IValidator<CreateCategoryVM>, CreateCategoryVMValidator>();
        builder.Services.AddScoped<IValidator<EditCategoryVM>, EditCategoryVMValidator>();
        builder.Services.AddScoped<IValidator<CreateProductVM>, CreateProductVMValidator>();
        builder.Services.AddScoped<IValidator<ConfirmEmailVM>, ConfirmEmailVMValidator>();
        builder.Services.AddScoped<IValidator<ResendEmailConfirmationVM>, ResendEmailConfirmationVMValidator>();
        builder.Services.AddScoped<IValidator<ForgotPasswordVM>, ForgotPasswordVMValidator>();
        builder.Services.AddScoped<IValidator<ResetPasswordVM>, ResetPasswordVMValidator>();
    }

    private static void ConfigureRepositories(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<GeneralRepository>();
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
        builder.Services.AddScoped<ICartRepository, CartRepository>();
        builder.Services.AddScoped<IEmailRepository, EmailRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
        builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
        builder.Services.AddScoped<IProductRatingRepository, ProductRatingRepository>();
        builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        builder.Services.AddScoped<IProductRecommenderRepository, ProductRecommenderRepository>();
        builder.Services.AddScoped<IApplicaionUserRepository,ApplicationUserRepository>();
        builder.Services.AddScoped<IRoleRepository,RoleRepository>();
        builder.Services.AddScoped<IGeneralRepository, GeneralRepository>();
        builder.Services.AddScoped<IBackgroundJobsRepository, BackgroundJobsRepository>();
        builder.Services.AddScoped<ICloudinaryRepository, CloudinaryRepository>();
        builder.Services.AddScoped<IModelTrainerRepository, ModelTrainerRepository>();

    }
}