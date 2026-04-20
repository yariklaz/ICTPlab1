using System;
using System.Collections.Generic;

namespace QuestBooking.Domain.Model
{

    public partial class Extraservice : Entity
    {

        public string ServiceName { get; set; }

        public decimal Price { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}