using AKAVER_Server.Classes.FileInfo;
using AKAVER_Server.Classes.PythonExecution;
using AKAVER_Server.Contexts;
using AKAVER_Server.Controllers;
using AKAVER_Server.Interfaces.IFileInfo;
using AKAVER_Server.Interfaces.IFileService;
using AKAVER_Server.Interfaces.IPythonExecutor;
using AKAVER_Server.Interfaces.IUser;
using AKAVER_Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.Configure<FileStorageConfig>(
	builder.Configuration.GetSection("FileStorage"));

builder.Services.AddSingleton<IFileStorageConfig>(sp =>
	sp.GetRequiredService<IOptions<FileStorageConfig>>().Value);

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer   = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                throw new ArgumentNullException("Jwt:Key")))
    };

    // Логирование для отладки
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"User {userId} authenticated successfully");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IPythonEnvironmentHelper, PythonEnvironmentHelper>();

builder.Services.Configure<PythonExecutorConfig>(
    builder.Configuration.GetSection("PythonExecutorConfig"));

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
});

builder.Services.AddSingleton<IPythonExecutorService>(provider =>
{
    var config = provider.GetRequiredService<IOptions<PythonExecutorConfig>>().Value;
    var pythonEnvironmentHelper = provider.GetRequiredService<IPythonEnvironmentHelper>();

    var service = new PythonExecutorService(
        config.MaxConcurrentExecutions,
        pythonEnvironmentHelper);

    service.Configure(c =>
    {
        c.FileName = config.FileName;
        c.WorkingDirectory = string.IsNullOrEmpty(config.WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : config.WorkingDirectory;
        c.CsvInputFolder         = config.CsvInputFolder;
        c.CsvOutputFolder        = config.CsvOutputFolder;
        c.TimeoutSeconds         = config.TimeoutSeconds;
        c.RedirectStandardOutput = config.RedirectStandardError;
        c.RedirectStandardError  = config.RedirectStandardError;
        c.OutputEncoding         = config.OutputEncoding;
    });

    return service;
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AKAVER API",
        Version = "v1",
        Description = "API для системы AKAVER"
    });

    // Cхема безопасности
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT-токен в формате: Bearer {ваш_токен}"
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        options.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
    });
}

app.UseCors("AllowAllOrigins");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();