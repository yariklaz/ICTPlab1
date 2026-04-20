using System;
using System.Collections.Generic;

namespace QuestBooking.Domain.Model
{

    public partial class Promocode : Entity
    {
        public string Code { get; set; }

        public int DiscountPercent { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public virtual Booking Booking { get; set; }
    }
}