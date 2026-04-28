using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // <--- ÄÎÄÀÍÎ: Á³áë³îòåêà äëÿ Cookie-àâòîğèçàö³¿
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;
using QuestBooking.Services;

var builder = WebApplication.CreateBuilder(args);

// === 1. ÄÎÄÀÂÀÍÍß ÁÀÇÎÂÈÕ ÑÅĞÂ²Ñ²Â MVC ===
builder.Services.AddControllersWithViews();


// === 1.5 ÄÎÄÀÂÀÍÍß ÀÓÒÅÍÒÈÔ²ÊÀÖ²¯ (Åòàï 1.7) === <--- ÄÎÄÀÍÎ: Íàëàøòóâàííÿ íàøîãî "ïå÷èâà"
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/Account/Login");
        options.AccessDeniedPath = new Microsoft.AspNetCore.Http.PathString("/Account/AccessDenied");
    });


// === 2. Ï²ÄÊËŞ×ÅÍÍß ÁÀÇÈ ÄÀÍÈÕ (PostgreSQL) ===
builder.Services.AddDbContext<QuestBookingIcptContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// === 3. ĞÅªÑÒĞÀÖ²ß ÔÀÁĞÈÊÈ (Åòàï 6: ²ìïîğò/Åêñïîğò Excel) ===
builder.Services.AddScoped<IDataPortServiceFactory<Questroom>, QuestroomDataPortFactory>();


// === 4. ÍÀËÀØÒÓÂÀÍÍß ÓÊĞÀ¯ÍÑÜÊÎ¯ ËÎÊÀË²ÇÀÖ²¯ ===
var defaultCulture = new CultureInfo("uk-UA");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = new List<CultureInfo> { defaultCulture };
    options.SupportedUICultures = new List<CultureInfo> { defaultCulture };
});


var app = builder.Build();

// === ÍÀËÀØÒÓÂÀÍÍß PIPELINE (ÊÎÍÂÅªĞÀ ÇÀÏÈÒ²Â) ===
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// === 5. ÇÀÑÒÎÑÓÂÀÍÍß ËÎÊÀË²ÇÀÖ²¯ ===
app.UseRequestLocalization(localizationOptions);


// === 6. ÀÓÒÅÍÒÈÔ²ÊÀÖ²ß ÒÀ ÀÂÒÎĞÈÇÀÖ²ß === <--- ÄÎÄÀÍÎ: UseAuthentication
app.UseAuthentication(); // Ñïî÷àòêó ïåğåâ³ğÿºìî: Õòî òè òàêèé? (Ëîã³í/Ïàğîëü)
app.UseAuthorization();  // Ïîò³ì ïåğåâ³ğÿºìî: Ùî òîá³ ìîæíà ğîáèòè? (Ğîë³: Admin/Client)


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Questrooms}/{action=Index}/{id?}");

app.Run();