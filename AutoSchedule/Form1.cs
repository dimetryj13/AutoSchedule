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
        private const int BASE_ROW_HEIGHT = 33; // 33px на каждую неделю (в сумме пара = 66px)
        private const int BASE_HEADER_HEIGHT = 40;
        private const float BASE_FONT_SIZE_DAY = 12f;
        private const float BASE_FONT_SIZE_PAIR = 12f;
        private const int BASE_WIDTH_DAY = 40;
        private const int BASE_WIDTH_PAIR = 40;
        private const int BASE_WIDTH_SUBJ = 200; // Колонка дисциплины
        private const int BASE_WIDTH_ROOM = 50;  // Новая колонка аудитории

        // Кисти для рисования границ ячеек
        private Pen _gridPen = new Pen(Color.LightGray, 1);
        private Pen _thickPen = new Pen(Color.Black, 2);

        // Переменная для хранения текущей выбранной группы
        private Models.GroupList _selectedGroup = null;

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
            flpAssignments.Visible = false;
            flpAssignments.SuspendLayout();
            flpAssignments.Controls.Clear();

            if (_selectedGroup == null)
            {
                Label lbl = new Label { Text = "Выберите группу...", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray };
                flpAssignments.Controls.Add(lbl);
                flpAssignments.ResumeLayout();
                flpAssignments.Visible = true;
                return;
            }

            var allItems = globalPool.GetAvailableItems();
            // ФИЛЬТРАЦИЯ: Добавили Trim() и StringComparison для надежности
            var availableItems = allItems
                .Where(i => i != null && string.Equals(
                    i.PlanReference?.Group?.GroupName?.Trim(),
                    _selectedGroup.GroupName?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (availableItems.Count > 0)
            {
                // Отрисовка карточек (твой стандартный код с прогресс-баром)
                foreach (var item in availableItems)
                {
                    var card = new Controls.ScheduleCardControl(item);
                    // ... (загрузка комнат и события)
                    card.MouseEnter += (s, args) => { ShowRoomsForCard(card); };
                    card.MouseLeave += (s, args) => { ClearRoomIndicators(); };
                    card.RoomSelectionChanged += (s, room) => UpdateRoomHighlight(room);
                    flpAssignments.Controls.Add(card);
                }
            }
            flpAssignments.ResumeLayout();
            flpAssignments.Visible = true;
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
            if (groups == null) return;
            var activeGroups = groups.Where(g => g != null && g.Actually).ToList();
            if (activeGroups.Count == 0) return;

            pnlScheduleContainer.SuspendLayout();
            pnlScheduleContainer.Controls.Clear();

            dgvSchedule = new DataGridView();
            dgvSchedule.Dock = DockStyle.Fill;
            dgvSchedule.AllowUserToAddRows = false;
            dgvSchedule.RowHeadersVisible = false;
            dgvSchedule.BackgroundColor = Color.White;
            dgvSchedule.AllowDrop = true;
            dgvSchedule.ReadOnly = true;

            SetDoubleBuffered(dgvSchedule);

            dgvSchedule.Columns.Add("colDay", "День");
            dgvSchedule.Columns.Add("colPair", "Пара");
            dgvSchedule.Columns["colDay"].Frozen = true;
            dgvSchedule.Columns["colPair"].Frozen = true;

            // ГЕНЕРАЦИЯ ДВОЙНЫХ КОЛОНОК (Дисциплина + Ауд)
            foreach (var group in activeGroups)
            {
                string gName = string.IsNullOrEmpty(group.GroupName) ? "Без названия" : group.GroupName;

                int idxSubj = dgvSchedule.Columns.Add($"g_{group.GroupId}_subj", gName);
                dgvSchedule.Columns[idxSubj].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvSchedule.Columns[idxSubj].Tag = group; // <--- ВАЖНО: Запоминаем группу

                int idxRoom = dgvSchedule.Columns.Add($"g_{group.GroupId}_room", "Ауд.");
                dgvSchedule.Columns[idxRoom].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvSchedule.Columns[idxRoom].Tag = group; // <--- ВАЖНО: Запоминаем группу
            }

            dgvSchedule.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSchedule.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvSchedule.ColumnHeadersHeight = 40;
            dgvSchedule.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvSchedule.EnableHeadersVisualStyles = false;

            // ГЕНЕРАЦИЯ 72 СТРОК (Светлая и Темная неделя)
            string[] days = { "ПОНЕДЕЛЬНИК", "ВТОРНИК", "СРЕДА", "ЧЕТВЕРГ", "ПЯТНИЦА", "СУББОТА" };
            for (int d = 0; d < 6; d++)
            {
                for (int p = 1; p <= 6; p++)
                {
                    int r1 = dgvSchedule.Rows.Add();
                    dgvSchedule.Rows[r1].Cells["colDay"].Value = days[d];
                    dgvSchedule.Rows[r1].Cells["colPair"].Value = p.ToString();
                    dgvSchedule.Rows[r1].Tag = new Tuple<int, int, int>(d + 1, p, 1); // 1 = Светлая

                    int r2 = dgvSchedule.Rows.Add();
                    dgvSchedule.Rows[r2].Cells["colDay"].Value = days[d];
                    dgvSchedule.Rows[r2].Cells["colPair"].Value = p.ToString();
                    dgvSchedule.Rows[r2].Tag = new Tuple<int, int, int>(d + 1, p, 2); // 2 = Темная

                    for (int c = 2; c < dgvSchedule.Columns.Count; c++)
                    {
                        dgvSchedule.Rows[r1].Cells[c].Style.BackColor = Color.White;
                        dgvSchedule.Rows[r2].Cells[c].Style.BackColor = Color.FromArgb(245, 245, 245);
                    }
                }
            }

            dgvSchedule.CellPainting += DgvSchedule_CellPainting;
            dgvSchedule.Paint += DgvSchedule_Paint;
            dgvSchedule.MouseWheel += DgvSchedule_MouseWheel;
            dgvSchedule.Scroll += (s, ev) => dgvSchedule.Invalidate();

            // СОБЫТИЕ КЛИКА ПО ГРУППЕ
            dgvSchedule.ColumnHeaderMouseClick += DgvSchedule_ColumnHeaderMouseClick;

            ApplyZoom();

            pnlScheduleContainer.Controls.Add(dgvSchedule);
            pnlScheduleContainer.ResumeLayout();
        }
        private void DgvSchedule_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                // Масштабирование
                if (e.Delta > 0) _zoomLevel += ZOOM_STEP;
                else _zoomLevel -= ZOOM_STEP;

                _zoomLevel = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _zoomLevel));
                ApplyZoom();
                ((HandledMouseEventArgs)e).Handled = true;
            }
            else if (Control.ModifierKeys == Keys.Shift)
            {
                // Горизонтальная прокрутка
                if (e.Delta > 0)
                {
                    // Ограничение левой границы (индексы 0 и 1 закреплены)
                    if (dgvSchedule.FirstDisplayedScrollingColumnIndex > 2)
                        dgvSchedule.FirstDisplayedScrollingColumnIndex--;
                }
                else
                {
                    // Ограничение правой границы
                    if (dgvSchedule.FirstDisplayedScrollingColumnIndex >= 2 &&
                        dgvSchedule.FirstDisplayedScrollingColumnIndex < dgvSchedule.ColumnCount - 1)
                    {
                        dgvSchedule.FirstDisplayedScrollingColumnIndex++;
                    }
                }
                ((HandledMouseEventArgs)e).Handled = true;
            }
        }
        private void ApplyZoom()
        {
            if (dgvSchedule == null) return;
            dgvSchedule.SuspendLayout();

            dgvSchedule.ColumnHeadersHeight = (int)(BASE_HEADER_HEIGHT * _zoomLevel);
            foreach (DataGridViewRow row in dgvSchedule.Rows)
                row.Height = (int)(BASE_ROW_HEIGHT * _zoomLevel);

            dgvSchedule.Columns["colDay"].Width = (int)(BASE_WIDTH_DAY * _zoomLevel);
            dgvSchedule.Columns["colPair"].Width = (int)(BASE_WIDTH_PAIR * _zoomLevel);

            // Масштабируем: четные колонки - предмет (200px), нечетные - аудитория (50px)
            for (int i = 2; i < dgvSchedule.Columns.Count; i += 2)
            {
                dgvSchedule.Columns[i].Width = (int)(BASE_WIDTH_SUBJ * _zoomLevel);
                if (i + 1 < dgvSchedule.Columns.Count)
                    dgvSchedule.Columns[i + 1].Width = (int)(BASE_WIDTH_ROOM * _zoomLevel);
            }

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

            // ПОДСВЕТКА ВЫБРАННОЙ ГРУППЫ ГОЛУБЫМ ЦВЕТОМ
            if (!isHeaderCol && _selectedGroup != null)
            {
                var colGroup = dgvSchedule.Columns[e.ColumnIndex].Tag as Models.GroupList;
                if (colGroup != null && colGroup.GroupName == _selectedGroup.GroupName)
                {
                    bool isDarkWeek = e.RowIndex % 2 != 0;
                    Color highlightColor = isDarkWeek ? Color.FromArgb(210, 230, 255) : Color.AliceBlue;
                    using (SolidBrush bgBrush = new SolidBrush(highlightColor))
                        e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                }
                else e.PaintBackground(e.CellBounds, true);
            }
            else e.PaintBackground(e.CellBounds, !isHeaderCol);

            e.Graphics.DrawLine(_gridPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);

            if (!isHeaderCol) e.Graphics.DrawLine(_gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            else
            {
                if (e.ColumnIndex == 0 && (e.RowIndex + 1) % 12 == 0) // Конец дня (12 строк)
                    e.Graphics.DrawLine(_gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                else if (e.ColumnIndex == 1 && (e.RowIndex + 1) % 2 == 0) // Конец пары (2 строки)
                    e.Graphics.DrawLine(_gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            if (isHeaderCol) e.Handled = true;
            else { e.PaintContent(e.CellBounds); e.Handled = true; }
        }

        private void DgvSchedule_Paint(object sender, PaintEventArgs e)
        {
            if (dgvSchedule == null || dgvSchedule.Rows.Count == 0) return;
            int firstRow = dgvSchedule.FirstDisplayedScrollingRowIndex;
            if (firstRow < 0) return;
            int lastRow = firstRow + dgvSchedule.DisplayedRowCount(true);

            for (int i = firstRow; i <= lastRow && i < dgvSchedule.Rows.Count; i++)
            {
                if ((i + 1) % 12 == 0)
                {
                    var rect = dgvSchedule.GetRowDisplayRectangle(i, true);
                    if (rect.Height > 0) e.Graphics.DrawLine(_thickPen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
                }
            }

            var rectNav = dgvSchedule.GetColumnDisplayRectangle(1, true);
            if (rectNav.Width > 0) e.Graphics.DrawLine(_thickPen, rectNav.Right - 1, 0, rectNav.Right - 1, dgvSchedule.Height);

            // Разделители групп
            for (int c = 3; c < dgvSchedule.Columns.Count; c += 2)
            {
                var rectCol = dgvSchedule.GetColumnDisplayRectangle(c, true);
                if (rectCol.Width > 0) e.Graphics.DrawLine(_thickPen, rectCol.Right - 1, 0, rectCol.Right - 1, dgvSchedule.Height);
            }

            Font fontDay = new Font("Segoe UI", BASE_FONT_SIZE_DAY * _zoomLevel, FontStyle.Bold);
            Font fontPair = new Font("Segoe UI", BASE_FONT_SIZE_PAIR * _zoomLevel, FontStyle.Bold);

            // Отрисовка Дней
            int startDayBlock = (firstRow / 12) * 12;
            for (int i = startDayBlock; i <= lastRow; i += 12)
            {
                if (i >= dgvSchedule.Rows.Count) break;
                Rectangle rectDay = GetMergedRectangle(dgvSchedule, 0, i, 12);
                if (rectDay.Height > 20)
                {
                    string dayText = dgvSchedule.Rows[i].Cells["colDay"].Value?.ToString() ?? "";
                    DrawVerticalText(e.Graphics, dgvSchedule, dayText, fontDay, rectDay);
                }
            }

            // Отрисовка Пар
            int startPairBlock = (firstRow / 2) * 2;
            for (int i = startPairBlock; i <= lastRow; i += 2)
            {
                if (i >= dgvSchedule.Rows.Count) break;
                Rectangle rectPair = GetMergedRectangle(dgvSchedule, 1, i, 2);
                if (rectPair.Height > 10)
                {
                    string pairText = dgvSchedule.Rows[i].Cells["colPair"].Value?.ToString() ?? "";
                    TextRenderer.DrawText(e.Graphics, pairText, fontPair, rectPair, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
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

        private void DgvSchedule_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 2) return;

            var clickedCol = dgvSchedule.Columns[e.ColumnIndex];
            var group = clickedCol.Tag as Models.GroupList;

            if (group != null)
            {
                if (_selectedGroup != null && _selectedGroup.GroupName == group.GroupName)
                    _selectedGroup = null; // Снимаем выделение
                else
                    _selectedGroup = group; // Выделяем новую

                dgvSchedule.ClearSelection();
                UpdateColumnHighlights();

                PopulateAssignmentCards(); // Обновляем карточки слева
            }
        }

        private void UpdateColumnHighlights()
        {
            for (int i = 2; i < dgvSchedule.Columns.Count; i++)
            {
                var colGroup = dgvSchedule.Columns[i].Tag as Models.GroupList;
                bool isSelected = (_selectedGroup != null && colGroup != null && colGroup.GroupName == _selectedGroup.GroupName);
                dgvSchedule.Columns[i].HeaderCell.Style.BackColor = isSelected ? Color.LightSkyBlue : Color.FromArgb(240, 240, 240);
            }
            dgvSchedule.Invalidate();
        }

        // --- ОБРАБОТЧИК КНОПКИ ВЫБОРА ГРУППЫ ---
        private void btnSelectGroup_Click(object sender, EventArgs e)
        {
            if (groups == null || groups.Count == 0) return;

            ContextMenuStrip menu = new ContextMenuStrip();
            foreach (var g in groups.Where(g => g.Actually))
            {
                var item = menu.Items.Add(g.GroupName);
                item.Tag = g;
                item.Click += (s, args) =>
                {
                    _selectedGroup = (Models.GroupList)((ToolStripMenuItem)s).Tag;
                    UpdateColumnHighlights();
                    PopulateAssignmentCards();
                };
            }
            menu.Show(Cursor.Position);
        }
    }
}