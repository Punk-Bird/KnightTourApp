using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace KnightTourApp;

public partial class MainForm : Form
{
    private int _n = 6;
    private int _cellSize;
    private int? _startRow;
    private int? _startCol;

    private KnightTourStepper? _stepper;

    // Анимация коня
    private readonly System.Windows.Forms.Timer _animTimer;
    private PointF _knightPos;
    private PointF _animFrom, _animTo;
    private double _animElapsed;
    private double _animDuration = 0.18;
    private bool _isAnimating;

    // Автопроигрывание
    private readonly System.Windows.Forms.Timer _autoTimer;
    private bool _autoPlaying;

    // Счётчик для журнала
    private long _logStep;

    // Эксперимент
    private class ExperimentResult
    {
        public int N;
        public bool Skip;          // true = был хотя бы один откат, точка пропускается
        public double ElapsedMs;
    }
    private readonly List<ExperimentResult> _results = new();
    private bool _experimentRunning;

    // Цвета
    private readonly Color _visitedColor = Color.FromArgb(180, 215, 250);
    private readonly Color _startColor = Color.FromArgb(120, 210, 120);
    private readonly Color _pathColor = Color.FromArgb(215, 60, 60);
    private readonly Color _knightColor = Color.FromArgb(30, 30, 30);
    private readonly Color _candOutline = Color.FromArgb(90, 90, 90);
    private readonly Color _candTriedFill = Color.FromArgb(210, 210, 210);
    private readonly Color _candNextFill = Color.FromArgb(120, 220, 120);
    private readonly Color _deadEndColor = Color.FromArgb(240, 90, 90);

    public MainForm()
    {
        InitializeComponent();
        _n = (int)numSize.Value;
        RecalcCellSize();

        _animTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _animTimer.Tick += AnimTimer_Tick;

        _autoTimer = new System.Windows.Forms.Timer { Interval = 800 };
        _autoTimer.Tick += AutoTimer_Tick;

        lblInfo.Text = "Выберите стартовую клетку";
        UpdateSpeed();
        UpdateStats();
    }

    private void RecalcCellSize()
    {
        int s = Math.Min(boardPanel.Width, boardPanel.Height);
        _cellSize = s / _n;
    }

    private PointF CellCenter(int row, int col) =>
        new(col * _cellSize + _cellSize / 2f, row * _cellSize + _cellSize / 2f);

    // ============================================================
    //                        ОТРИСОВКА
    // ============================================================

