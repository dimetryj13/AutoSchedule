using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class TeacherRoomPref
    {
        public int Id { get; set; }           // Поле "Код" в БД
        public int TeacherID { get; set; }
        public int RoomID { get; set; }       // В БД поле называется "RoomNumber", но хранит числовой ID
        public int Priority { get; set; }
    }
}
