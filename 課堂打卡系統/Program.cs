using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using 課堂打卡系統.Options;
using 課堂打卡系統.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "ClassAttendance.Admin";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.Configure<AdminAuthOptions>(builder.Configuration.GetSection(AdminAuthOptions.SectionName));
builder.Services.Configure<StudentAuthOptions>(builder.Configuration.GetSection(StudentAuthOptions.SectionName));
builder.Services.Configure<AttendanceSecurityOptions>(builder.Configuration.GetSection(AttendanceSecurityOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAdminAuthService, AdminAuthService>();
builder.Services.AddSingleton<IStudentAuthService, StudentAuthService>();
builder.Services.AddSingleton<IQrTokenService, QrTokenService>();
builder.Services.AddSingleton<IAttendanceQueryService, AttendanceQueryService>();
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();
var supportedCultures = new[] { new CultureInfo("zh-TW") };

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("zh-TW"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Attendance}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
