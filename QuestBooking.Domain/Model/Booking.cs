using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestBooking.Domain.Model
{
    public partial class Booking : Entity
    {

        public int ClientId { get; set; }

        public int SlotId { get; set; }

        public int? PromocodeId { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public virtual Client Client { get; set; }

        public virtual Promocode Promocode { get; set; }

        public virtual Timeslot Slot { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Оберіть дату бронювання")]
        [Display(Name = "Дата візиту")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        public virtual ICollection<Extraservice> Services { get; set; } = new List<Extraservice>();
    }
}
