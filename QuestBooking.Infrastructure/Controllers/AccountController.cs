using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuestBooking.Infrastructure.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuestBookingIcptContext _context;

        public AccountController(QuestBookingIcptContext context)
        {
            _context = context;
        }

        // === 1. РЕЄСТРАЦІЯ ===
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AppUser model)
        {
            if (ModelState.IsValid)
            {
                // Перевіряємо, чи немає вже такого email
                if (await _context.AppUsers.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Користувач з таким Email вже існує!");
                    return View(model);
                }

                // ВАЖЛИВО ДЛЯ ЛАБИ: Першого користувача можеш потім вручну зробити "Admin" в базі,
                // а всі нові за замовчуванням будуть "Client".
                model.Role = "Client";

                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                _context.AppUsers.Add(model);
                await _context.SaveChangesAsync();

                // Одразу авторизуємо після реєстрації
                await Authenticate(model);
                return RedirectToAction("Index", "Questrooms"); // Перенаправляємо на список квестів
            }
            return View(model);
        }

        // === 2. ВХІД (ЛОГІН) ===
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            // 1. Шукаємо користувача ТІЛЬКИ за email
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

            // 2. Якщо користувач існує, перевіряємо чи збігається введений пароль із хешем у базі
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                await Authenticate(user);
                return RedirectToAction("Index", "Questrooms");
            }

            ModelState.AddModelError("", "Некоректний Email або пароль.");
            return View();
        }

        // === 3. МЕХАНІЗМ СТВОРЕННЯ "ПЕЧИВА" (СЕСІЇ) ===
        private async Task Authenticate(AppUser user)
        {
            // Записуємо дані про користувача у його "паспорт" (сесію)
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role ?? "Client") // Ось тут ми видаємо Роль!
            };

            var id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        // === 4. ВИХІД ===
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // Сюди буде кидати тих, хто спробує зайти на сторінку адміна без прав
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}