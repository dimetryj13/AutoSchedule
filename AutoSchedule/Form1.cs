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
        // Переменная для самой шахматки
        private DataGridView dgvSchedule;

        // --- ПЕРЕМЕННЫЕ МАСШТАБИРОВАНИЯ ---
        private float _zoomLevel = 1.0f;
        private const float ZOOM_STEP = 0.1f;
        private const float MIN_ZOOM = 0.5f;
        private const float MAX_ZOOM = 2.0f;

        // Базовые размеры (эталонные)
        private const int BASE_ROW_HEIGHT = 66;
        private const int BASE_HEADER_HEIGHT = 40;
        private const float BASE_FONT_SIZE_DAY = 12f;
        private const float BASE_FONT_SIZE_PAIR = 12f;
        private const int BASE_WIDTH_DAY = 40;
        private const int BASE_WIDTH_PAIR = 30;
        private const int BASE_WIDTH_GROUP = 250;

        // Кисти для рисования границ ячеек
        private Pen _gridPen = new Pen(Color.LightGray, 1);
        private Pen _thickPen = new Pen(Color.Black, 2);

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

            // Создаем и показываем прогресс-бар в центре шахматки
            ProgressBar pb = new ProgressBar { Maximum = availableItems.Count, Value = 0, Width = 300, Height = 30 };
            pb.Left = (pnlScheduleContainer.Width - pb.Width) / 2;
            pb.Top = (pnlScheduleContainer.Height - pb.Height) / 2;

            Label lbl = new Label { Text = "Загрузка карточек...", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lbl.Left = pb.Left; lbl.Top = pb.Top - 25;

            pnlScheduleContainer.Controls.Add(pb);
            pnlScheduleContainer.Controls.Add(lbl);
            pb.BringToFront(); lbl.BringToFront();

            flpAssignments.Visible = false;
            flpAssignments.SuspendLayout();
            flpAssignments.Controls.Clear();

            foreach (var item in availableItems)
            {
                var card = new Controls.ScheduleCardControl(item);
                var priorityRoomIds = teacherRoomPrefs
                    .Where(p => p.TeacherID == item.AssignedTeacher?.TeacherID)
                    .Select(p => p.RoomID).ToList();

                card.LoadRooms(classrooms, priorityRoomIds);

                // Подписки на события
                card.MouseEnter += (s, e) => { ShowRoomsForCard(card); };
                card.MouseLeave += (s, e) => { ClearRoomIndicators(); };
                card.RoomSelectionChanged += (s, room) => UpdateRoomHighlight(room);

                flpAssignments.Controls.Add(card);

                pb.Value++;
                if (pb.Value % 20 == 0) Application.DoEvents();
            }

            flpAssignments.ResumeLayout();
            flpAssignments.Visible = true;

            pnlScheduleContainer.Controls.Remove(pb);
            pnlScheduleContainer.Controls.Remove(lbl);
        }
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
            // Фильтруем только актуальные группы
            var activeGroups = groups.Where(g => g.Actually).ToList(); //
            if (activeGroups.Count == 0) return;

            pnlScheduleContainer.SuspendLayout();
            pnlScheduleContainer.Controls.Clear();

            // Внутри GenerateScheduleGrid, после создания dgvSchedule:
            dgvSchedule.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSchedule.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            dgvSchedule = new DataGridView();
            dgvSchedule.Dock = DockStyle.Fill;
            dgvSchedule.AllowUserToAddRows = false;
            dgvSchedule.RowHeadersVisible = false;
            dgvSchedule.BackgroundColor = Color.White;
            dgvSchedule.AllowDrop = true;
            dgvSchedule.ReadOnly = true;

            // Включаем DoubleBuffering (уже есть в твоем коде через рефлексию)
            SetDoubleBuffered(dgvSchedule);

            // Настройка колонок
            dgvSchedule.Columns.Add("colDay", "День");
            dgvSchedule.Columns.Add("colPair", "#");
            dgvSchedule.Columns["colDay"].Frozen = true;
            dgvSchedule.Columns["colPair"].Frozen = true;

            foreach (var group in activeGroups)
            {
                int idx = dgvSchedule.Columns.Add($"g_{group.GroupId}", group.GroupName);
                // ЗАПРЕТ СОРТИРОВКИ (Фото 1 и 2)
                dgvSchedule.Columns[idx].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // Настройка строк (6 дней * 6 пар)
            string[] days = { "ПОНЕДЕЛЬНИК", "ВТОРНИК", "СРЕДА", "ЧЕТВЕРГ", "ПЯТНИЦА", "СУББОТА" };
            for (int d = 0; d < 6; d++)
            {
                for (int p = 1; p <= 6; p++)
                {
                    int rowIndex = dgvSchedule.Rows.Add();
                    dgvSchedule.Rows[rowIndex].Cells["colDay"].Value = days[d];
                    dgvSchedule.Rows[rowIndex].Cells["colPair"].Value = p.ToString();
                }
            }

            // События
            dgvSchedule.CellPainting += DgvSchedule_CellPainting;
            dgvSchedule.Paint += DgvSchedule_Paint;
            dgvSchedule.MouseWheel += DgvSchedule_MouseWheel; // Для Зума

            ApplyZoom(); // Применяем начальный масштаб

            pnlScheduleContainer.Controls.Add(dgvSchedule);
            pnlScheduleContainer.ResumeLayout();
        }

        // --- ЛОГИКА ЗУМА ---
        private void DgvSchedule_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                if (e.Delta > 0) _zoomLevel += ZOOM_STEP;
                else _zoomLevel -= ZOOM_STEP;

                _zoomLevel = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _zoomLevel));
                ApplyZoom();
                ((HandledMouseEventArgs)e).Handled = true;
            }
            // НОВАЯ ЛОГИКА ДЛЯ SHIFT (Горизонтальный скролл)
            else if (Control.ModifierKeys == Keys.Shift)
            {
                if (e.Delta > 0) // Крутим вверх — скролл влево
                {
                    if (dgvSchedule.FirstDisplayedScrollingColumnIndex > 0)
                        dgvSchedule.FirstDisplayedScrollingColumnIndex--;
                }
                else // Крутим вниз — скролл вправо
                {
                    if (dgvSchedule.FirstDisplayedScrollingColumnIndex < dgvSchedule.ColumnCount - 1)
                        dgvSchedule.FirstDisplayedScrollingColumnIndex++;
                }
                ((HandledMouseEventArgs)e).Handled = true;
            }
        }

        private void ApplyZoom()
        {
            if (dgvSchedule == null) return;

            dgvSchedule.SuspendLayout();

            // Масштабируем высоту строк и заголовка
            dgvSchedule.ColumnHeadersHeight = (int)(BASE_HEADER_HEIGHT * _zoomLevel);
            foreach (DataGridViewRow row in dgvSchedule.Rows)
                row.Height = (int)(BASE_ROW_HEIGHT * _zoomLevel);

            // Масштабируем ширину колонок
            dgvSchedule.Columns["colDay"].Width = (int)(BASE_WIDTH_DAY * _zoomLevel);
            dgvSchedule.Columns["colPair"].Width = (int)(BASE_WIDTH_PAIR * _zoomLevel);

            for (int i = 2; i < dgvSchedule.Columns.Count; i++)
                dgvSchedule.Columns[i].Width = (int)(BASE_WIDTH_GROUP * _zoomLevel);

            dgvSchedule.ResumeLayout();
            dgvSchedule.Invalidate();
        }
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

        private void DgvSchedule_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            bool isHeaderCol = (e.ColumnIndex == 0 || e.ColumnIndex == 1);
            e.PaintBackground(e.CellBounds, !isHeaderCol);

            // Рисуем правую границу ячейки
            e.Graphics.DrawLine(_gridPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);

            // Рисуем нижнюю границу ТОЛЬКО если это не столбец дней недели (создаем иллюзию объединения)
            if (!isHeaderCol)
            {
                e.Graphics.DrawLine(_gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            // Блокируем стандартный текст для первых двух столбцов, рисуем сами в Paint
            if (isHeaderCol) e.Handled = true;
            else
            {
                e.PaintContent(e.CellBounds);
                e.Handled = true;
            }
        }

        private void DgvSchedule_Paint(object sender, PaintEventArgs e)
        {
            if (dgvSchedule == null || dgvSchedule.Rows.Count == 0) return;

            int firstRow = dgvSchedule.FirstDisplayedScrollingRowIndex;
            if (firstRow < 0) return;
            int lastRow = firstRow + dgvSchedule.DisplayedRowCount(true);

            // Рисуем дни недели (Вертикально)
            // startDayBlock гарантирует, что мы всегда начинаем рисовать с начала блока текущего дня
            int startDayBlock = (firstRow / 6) * 6;

            Font fontDay = new Font("Segoe UI", BASE_FONT_SIZE_DAY * _zoomLevel, FontStyle.Bold);

            for (int i = startDayBlock; i <= lastRow; i += 6)
            {
                if (i >= dgvSchedule.Rows.Count) break;

                // Получаем один прямоугольник на 6 ячеек
                Rectangle rectDay = GetMergedRectangle(dgvSchedule, 0, i, 6);

                if (rectDay.Height > 10)
                {
                    string dayText = dgvSchedule.Rows[i].Cells["colDay"].Value?.ToString() ?? "";
                    DrawVerticalText(e.Graphics, dgvSchedule, dayText, fontDay, rectDay);
                }

                // Рисуем жирную разделительную линию в конце дня
                if (rectDay.Bottom > 0 && rectDay.Bottom < dgvSchedule.Height)
                    e.Graphics.DrawLine(_thickPen, 0, rectDay.Bottom - 1, dgvSchedule.Width, rectDay.Bottom - 1);
            }
        }
        private Rectangle GetMergedRectangle(DataGridView grid, int colIndex, int startRow, int count)
        {
            Rectangle res = Rectangle.Empty;
            for (int k = 0; k < count; k++)
            {
                int rowIndex = startRow + k;
                if (rowIndex >= grid.Rows.Count) break;
                Rectangle r = grid.GetCellDisplayRectangle(colIndex, rowIndex, false);
                if (r.Width == 0) continue;
                if (res == Rectangle.Empty) res = r;
                else res = Rectangle.Union(res, r);
            }
            return res;
        }

        // Метод обновления подсветки аудиторий в карусели
        private void UpdateRoomHighlight(Models.Classroom selectedRoom)
        {
            // Сначала гасим все рамки
            foreach (var indicator in _roomIndicators.Values) 
                indicator.Highlight(false);

            // Если выбрана конкретная аудитория (не "?" и не "..."), подсвечиваем её
            if (selectedRoom != null && _roomIndicators.ContainsKey(selectedRoom.RoomID))
            {
                var activeIndicator = _roomIndicators[selectedRoom.RoomID];
                activeIndicator.Highlight(true);
                
                // Прокручиваем карусель к выбранной аудитории
                flpRoomIndicators.ScrollControlIntoView(activeIndicator); 
            }
        }

        private void DrawVerticalText(Graphics g, DataGridView grid, string text, Font font, Rectangle rect)
        {
            var state = g.Save();
            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;
            g.SetClip(new Rectangle(0, 0, grid.Width, grid.Height));
            g.TranslateTransform(cx, cy);
            g.RotateTransform(-90); // Поворот текста
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(text, font, Brushes.Black, 0, 0, sf);
            }
            g.Restore(state);
        }

    }
}