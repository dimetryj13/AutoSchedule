using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.IO;

namespace AutoSchedule
{
    public partial class Form1 : Form
    {
        // Строка подключения (изначально пустая или к шаблону)
        string connectionString = "";
        // Список путей к БД
        List<string> recentDatabases = new List<string>();
        // Путь к нашей папке с базами
        string dbFolderPath = Path.Combine(Application.StartupPath, "Databases");
        int currentScrollIndex = 0;

        // --- ХРАНИЛИЩА ДАННЫХ ИЗ БД ---
        List<Models.Classroom> classrooms = new List<Models.Classroom>();
        List<Models.GroupList> groups = new List<Models.GroupList>();
        List<Models.Subject> subjects = new List<Models.Subject>();
        List<Models.Teacher> teachers = new List<Models.Teacher>();
        List<Models.AcademicPlan> academicPlans = new List<Models.AcademicPlan>();
        List<Models.TeacherDayOff> teacherDaysOff = new List<Models.TeacherDayOff>();
        List<Models.Schedule> schedules = new List<Models.Schedule>();
        List<Models.SessionSetting> sessionSettings = new List<Models.SessionSetting>();
        List<Models.SessionSchedule> sessionSchedules = new List<Models.SessionSchedule>();
        List<Models.TeacherAvailability> teacherAvailabilities = new List<Models.TeacherAvailability>();
        List<Models.TeacherRoomPref> teacherRoomPrefs = new List<Models.TeacherRoomPref>();
        List<Models.EnrichedAcademicPlan> enrichedPlans = new List<Models.EnrichedAcademicPlan>();
        List<Models.Student> students = new List<Models.Student>();
        // ------------------------------

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 1. Проверяем наличие папки для БД, если нет — создаем
            if (!Directory.Exists(dbFolderPath))
            {
                Directory.CreateDirectory(dbFolderPath);
            }

            // 2. Загружаем и сортируем файлы
            RefreshDatabaseList();
        }

        private void RefreshDatabaseList()
        {
            if (Directory.Exists(dbFolderPath))
            {
                // Берем все файлы accdb, сортируем по дате изменения
                recentDatabases = Directory.GetFiles(dbFolderPath, "*.accdb")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                // Если список пуст, карусель будет пустой
                UpdateCarousel();
            }
        }

        private void UpdateCarousel()
        {
            panelRecent.Controls.Clear();
            if (recentDatabases.Count == 0) return;

            int buttonWidth = 100;
            int buttonHeight = panelRecent.Height - 10;
            int spacing = 10;

            // Важно: проверяем ширину панели, чтобы не делить на ноль
            if (panelRecent.Width <= 0) return;

            int visibleCount = panelRecent.Width / (buttonWidth + spacing);

            for (int i = 0; i < visibleCount; i++)
            {
                int dataIndex = currentScrollIndex + i;
                if (dataIndex >= recentDatabases.Count) break;

                string fullPath = recentDatabases[dataIndex];
                Button btn = new Button();
                btn.Width = buttonWidth;
                btn.Height = buttonHeight;
                btn.Top = 5;
                btn.Left = i * (buttonWidth + spacing);
                btn.Text = Path.GetFileNameWithoutExtension(fullPath);
                btn.Tag = fullPath;
                btn.Click += RecentDbButton_Click;
                panelRecent.Controls.Add(btn);
            }
        }

