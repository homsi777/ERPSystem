using ERPSystem.Api.Services;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddSingleton<ICurrentUserService, ApiCurrentUserService>();
builder.Services.AddSingleton<ICurrentBranchService, ApiCurrentBranchService>();
builder.Services.AddScoped<IPermissionService, ApiPermissionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => "OK");

app.Run();
