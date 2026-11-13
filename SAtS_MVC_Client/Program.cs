using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SAtS_MVC_Client.Data;
using SAtS_MVC_Client.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Caleb McIntyre - 12/11/2026: uses ApplicationUser (has the custom FullName field)
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

//------------------------------------------------------------
// Caleb McIntyre - 06/11/2026
//Register the Google sign-in service
builder.Services.AddAuthentication().AddGoogle(options =>
{
    //Read the Google Client ID
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;

    //Read the Google Client Secret
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
});
//------------------------------------------------------------

//------------------------------------------------------------
// Fix: the API's address comes from configuration ("ApiBaseUrl"),
// so the site works on Azure as well as locally. Locally it's
// https://localhost:7147/ (appsettings.json); on Azure it's set as
// an App Service setting pointing at the deployed API.
//------------------------------------------------------------
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7147/";
if (!apiBaseUrl.EndsWith("/"))
{
    apiBaseUrl += "/";
}
builder.Services.AddHttpClient("SAtSApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
