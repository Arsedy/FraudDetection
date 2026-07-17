using FraudDetection.Dashboard.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Components with Interactive Server rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register HttpClient pointing to the WebAPI
string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient("FraudAPI", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
