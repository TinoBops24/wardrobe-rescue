using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using INF4027W_BPTTIN002_MiniPrj_2026.Filters;
using INF4027W_BPTTIN002_MiniPrj_2026.Middleware;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;

var builder = WebApplication.CreateBuilder(args);

// Firebase Configuration
var projectId = builder.Configuration["Firebase:ProjectId"]!;
var credentialsJson = builder.Configuration["Firebase:CredentialsJson"];
var credentialsPath = builder.Configuration["Firebase:CredentialsPath"];

GoogleCredential firebaseCredential;

if (!string.IsNullOrEmpty(credentialsJson))
{
    // Azure: credentials loaded from environment variable
    firebaseCredential = GoogleCredential.FromJson(credentialsJson);
}
else
{
    // Local: credentials loaded from file path
    var fullPath = Path.Combine(builder.Environment.ContentRootPath, credentialsPath!);
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fullPath);
    firebaseCredential = GoogleCredential.GetApplicationDefault();
}

if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = firebaseCredential,
        ProjectId = projectId
    });
}

// Service Registration
builder.Services.AddSingleton(_ => new FirestoreDbBuilder
{
    ProjectId = projectId,
    Credential = firebaseCredential
}.Build());

builder.Services.AddHttpClient<AiService>();
builder.Services.AddHttpClient<FirebaseAuthService>();
builder.Services.AddScoped<FirestoreService>();
builder.Services.AddScoped<BundleService>();

// Caching
builder.Services.AddMemoryCache();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Response Compression (production only)
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });
}

// Razor Pages with automatic admin protection.
// Every page under /Pages/Admin/ is protected via AdminAuthorization filter.
// No attributes needed on individual page models.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddFolderApplicationModelConvention(
        "/Admin",
        model => model.Filters.Add(new AdminAuthorization()));
});

var app = builder.Build();

// Cache Warming
using (var scope = app.Services.CreateScope())
{
    var firestoreService = scope.ServiceProvider.GetRequiredService<FirestoreService>();
    await firestoreService.WarmCacheAsync();
}

// HTTP Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseResponseCompression();
}

// Clean error pages for 404, 403, 500 in all environments
app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Remember Me middleware runs after session and before authorization
app.UseMiddleware<RememberMe>();
app.UseAuthorization();
app.MapRazorPages();
app.Run();