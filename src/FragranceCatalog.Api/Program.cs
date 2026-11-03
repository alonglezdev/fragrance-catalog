using FragranceCatalog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

app.Run();
