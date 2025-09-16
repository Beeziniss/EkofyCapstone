using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Audio;
using CloudinaryDotNet;
using EkofyApp.Application.DatabaseContext;
using EkofyApp.Application.Mappers;
using EkofyApp.Application.Models;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Application.ServiceInterfaces.BillingPortalConfigurations;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Application.ServiceInterfaces.Coupons;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ServiceInterfaces.RequestHubs;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Cloudinary;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Momo;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Artist;
using EkofyApp.Domain.Enums.BillingPortalConfig;
using EkofyApp.Domain.Enums.Coupons;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings;
using EkofyApp.Domain.Settings.AWS;
using EkofyApp.Domain.Settings.Momo;
using EkofyApp.Domain.Settings.Redis;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services;
using EkofyApp.Infrastructure.Services.Artists;
using EkofyApp.Infrastructure.Services.Auth;
using EkofyApp.Infrastructure.Services.BillingPortalConfigurations;
using EkofyApp.Infrastructure.Services.Categories;
using EkofyApp.Infrastructure.Services.Chat;
using EkofyApp.Infrastructure.Services.Coupons;
using EkofyApp.Infrastructure.Services.Jobs;
using EkofyApp.Infrastructure.Services.Recordings;
using EkofyApp.Infrastructure.Services.RequestHubs;
using EkofyApp.Infrastructure.Services.Subscriptions;
using EkofyApp.Infrastructure.Services.Tracks;
using EkofyApp.Infrastructure.Services.Users;
using EkofyApp.Infrastructure.Services.UserSubscriptions;
using EkofyApp.Infrastructure.Services.Works;
using EkofyApp.Infrastructure.ThirdPartyServices.AWS;
using EkofyApp.Infrastructure.ThirdPartyServices.Cloudinaries;
using EkofyApp.Infrastructure.ThirdPartyServices.FFMPEG;
using EkofyApp.Infrastructure.ThirdPartyServices.Payment.Momo;
using EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
using EkofyApp.Infrastructure.ThirdPartyServices.Redis;
using FluentValidation;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refit;
using StackExchange.Redis;
using Stripe;
using Syncfusion.Licensing;
using System.Security.Claims;
using System.Text;

namespace EkofyApp.Infrastructure.DependencyInjections;
public static class DependencyInjection
{
    public static void AddDependencyInjection(this IServiceCollection services)
    {
        services.AddHangfire();

        //services.AddAutoMapper(typeof(MappingProfile)); // OLD VERSION 14.0.1
        services.AddAutoMapperExtension();
        services.AddSyncfusionExtension();
        services.AddStripeExtension();
        services.ConfigRoute();

        services.AddHttpContextAccessor();

        services.AddAuthentication();
        services.AddAuthorization();
        services.AddCors();

        services.AddDatabase();
        services.AddRedis();
        services.AddServices();

        services.AddGrpcClient();

        services.AddMomo();
        services.AddAmazonWebService();
        services.AddCloudinary();

        services.AddSignalR();

        services.AddValidation();
        services.AddEnumMemberSerializer();

        //services.AddSwaggerGen();
    }

    public static void AddStripeExtension(this IServiceCollection services)
    {
        // Load Stripes settings from environment variables
        string stripeApiKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY") ?? throw new UnconfiguredEnvironmentCustomException("STRIPE_API_KEY is not set in the environment");

        string secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? throw new UnconfiguredEnvironmentCustomException("STRIPE_SECRET_KEY is not set in the environment");

        StripeConfiguration.ApiKey = secretKey;

        StripeSetting stripeSetting = new()
        {
            AccountSigningSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET_ACCOUNT") ?? throw new UnconfiguredEnvironmentCustomException("STRIPE_WEBHOOK_SECRET_ACCOUNT is not set in the environment"),
            AccountV2SigningSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET_ACCOUNT_V2") ?? throw new UnconfiguredEnvironmentCustomException("STRIPE_WEBHOOK_SECRET_ACCOUNT_V2 is not set in the environment"),
            CustomerSigningSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET_CUSTOMER") ?? throw new UnconfiguredEnvironmentCustomException("STRIPE_WEBHOOK_SECRET_CUSTOMER is not set in the environment"),
            SubscriptionSigningSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET_SUBSCRIPTION") ?? throw new UnconfiguredEnvironmentCustomException("STRIPE_WEBHOOK_SECRET_SUBSCRIPTION is not set in the environment"),
            CheckoutSessionSigningSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET_CHECKOUT_SESSION") ?? throw new UnconfiguredEnvironmentCustomException("STRIPE_WEBHOOK_SECRET_CHECKOUT_SESSION is not set in the environment")
        };

        services.AddSingleton(stripeSetting);
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
    }

