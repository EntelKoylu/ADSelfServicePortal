using AdSelfServicePortal.Services;
using Serilog; // Serilog kütüphanesini ekledik

var builder = WebApplication.CreateBuilder(args);

// 1. SERILOG KURULUMU (EN BAŞA)
// Ayarları appsettings.json dosyasından okur
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ---------------------------------------------------------

// 2. SERVİSLERİ EKLEME
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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