    private void BoardPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.White);

        DrawChessboard(g);
        DrawSelectedStart(g);
        DrawPathCells(g);
        DrawCandidates(g);
        DrawDeadEndFlash(g);
        DrawPathLines(g);
        DrawKnight(g);
    }

    private void DrawChessboard(Graphics g)
    {
        var clip = Rectangle.Round(g.VisibleClipBounds);
        for (int r = 0; r < _n; r++)
            for (int c = 0; c < _n; c++)
            {
                var rect = new Rectangle(c * _cellSize, r * _cellSize, _cellSize, _cellSize);
                if (!clip.IntersectsWith(rect)) continue;

                bool light = (r + c) % 2 == 0;
                using var b = new SolidBrush(light
                    ? Color.FromArgb(240, 217, 181)
                    : Color.FromArgb(181, 136, 99));
                g.FillRectangle(b, rect);
                g.DrawRectangle(Pens.Gray, rect);
            }
    }

    private void DrawSelectedStart(Graphics g)
    {
        if (_stepper != null) return;
        if (_startRow == null || _startCol == null) return;

        var rect = new Rectangle(_startCol.Value * _cellSize,
                                 _startRow.Value * _cellSize,
                                 _cellSize, _cellSize);
        using var b = new SolidBrush(_startColor);
        g.FillRectangle(b, rect);
        using var p = new Pen(Color.FromArgb(40, 140, 40), 2f);
        g.DrawRectangle(p, rect);
    }

    private void DrawPathCells(Graphics g)
    {
        if (_stepper == null) return;
        var clip = Rectangle.Round(g.VisibleClipBounds);
        var path = _stepper.Path;

        using var font = new Font("Segoe UI", Math.Max(6f, _cellSize * 0.28f), FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.FromArgb(40, 40, 60));

        for (int i = 0; i < path.Count; i++)
        {
            var (r, c) = path[i];
            var rect = new Rectangle(c * _cellSize, r * _cellSize, _cellSize, _cellSize);
            if (!clip.IntersectsWith(rect)) continue;

            Color fill = i == 0 ? _startColor : _visitedColor;
            using var b = new SolidBrush(Color.FromArgb(200, fill));
            g.FillRectangle(b, rect);

            string text = (i + 1).ToString();
            var sz = g.MeasureString(text, font);
            float tx = rect.X + (rect.Width - sz.Width) / 2;
            float ty = rect.Y + (rect.Height - sz.Height) / 2;
            g.DrawString(text, font, textBrush, tx, ty);
        }
    }

    private void DrawCandidates(Graphics g)
    {
        if (!chkShowCandidates.Checked) return;
        if (_stepper == null || _stepper.IsSolved) return;

        var cands = _stepper.CurrentCandidates;
        if (cands.Count == 0) return;

        var weights = _stepper.CurrentCandidateWeights;
        bool useW = chkWarnsdorff.Checked && weights.Count == cands.Count;

        int tried = _stepper.CurrentCandidateIndex;
        int next = tried + 1;

        float radius = Math.Max(7f, _cellSize * 0.24f);
        using var numFont = new Font("Segoe UI", Math.Max(5f, _cellSize * 0.18f), FontStyle.Regular);
        using var wFont = new Font("Segoe UI", Math.Max(6f, _cellSize * 0.22f), FontStyle.Bold);

        for (int i = 0; i < cands.Count; i++)
        {
            var (r, c) = cands[i];
            PointF center = CellCenter(r, c);

            bool isTried = i <= tried;
            bool isNext = i == next;

            Color fill;
            if (isNext) fill = _candNextFill;
            else if (isTried) fill = _candTriedFill;
            else fill = Color.FromArgb(60, Color.White);

            using var b = new SolidBrush(fill);
            g.FillEllipse(b, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            using var p = new Pen(isNext ? Color.FromArgb(40, 140, 40) : _candOutline,
                                  isNext ? 2f : 1f);
            g.DrawEllipse(p, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            if (isNext)
            {
                // двойная обводка для «следующего»
                using var p2 = new Pen(Color.FromArgb(40, 140, 40), 1f);
                g.DrawEllipse(p2, center.X - radius - 2, center.Y - radius - 2,
                              (radius + 2) * 2, (radius + 2) * 2);
            }

            // В центре — вес (или номер, если эвристика выключена)
            string centerText = useW ? weights[i].ToString() : (i + 1).ToString();
            var csz = g.MeasureString(centerText, wFont);
            using var tb = new SolidBrush(Color.FromArgb(20, 20, 20));
            g.DrawString(centerText, wFont, tb,
                center.X - csz.Width / 2f, center.Y - csz.Height / 2f);

            // Сбоку мелким — порядковый номер (только когда эвристика включена)
            if (useW)
            {
                string ord = (i + 1).ToString();
                var osz = g.MeasureString(ord, numFont);
                using var ordBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
                g.DrawString(ord, numFont, ordBrush,
                    center.X + radius + 2, center.Y + radius - osz.Height);
            }
        }
    }

    private void DrawDeadEndFlash(Graphics g)
    {
        if (_stepper?.LastBacktrackedFrom == null) return;
        var (r, c) = _stepper.LastBacktrackedFrom.Value;

        var rect = new Rectangle(c * _cellSize, r * _cellSize, _cellSize, _cellSize);
        using var b = new SolidBrush(Color.FromArgb(120, _deadEndColor));
        g.FillRectangle(b, rect);
        using var p = new Pen(_deadEndColor, 3f);
        g.DrawRectangle(p, rect);

        var cx = rect.X + rect.Width / 2f;
        var cy = rect.Y + rect.Height / 2f;
        float d = rect.Width * 0.25f;
        g.DrawLine(p, cx - d, cy - d, cx + d, cy + d);
        g.DrawLine(p, cx + d, cy - d, cx - d, cy + d);
    }

    private void DrawPathLines(Graphics g)
    {
        if (_stepper == null) return;
        var path = _stepper.Path;
        if (path.Count < 2) return;

        var clip = Rectangle.Round(g.VisibleClipBounds);
        using var pen = new Pen(_pathColor, Math.Max(2f, _cellSize * 0.06f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        for (int i = 0; i < path.Count - 1; i++)
        {
            PointF p1 = CellCenter(path[i].row, path[i].col);
            PointF p2 = CellCenter(path[i + 1].row, path[i + 1].col);

            var lr = Rectangle.FromLTRB(
                (int)Math.Min(p1.X, p2.X) - 3, (int)Math.Min(p1.Y, p2.Y) - 3,
                (int)Math.Max(p1.X, p2.X) + 3, (int)Math.Max(p1.Y, p2.Y) + 3);
            if (!clip.IntersectsWith(lr)) continue;
            g.DrawLine(pen, p1, p2);
        }
    }

    private void DrawKnight(Graphics g)
    {
        if (_startRow == null && _stepper == null) return;

        using var font = new Font("Segoe UI Symbol", _cellSize * 0.55f, FontStyle.Bold);
        const string k = "\u265E";
        var sz = g.MeasureString(k, font);
        float x = _knightPos.X - sz.Width / 2f;
        float y = _knightPos.Y - sz.Height / 2f;

        using var b = new SolidBrush(_knightColor);
        g.DrawString(k, font, b, x, y);
    }

    // ============================================================
    //                        АНИМАЦИЯ
    // ============================================================

    private void AnimateKnightTo(PointF target)
    {
        _animFrom = _knightPos;
        _animTo = target;
        _animElapsed = 0;
        _isAnimating = true;
        _animTimer.Start();
    }

    /// <summary>Прервать анимацию, если она идёт — конь мгновенно перескакивает в цель.</summary>
    private void CancelAnimation()
    {
        if (_animTimer.Enabled)
        {
            _animTimer.Stop();
            _knightPos = _animTo;
        }
        _isAnimating = false;
    }

    private void AnimTimer_Tick(object? sender, EventArgs e)
    {
        _animElapsed += _animTimer.Interval / 1000.0;
        double t = Math.Min(1.0, _animElapsed / _animDuration);
        double eased = EaseInOutCubic(t);

        var oldPos = _knightPos;
        _knightPos = new PointF(
            (float)(_animFrom.X + (_animTo.X - _animFrom.X) * eased),
            (float)(_animFrom.Y + (_animTo.Y - _animFrom.Y) * eased));

        int pad = _cellSize;
        var r1 = new Rectangle((int)oldPos.X - pad, (int)oldPos.Y - pad, pad * 2, pad * 2);
        var r2 = new Rectangle((int)_knightPos.X - pad, (int)_knightPos.Y - pad, pad * 2, pad * 2);
        boardPanel.Invalidate(Rectangle.Union(r1, r2));

        if (t >= 1.0)
        {
            _knightPos = _animTo;
            _isAnimating = false;
            _animTimer.Stop();
        }
    }

    private static double EaseInOutCubic(double t) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    // ============================================================
    //                       АВТОПРОИГРЫВАНИЕ
    // ============================================================

    private void BtnPlayPause_Click(object? sender, EventArgs e)
    {
        if (_stepper == null) return;

        _autoPlaying = !_autoPlaying;
        if (_autoPlaying)
        {
            btnPlayPause.Text = "⏸ Пауза";
            _autoTimer.Start();
        }
        else
        {
            btnPlayPause.Text = "▶ Авто";
            _autoTimer.Stop();
        }
    }

    private void AutoTimer_Tick(object? sender, EventArgs e)
    {
        if (_isAnimating) return;
        PerformStep();
    }

    /// <summary>
    /// Обновляет интервалы автоплея и длительность скольжения коня
    /// в зависимости от положения слайдера скорости.
    /// v=1 — самый медленный (≈2.5 с между шагами), v=10 — самый быстрый (≈30 мс).
    /// </summary>
    private void UpdateSpeed()
    {
        int v = tbSpeed.Value;                    // 1..10
        int interval = 30 + (10 - v) * 275;       // 30..2505 мс
        _autoTimer.Interval = interval;

        // Анимация занимает ~35% интервала, но не меньше 0.12 с и не больше 0.7 с
        _animDuration = Math.Max(0.12, Math.Min(0.7, interval / 1000.0 * 0.35));
    }

    // ============================================================
    //                       ШАГИ
    // ============================================================

    private void PerformStep()
    {
        if (_stepper == null || _stepper.IsSolved) return;

        var res = _stepper.StepForward();

        if (res == KnightTourStepper.StepResult.AlreadySolved) return;

        PointF target = CellCenter(_stepper.CurrentRow, _stepper.CurrentCol);

        if (res == KnightTourStepper.StepResult.SolutionFound)
        {
            CancelAnimation();
            _knightPos = target;
        }
        else
        {
            AnimateKnightTo(target);
        }

        boardPanel.Invalidate();
        UpdateStats();
        UpdateButtonsEnabled();

        LogStep(res);

        if (res == KnightTourStepper.StepResult.SolutionFound)
        {
            lblInfo.Text = $"Решение найдено! Ходов: {_stepper.Path.Count}";
            StopAutoPlay();
        }
        else if (res == KnightTourStepper.StepResult.NoSolution)
        {
            lblInfo.Text = "Все ветки перебраны — решения нет.";
            StopAutoPlay();
        }
        else
        {
            string action = res == KnightTourStepper.StepResult.Backtracked
                ? "откат" : "ход вперёд";
            lblInfo.Text = $"Глубина {_stepper.Depth}/{_n * _n} — {action}";
        }
    }

    private void StopAutoPlay()
    {
        _autoPlaying = false;
        _autoTimer.Stop();
        btnPlayPause.Text = "▶ Авто";
    }

    // ============================================================
    //                       ЖУРНАЛ
    // ============================================================

    private void LogStep(KnightTourStepper.StepResult res)
    {
        if (_stepper == null) return;
        _logStep++;

        string text;
        Color color;

        switch (res)
        {
            case KnightTourStepper.StepResult.Advanced:
                {
                    var (r, c) = (_stepper.CurrentRow, _stepper.CurrentCol);
                    string info = _stepper.LastDecisionInfo ?? "";
                    text = $"#{_logStep,-4} → {info}  | гл.{_stepper.Depth}/{_n * _n}";
                    color = Color.FromArgb(30, 100, 180);
                    break;
                }
            case KnightTourStepper.StepResult.Backtracked:
                {
                    var b = _stepper.LastBacktrackedFrom!.Value;
                    text = $"#{_logStep,-4} ↩ тупик ({b.row},{b.col}) → откат на " +
                           $"({_stepper.CurrentRow},{_stepper.CurrentCol})  гл.{_stepper.Depth}";
                    color = Color.FromArgb(200, 60, 60);
                    break;
                }
            case KnightTourStepper.StepResult.SolutionFound:
                text = $"#{_logStep,-4} РЕШЕНИЕ! Все {_n * _n} клеток пройдены.";
                color = Color.FromArgb(20, 140, 20);
                break;
            case KnightTourStepper.StepResult.NoSolution:
                text = $"#{_logStep,-4} Решения нет — все ветки перебраны.";
                color = Color.FromArgb(150, 0, 0);
                break;
            default:
                return;
        }

        AppendLog(text, color);
    }

    private void AppendLog(string text, Color color)
    {
        stepLog.SelectionStart = stepLog.TextLength;
        stepLog.SelectionLength = 0;
        stepLog.SelectionColor = color;
        stepLog.AppendText(text + "\n");
        stepLog.SelectionColor = stepLog.ForeColor;

        // Ограничиваем размер журнала
        if (stepLog.Lines.Length > 2000)
        {
            var keep = stepLog.Lines.Skip(stepLog.Lines.Length - 1000).ToArray();
            stepLog.Lines = keep;
        }

        stepLog.SelectionStart = stepLog.TextLength;
        stepLog.ScrollToCaret();
    }

    private void AppendLogNeutral(string text)
        => AppendLog(text, Color.FromArgb(90, 90, 90));

    // ============================================================
    //                       ОБРАБОТЧИКИ
    // ============================================================

    private void NumSize_ValueChanged(object? sender, EventArgs e)
    {
        if (_n == (int)numSize.Value) return;
        FullReset();
    }

    private void BoardPanel_MouseClick(object? sender, MouseEventArgs e)
    {
        CancelAnimation();
        StopAutoPlay();

        int col = e.X / _cellSize;
        int row = e.Y / _cellSize;
        if (row < 0 || row >= _n || col < 0 || col >= _n) return;

        if (_n % 2 == 1 && (row + col) % 2 == 1)
        {
            MessageBox.Show(
                "Для нечётного n не существует решения при старте на тёмных клетках",
                "Недопустимая клетка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _stepper = null;
        _startRow = row;
        _startCol = col;
        _knightPos = CellCenter(row, col);

        UpdateButtonsEnabled();
        UpdateStats();
        lblInfo.Text = $"Старт: ({row}, {col}) — нажмите «Запустить»";
        boardPanel.Invalidate();
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_startRow == null || _startCol == null)
        {
            MessageBox.Show("Сначала выберите стартовую клетку кликом по доске.",
                "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        CancelAnimation();
        StopAutoPlay();

        _n = (int)numSize.Value;
        RecalcCellSize();

        _stepper = new KnightTourStepper(_n, chkWarnsdorff.Checked);
        _stepper.Start(_startRow.Value, _startCol.Value);

        _knightPos = CellCenter(_startRow.Value, _startCol.Value);

        // Чистим журнал
        stepLog.Clear();
        _logStep = 1;
        AppendLog($"#{_logStep,-4} Старт в ({_startRow.Value},{_startCol.Value}). " +
                  $"Режим: {(chkWarnsdorff.Checked ? "с эвристикой" : "без эвристики")}",
                  Color.FromArgb(20, 140, 20));

        UpdateButtonsEnabled();
        UpdateStats();

        string hint = chkWarnsdorff.Checked
            ? "Готово. Эвристика включена."
            : "Готово. Эвристика выключена. " +
              "Поиск может идти долго.";
        lblInfo.Text = hint;
        boardPanel.Invalidate();
    }

    private void BtnStepForward_Click(object? sender, EventArgs e)
    {
        CancelAnimation();
        PerformStep();
    }

    private void BtnStepBack_Click(object? sender, EventArgs e)
    {
        CancelAnimation();
        StopAutoPlay();

        if (_stepper == null) return;
        if (!_stepper.StepBackward()) return;

        _knightPos = CellCenter(_stepper.CurrentRow, _stepper.CurrentCol);
        _isAnimating = false;
        _animTimer.Stop();

        _logStep++;
        AppendLog($"#{_logStep,-4} ↶ откат пользователем. Глубина {_stepper.Depth}/{_n * _n}",
                  Color.FromArgb(120, 120, 120));

        UpdateStats();
        UpdateButtonsEnabled();
        lblInfo.Text = $"Откат. Глубина {_stepper.Depth}/{_n * _n}";
        boardPanel.Invalidate();
    }

    private void BtnFinish_Click(object? sender, EventArgs e)
    {
        if (_stepper == null) return;

        CancelAnimation();
        StopAutoPlay();

        var sw = Stopwatch.StartNew();
        int guard = 0;

        while (!_stepper.IsSolved)
        {
            var r = _stepper.StepForward();
            if (r is KnightTourStepper.StepResult.NoSolution
                  or KnightTourStepper.StepResult.AlreadySolved) break;

            // Периодически отдаём управление UI, чтобы окно не «зависло»
            if (++guard % 20000 == 0)
                Application.DoEvents();
        }
        sw.Stop();

        _knightPos = CellCenter(_stepper.CurrentRow, _stepper.CurrentCol);
        _isAnimating = false;

        if (_stepper.IsSolved)
        {
            lblInfo.Text = $"Решено за {sw.ElapsedMilliseconds} мс. " +
                           $"Ходов: {_stepper.Path.Count}";
            _logStep++;
            AppendLog($"#{_logStep,-4} Решение за {sw.ElapsedMilliseconds} мс.",
                      Color.FromArgb(20, 140, 20));
        }
        else
        {
            lblInfo.Text = $"Решения нет (перебор завершён за {sw.ElapsedMilliseconds} мс).";
            _logStep++;
            AppendLog($"#{_logStep,-4} Перебор завершён: решения нет.",
                      Color.FromArgb(150, 0, 0));
        }

        UpdateStats();
        UpdateButtonsEnabled();
        boardPanel.Invalidate();
    }

    private void BtnReset_Click(object? sender, EventArgs e) => FullReset();

    private void FullReset()
    {
        CancelAnimation();
        StopAutoPlay();

        _n = (int)numSize.Value;
        RecalcCellSize();
        _stepper = null;
        _startRow = null;
        _startCol = null;
        _logStep = 0;

        stepLog.Clear();

        UpdateButtonsEnabled();
        UpdateStats();
        lblInfo.Text = "Выберите стартовую клетку";
        boardPanel.Invalidate();
    }

    // ============================================================
    //                       UI-СИНХРОНИЗАЦИЯ
    // ============================================================

    /// <summary>
    /// Кнопки больше НЕ блокируются во время анимации коня —
    /// любое нажатие мгновенно прерывает скольжение.
    /// </summary>
    private void UpdateButtonsEnabled()
    {
        bool has = _stepper != null;
        bool solved = has && _stepper!.IsSolved;

        btnStepBack.Enabled = has;
        btnStepForward.Enabled = has && !solved;
        btnPlayPause.Enabled = has && !solved;

        // «Закончить обход» = мгновенно домотать до конца.
        // Без эвристики это может занять часы (backtracking взрывается),
        // поэтому кнопка активна только при включённой эвристике.
        btnFinish.Enabled = has && !solved && chkWarnsdorff.Checked;
    }

    private void UpdateStats()
    {
        if (_stepper == null)
        {
            lblStats.Text = "Ходов: 0  Откатов: 0  Глубина: 0";
            return;
        }
        lblStats.Text =
            $"Ходов: {_stepper.Attempts,-10}" +
            $"Откатов: {_stepper.Backtracks,-10}" +
            $"Глубина: {_stepper.Depth}";
    }

    // ============================================================
    //                  АДАПТИВНАЯ РАСКЛАДКА
    // ============================================================

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (boardPanel == null || analysisPanel == null || stepLog == null) return;
        if (WindowState == FormWindowState.Minimized) return;

        int leftMargin = boardPanel.Left;
        int topY = boardPanel.Top;
        int logWidth = 260;
        int gap = 12;

        int boardAreaWidth = ClientSize.Width - analysisPanel.Width;
        int availW = boardAreaWidth - leftMargin - gap - logWidth - gap;
        int availH = ClientSize.Height - topY - 20;
        int size = Math.Min(availW, availH);
        if (size < 250) size = 250;

        if (boardPanel.Width != size)
        {
            boardPanel.Size = new Size(size, size);
            RecalcCellSize();
        }

        stepLog.Location = new Point(boardPanel.Right + gap, boardPanel.Top);
        stepLog.Size = new Size(logWidth, boardPanel.Height);
        lblLogHeader.Location = new Point(stepLog.Left, stepLog.Top - 18);

        boardPanel.Invalidate();
        chartPanel?.Invalidate();
    }

    // ============================================================
    //                 АНАЛИТИЧЕСКИЙ ЭКСПЕРИМЕНТ
    // ============================================================

    private async void BtnRunExperiment_Click(object? sender, EventArgs e)
    {
        if (_experimentRunning) return;
        _experimentRunning = true;
        btnRunExperiment.Enabled = false;

        int maxN = (int)numMaxN.Value;
        _results.Clear();
        resultLog.Clear();
        chartPanel.Invalidate();

        progressBar.Minimum = 0;
        progressBar.Maximum = maxN - 4;
        progressBar.Value = 0;

        for (int n = 5; n <= maxN; n++)
        {
            lblExperimentStatus.Text = $"Эксперимент n = {n}…";
            Application.DoEvents();

            var r = await Task.Run(() => RunExperimentForN(n));
            _results.Add(r);

            AppendExperimentResult(r);

            progressBar.Value = n - 4;
            chartPanel.Invalidate();
        }

        lblExperimentStatus.Text = $"Готово. Выполнено {_results.Count} экспериментов.";
        btnRunExperiment.Enabled = true;
        _experimentRunning = false;
    }

    private ExperimentResult RunExperimentForN(int n)
    {
        // Прогрев JIT
        KnightTourStepper.Solve(n, 0, 0, useWarnsdorff: true, abortOnBacktrack: true);

        // === Автоподбор подходящего старта ===
        (int r, int c)? good = null;

        foreach (var (r, c) in StartCandidates(n))
        {
            var probe = KnightTourStepper.Solve(n, r, c,
                useWarnsdorff: true, abortOnBacktrack: true);

            if (probe.solved && probe.backtracks == 0)
            {
                good = (r, c);
                break;
            }
        }

        if (good == null)
        {
            return new ExperimentResult { N = n, Skip = true, ElapsedMs = 0 };
        }

        // === Замер на валидном старте ===
        int runs = n <= 20 ? 50 : 1;
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < runs; i++)
            KnightTourStepper.Solve(n, good.Value.r, good.Value.c,
                useWarnsdorff: true, abortOnBacktrack: true);

        sw.Stop();

        return new ExperimentResult
        {
            N = n,
            Skip = false,
            ElapsedMs = sw.Elapsed.TotalMilliseconds / runs
        };
    }

    /// <summary>
    /// Возвращает последовательность кандидатов стартовых клеток.
    /// Сначала углы, потом их окрестности, потом диагонали.
    /// На нечётной доске тёмные клетки автоматически пропускаются.
    /// </summary>
    private static IEnumerable<(int r, int c)> StartCandidates(int n)
    {
        int last = n - 1;
        int mid = n / 2;

        // Углы
        var corners = new (int, int)[]
        {
            (0, 0), (0, last), (last, 0), (last, last),
            // Рядом с углами
            (0, 1), (1, 0), (1, 1),
            (0, last - 1), (last - 1, 0), (last - 1, last - 1),
            (1, last), (last, 1),
            // Ещё чуть дальше
            (0, 2), (2, 0), (2, 2),
            (0, last - 2), (last - 2, 0),
            (last - 2, last - 2), (2, last), (last, 2),
            // Центральные диагонали
            (mid, mid), (mid, mid + 1), (mid + 1, mid),
            (1, 2), (2, 1), (last - 2, last - 1), (last - 1, last - 2)
        };

        var seen = new HashSet<(int, int)>();
        foreach (var (r, c) in corners)
        {
            if (r < 0 || r >= n || c < 0 || c >= n) continue;
            if (n % 2 == 1 && (r + c) % 2 == 1) continue;  // тёмная клетка
            if (seen.Add((r, c))) yield return (r, c);
        }
    }

    private void AppendExperimentResult(ExperimentResult r)
    {
        if (resultLog.TextLength == 0)
        {
            resultLog.AppendText($"{"n",-6}{"время, мс",-14}\r\n");
            resultLog.AppendText(new string('-', 28) + "\r\n");
        }

        string timeText = r.Skip ? "—" : r.ElapsedMs.ToString("F3");
        resultLog.AppendText($"{r.N,-6}{timeText,-14}\r\n");

        resultLog.SelectionStart = resultLog.TextLength;
        resultLog.ScrollToCaret();
    }

    // ============================================================
    //                     ГРАФИК РЕЗУЛЬТАТОВ
    // ============================================================

    private void ChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.White);

        var rect = chartPanel.ClientRectangle;
        int leftM = 90, rightM = 30, topM = 20, bottomM = 70;
        var plot = new Rectangle(
            leftM, topM,
            Math.Max(10, rect.Width - leftM - rightM),
            Math.Max(10, rect.Height - topM - bottomM));

        using var axisFont = new Font("Segoe UI", 8F);
        using var axisBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        using var gridPen = new Pen(Color.FromArgb(232, 232, 232));

        g.DrawRectangle(Pens.Gray, plot);

        // Только валидные точки (без откатов) — остальные полностью игнорируем
        var valid = _results.Where(x => !x.Skip).OrderBy(x => x.N).ToList();

        if (valid.Count == 0)
        {
            using var f = new Font("Segoe UI", 10F);
            using var b = new SolidBrush(Color.Gray);
            string msg = _results.Count == 0
                ? "Нажмите «Запустить серию»"
                : "Нет точек без откатов — все n пропущены";
            var sz = g.MeasureString(msg, f);
            g.DrawString(msg, f, b,
                plot.X + (plot.Width - sz.Width) / 2,
                plot.Y + (plot.Height - sz.Height) / 2);
            return;
        }

        int minN = valid.Min(x => x.N);
        int maxN = valid.Max(x => x.N);

        // --- ось Y ---
        double maxMs = valid.Select(x => x.ElapsedMs).DefaultIfEmpty(1).Max();
        if (maxMs <= 0) maxMs = 1;

        string unit = "мс";
        double divider = 1.0;
        if (maxMs >= 3000) { unit = "с"; divider = 1000.0; }

        double maxScaled = maxMs / divider;
        double yStep = NiceStep(maxScaled, 6);
        double yMax = Math.Ceiling(maxScaled / yStep) * yStep;
        if (yMax <= 0) yMax = yStep;

        // --- ось X ---
        int xRange = maxN - minN;
        int xStep = NiceIntStep(xRange, 10);

        float PlotX(int n) =>
            plot.Left + (n - minN) / (float)Math.Max(1, maxN - minN) * plot.Width;

        float PlotY(double v) =>
            plot.Bottom - (float)(v / yMax) * plot.Height;

        // --- сетка и подписи Y ---
        for (double yv = 0; yv <= yMax + 1e-9; yv += yStep)
        {
            float yp = PlotY(yv);
            g.DrawLine(gridPen, plot.Left, yp, plot.Right, yp);
            string lbl = yv.ToString("0.##") + " " + unit;
            var sz = g.MeasureString(lbl, axisFont);
            g.DrawString(lbl, axisFont, axisBrush,
                plot.Left - sz.Width - 6, yp - sz.Height / 2);
        }

        // --- сетка и подписи X ---
        for (int nv = minN; nv <= maxN; nv += xStep)
        {
            float xp = PlotX(nv);
            g.DrawLine(gridPen, xp, plot.Top, xp, plot.Bottom);
            string lbl = nv.ToString();
            var sz = g.MeasureString(lbl, axisFont);
            g.DrawString(lbl, axisFont, axisBrush,
                xp - sz.Width / 2, plot.Bottom + 4);
        }
        if ((maxN - minN) % xStep != 0)
        {
            float xp = PlotX(maxN);
            string lbl = maxN.ToString();
            var sz = g.MeasureString(lbl, axisFont);
            g.DrawString(lbl, axisFont, axisBrush,
                xp - sz.Width / 2, plot.Bottom + 4);
        }

        // --- линия ---
        if (valid.Count >= 2)
        {
            using var pen = new Pen(Color.FromArgb(40, 100, 200), 1.6f);
            var pts = valid.Select(x => new PointF(
                PlotX(x.N), PlotY(x.ElapsedMs / divider))).ToArray();
            g.DrawLines(pen, pts);
        }

        // --- точки ---
        using var okBrush = new SolidBrush(Color.FromArgb(40, 100, 200));
        foreach (var r in valid)
        {
            float xp = PlotX(r.N);
            float yp = PlotY(r.ElapsedMs / divider);
            g.FillEllipse(okBrush, xp - 2.6f, yp - 2.6f, 5.2f, 5.2f);
        }

        // --- аппроксимация (квадратичный тренд) ---
        if (chkShowApprox.Checked && valid.Count >= 3)
        {
            var fit = FitQuadratic(valid);
            if (fit.HasValue)
            {
                using var fitPen = new Pen(Color.FromArgb(230, 120, 40), 2f)
                {
                    DashStyle = DashStyle.Dash
                };

                // Локальная функция для плавной отрисовки по X (float, без округления)
                float PlotXf(float nVal) =>
                    plot.Left + (nVal - minN) / (float)Math.Max(1, maxN - minN) * plot.Width;

                var curvePts = new List<PointF>();
                int steps = 150; // чем больше, тем плавнее

                for (int i = 0; i <= steps; i++)
                {
                    float xVal = minN + (maxN - minN) * (i / (float)steps);
                    double yVal = fit.Value.A * xVal * xVal + fit.Value.B * xVal + fit.Value.C;

                    // Не уходим ниже нуля
                    if (yVal < 0) yVal = 0;

                    curvePts.Add(new PointF(PlotXf(xVal), PlotY(yVal / divider)));
                }

                if (curvePts.Count > 1)
                    g.DrawLines(fitPen, curvePts.ToArray());

                // Вывод уравнения и R²
                using var fitFont = new Font("Consolas", 8F);
                using var fitBrush = new SolidBrush(Color.FromArgb(200, 100, 20));

                string eq = $"y = {fit.Value.A:E2}·n² + {fit.Value.B:E2}·n + {fit.Value.C:E2}";
                string r2str = $"R² = {fit.Value.R2:F4}";

                g.DrawString(eq, fitFont, fitBrush, plot.Left + 8, plot.Top + 6);
                g.DrawString(r2str, fitFont, fitBrush, plot.Left + 8, plot.Top + 18);
            }

        }


        // --- подписи осей ---
        using var titleFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        g.DrawString("n", titleFont, axisBrush, plot.Right - 6, plot.Bottom + 24);

        string yTitle = $"Время, {unit}";
        var ysz = g.MeasureString(yTitle, titleFont);
        var state = g.Save();
        g.TranslateTransform(14, plot.Top + plot.Height / 2f + ysz.Width / 2f);
        g.RotateTransform(-90);
        g.DrawString(yTitle, titleFont, axisBrush, 0, 0);
        g.Restore(state);

        // --- Подписи под графиком (Легенда) ---
        using var legendFont = new Font("Segoe UI", 8.5F);
        float ly = plot.Bottom + 44; // 44 пикселя от низа графика

        // 1. Результаты эксперимента
        float lx1 = plot.Left + plot.Width * 0.12f;
        g.FillEllipse(okBrush, lx1, ly + 2, 8, 8);
        g.DrawString("Результаты эксперимента", legendFont, axisBrush, lx1 + 14, ly - 1);

        // 2. Аппроксимация
        float lx2 = plot.Left + plot.Width * 0.52f;
        using var fitLegendPen = new Pen(Color.FromArgb(170, 230, 80, 0), 3f)
        {
            DashStyle = DashStyle.Dash
        };
        g.DrawLine(fitLegendPen, lx2, ly + 6, lx2 + 24, ly + 6);
        g.DrawString("Квадратичная аппроксимация", legendFont, axisBrush, lx2 + 30, ly - 1);
    }

    private static double NiceStep(double range, int targetTicks)
    {
        if (range <= 0 || targetTicks <= 0) return 1;
        double raw = range / targetTicks;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag;
        double nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
        return nice * mag;
    }

    private static int NiceIntStep(int range, int targetTicks)
    {
        if (range <= 0) return 1;
        int raw = Math.Max(1, range / targetTicks);
        int[] steps = { 1, 2, 5, 10, 20, 25, 50, 100, 200, 500, 1000 };
        foreach (var s in steps) if (raw <= s) return s;
        return 2000;
    }

    /// <summary>
    /// Строит квадратичный тренд y = A*n^2 + B*n + C методом наименьших квадратов.
    /// Возвращает коэффициенты и R². Null — если точек мало или система вырождена.
    /// </summary>
    private (double A, double B, double C, double R2)? FitQuadratic(List<ExperimentResult> pts)
    {
        int n = pts.Count;
        if (n < 3) return null;

        double sumX = 0, sumX2 = 0, sumX3 = 0, sumX4 = 0;
        double sumY = 0, sumXY = 0, sumX2Y = 0;

        foreach (var p in pts)
        {
            double x = p.N;
            double y = p.ElapsedMs;
            double x2 = x * x;

            sumX += x;
            sumX2 += x2;
            sumX3 += x2 * x;
            sumX4 += x2 * x2;

            sumY += y;
            sumXY += x * y;
            sumX2Y += x2 * y;
        }

        // Система нормальных уравнений для y = A*n^2 + B*n + C
        // A*sum(n^4) + B*sum(n^3) + C*sum(n^2) = sum(y*n^2)
        // A*sum(n^3) + B*sum(n^2) + C*sum(n)   = sum(y*n)
        // A*sum(n^2) + B*sum(n)   + C*N        = sum(y)
        double[,] M = new double[3, 4]
        {
            { sumX4, sumX3, sumX2, sumX2Y }, // строка для A
            { sumX3, sumX2, sumX,  sumXY  }, // строка для B
            { sumX2, sumX,  n,     sumY   }  // строка для C
        };

        // Прямой ход метода Гаусса
        for (int i = 0; i < 3; i++)
        {
            int maxRow = i;
            for (int k = i + 1; k < 3; k++)
                if (Math.Abs(M[k, i]) > Math.Abs(M[maxRow, i]))
                    maxRow = k;

            for (int k = i; k < 4; k++)
            {
                double tmp = M[maxRow, k];
                M[maxRow, k] = M[i, k];
                M[i, k] = tmp;
            }

            if (Math.Abs(M[i, i]) < 1e-12) return null; // вырожденная матрица

            for (int k = i + 1; k < 3; k++)
            {
                double factor = M[k, i] / M[i, i];
                for (int j = i; j < 4; j++)
                    M[k, j] -= factor * M[i, j];
            }
        }

        // Обратный ход
        double C = M[2, 3] / M[2, 2];
        double B = (M[1, 3] - M[1, 2] * C) / M[1, 1];
        double A = (M[0, 3] - M[0, 1] * B - M[0, 2] * C) / M[0, 0];

        // Вычисление R²
        double meanY = sumY / n;
        double ssTot = 0, ssRes = 0;
        foreach (var p in pts)
        {
            double pred = A * p.N * p.N + B * p.N + C;
            ssRes += Math.Pow(p.ElapsedMs - pred, 2);
            ssTot += Math.Pow(p.ElapsedMs - meanY, 2);
        }

        double r2 = ssTot > 0 ? 1 - ssRes / ssTot : 1;

        return (A, B, C, r2);
    }

}