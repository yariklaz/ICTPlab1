using System;
using System.Collections.Generic;

namespace QuestBooking.Domain.Model
{
    public partial class Timeslot : Entity
    {
        public int RoomId { get; set; }

        public DateTime StartTime { get; set; }

        public bool? IsAvailable { get; set; }

        public virtual Booking Booking { get; set; }

        public virtual Questroom Room { get; set; }
    }
}