using AKAvR_IS.Classes.PythonExecution;
using AKAvR_IS.Contexts;
using AKAvR_IS.Controllers;
using AKAvR_IS.Interfaces.IPythonExecutor;
using AKAvR_IS.Interfaces.IUser;
using AKAvR_IS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.Configure<PythonExecutorConfig>(
    builder.Configuration.GetSection("PythonExecutorConfig"));

builder.Services.AddSingleton<IPythonExecutorService>(provider =>
{
    var config = provider.GetRequiredService<IOptions<PythonExecutorConfig>>().Value;

    var service = new PythonExecutorService(config.MaxConcurrentExecutions);
    service.Configure(c =>
    {
        c.PythonPath = config.PythonPath;
        c.WorkingDirectory = string.IsNullOrEmpty(config.WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : config.WorkingDirectory;
        c.TimeoutSeconds = config.TimeoutSeconds;
        c.RedirectStandardOutput = config.RedirectStandardOutput;
        c.RedirectStandardError = config.RedirectStandardError;
        c.OutputEncoding = config.OutputEncoding;
    });

    return service;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
