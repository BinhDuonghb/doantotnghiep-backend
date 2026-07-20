using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace DOANTOTNGHIEP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "DOANTOTNGHIEP API", Version = "v1" });

                // Add Bearer token authentication
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and your token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
                });

                // Apply Bearer auth globally
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // Register health check service
            builder.Services.AddSingleton<DOANTOTNGHIEP.src.Services.HealthCheckService>();

            // Register authentication services
            builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
            builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IQuizRepository, QuizRepository>();
            builder.Services.AddScoped<IQuizService, QuizService>();
            builder.Services.AddScoped<ITargetRepository, TargetRepository>();
            builder.Services.AddScoped<ITargetService, TargetService>();

            // Register AI Quiz Service with typed HttpClient
            builder.Services.AddHttpClient<IAiAssistantService, AiAssistantService>(client =>
            {
                // Base address and auth header are set in AiAssistantService constructor
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            // Register AI Quiz settings via IOptions pattern
            builder.Services.Configure<AiAssistantSettings>(builder.Configuration.GetSection(AiAssistantSettings.SectionName));

            // Register Prompt Template Settings via IOptions pattern
            builder.Services.Configure<PromptTemplateSettings>(
                builder.Configuration.GetSection(PromptTemplateSettings.SectionName));

            // Register Prompt Template Service
            builder.Services.AddSingleton<IPromptTemplateService, PromptTemplateService>();

            // Configure JWT authentication
            var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing in configuration");
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer is missing in configuration");
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience is missing in configuration");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Return ProblemDetails JSON body on 401 challenges
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        // Skip default WWW-Authenticate header behavior
                        context.HandleResponse();

                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var problemDetails = new
                        {
                            title = "Unauthorized",
                            status = 401,
                            detail = context.ErrorDescription ?? "Invalid or missing access token."
                        };

                        await context.Response.WriteAsJsonAsync(problemDetails);
                    }
                };
            });

            // Configure authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AuthenticatedUser", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim(ClaimTypes.NameIdentifier));

                options.AddPolicy("Admin", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim(ClaimTypes.NameIdentifier)
                          .RequireClaim(ClaimTypes.Role, "Admin"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}