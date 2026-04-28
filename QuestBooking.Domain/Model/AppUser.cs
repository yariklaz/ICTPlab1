using System.ComponentModel.DataAnnotations;

namespace QuestBooking.Domain.Model
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Поле 'Повне ім'я' є обов'язковим.")]
        [MinLength(2, ErrorMessage = "Ім'я має містити щонайменше 2 символи.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Поле 'Телефон' є обов'язковим.")]
        [RegularExpression(@"^(?:\+380|380|0)\d{9}$", ErrorMessage = "Введіть коректний український номер (наприклад: 0501234567 або +380501234567).")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Поле 'Email' є обов'язковим.")]
        [EmailAddress(ErrorMessage = "Введіть коректну адресу електронної пошти.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Поле 'Пароль' є обов'язковим.")]
        [MinLength(6, ErrorMessage = "Пароль має містити щонайменше 6 символів.")]
        // За бажанням, можеш розкоментувати рядок нижче, щоб вимагати ще й літери та цифри:
        // [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{6,}$", ErrorMessage = "Пароль має містити мінімум 6 символів, літери та цифри.")]
        public string Password { get; set; }

        public string Role { get; set; } = "Client";
    }
}