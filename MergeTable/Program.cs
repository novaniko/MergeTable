using MergeTable.Services;
using MergeTable.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TableService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapTableEndpoints();

app.Run();
