using Microsoft.AspNetCore.Identity;

namespace QuestBooking.Domain.Model
{
    // IdentityUser вже містить поля: Email, Password, Phone тощо.
    // Ми просто додаємо те, що просить методичка.
    public class User : IdentityUser
    {
        public int Year { get; set; }
        public string FullName { get; set; } // Нове поле
    }
}