namespace AutoSchedule
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.tlpMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.flpAssignments = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlScheduleContainer = new System.Windows.Forms.Panel();
            this.flpRoomIndicators = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlToolbar = new System.Windows.Forms.Panel();
            this.btnOpenDb = new System.Windows.Forms.Button();
            this.btnSelectGroup = new System.Windows.Forms.Button();
            this.tlpMainLayout.SuspendLayout();
            this.pnlToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpMainLayout
            // 
            this.tlpMainLayout.ColumnCount = 2;
            this.tlpMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19F));
            this.tlpMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 81F));
            this.tlpMainLayout.Controls.Add(this.flpAssignments, 0, 0);
            this.tlpMainLayout.Controls.Add(this.pnlScheduleContainer, 1, 1);
            this.tlpMainLayout.Controls.Add(this.flpRoomIndicators, 1, 2);
            this.tlpMainLayout.Controls.Add(this.pnlToolbar, 1, 0);
            this.tlpMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMainLayout.Location = new System.Drawing.Point(0, 0);
            this.tlpMainLayout.Name = "tlpMainLayout";
            this.tlpMainLayout.RowCount = 3;
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 78F));
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.tlpMainLayout.Size = new System.Drawing.Size(800, 450);
            this.tlpMainLayout.TabIndex = 0;
            this.tlpMainLayout.Paint += new System.Windows.Forms.PaintEventHandler(this.tlpMainLayout_Paint);
            // 
            // flpAssignments
            // 
            this.flpAssignments.AutoScroll = true;
            this.flpAssignments.BackColor = System.Drawing.SystemColors.ControlLight;
            this.flpAssignments.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpAssignments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpAssignments.Location = new System.Drawing.Point(10, 10);
            this.flpAssignments.Margin = new System.Windows.Forms.Padding(10);
            this.flpAssignments.Name = "flpAssignments";
            this.tlpMainLayout.SetRowSpan(this.flpAssignments, 3);
            this.flpAssignments.Size = new System.Drawing.Size(132, 430);
            this.flpAssignments.TabIndex = 0;
            // 
            // pnlScheduleContainer
            // 
            this.pnlScheduleContainer.BackColor = System.Drawing.SystemColors.Window;
            this.pnlScheduleContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlScheduleContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlScheduleContainer.Location = new System.Drawing.Point(155, 61);
            this.pnlScheduleContainer.Name = "pnlScheduleContainer";
            this.pnlScheduleContainer.Size = new System.Drawing.Size(642, 345);
            this.pnlScheduleContainer.TabIndex = 1;
            // 
            // flpRoomIndicators
            // 
            this.flpRoomIndicators.AutoScroll = true;
            this.flpRoomIndicators.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.flpRoomIndicators.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpRoomIndicators.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpRoomIndicators.Location = new System.Drawing.Point(155, 412);
            this.flpRoomIndicators.Name = "flpRoomIndicators";
            this.flpRoomIndicators.Padding = new System.Windows.Forms.Padding(10);
            this.flpRoomIndicators.Size = new System.Drawing.Size(642, 35);
            this.flpRoomIndicators.TabIndex = 2;
            // 
            // pnlToolbar
            // 
            this.pnlToolbar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlToolbar.Controls.Add(this.btnSelectGroup);
            this.pnlToolbar.Controls.Add(this.btnOpenDb);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlToolbar.Location = new System.Drawing.Point(155, 3);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Size = new System.Drawing.Size(642, 52);
            this.pnlToolbar.TabIndex = 3;
            // 
            // btnOpenDb
            // 
            this.btnOpenDb.Location = new System.Drawing.Point(3, -1);
            this.btnOpenDb.Name = "btnOpenDb";
            this.btnOpenDb.Size = new System.Drawing.Size(75, 23);
            this.btnOpenDb.TabIndex = 0;
            this.btnOpenDb.Text = "button1";
            this.btnOpenDb.UseVisualStyleBackColor = true;
            this.btnOpenDb.Click += new System.EventHandler(this.btnOpenDb_Click);
            // 
            // btnSelectGroup
            // 
            this.btnSelectGroup.Location = new System.Drawing.Point(101, -1);
            this.btnSelectGroup.Name = "btnSelectGroup";
            this.btnSelectGroup.Size = new System.Drawing.Size(146, 23);
            this.btnSelectGroup.TabIndex = 1;
            this.btnSelectGroup.Text = "Выбрать группу";
            this.btnSelectGroup.UseVisualStyleBackColor = true;
            this.btnSelectGroup.Click += new System.EventHandler(this.btnSelectGroup_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tlpMainLayout);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tlpMainLayout.ResumeLayout(false);
            this.pnlToolbar.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpMainLayout;
        private System.Windows.Forms.FlowLayoutPanel flpAssignments;
        private System.Windows.Forms.Panel pnlScheduleContainer;
        private System.Windows.Forms.FlowLayoutPanel flpRoomIndicators;
        private System.Windows.Forms.Panel pnlToolbar;
        private System.Windows.Forms.Button btnOpenDb;
        private System.Windows.Forms.Button btnSelectGroup;
    }
}

