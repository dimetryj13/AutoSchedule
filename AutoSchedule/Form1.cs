using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using AutoSchedule.Models;
using AutoSchedule.Services;

namespace AutoSchedule
{
    public partial class Form1 : Form
    {
        // --- ПОЛЯ ДАННЫХ ---
        string connectionString = "";
        string dbFolderPath = Path.Combine(Application.StartupPath, "Databases");

        // Хранилища данных
        List<Classroom> classrooms = new List<Classroom>();
        List<GroupList> groups = new List<GroupList>();
        List<Subject> subjects = new List<Subject>();
        List<Teacher> teachers = new List<Teacher>();
        List<AcademicPlan> academicPlans = new List<AcademicPlan>();
        List<TeacherDayOff> teacherDaysOff = new List<TeacherDayOff>();
        List<Schedule> schedules = new List<Schedule>();
        List<TeacherRoomPref> teacherRoomPrefs = new List<TeacherRoomPref>();
        List<EnrichedAcademicPlan> enrichedPlans = new List<EnrichedAcademicPlan>();

        // Сервисы
        AssignmentPool globalPool = new AssignmentPool();
        ValidationService globalValidator;

        public Form1()
        {
            InitializeComponent();
            // Включаем двойную буферизацию для плавной отрисовки
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Создаем папку БД, если её нет
            if (!Directory.Exists(dbFolderPath)) Directory.CreateDirectory(dbFolderPath);

            // Пока мы не сделали новые кнопки в Toolbar, можно временно вызвать загрузку 
            // какой-то конкретной базы для теста, если она есть в папке:
            // string testDb = Path.Combine(dbFolderPath, "University.accdb");
            // if (File.Exists(testDb)) LoadDatabaseData(testDb);
        }

        public void LoadDatabaseData(string dbPath)
        {
            try
            {
                DatabaseManager dbManager = new DatabaseManager(dbPath);

                // 1. Загрузка из БД
                classrooms = dbManager.GetClassrooms();
                groups = dbManager.GetGroups();
                subjects = dbManager.GetSubjects();
                teachers = dbManager.GetTeachers();
                academicPlans = dbManager.GetAcademicPlans();
                teacherDaysOff = dbManager.GetTeacherDaysOff();
                schedules = dbManager.GetSchedules();
                teacherRoomPrefs = dbManager.GetTeacherRoomPrefs();

                // 2. Связывание (Mapping)
                DataMappingService mappingService = new DataMappingService(groups, subjects, teachers);
                enrichedPlans = mappingService.MapAcademicPlans(academicPlans);

                // 3. Инициализация пула карточек
                globalPool.Initialize(enrichedPlans);

                // 4. Инициализация валидатора
                globalValidator = new ValidationService(teacherDaysOff, schedules);

                // 5. ВИЗУАЛИЗАЦИЯ: Выводим карточки в левую панель
                PopulateAssignmentCards();

                int totalCards = globalPool.GetAvailableItems().Count;
                MessageBox.Show($"База загружена. Сгенерировано карточек: {totalCards}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке: " + ex.Message);
            }
        }

        // --- НОВЫЙ МЕТОД: Отрисовка карточек в левой панели ---
        // --- НОВЫЙ МЕТОД: Отрисовка карточек с прогресс-баром ---
        private void PopulateAssignmentCards()
        {
            var availableItems = globalPool.GetAvailableItems();
            if (availableItems.Count == 0) return;

            // 1. Создаем прогресс-бар и текст программно (по центру шахматки)
            ProgressBar pb = new ProgressBar();
            pb.Style = ProgressBarStyle.Continuous;
            pb.Maximum = availableItems.Count;
            pb.Value = 0;
            pb.Width = 300;
            pb.Height = 30;
            // Центрируем по экрану
            pb.Left = (pnlScheduleContainer.Width - pb.Width) / 2;
            pb.Top = (pnlScheduleContainer.Height - pb.Height) / 2;

            Label lbl = new Label();
            lbl.Text = "Генерация карточек расписания...";
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lbl.Left = pb.Left;
            lbl.Top = pb.Top - 30;

            // Добавляем на форму
            pnlScheduleContainer.Controls.Add(pb);
            pnlScheduleContainer.Controls.Add(lbl);

            // 2. ОТКЛЮЧАЕМ графическое обновление левой панели для дикой скорости
            flpAssignments.SuspendLayout();
            flpAssignments.Controls.Clear();

            // 3. Генерируем карточки
            foreach (var item in availableItems)
            {
                var card = new Controls.ScheduleCardControl(item);

                var priorityRoomIds = teacherRoomPrefs
                    .Where(p => p.TeacherID == item.AssignedTeacher?.TeacherID)
                    .Select(p => p.RoomID)
                    .ToList();

                card.LoadRooms(classrooms, priorityRoomIds);
                flpAssignments.Controls.Add(card);

                // Двигаем прогресс-бар
                pb.Value++;

                // Каждые 15 карточек даем интерфейсу "подышать" и перерисоваться
                if (pb.Value % 15 == 0) Application.DoEvents();
            }

            // 4. ВКЛЮЧАЕМ панель обратно (карточки появляются мгновенно все вместе)
            flpAssignments.ResumeLayout();

            // 5. Убираем прогресс-бар после завершения
            pnlScheduleContainer.Controls.Remove(pb);
            pnlScheduleContainer.Controls.Remove(lbl);
            pb.Dispose();
            lbl.Dispose();
        }
        // Пустые заглушки для событий, если они остались в дизайнере (чтобы не было ошибок)
        private void tlpMainLayout_Paint(object sender, PaintEventArgs e) { }

        private void btnOpenDb_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Access Database (*.accdb)|*.accdb|Старые базы Access (*.mdb)|*.mdb";
            openFileDialog.Title = "Выберите файл базы данных";

            // Если пользователь выбрал файл и нажал ОК
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = openFileDialog.FileName;

                // Настраиваем глобальную строку подключения
                connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={selectedPath};";

                // Шаг 4: Вызываем наш главный метод загрузки, который всё обработает и нарисует карточки
                LoadDatabaseData(selectedPath);
            }
        }
    }
}