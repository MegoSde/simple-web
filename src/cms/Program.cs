using cms.Data;
using cms.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(cs));
// API controllers
builder.Services.AddControllers();

builder.AddAppHealth();
builder.Services.AddIdentityWithCookies();


var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseHttpsRedirection();

await app.ApplyMigrationsAndSeedAsync();
app.Run();
