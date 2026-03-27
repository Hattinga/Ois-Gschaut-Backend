using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=oisgschaut.db"));

// Named HttpClients for external APIs
builder.Services.AddHttpClient<TmdbService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Tmdb:BaseUrl"] ?? "https://api.themoviedb.org");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<TvMazeService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["TvMaze:BaseUrl"] ?? "https://api.tvmaze.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<MediaSyncService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  var uri = new Uri(origin);
                  return uri.Host == "localhost" || uri.Host == "127.0.0.1";
              })
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", (HttpContext context) =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();
