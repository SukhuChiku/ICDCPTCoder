using Backend.Contracts;
using Backend.Service;
using Backend.Endpoints;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Contracts.Interfaces;

var builder = WebApplication.CreateBuilder(args);

//  CHANGE 1: Get connection string from environment variable (for Docker) or appsettings.json (for local)
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("Default");

//  CHANGE 2: Add DbContext with environment-aware connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

//  CHANGE 3: Get Presidio URLs from environment variables (for Docker) or use localhost (for local)
var presidioAnalyzerUrl = Environment.GetEnvironmentVariable("PRESIDIO_ANALYZER_URL") 
    ?? "http://localhost:5001";  // Default for local development

var presidioAnonymizerUrl = Environment.GetEnvironmentVariable("PRESIDIO_ANONYMIZER_URL") 
    ?? "http://localhost:5002";  // Default for local development

// CHANGE 4: Configure HttpClient with dynamic URLs
builder.Services.AddHttpClient<IPresidioService, PresidioService>(client =>
{
    client.BaseAddress = new Uri(presidioAnalyzerUrl);
});

//  CHANGE 5: Add a named HttpClient for Anonymizer
builder.Services.AddHttpClient("PresidioAnonymizer", client =>
{
    client.BaseAddress = new Uri(presidioAnonymizerUrl);
});

// Only register application services if not running migrations
if (!Environment.GetCommandLineArgs().Contains("migrations"))
{
    builder.Services.AddScoped<IPhiRedactionService, PhiRedactionService>();
    builder.Services.AddScoped<IPatientService, PatientService>();
    builder.Services.AddScoped<IAddDoctorsNoteService, AddDoctorsNoteService>();
    builder.Services.AddScoped<IAddDoctorsNoteRespository, AddDoctorsNoteRepository>();
    builder.Services.AddScoped<IOpenAIService, OpenAIService>();
}

// CHANGE 6: Configure Kestrel to listen on the correct port for Docker
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Contains("8080") == true 
        ? 8080 
        : 5116; // Use 8080 in Docker, 5116 locally
    options.ListenAnyIP(port);
});

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapCptIcdEndpoint();

app.UseStaticFiles();
app.MapRazorPages();

// Register endpoints
app.MapPatientVisitsEndpoint();
app.MapAddDoctorsNoteEndpoint();

app.Run();