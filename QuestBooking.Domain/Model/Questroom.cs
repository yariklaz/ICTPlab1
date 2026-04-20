using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuestBooking.Domain.Model
{
    public partial class Questroom : Entity
    {
        [Required(ErrorMessage = "Назва квесту обов'язкова")]
        [Display(Name = "Назва квест-кімнати")]
        public string Title { get; set; } // Видалили "= null!"

        [Display(Name = "Опис квесту")]
        public string Description { get; set; } // Видалили "?"

        [Required(ErrorMessage = "Вкажіть кількість гравців")]
        [Display(Name = "Макс. гравців")]
        public int MaxPlayers { get; set; }

        [Required(ErrorMessage = "Ціна має бути вказана")]
        [Display(Name = "Базова ціна (грн)")]
        public decimal BasePrice { get; set; }

        [Required(ErrorMessage = "Вкажіть тривалість квесту")]
        [Display(Name = "Тривалість (хв)")]
        public int DurationMinutes { get; set; }

        public virtual ICollection<Timeslot> Timeslots { get; set; }
    }
}