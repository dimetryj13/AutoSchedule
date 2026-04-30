using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class Schedule
    {
        public int ScheduleID { get; set; }
        public int DayOfWeek { get; set; }
        public int PairNumber { get; set; }
        public int GroupID { get; set; } // В БД указан числовой тип
        public int TeacherID { get; set; }
        public int SubjectID { get; set; }
        public int RoomID { get; set; }
    }
}
