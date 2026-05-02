using AutoSchedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Services
{
    public class DataMappingService
    {
        // Словари для мгновенного поиска объектов по их ID (работают быстрее, чем списки)
        private Dictionary<string, GroupList> _groupsDict;
        private Dictionary<int, Subject> _subjectsDict;
        private Dictionary<int, Teacher> _teachersDict;

        public DataMappingService(List<GroupList> groups, List<Subject> subjects, List<Teacher> teachers)
        {
            // Превращаем списки в словари. Это ускорит поиск при связывании сотен строк плана
            // Обрати внимание: у группы ключ - это Id.ToString(), так как в твоей БД GroupID в плане был текстом
            _groupsDict = groups.ToDictionary(g => g.GroupId.ToString(), g => g);
            _subjectsDict = subjects.ToDictionary(s => s.SubjectID, s => s);
            _teachersDict = teachers.ToDictionary(t => t.TeacherID, t => t);
        }

        // Метод, который берет "сырые" планы и возвращает "обогащенные"
        public List<EnrichedAcademicPlan> MapAcademicPlans(List<AcademicPlan> rawPlans)
        {
            List<EnrichedAcademicPlan> enrichedPlans = new List<EnrichedAcademicPlan>();

            foreach (var raw in rawPlans)
            {
                var enriched = new EnrichedAcademicPlan
                {
                    PlanID = raw.PlanID,
                    Hours = raw.Hours,
                    LectureInWeek = raw.LectureInWeek,
                    LabsInWeek = raw.LabsInWeek,
                    PracticeInWeek = raw.PracticeInWeek,
                    Semester = raw.Semester
                };

                // Безопасно пытаемся найти и привязать группу
                if (!string.IsNullOrEmpty(raw.GroupID) && _groupsDict.ContainsKey(raw.GroupID))
                    enriched.Group = _groupsDict[raw.GroupID];

                // Безопасно привязываем предмет
                if (_subjectsDict.ContainsKey(raw.SubjectID))
                    enriched.Subject = _subjectsDict[raw.SubjectID];

                // Безопасно привязываем преподавателей (0 или пустота означает отсутствие)
                if (_teachersDict.ContainsKey(raw.LectureTeacher))
                    enriched.LectureTeacher = _teachersDict[raw.LectureTeacher];

                if (_teachersDict.ContainsKey(raw.PracticeTeacher))
                    enriched.PracticeTeacher = _teachersDict[raw.PracticeTeacher];

                enrichedPlans.Add(enriched);
            }

            return enrichedPlans;
        }
    }
}
