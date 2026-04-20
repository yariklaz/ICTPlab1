using System;
using System.Collections.Generic;

namespace QuestBooking.Domain.Model
{
    public partial class Client : Entity
    {

        public string FullName { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}