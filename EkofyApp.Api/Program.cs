using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.BackgroundJobs;
using EkofyApp.Infrastructure.DependencyInjections;
using EkofyApp.Infrastructure.Services.Chat;
using Hangfire;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;

namespace EkofyApp.Api;
public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load environment variables from .env file
        EnvironmentVariableLoader.LoadEnvironmentVariable();

        // Add services to the container.
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<RESTExceptionFilter>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Register Serilog 
        builder.Host.UseSerilog((hostingContext, LoggerConfiguration) =>
        {
            LoggerConfiguration
                .Enrich.With(new CustomDateFormatter())
                .ReadFrom.Configuration(hostingContext.Configuration)
                .WriteTo.Seq(Environment.GetEnvironmentVariable("SEQ_URL")!);
        });

        //Log.Logger = new LoggerConfiguration()
        //    .WriteTo.Async(a => a.File(@"F:\Logs\AEM\log.txt"))
        //    .CreateLogger();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddDependencyInjection();

        builder.Services.RegisterGraphQLServer();

        // Chưa config được bên dependency injection
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "EkofyApp API", Version = "v1" });
            options.CustomSchemaIds(type => type.FullName);

            // JWT ListenerRegisterRequest without requiring "Bearer " prefix
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token directly (without 'Bearer ' prefix)",
                BearerFormat= "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        

        var app = builder.Build();

        // Initialize policies or any other startup logic
        // TODO: Nhớ bỏ comment khi chạy thực tế
        using (IServiceScope scope = app.Services.CreateScope())
        {
            // Initialize Royalty Policy
            //IRoyaltyPolicyService royaltyPolicyService = scope.ServiceProvider.GetRequiredService<IRoyaltyPolicyService>();
            //royaltyPolicyService.InitializePolicyAsync().GetAwaiter().GetResult();

            // Initialize Legal Policy
            //ILegalPolicyService legalPolicyService = scope.ServiceProvider.GetRequiredService<ILegalPolicyService>();
            //legalPolicyService.InitializePolicyAsync().GetAwaiter().GetResult();
        }

        // Configure the HTTP request pipeline.
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
            //app.UseSwaggerUI();
        }

        app.UseHangfireDashboard("/hangfire");
        app.UseHangfireServer();
        app.ConfigureJobs();

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors("AllowAll");

        app.MapControllers();

        app.UseWebSockets();
        app.MapGraphQL("/graphql");

        app.MapHub<ChatHub>("/chat");

        app.UseStaticFiles();

        app.Run();
    }
}
