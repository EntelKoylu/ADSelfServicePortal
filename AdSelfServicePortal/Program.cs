using AdSelfServicePortal.Services;
using Serilog; // Serilog kütüphanesini ekledik

var builder = WebApplication.CreateBuilder(args);

// 1. SERILOG KURULUMU (EN BAŞA)
// Ayarları appsettings.json dosyasından okur
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ---------------------------------------------------------

// 2. SERVİSLERİ EKLEME
builder.Services.AddControllersWithViews(options =>
{
    // Tüm POST isteklerinde otomatik Anti-Forgery Token doğrulaması
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Anti-Forgery ayarları
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Kendi Servislerimiz
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdService>();

var app = builder.Build();

// ---------------------------------------------------------

// 3. MIDDLEWARE VE AYARLAR

// Hata olduğunda loglayalım ve hata sayfasına atalım
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Güvenlik Header'ları
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "img-src 'self' https://sanalpospro.com data:; " +
        "connect-src 'self';";
    await next();
});

// Serilog'un HTTP isteklerini (Giren çıkan trafiği) loglaması için
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    // Uygulama Başlıyor Logu
    Log.Information("Uygulama başlatılıyor...");
    app.Run();
}
catch (Exception ex)
{
    // Kritik bir hata olursa (Örn: DB bağlanamazsa) buraya düşer
    Log.Fatal(ex, "Uygulama beklenmedik bir şekilde durdu!");
}
finally
{
    // Kapanırken log dosyasını temizle ve kapat
    Log.CloseAndFlush();
}