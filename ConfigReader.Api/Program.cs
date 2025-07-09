using ConfigReader.Api.Extensions;
using ConfigReader.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Services configuration
builder.Services.ConfigureServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Pipeline configuration
app.ConfigurePipeline();

app.Run();

// Test projeleri için gerekli
public partial class Program;
