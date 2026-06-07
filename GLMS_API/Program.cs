// =============================================================
//  GLMS Web API – Program.cs
//  Registers: EF Core, Swagger/OpenAPI, Firebase JWT Auth,
//             HttpClient (for currency), CORS, and services.
// =============================================================

using FirebaseAdmin;
using GLMS_API.Models;
using GLMS_API.Services;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ───────────────────────────────────────────────
builder.Services.AddDbContext<GlmsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. Application Services ───────────────────────────────────
builder.Services.AddScoped<ILogisticsService, LogisticsService>();
builder.Services.AddHttpClient();

// ── 3. Firebase Admin SDK (server-side token verification) ────
//  Place your Firebase service-account JSON at the path below,
//  OR set env var GOOGLE_APPLICATION_CREDENTIALS to that path.
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(
        builder.Configuration["Firebase:ServiceAccountPath"]
            ?? "firebase-service-account.json")
    });
}
// ── 4. Authentication – Firebase JWT Bearer ───────────────────
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// ── 5. Controllers + JSON ─────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// ── 6. Swagger / OpenAPI ──────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GLMS API",
        Version = "v1",
        Description = "Global Logistics Management System – REST API"
    });

    // Let Swagger pass Firebase Bearer tokens for testing
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your Firebase ID token: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── 7. CORS – allow the MVC front-end ─────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("MvcClient", policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins:MvcApp"] ?? "https://localhost:7160",
                "http://localhost:5137",   
                "https://localhost:7160" )
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ─────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GLMS API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseCors("MvcClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();