        private void LoadDatabaseData(string dbPath)
        {
            try
            {
                // Создаем нашего менеджера, передавая ему путь к выбранной базе
                DatabaseManager dbManager = new DatabaseManager(dbPath);

                // Выгружаем данные из БД в оперативную память
                classrooms = dbManager.GetClassrooms();
                groups = dbManager.GetGroups();
                subjects = dbManager.GetSubjects();
                teachers = dbManager.GetTeachers();
                academicPlans = dbManager.GetAcademicPlans();
                teacherDaysOff = dbManager.GetTeacherDaysOff();
                schedules = dbManager.GetSchedules();
                sessionSettings = dbManager.GetSessionSettings();
                sessionSchedules = dbManager.GetSessionSchedules();
                teacherAvailabilities = dbManager.GetTeacherAvailability();
                teacherRoomPrefs = dbManager.GetTeacherRoomPrefs();

                // --- СВЯЗЫВАНИЕ ДАННЫХ (DATA MAPPING) ---
                Services.DataMappingService mappingService = new Services.DataMappingService(groups, subjects, teachers);
                enrichedPlans = mappingService.MapAcademicPlans(academicPlans);

                // Давай заодно проверим, сработала ли связь, добавив вывод в MessageBox
                int testLinkedCount = enrichedPlans.Count(p => p.Subject != null);
                // Для проверки выведем небольшое сообщение (потом его можно убрать)
                MessageBox.Show(
                    $"База успешно загружена!\n" +
                    $"Аудиторий: {classrooms.Count}\n" +
                    $"Преподавателей: {teachers.Count}\n" +
                    $"Групп: {groups.Count}"+
                    $"Связанных планов: {testLinkedCount}",
                    "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных из БД: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Этот метод будет вызываться каждый раз, когда мы нажимаем на ЛЮБУЮ сгенерированную кнопку БД
        private void RecentDbButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string path = clickedButton.Tag.ToString(); // Достаем путь из "секретного кармана"

            // Обновляем строку подключения
           // connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};";
            //MessageBox.Show("БД выбрана:\n" + path);

            connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};";

            // ВЫЗЫВАЕМ НАШ НОВЫЙ МЕТОД:
            LoadDatabaseData(path);
        }

        // Для кнопки "Влево"
        private void btnLeft_Click(object sender, EventArgs e)
        {
            // Сдвигаемся влево, только если мы не в самом начале списка
            if (currentScrollIndex > 0)
            {
                currentScrollIndex--;
                UpdateCarousel(); // Перерисовываем кнопки
            }
        }

        // Для кнопки "Вправо"
        private void btnRight_Click(object sender, EventArgs e)
        {
            // Вычисляем, сколько кнопок помещается на панели
            int buttonWidth = 100;
            int spacing = 10;
            int visibleCount = panelRecent.Width / (buttonWidth + spacing);

            // Сдвигаемся вправо, только если за краем экрана еще остались базы данных
            if (currentScrollIndex + visibleCount < recentDatabases.Count)
            {
                currentScrollIndex++;
                UpdateCarousel(); // Перерисовываем кнопки
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = dbFolderPath; // Сразу открываем нашу папку
            saveFileDialog.Filter = "Access Database (*.accdb)|*.accdb";
            saveFileDialog.FileName = "NewSchedule.accdb";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string targetPath = saveFileDialog.FileName;
                string sourcePath = Path.Combine(Application.StartupPath, "University.accdb");
                try
                {
                    // Копируем файл
                    File.Copy(sourcePath, targetPath, true);

                    // После создания просто обновляем весь список из папки
                    RefreshDatabaseList();

                    MessageBox.Show("База создана в папке проекта и добавлена в список.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Access Database (*.accdb)|*.accdb|Старые базы Access (*.mdb)|*.mdb";
            openFileDialog.Title = "Выберите файл базы данных";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = openFileDialog.FileName;
                string fileName = Path.GetFileName(selectedPath);
                string targetPath = Path.Combine(dbFolderPath, fileName);

                // Если выбранный файл находится НЕ в нашей рабочей папке Databases, копируем его туда
                if (selectedPath != targetPath)
                {
                    try
                    {
                        File.Copy(selectedPath, targetPath, true);
                        selectedPath = targetPath; // Теперь наша цель — скопированный файл
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при копировании базы в рабочую папку: " + ex.Message);
                        return;
                    }
                }

                // Настраиваем подключение
                connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={selectedPath};";

                // Делаем этот файл "самым новым", чтобы он прыгнул на 1 место в карусели
                File.SetLastWriteTime(selectedPath, DateTime.Now);

                // Обновляем карусель
                RefreshDatabaseList();

                MessageBox.Show("База данных выбрана и добавлена в работу:\n" + selectedPath);
            }
        }

        private void btnViewAll_Click(object sender, EventArgs e)
        {
            FormDbList popup = new FormDbList(); // Теперь без параметров
            if (popup.ShowDialog() == DialogResult.OK)
            {
                string path = popup.SelectedPath;
                connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};";

                // Обновляем время доступа к файлу, чтобы он стал первым в списке
                File.SetLastWriteTime(path, DateTime.Now);

                RefreshDatabaseList();
               // MessageBox.Show("База загружена.");
                // ВЫЗЫВАЕМ НАШ НОВЫЙ МЕТОД:
                LoadDatabaseData(path);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}