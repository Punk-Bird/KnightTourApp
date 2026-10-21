namespace KnightTourApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private DoubleBufferedPanel boardPanel;
    private NumericUpDown numSize;
    private Button btnStart;
    private Button btnReset;
    private Button btnStepBack;
    private Button btnStepForward;
    private Button btnFinish;
    private Button btnPlayPause;
    private CheckBox chkShowCandidates;
    private CheckBox chkWarnsdorff;
    private CheckBox chkShowApprox;
    private TrackBar tbSpeed;
    private Label lblSpeed;
    private Label lblInfo;
    private Label lblStats;
    private Label lblLogHeader;
    private RichTextBox stepLog;
    private NumericUpDown numMaxN;
    private Button btnRunExperiment;
    private Panel analysisPanel;
    private DoubleBufferedPanel chartPanel;
    private Label lblAnalysisHeader;
    private Label lblMaxN;
    private ProgressBar progressBar;
    private Label lblExperimentStatus;
    private Label lblResultLogHeader;
    private TextBox resultLog;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        this.numMaxN = new NumericUpDown();
        this.btnRunExperiment = new Button();
        this.analysisPanel = new Panel();
        this.chartPanel = new DoubleBufferedPanel();
        this.lblAnalysisHeader = new Label();
        this.lblMaxN = new Label();
        this.progressBar = new ProgressBar();
        this.lblExperimentStatus = new Label();
        this.lblResultLogHeader = new Label();
        this.resultLog = new TextBox();

        this.boardPanel = new DoubleBufferedPanel();
        this.numSize = new NumericUpDown();
        this.btnStart = new Button();
        this.btnReset = new Button();
        this.btnStepBack = new Button();
        this.btnStepForward = new Button();
        this.btnFinish = new Button();
        this.btnPlayPause = new Button();
        this.chkShowCandidates = new CheckBox();
        this.chkWarnsdorff = new CheckBox();
        this.chkShowApprox = new CheckBox();
        this.tbSpeed = new TrackBar();
        this.lblSpeed = new Label();
        this.lblInfo = new Label();
        this.lblStats = new Label();
        this.lblLogHeader = new Label();
        this.stepLog = new RichTextBox();

        // ---- boardPanel ----
        this.boardPanel.Location = new Point(12, 142);
        this.boardPanel.Size = new Size(660, 660);
        this.boardPanel.BorderStyle = BorderStyle.FixedSingle;
        this.boardPanel.BackColor = Color.White;
        this.boardPanel.Paint += BoardPanel_Paint;
        this.boardPanel.MouseClick += BoardPanel_MouseClick;

        // ---- numSize ----
        this.numSize.Location = new Point(12, 10);
        this.numSize.Minimum = 5;
        this.numSize.Maximum = 20;
        this.numSize.Value = 6;
        this.numSize.Width = 55;
        this.numSize.ValueChanged += NumSize_ValueChanged;

        // ---- btnStart ----
        this.btnStart.Text = "Запустить";
        this.btnStart.Location = new Point(75, 9);
        this.btnStart.Size = new Size(90, 28);
        this.btnStart.Click += BtnStart_Click;

        // ---- btnReset ----
        this.btnReset.Text = "Сброс";
        this.btnReset.Location = new Point(170, 9);
        this.btnReset.Size = new Size(70, 28);
        this.btnReset.Click += BtnReset_Click;

        // ---- lblInfo ----
        this.lblInfo.Location = new Point(250, 12);
        this.lblInfo.Size = new Size(690, 24);
        this.lblInfo.Font = new Font("Segoe UI", 9F);
        this.lblInfo.ForeColor = Color.DarkSlateGray;

        // ---- btnStepBack ----
        this.btnStepBack.Text = "◀ Шаг назад";
        this.btnStepBack.Location = new Point(12, 46);
        this.btnStepBack.Size = new Size(110, 28);
        this.btnStepBack.Enabled = false;
        this.btnStepBack.Click += BtnStepBack_Click;

        // ---- btnStepForward ----
        this.btnStepForward.Text = "Шаг вперёд ▶";
        this.btnStepForward.Location = new Point(128, 46);
        this.btnStepForward.Size = new Size(120, 28);
        this.btnStepForward.Enabled = false;
        this.btnStepForward.Click += BtnStepForward_Click;

        // ---- btnPlayPause ----
        this.btnPlayPause.Text = "▶ Авто";
        this.btnPlayPause.Location = new Point(254, 46);
        this.btnPlayPause.Size = new Size(80, 28);
        this.btnPlayPause.Enabled = false;
        this.btnPlayPause.Click += BtnPlayPause_Click;

        // ---- btnFinish ----
        this.btnFinish.Text = "Закончить обход";
        this.btnFinish.Location = new Point(340, 46);
        this.btnFinish.Size = new Size(130, 28);
        this.btnFinish.Enabled = false;
        this.btnFinish.Click += BtnFinish_Click;

        // ---- chkShowCandidates ----
        this.chkShowCandidates.Text = "Показывать варианты ходов";
        this.chkShowCandidates.Location = new Point(12, 82);
        this.chkShowCandidates.Size = new Size(210, 22);
        this.chkShowCandidates.Checked = true;
        this.chkShowCandidates.CheckedChanged += (s, e) => boardPanel.Invalidate();

        // ---- chkWarnsdorff ----
        this.chkWarnsdorff.Text = "Эвристика Варнсдорфа";
        this.chkWarnsdorff.Location = new Point(230, 82);
        this.chkWarnsdorff.Size = new Size(180, 22);
        this.chkWarnsdorff.Checked = false; // по умолчанию выключено
        this.chkWarnsdorff.ForeColor = Color.DarkSlateGray;
        this.chkWarnsdorff.CheckedChanged += (s, e) => UpdateButtonsEnabled();

        // ---- chkShowApprox ----
        this.chkShowApprox.Text = "";
        this.chkShowApprox.Location = new Point(400, 97);
        this.chkShowApprox.Size = new Size(250, 22);
        this.chkShowApprox.Checked = true;
        this.chkShowApprox.ForeColor = Color.DarkSlateGray;
        this.chkShowApprox.CheckedChanged += (s, e) => chartPanel.Invalidate();

        // ---- tbSpeed ----
        this.tbSpeed.Location = new Point(420, 80);
        this.tbSpeed.Size = new Size(200, 26);
        this.tbSpeed.Minimum = 1;
        this.tbSpeed.Maximum = 10;
        this.tbSpeed.Value = 3;
        this.tbSpeed.TickFrequency = 1;
        this.tbSpeed.ValueChanged += (s, e) =>
        {
            lblSpeed.Text = $"Скорость: {tbSpeed.Value}";
            UpdateSpeed();
        };

        // ---- lblSpeed ----
        this.lblSpeed.Text = "Скорость: 3";
        this.lblSpeed.Location = new Point(626, 84);
        this.lblSpeed.Size = new Size(120, 22);
        this.lblSpeed.ForeColor = Color.DarkSlateGray;

        // ---- lblStats ----
        this.lblStats.Location = new Point(12, 112);
        this.lblStats.Size = new Size(660, 22);
        this.lblStats.Font = new Font("Consolas", 9F);
        this.lblStats.ForeColor = Color.FromArgb(60, 60, 60);

        // ---- lblLogHeader ----
        this.lblLogHeader.Text = "Журнал шагов";
        this.lblLogHeader.Location = new Point(690, 142);
        this.lblLogHeader.Size = new Size(250, 20);
        this.lblLogHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        this.lblLogHeader.ForeColor = Color.DarkSlateGray;

        // ---- logBox ----
        this.stepLog.Location = new Point(690, 165);
        this.stepLog.Size = new Size(248, 637);
        this.stepLog.ReadOnly = true;
        this.stepLog.BackColor = Color.FromArgb(250, 250, 250);
        this.stepLog.BorderStyle = BorderStyle.FixedSingle;
        this.stepLog.Font = new Font("Consolas", 9F);
        this.stepLog.WordWrap = true;
        this.stepLog.ScrollBars = RichTextBoxScrollBars.Vertical;

        // ================= analysisPanel =================
        this.analysisPanel.Dock = DockStyle.Right;
        this.analysisPanel.Width = 680;
        this.analysisPanel.BackColor = Color.FromArgb(245, 246, 250);
        this.analysisPanel.BorderStyle = BorderStyle.FixedSingle;

        // ---- lblAnalysisHeader ----
        this.lblAnalysisHeader.Text = "Аналитический эксперимент";
        this.lblAnalysisHeader.Location = new Point(10, 12);
        this.lblAnalysisHeader.Size = new Size(460, 22);
        this.lblAnalysisHeader.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        this.lblAnalysisHeader.ForeColor = Color.FromArgb(40, 40, 60);

        // ---- lblMaxN ----
        this.lblMaxN.Text = "Макс. n:";
        this.lblMaxN.Location = new Point(10, 46);
        this.lblMaxN.Size = new Size(70, 22);

        // ---- numMaxN ----
        this.numMaxN.Location = new Point(80, 44);
        this.numMaxN.Minimum = 5;
        this.numMaxN.Maximum = 1000;
        this.numMaxN.Value = 100;
        this.numMaxN.Width = 70;

        // ---- btnRunExperiment ----
        this.btnRunExperiment.Text = "Запустить серию";
        this.btnRunExperiment.Location = new Point(160, 42);
        this.btnRunExperiment.Size = new Size(310, 28);
        this.btnRunExperiment.Click += BtnRunExperiment_Click;

        // ---- progressBar ----
        this.progressBar.Location = new Point(10, 78);
        this.progressBar.Size = new Size(460, 16);

        // ---- lblExperimentStatus ----
        this.lblExperimentStatus.Location = new Point(10, 98);
        this.lblExperimentStatus.Size = new Size(380, 20);
        this.lblExperimentStatus.ForeColor = Color.DarkSlateGray;

        // ---- lblExperimentStatus ----
        this.lblExperimentStatus.Location = new Point(10, 98);
        this.lblExperimentStatus.Size = new Size(460, 20);
        this.lblExperimentStatus.ForeColor = Color.DarkSlateGray;

        // ---- chartPanel ----
        this.chartPanel.Location = new Point(10, 122);
        this.chartPanel.Size = new Size(660, 420);
        this.chartPanel.BorderStyle = BorderStyle.FixedSingle;
        this.chartPanel.BackColor = Color.White;
        this.chartPanel.Paint += ChartPanel_Paint;

        // ---- lblResultLogHeader ----
        this.lblResultLogHeader.Text = "Результаты эксперимента";
        this.lblResultLogHeader.Location = new Point(10, 550);
        this.lblResultLogHeader.Size = new Size(460, 18);
        this.lblResultLogHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        this.lblResultLogHeader.ForeColor = Color.DarkSlateGray;

        // ---- resultLog ----
        this.resultLog.Location = new Point(10, 570);
        this.resultLog.Size = new Size(660, 250);
        this.resultLog.ReadOnly = true;
        this.resultLog.BackColor = Color.White;
        this.resultLog.BorderStyle = BorderStyle.FixedSingle;
        this.resultLog.Font = new Font("Consolas", 8.5F);
        this.resultLog.WordWrap = false;
        this.resultLog.Multiline = true;
        this.resultLog.ScrollBars = ScrollBars.Vertical;
        this.resultLog.Anchor = AnchorStyles.Top | AnchorStyles.Left
                              | AnchorStyles.Bottom | AnchorStyles.Right;

        // складываем всё в analysisPanel
        this.analysisPanel.Controls.Add(this.lblAnalysisHeader);
        this.analysisPanel.Controls.Add(this.lblMaxN);
        this.analysisPanel.Controls.Add(this.numMaxN);
        this.analysisPanel.Controls.Add(this.btnRunExperiment);
        this.analysisPanel.Controls.Add(this.progressBar);
        this.analysisPanel.Controls.Add(this.lblExperimentStatus);
        this.analysisPanel.Controls.Add(this.chartPanel);
        this.analysisPanel.Controls.Add(this.lblResultLogHeader);
        this.analysisPanel.Controls.Add(this.resultLog);
        this.analysisPanel.Controls.Add(this.chkShowApprox);

        // ---- Form ----
        this.ClientSize = new Size(950, 815);
        this.Text = "Обход доски конём — визуализация рекурсии";
        this.Controls.Add(this.boardPanel);
        this.Controls.Add(this.numSize);
        this.Controls.Add(this.btnStart);
        this.Controls.Add(this.btnReset);
        this.Controls.Add(this.btnStepBack);
        this.Controls.Add(this.btnStepForward);
        this.Controls.Add(this.btnPlayPause);
        this.Controls.Add(this.btnFinish);
        this.Controls.Add(this.chkShowCandidates);
        this.Controls.Add(this.chkWarnsdorff);
        this.Controls.Add(this.tbSpeed);
        this.Controls.Add(this.lblSpeed);
        this.Controls.Add(this.lblInfo);
        this.Controls.Add(this.lblStats);
        this.Controls.Add(this.lblLogHeader);
        this.Controls.Add(this.stepLog);
        this.Controls.Add(this.analysisPanel);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;

        this.ClientSize = new Size(1640, 830);
        this.MinimumSize = new Size(1400, 700);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.Resize += MainForm_Resize;

        this.MaximizeBox = false;
    }
}