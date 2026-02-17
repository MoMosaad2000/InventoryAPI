using InventoryAPI;
using InventoryAPI.Data;
using InventoryAPI.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ================== CORS ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("FrontendProd", policy =>
    {
        policy
            .WithOrigins(
                "https://www.farah.saudisea.com.sa",
                "http://www.farah.saudisea.com.sa",

                // ✅ السماح بالاختبار من اللوكال حتى لو السيرفر Production
                "http://localhost:3000",
                "http://127.0.0.1:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// ================== Auth ==================
builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);

// ================== Controllers / JSON ==================
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "InventoryAPI", Version = "v1" });

    c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

// HttpClient
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("basma");


// ================== DB ==================
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<InventoryDbContext>(o =>
    o.UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 41)))
);

// لو عندك FifoPricingService فعّال سيبه، وإلا احذفه
builder.Services.AddScoped<InventoryAPI.Services.IFifoPricingService, InventoryAPI.Services.FifoPricingService>();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Static files / Uploads
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "Uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

// CORS
if (app.Environment.IsDevelopment())
    app.UseCors("FrontendDev");
else
    app.UseCors("FrontendProd");

// Routing
app.UseRouting();

// Auth
app.UseAuthentication();
app.UseAuthorization();




// Controllers
app.MapControllers();

app.Run();
