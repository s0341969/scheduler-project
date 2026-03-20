using RagApi.Options;
using RagApi.Repositories;
using RagApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<OpenAiEmbeddingClient>();
builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));

builder.Services.AddSingleton<PdfTextExtractor>();
builder.Services.AddSingleton<RagRepository>();
builder.Services.AddSingleton<RagService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<RagRepository>();
    await repository.EnsureSchemaAsync();
}

app.Run();
