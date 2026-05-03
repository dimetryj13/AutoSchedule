using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoSchedule.Models;

namespace AutoSchedule.Controls
{
    public partial class RoomIndicatorControl : UserControl
    {
        public Classroom RoomData { get; private set; }
        private bool _isHighlighted = false;
        private bool _isPriority = false;

        public RoomIndicatorControl(Classroom room, bool isPriority)
        {
            InitializeComponent();
            RoomData = room;
            _isPriority = isPriority;

            this.Width = 70;
            this.Height = 36;
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.Margin = new Padding(4, 5, 4, 5);
        }

        public void Highlight(bool active)
        {
            if (_isHighlighted != active)
            {
                _isHighlighted = active;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(2, 2, this.Width - 4, this.Height - 4);
            GraphicsPath path = new GraphicsPath();
            int radius = 4;
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseAllFigures();

            // Фон: если приоритет, делаем его слегка теплым желтоватым, иначе белым
            Color bgColor = _isPriority ? Color.FromArgb(255, 252, 235) : Color.White;
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillPath(bgBrush, path);
            }

            // РАМКА
            if (_isHighlighted)
            {
                using (Pen activePen = new Pen(Color.DodgerBlue, 3f))
                    e.Graphics.DrawPath(activePen, path);
            }
            else if (_isPriority)
            {
                // Особенная золотая рамка для приоритетных аудиторий
                using (Pen priorityPen = new Pen(Color.Goldenrod, 2f))
                    e.Graphics.DrawPath(priorityPen, path);
            }
            else
            {
                using (Pen inactivePen = new Pen(Color.LightGray, 1f))
                    e.Graphics.DrawPath(inactivePen, path);
            }

            Font roomFont = new Font("Segoe UI", 10, FontStyle.Bold);
            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(RoomData.RoomNumber, roomFont, Brushes.Black, rect, sf);

            Font capacityFont = new Font("Segoe UI", 6, FontStyle.Regular);
            StringFormat capSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
            e.Graphics.DrawString($"{RoomData.Capacity} чел", capacityFont, Brushes.Gray, new RectangleF(rect.X, rect.Y, rect.Width - 2, rect.Height - 2), capSf);
        }
    }
}