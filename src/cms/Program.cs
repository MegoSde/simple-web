using cms.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppHealth();

// API controllers
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.UseHttpsRedirection();

app.Run();
