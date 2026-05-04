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
        // Словарь для связи ID аудитории с её визуальным индикатором
        private Dictionary<int, Controls.RoomIndicatorControl> _roomIndicators = new Dictionary<int, Controls.RoomIndicatorControl>();

        // Сервисы
        AssignmentPool globalPool = new AssignmentPool();
        ValidationService globalValidator;

        public Form1()
        {
            InitializeComponent();
            SetDoubleBuffered(flpAssignments);
            SetDoubleBuffered(flpRoomIndicators);
            SetDoubleBuffered(pnlScheduleContainer);
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

                // Генерируем сетку
                GenerateScheduleGrid();

                // --- ВИЗУАЛИЗАЦИЯ: Выводим индикаторы аудиторий вниз ---
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

            // Включаем визуальный фидбек
            flpAssignments.Visible = false; // Прячем панель на время добавления (это КРИТИЧЕСКИ ускоряет WinForms)
            flpAssignments.SuspendLayout();
            flpAssignments.Controls.Clear();

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

                // Подгружаем приоритеты
                var priorityRoomIds = teacherRoomPrefs
                    .Where(p => p.TeacherID == item.AssignedTeacher?.TeacherID)
                    .Select(p => p.RoomID).ToList();

                card.LoadRooms(classrooms, priorityRoomIds);

                // События
                card.MouseEnter += (s, e) => { ShowRoomsForCard(card); };
                card.MouseLeave += (s, e) => { ClearRoomIndicators(); };
                card.RoomSelectionChanged += Card_RoomSelectionChanged;

                flpAssignments.Controls.Add(card);
            
            // При прокрутке колесика — бегаем синей рамкой по списку внизу
            card.RoomSelectionChanged += (s, selectedRoom) =>
                {
                    foreach (var indicator in _roomIndicators.Values) indicator.Highlight(false);
                    if (selectedRoom != null && _roomIndicators.ContainsKey(selectedRoom.RoomID))
                    {
                        var activeIndicator = _roomIndicators[selectedRoom.RoomID];
                        activeIndicator.Highlight(true);
                        flpRoomIndicators.ScrollControlIntoView(activeIndicator); // Авто-скролл карусели
                    }
                };
                // Двигаем прогресс-бар
                pb.Value++;

                // Каждые 15 карточек даем интерфейсу "подышать" и перерисоваться
                if (pb.Value % 15 == 0) Application.DoEvents();
            }

            // 4. ВКЛЮЧАЕМ панель обратно (карточки появляются мгновенно все вместе)
            flpAssignments.ResumeLayout();
            flpAssignments.Visible = true;

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
        // Показывает индикаторы только для наведенной карточки
        // Показывает индикаторы только для наведенной карточки
        private void ShowRoomsForCard(Controls.ScheduleCardControl card)
        {
            flpRoomIndicators.SuspendLayout();
            flpRoomIndicators.Controls.Clear();
            _roomIndicators.Clear();

            flpRoomIndicators.WrapContents = false;

            // --- УМНАЯ ВЫСОТА КНОПОК ---
            // Узнаем стандартную высоту горизонтального ползунка в Windows (обычно около 17px)
            int scrollBarHeight = SystemInformation.HorizontalScrollBarHeight;

            // Вычисляем идеальную высоту: Высота панели минус скроллбар минус отступы сверху/снизу (около 15px)
            int targetHeight = flpRoomIndicators.Height - scrollBarHeight - 15;

            // Защита: если панель слишком сильно сплющили, задаем минимальную высоту, чтобы текст не исчез
            if (targetHeight < 25) targetHeight = 25;

            foreach (var room in card.AvailableRooms)
            {
                bool isPriority = card.PriorityRoomIds.Contains(room.RoomID);
                var indicator = new Controls.RoomIndicatorControl(room, isPriority);

                // Применяем вычисленную высоту к кнопке!
                indicator.Height = targetHeight;

                _roomIndicators.Add(room.RoomID, indicator);
                flpRoomIndicators.Controls.Add(indicator);
            }

            var selected = card.SelectedRoom;
            if (selected != null && _roomIndicators.ContainsKey(selected.RoomID))
            {
                var activeIndicator = _roomIndicators[selected.RoomID];
                activeIndicator.Highlight(true);
                flpRoomIndicators.ScrollControlIntoView(activeIndicator);
            }

            flpRoomIndicators.ResumeLayout();
        }
        // Очищает панель
        private void ClearRoomIndicators()
        {
            flpRoomIndicators.Controls.Clear();
            _roomIndicators.Clear();
        }

        // --- НОВЫЙ МЕТОД: Генерация главной сетки (Шахматки) ---
        private void GenerateScheduleGrid()
        {
            if (groups == null || groups.Count == 0) return;

            pnlScheduleContainer.SuspendLayout();
            pnlScheduleContainer.Controls.Clear();

            // 1. Контейнер со скроллом
            Panel scrollWrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            SetDoubleBuffered(scrollWrapper);

            // 2. Сетка
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top, // Оставляем Top, но будем управлять шириной
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.White
            };
            SetDoubleBuffered(grid);
            grid.SuspendLayout();

            int colCount = 2 + groups.Count;
            grid.ColumnCount = colCount;

            // Настройка столбцов
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40)); // Дни
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30)); // Номер пары

            // Если групп мало — растягиваем их на весь экран, если много — фиксируем ширину
            float groupWidth = groups.Count < 5 ? (100f / groups.Count) : 250f;
            SizeType sType = groups.Count < 5 ? SizeType.Percent : SizeType.Absolute;

            for (int i = 0; i < groups.Count; i++)
                grid.ColumnStyles.Add(new ColumnStyle(sType, groupWidth));

            // 3. Шапка
            grid.RowCount = 1;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.Controls.Add(CreateHeaderLabel("Дни"), 0, 0);
            grid.Controls.Add(CreateHeaderLabel("#"), 1, 0);

            for (int i = 0; i < groups.Count; i++)
                grid.Controls.Add(CreateHeaderLabel(groups[i].GroupName), i + 2, 0);

            // 4. Строки (Дни * Пары)
            string[] days = { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" };
            int pairsPerDay = 6;

            for (int d = 0; d < days.Length; d++)
            {
                for (int p = 0; p < pairsPerDay; p++)
                {
                    int rowIndex = grid.RowCount++;
                    grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));

                    if (p == 0)
                    {
                        var lblDay = CreateHeaderLabel(days[d]);
                        grid.Controls.Add(lblDay, 0, rowIndex);
                        grid.SetRowSpan(lblDay, pairsPerDay);
                    }

                    grid.Controls.Add(CreateHeaderLabel((p + 1).ToString()), 1, rowIndex);

                    // Ячейки-мишени для Drag-and-Drop
                    for (int g = 0; g < groups.Count; g++)
                    {
                        Panel cell = new Panel
                        {
                            Dock = DockStyle.Fill,
                            Margin = new Padding(1),
                            BackColor = Color.FromArgb(250, 250, 250),
                            AllowDrop = true,
                            Tag = new Tuple<int, int, int>(d + 1, p + 1, groups[g].GroupId)
                        };
                        grid.Controls.Add(cell, g + 2, rowIndex);
                    }
                }
            }

            grid.ResumeLayout();
            scrollWrapper.Controls.Add(grid);
            pnlScheduleContainer.Controls.Add(scrollWrapper);
            pnlScheduleContainer.ResumeLayout();
        }

        // Вспомогательный метод для красивых заголовков
        private Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 240, 240)
            };
        }
        public static void SetDoubleBuffered(Control control)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }

    }
}