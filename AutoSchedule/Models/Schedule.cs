using System;

namespace AutoSchedule.Models
{
    public class Schedule
    {
        public int ScheduleID { get; set; }
        public string GroupName { get; set; } // Теперь текст
        public int DayOfWeek { get; set; }
        public int PairNumber { get; set; }
        public int WeekType { get; set; }     // Тип недели
        public string SubjectName { get; set; } // Теперь текст
        public string TeacherName { get; set; } // Теперь текст
        public string RoomNumber { get; set; }  // Теперь текст
        public int LessonType { get; set; }     // Лекция/Практика/Лаба
    }
}