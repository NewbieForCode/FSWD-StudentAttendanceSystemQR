using Microsoft.EntityFrameworkCore;
using GRP5_StudentAttendanceSystem.Models;
using GRP5_StudentAttendanceSystem.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddDbContext<SAtS_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
));

builder.Services.AddHttpClient();

// Caleb McIntyre - 13/11/2026: NZ public holidays from the Nager.Date API (cached)
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<PublicHolidayService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