    public static void AddSyncfusionExtension(this IServiceCollection services)
    {
        string licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") ?? throw new UnconfiguredEnvironmentCustomException("SYNC_FUSION_LICENSE_KEY is not set in the environment");
        SyncfusionLicenseProvider.RegisterLicense(licenseKey);
    }

    public static void AddAutoMapperExtension(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.LicenseKey = Environment.GetEnvironmentVariable("AUTOMAPPER_LICENSE_KEY") ?? throw new UnconfiguredEnvironmentCustomException("AUTOMAPPER_LICENSE_KEY is not set in the environment");
            cfg.AddProfile<MappingProfile>();
        });
    }

    public static void AddValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ValidatorAssembly>();
    }

    public static void AddRedis(this IServiceCollection services)
    {
        string publicEndpoint = Environment.GetEnvironmentVariable("REDIS_PUBLIC_ENDPOINT") ?? throw new Exception("REDIS_PUBLIC_ENDPOINT is not set in the environment");

        string username = Environment.GetEnvironmentVariable("REDIS_USERNAME") ?? throw new UnconfiguredEnvironmentCustomException("REDIS_USERNAME is not set in the environment");

        string password = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? throw new Exception("REDIS_PASSWORD is not set in the environment");

        RedisSetting redisSetting = new()
        {
            ConnectionStringNoSSL = $"rediss://{username}:{password}@{publicEndpoint}",
            ConnectionStringSSL = $"rediss://{username}:{password}@{publicEndpoint}",
            PublicEndpoint = publicEndpoint,
            Username = username,
            Password = password
        };

        ConfigurationOptions options;
        if (HelperMethod.IsWindows())
        {
            options = new()
            {
                EndPoints = { publicEndpoint },
                User = username,
                Password = password,
                Ssl = false, // Set true nếu dùng trong môi trường Production hoặc an toàn như SSL/TLS
                AbortOnConnectFail = false
            };
        }
        else if (HelperMethod.IsLinux())
        {
            options = new()
            {
                EndPoints = { publicEndpoint },
                User = username,
                Password = password,
                Ssl = true, // Set true nếu dùng trong môi trường Production hoặc an toàn như SSL/TLS
                AbortOnConnectFail = false
            };
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system for Redis configuration.");
        }

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options));
        //services.AddSingleton<IConnectionMultiplexer>(sp =>
        //{
        //    string redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? throw new UnconfiguredEnvironmentCustomException("REDIS_CONNECTION_STRING is not set");

        //    return ConnectionMultiplexer.Connect(redisConnectionString);
        //});

        services.AddScoped(sp =>
        {
            IConnectionMultiplexer multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            return multiplexer.GetDatabase();
        });

        services.AddSingleton(redisSetting);
        services.AddScoped<IRedisCacheService, RedisCacheService>();
    }

    public static void AddGrpcClient(this IServiceCollection services)
    {
        // Register gRPC client with DI
        services.AddGrpcClient<AudioAnalyzer.AudioAnalyzerClient>(options =>
        {
            options.Address = new Uri(Environment.GetEnvironmentVariable("GRPC_CLIENT") ?? throw new UnconfiguredEnvironmentCustomException("GRPC_CLIENT is not set or configured"));

            // Set the maximum message size for gRPC
            options.ChannelOptionsActions.Add(channelOptions =>
            {
                // 50MB
                channelOptions.MaxReceiveMessageSize = 50 * 1024 * 1024;
                channelOptions.MaxSendMessageSize = 50 * 1024 * 1024;
            });
        });
    }

    public static void AddDatabase(this IServiceCollection services)
    {
        // Load MongoDB settings from environment variables
        string connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") ?? throw new UnconfiguredEnvironmentCustomException("Connection String Database is not set in the environment variables");
        string databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME") ?? throw new UnconfiguredEnvironmentCustomException("Database DisplayName is not set in the environment variables");

        // Register the MongoDB settings as a singleton
        MongoDbSetting mongoDbSettings = new()
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };

        // Register the MongoDBSetting with DI
        services.AddSingleton(mongoDbSettings);

        // Register MongoClient as singleton, sharing the connection across all usages
        services.AddSingleton<IMongoClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MongoDbLogger>>();

            MongoClientSettings settings = MongoClientSettings.FromConnectionString(mongoDbSettings.ConnectionString);

            #region Logging configuration
            settings.ClusterConfigurator = builder =>
            {
                builder.Subscribe<CommandStartedEvent>(e =>
                {
                    // Lọc các field không cần thiết
                    BsonDocument filtered = [.. e.Command.Elements
                            .Where(el =>
                                el.Name != "lsid" &&
                                el.Name != "signature" &&
                                el.Name != "$clusterTime")];
                    //el.DisplayName != "$db" &&
                    //el.DisplayName != "cursor")];

                    // Log JSON format sau khi đã lọc
                    string json = JsonConvert.SerializeObject(JObject.Parse(filtered.ToJson()), Formatting.Indented);
                    logger.LogInformation("[MongoDB Command] {CommandName}:\n{Json}", e.CommandName, json);
                });

                // Optional: log command success/failure
                builder.Subscribe<CommandSucceededEvent>(e =>
                {
                    logger.LogInformation("[MongoDB Succeeded] {CommandName} - Duration: {Duration}", e.CommandName, e.Duration);
                });

                builder.Subscribe<CommandFailedEvent>(e =>
                {
                    logger.LogError(e.Failure, "[MongoDB Failed] {CommandName}", e.CommandName);
                });
            };
            return new MongoClient(settings);
            #endregion

            //return new MongoClient(mongoDbSettings.ConnectionString);
        });
        //services.AddSingleton<IMongoClient>(_lazyClient.Value);

        // Register IMongoDatabase as a scoped service
        services.AddScoped(sp =>
        {
            IMongoClient client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoDbSettings.DatabaseName);
        });

        // Register the MongoDB context (or client)
        services.AddSingleton<EkofyDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    public static void ConfigRoute(this IServiceCollection services)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
        });

        services.Configure<DataProtectionTokenProviderOptions>(otp =>
        {
            otp.TokenLifespan = TimeSpan.FromMinutes(3);
        });
    }

    public static void AddServices(this IServiceCollection services)
    {
        // Business Services
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<IAudioAnalysisService, AudioFeatureService>();
        services.AddScoped<IAudioFingerprintService, AudioFingerprintService>();
        services.AddScoped<IJsonWebToken, JsonWebToken>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEffectiveEntitlementService, EffectiveEntitlementService>();
        services.AddScoped<ISubscriptionService, Services.Subscriptions.SubscriptionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWorkService, WorkService>();
        services.AddScoped<IRecordingService, RecordingService>();
        services.AddScoped<IRequestHubService, RequestHubService>();
        services.AddScoped<ICouponCustomService, CouponCustomService>();
        services.AddScoped<IBillingPortalConfigurationService, BillingPortalConfigurationService>();
        services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
        services.AddScoped<IEffectiveEntitlementService, EffectiveEntitlementService>();
        //services.AddScoped<IChatService, ChatService>();

        // GraphQL Services
        services.AddScoped<IChatGraphQLService, ChatGraphQLService>();

        // Third Party Services
        services.AddScoped<IFfmpegService, FfmpegService>();
    }

    public static void AddCloudinary(this IServiceCollection services)
    {
        // Get the Cloudinary URL from the environment variables loaded by .env
        string? cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
        if (string.IsNullOrEmpty(cloudinaryUrl))
        {
            throw new UnconfiguredEnvironmentCustomException("Cloudinary URL is not set in the environment variables");
        }

        // Initialize Cloudinary instance
        Cloudinary cloudinary = new(cloudinaryUrl)
        {
            Api = { Secure = true }
        };

        // Register the Cloudinary with DI
        services.AddSingleton(provider => cloudinary);

        // Register Cloudinary in DI container as a scoped service
        services.AddScoped<CloudinaryService>();

        services.AddScoped<ICloudinaryService, CloudinaryService>();
    }

    public static void AddAuthentication(this IServiceCollection services)
    {
        // Config the Google Identity
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        }).AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = Environment.GetEnvironmentVariable("Authentication_Google_ClientId") ?? throw new Exception("Google's ClientId property is not set in environment or not found");
            googleOptions.ClientSecret = Environment.GetEnvironmentVariable("Authentication_Google_ClientSecret") ?? throw new Exception("Google's Client Secret property is not set in environment or not found");

        }).AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                //tự cấp token
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,

                // Các issuer và audience hợp lệ
                //ValidIssuers = [Environment.GetEnvironmentVariable("JWT_ISSUER_PRODUCTION"), "https://localhost:7018"],
                //ValidAudiences = [Environment.GetEnvironmentVariable("JWT_AUDIENCE_PRODUCTION"), Environment.GetEnvironmentVariable("JWT_AUDIENCE_PRODUCTION_BE"), "https://localhost:7018"],

                //ký vào token
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWTSettings_SecretKey") ?? throw new Exception("JWT's Secret CancelMode property is not set in environment or not found"))),

                ClockSkew = TimeSpan.Zero,

                // Đặt RoleClaimType
                RoleClaimType = ClaimTypes.Role
            };

            // Cấu hình SignalR để đọc token
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Lấy origin từ request
                    string? origin = context.Request.Headers.Origin;

                    // Các origin được phép truy cập
                    IEnumerable<string?> securedOrigins = new[]
                    {
                        Environment.GetEnvironmentVariable("SPOTIFY_HUB_CORS_ORIGIN_FE_PRODUCTION"),
                        Environment.GetEnvironmentVariable("SPOTIFY_HUB_CORS_ORIGIN_FE_01_DEVELOPMENT"),
                        Environment.GetEnvironmentVariable("PAY_OS_CORE_ORIGIN")
                    }.Where(origin => !string.IsNullOrWhiteSpace(origin));

                    // Kiểm tra xem origin có trong danh sách được phép không
                    if (string.IsNullOrWhiteSpace(origin) || !securedOrigins.Any(securedOrigin => securedOrigin is not null && securedOrigin.Equals(origin, StringComparison.Ordinal)))
                    {
                        return Task.CompletedTask;
                    }

                    // Query chứa token, sử dụng nó
                    string? accessToken = context.Request.Query["access_token"];
                    PathString path = context.HttpContext.Request.Path;

                    // Các segment được bảo mật
                    IEnumerable<string?> securedSegments = new[]
                    {
                        Environment.GetEnvironmentVariable("SPOTIFYPOOL_HUB_COUNT_STREAM_URL"),
                        Environment.GetEnvironmentVariable("SPOTIFYPOOL_HUB_PLAYLIST_URL"),

                    }.Where(segment => !string.IsNullOrWhiteSpace(segment)); // Lọc ra các segment không rỗng

                    // Kiểm tra xem path có chứa segment cần xác thực không
                    if (!string.IsNullOrWhiteSpace(accessToken) && securedSegments.Any(segment => path.StartsWithSegments($"/{segment}", StringComparison.Ordinal)))
                    {
                        //context.Token = accessToken["Bearer ".Length..].Trim(); // SubString()
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };

            // Remove "Bearer " prefix
            // Chỉ remove Bearer prefix khi đang trong môi trường phát triển hoặc debug
            //opt.Events = new JwtBearerEvents
            //{
            //    OnMessageReceived = context =>
            //    {
            //        // Check if the token is present without "Bearer" prefix
            //        if (context.Request.Headers.ContainsKey("Authorization"))
            //        {
            //            var token = context.Request.Headers.Authorization.ToString();
            //            if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            //            {
            //                context.Token = token; // Set token without "Bearer" prefix
            //            }
            //        }
            //        return Task.CompletedTask;
            //    }
            //};
        });
    }

    public static void AddAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder().AddPolicy("GoogleOrJwt", policy =>
        {
            policy.AddAuthenticationSchemes(GoogleDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
        }).AddPolicy("AdminPolicy", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Admin");
        }).AddPolicy("UserPolicy", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("User");
        }).AddPolicy("ArtistPolicy", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Artist");
        });
    }

    public static void AddCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    public static void AddMomo(this IServiceCollection services)
    {
        // Load Momo API URL from environment variables
        string momoApiUrlBase = Environment.GetEnvironmentVariable("MOMO_API_URL_BASE") ?? throw new UnconfiguredEnvironmentCustomException("Base Address is not set in the environment variables");

        // Register Refit client for Momo API
        services.AddRefitClient<IMomoApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(momoApiUrlBase));

        // Config the MomoSetting from environment variables
        MomoSetting momoSetting = new()
        {
            AccessKey = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY") ?? throw new UnconfiguredEnvironmentCustomException("Momo's Access Key is not set in the environment variables"),
            SecretKey = Environment.GetEnvironmentVariable("MOMO_SECRET_KEY") ?? throw new UnconfiguredEnvironmentCustomException("Momo's Secret Key is not set in the environment variables"),
            PartnerCode = Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE") ?? throw new UnconfiguredEnvironmentCustomException("Momo's Partner Code is not set in the environment variables"),
            ReturnUrl = Environment.GetEnvironmentVariable("MOMO_RETURN_URL") ?? throw new UnconfiguredEnvironmentCustomException("Momo's Return URL is not set in the environment variables"),
            NotifyUrl = Environment.GetEnvironmentVariable("MOMO_NOTIFY_URL") ?? throw new UnconfiguredEnvironmentCustomException("Momo's Notify URL is not set in the environment variables"),
            RequestTypeQR = Environment.GetEnvironmentVariable("MOMO_REQUEST_TYPE_QR") ?? throw new UnconfiguredEnvironmentCustomException("Momo's Request Type QR is not set in the environment variables"),
            RequestTypeVisa = Environment.GetEnvironmentVariable("MOMO_REQUEST_TYPE_VISA") ?? throw new UnconfiguredEnvironmentCustomException("Momo's Request Type Visa is not set in the environment variables")
        };

        // Register MomoSetting with DI
        services.AddSingleton(momoSetting);

        // Register MomoService with DI
        services.AddScoped<IMomoService, MomoService>();
    }

    #region AWS
    public static void AddAmazonWebService(this IServiceCollection services)
    {
        string accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? throw new UnconfiguredEnvironmentCustomException("AWS_ACCESS_KEY_ID not set");
        string secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? throw new UnconfiguredEnvironmentCustomException("AWS_SECRET_ACCESS_KEY not set");
        string region = Environment.GetEnvironmentVariable("AWS_REGION") ?? throw new UnconfiguredEnvironmentCustomException("AWS_REGION not set");

        BasicAWSCredentials awsCredentials = new(accessKey, secretKey);
        RegionEndpoint awsRegion = RegionEndpoint.GetBySystemName(region);

        // Thêm S3 Client
        services.AddSingleton<IAmazonS3>(provider => new AmazonS3Client(awsCredentials, awsRegion));

        // Thêm MediaConvert Client
        //services.AddSingleton<IAmazonMediaConvert>(provider => new AmazonMediaConvertClient(awsCredentials, awsRegion));

        // Config the AWS Client
        AWSSetting awsSetting = new()
        {
            BucketName = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME") ?? throw new UnconfiguredEnvironmentCustomException("BucketName is not set in environment"),
            Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? throw new UnconfiguredEnvironmentCustomException("Region is not set in environment"),
            CloudFrontDomainUrl = Environment.GetEnvironmentVariable("AWS_CLOUDFRONT_DOMAIN_URL") ?? throw new UnconfiguredEnvironmentCustomException("CloudFrontDomainUrl is not set in environment"),
            CloudFrontDistributionId = Environment.GetEnvironmentVariable("AWS_CLOUDFRONT_DISTRIBUTION_ID") ?? throw new UnconfiguredEnvironmentCustomException("CloudFrontDistributionId is not set in environment"),
            KeyPairId = Environment.GetEnvironmentVariable("AWS_CLOUDFRONT_KEY_PAIR_ID") ?? throw new UnconfiguredEnvironmentCustomException("KeyPairId is not set in environment"),
            ResourcePrefixStreaming = Environment.GetEnvironmentVariable("AWS_MASTER_PREFIX_STREAMING") ?? throw new UnconfiguredEnvironmentCustomException("ResourcePrefixStreaming is not set in environment"),
            ResourcePrefixOriginal = Environment.GetEnvironmentVariable("AWS_MASTER_PREFIX_KEY_ORIGINAL") ?? throw new UnconfiguredEnvironmentCustomException("ResourcePrefixOriginal is not set in environment"),
            ResourcePrefixTesting = Environment.GetEnvironmentVariable("AWS_MASTER_PREFIX_KEY_TESTING") ?? throw new UnconfiguredEnvironmentCustomException("ResourcePrefixTesting is not set in environment"),
        };

        // Register the AWSSetting with DI
        services.AddSingleton(awsSetting);

        // AWS
        services.AddScoped<IAmazonS3Service, AmazonS3Service>();
        services.AddScoped<IAmazonCloudFrontService, AmazonCloudFrontService>();
    }
    #endregion

    public static void AddEnumMemberSerializer(this IServiceCollection services)
    {
        // Category
        BsonSerializer.RegisterSerializer(typeof(CategoryType), new EnumMemberSerializer<CategoryType>());

        // User
        //BsonSerializer.RegisterSerializer(typeof(UserProduct), new EnumMemberSerializer<UserProduct>());
        BsonSerializer.RegisterSerializer(typeof(UserRole), new EnumMemberSerializer<UserRole>());
        BsonSerializer.RegisterSerializer(typeof(UserStatus), new EnumMemberSerializer<UserStatus>());
        BsonSerializer.RegisterSerializer(typeof(UserGender), new EnumMemberSerializer<UserGender>());

        // Artist
        BsonSerializer.RegisterSerializer(typeof(ArtistType), new EnumMemberSerializer<ArtistType>());
        BsonSerializer.RegisterSerializer(typeof(ArtistRole), new EnumMemberSerializer<ArtistRole>());

        // Tracks
        //BsonSerializer.RegisterSerializer(typeof(PlaylistName), new EnumMemberSerializer<PlaylistName>());
        //BsonSerializer.RegisterSerializer(typeof(RestrictionReason), new EnumMemberSerializer<RestrictionReason>());
        BsonSerializer.RegisterSerializer(typeof(MoodType), new EnumMemberSerializer<MoodType>());
        BsonSerializer.RegisterSerializer(typeof(TrackType), new EnumMemberSerializer<TrackType>());

        // Cloudinary
        //BsonSerializer.RegisterSerializer(typeof(AudioTagChild), new EnumMemberSerializer<AudioTagChild>());
        //BsonSerializer.RegisterSerializer(typeof(AudioTagParent), new EnumMemberSerializer<AudioTagParent>());
        //BsonSerializer.RegisterSerializer(typeof(ImageTag), new EnumMemberSerializer<ImageTag>());

        // Album
        BsonSerializer.RegisterSerializer(typeof(AlbumType), new EnumMemberSerializer<AlbumType>());

        // Release
        BsonSerializer.RegisterSerializer(typeof(ReleaseStatus), new EnumMemberSerializer<ReleaseStatus>());

        // Reccomendation
        //BsonSerializer.RegisterSerializer(typeof(Algorithm), new EnumMemberSerializer<Algorithm>());

        // Document
        BsonSerializer.RegisterSerializer(typeof(DocumentType), new EnumMemberSerializer<DocumentType>());

        // Subscription
        BsonSerializer.RegisterSerializer(typeof(SubscriptionTier), new EnumMemberSerializer<SubscriptionTier>());
        BsonSerializer.RegisterSerializer(typeof(SubscriptionStatus), new EnumMemberSerializer<SubscriptionStatus>());
        BsonSerializer.RegisterSerializer(typeof(SubscriptionCycle), new EnumMemberSerializer<SubscriptionCycle>());

        // Coupon
        BsonSerializer.RegisterSerializer(typeof(CouponDurationType), new EnumMemberSerializer<CouponDurationType>());
        BsonSerializer.RegisterSerializer(typeof(CouponStatus), new EnumMemberSerializer<CouponStatus>());

        // BillingPortalConfiguration
        BsonSerializer.RegisterSerializer(typeof(StripeSubscriptionCancelMode), new EnumMemberSerializer<StripeSubscriptionCancelMode>());

        // Transaction
        BsonSerializer.RegisterSerializer(typeof(PaymentStatus), new EnumMemberSerializer<PaymentStatus>());
        BsonSerializer.RegisterSerializer(typeof(TransactionStatus), new EnumMemberSerializer<TransactionStatus>());

        // Common
        BsonSerializer.RegisterSerializer(typeof(EntitlementValueType), new EnumMemberSerializer<EntitlementValueType>());
        BsonSerializer.RegisterSerializer(typeof(RestrictionType), new EnumMemberSerializer<RestrictionType>());
        BsonSerializer.RegisterSerializer(typeof(CurrencyType), new EnumMemberSerializer<CurrencyType>());
        BsonSerializer.RegisterSerializer(typeof(PeriodTime), new EnumMemberSerializer<PeriodTime>());
        BsonSerializer.RegisterSerializer(typeof(PaymentMethodType), new EnumMemberSerializer<PaymentMethodType>());
    }

    public static void AddHangfire(this IServiceCollection service)
    {
        //lấy đường dẫn Mongo làm storage cho hangfire
        var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") ?? throw new UnconfiguredEnvironmentCustomException("Connection String Database is not set in the environment variables");

        var DatabaseName = Environment.GetEnvironmentVariable("MONGODB_HANGFIRE_STORAGE");

        //đăng ký Hangfire với MongoDB
        service.AddHangfire(configuration => configuration
            //mức tương thích dữ liệu của Hangfire 
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            //cấu hình Storage
            .UseMongoStorage(mongoConnectionString.ToString(), DatabaseName, new MongoStorageOptions
            {
                MigrationOptions = new MongoMigrationOptions
                {
                    //tự động xử lý khi có sự thay đổi cấu trúc dữ liệu
                    MigrationStrategy = new MigrateMongoMigrationStrategy(),
                    //sao lưu dữ liệu cũ trước khi thay đổi cấu trúc
                    BackupStrategy = new CollectionMongoBackupStrategy()
                },
                Prefix = "backgroundjobs.hangfire",
                CheckConnection = false,
                //CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
            }));

        //đăng ký Hangfire server
        service.AddHangfireServer(ServerOptions =>
        {
            ServerOptions.ServerName = "BackgroundJobs.Hangfire";
        });

        // Background Jobs Services
        service.AddTransient<IBackgoundService, BackgoundService>();
    }
}
