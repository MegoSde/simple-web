using Amazon.S3;
using cms.Data;
using cms.Extensions;
using cms.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(cs));
builder.Services.AddSingleton<IAmazonS3>(_ =>
    S3ClientFactory.Create(builder.Configuration));
builder.Services.AddScoped<MediaService>();

// API controllers
builder.Services.AddControllersWithViews();

builder.AddAppHealth();
builder.Services.AddIdentityWithCookies();


var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/error/{0}");
// MVC routes (conventional)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Web}/{action=Index}/{id?}");
//app.MapCmsImageProxyEndpoints();

app.UseHttpsRedirection();

//await app.ApplyMigrationsAndSeedAsync();
app.Run();
