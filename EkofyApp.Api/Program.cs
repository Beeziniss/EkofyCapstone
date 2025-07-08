using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL.Mutation;
using EkofyApp.Api.GraphQL.Query;
using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Infrastructure.DependencyInjections;
using EkofyApp.Infrastructure.Services.Chat;
using Microsoft.OpenApi.Models;
using Serilog;

namespace EkofyApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load environment variables from .env file
            EnvironmentVariableLoader.LoadEnvironmentVariable();

            // Add services to the container.
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<BaseExceptionFilter>();
            });

            // Register Serilog 
            builder.Host.UseSerilog((hostingContext, LoggerConfiguration) =>
            {
                LoggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration);
            });

            //Log.Logger = new LoggerConfiguration()
            //    .WriteTo.Async(a => a.File(@"F:\Logs\AEM\log.txt"))
            //    .CreateLogger();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddDependencyInjection();

            // Chưa config được bên dependency injection
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new() { Title = "EkofyApp API", Version = "v1" });
                options.CustomSchemaIds(type => type.FullName);

                // JWT Authentication without requiring "Bearer " prefix
                options.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token directly (without 'Bearer ' prefix)",
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "JWT"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddGraphQLServer().AddErrorFilter<GraphQLExceptionFilter>()
                .AddAuthorization().AddType<UploadType>()
                // Nếu expose field có data type UInt32 thì cần phải bind nó vào GraphQL
                .AddType(new UInt32Type())
                .BindRuntimeType<uint, UInt32Type>()
                .AddMaxExecutionDepthRule(5).AddMaxAllowedFieldCycleDepthRule(50)
                .AddMongoDbFiltering().AddMongoDbSorting().AddMongoDbProjections().AddMongoDbPagingProviders()
                .AddQueryType<QueryInitialization>().AddMutationType<MutationInitialization>()
                .AddTypes();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            // Empty
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EkofyApp API V1");
                    options.RoutePrefix = string.Empty; // Set Swagger UI at the app's root

                    // Inject CSS để tùy chỉnh giao diện
                    options.InjectStylesheet("/swagger-dark-theme.css");
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("AllowAll");

            app.MapControllers();

            app.MapGraphQL("/graphql");

            app.MapHub<ChatHub>("/chat");

            app.UseStaticFiles();

            app.Run();
        }
    }
}
