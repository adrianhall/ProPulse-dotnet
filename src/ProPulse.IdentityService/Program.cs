using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using JavaScriptEngineSwitcher.V8;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProPulse.IdentityService.Data;
using ProPulse.IdentityService.Extensions;
using ProPulse.IdentityService.Models;
using ProPulse.IdentityService.Services.Implementations;


// =======================================================================================
// Configuration
// =======================================================================================

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(); // Aspire Service Defaults

// =======================================================================================
// Services
// =======================================================================================

// SQL Server setup based on Aspire.
builder.AddSqlServerDbContext<ApplicationDbContext>("IdentityConnection", 
    configureDbContextOptions: options => options.UseOpenIddict());

// Add DatabaseInitializationService as a hosted service
builder.Services.AddHostedService<DatabaseInitializationService>();

// Configure Identity options from configuration
builder.Services.Configure<IdentityOptions>(options => 
{
    // Bind the options from configuration
    builder.Configuration.GetSection("Identity:Options").Bind(options);
    
    // Always ensure User.RequireUniqueEmail is set to true
    options.User.RequireUniqueEmail = true;
});

// Add ASP.NET Identity services
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
    .AddServer(options =>
    {
        // Enable the authorization, token, userinfo, and introspection endpoints
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserinfoEndpointUris("/connect/userinfo")
               .SetIntrospectionEndpointUris("/connect/introspect")
               .SetLogoutEndpointUris("/connect/logout");

        // Enable the client credentials, authorization code, and refresh token flows
        options.AllowClientCredentialsFlow()
               .AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        // Register scopes
        options.RegisterScopes("api", "profile", "email", "roles");

        // Register signing and encryption credentials
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough()
               .EnableLogoutEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// External login providers
builder.Services.AddAuthentication()
    .AddFacebook(builder.Configuration.GetSection("Identity:Providers:Facebook"))
    .AddGoogle(builder.Configuration.GetSection("Identity:Providers:Google"))
    .AddMicrosoftAccount(builder.Configuration.GetSection("Identity:Providers:MicrosoftAccount"));

// Email services required for sending transactional emails to support identity.
builder.Services.AddScoped<IEmailSender<ApplicationUser>, LoggingEmailSender>();

// LigerShark WebOptimizer
builder.Services.AddJsEngineSwitcher(options =>
{
    options.AllowCurrentProperty = false;
    options.DefaultEngineName = V8JsEngine.EngineName;
}).AddV8();

builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddScssBundle("/css/site.css", "/css/site.scss");

    if (!builder.Environment.IsDevelopment())
    {
        pipeline.MinifyCssFiles();
        pipeline.AddFiles("text/css", "/css/*");

        pipeline.MinifyJsFiles();
        pipeline.AddFiles("text/javascript", "/js/*");
    }
});

// Add MVC view architecure.
builder.Services.AddControllersWithViews();

// =======================================================================================
// Initialization
// =======================================================================================

var app = builder.Build();

// =======================================================================================
// HTTP Pipeline
// =======================================================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseWebOptimizer();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// =======================================================================================
// Endpoints
// =======================================================================================

app.MapDefaultControllerRoute();
app.MapControllers();

// Add service defaults endpoints
app.MapDefaultEndpoints();

// =======================================================================================
// Application Start
// =======================================================================================

app.Run();

// Make Program class accessible to tests
public partial class Program { }
