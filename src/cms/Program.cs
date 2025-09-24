using cms.Data;
using cms.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(cs));
// API controllers
builder.Services.AddControllersWithViews();

builder.AddAppHealth();
builder.Services.AddIdentityWithCookies();


var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "public")),
    RequestPath = "/public"
});

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/error/{0}");
// MVC routes (conventional)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Web}/{action=Index}/{id?}");

app.UseHttpsRedirection();

await app.ApplyMigrationsAndSeedAsync();
app.Run();
