using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class GroupList
    {
        public int Id { get; set; }               // Счетчик (В БД поле называется «Код»)
        public string GroupName { get; set; }     // Короткий текст (Название группы)
        public int StudentCount { get; set; }     // Числовой (Количество студентов)
        public bool IsFullTime { get; set; }      // Логический (Очная или заочная)
        public bool Actually { get; set; }        // Логический (Актуальна ли группа)
    }
}
