using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;
using QuestBooking.Services;

var builder = WebApplication.CreateBuilder(args);

// === 1. ÄÎÄÀÂÀÍÍß ÁÀÇÎÂÈÕ ÑÅĞÂ²Ñ²Â MVC ===
builder.Services.AddControllersWithViews();


// === 2. Ï²ÄÊËŞ×ÅÍÍß ÁÀÇÈ ÄÀÍÈÕ (PostgreSQL) ===
// ÓÂÀÃÀ: Ïåğåâ³ğ, ÷è òâîÿ ñòğ³÷êà ï³äêëş÷åííÿ â appsettings.json íàçèâàºòüñÿ "DefaultConnection". 
// ßêùî ³íàêøå (íàïğèêëàä, "QuestBookingDb"), ïğîñòî çì³íè íàçâó òóò.
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

// === 5. ÇÀÑÒÎÑÓÂÀÍÍß ËÎÊÀË²ÇÀÖ²¯ (Îáîâ'ÿçêîâî ïåğåä Authorization) ===
app.UseRequestLocalization(localizationOptions);

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Questrooms}/{action=Index}/{id?}");

app.Run();