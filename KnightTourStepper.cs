namespace KnightTourApp;

public class KnightTourStepper
{
    public enum StepResult
    {
        Advanced,
        Backtracked,
        SolutionFound,
        NoSolution,
        AlreadySolved
    }

    private readonly int _n;
    private readonly bool _useWarnsdorff;
    private int[,] _board;
    private List<(int row, int col)> _path = new();
    private List<List<(int row, int col)>> _candidates = new();
    private List<int> _candidateIdx = new();

    private readonly List<List<int>> _candidateWeights = new();

    /// <summary>
    /// Веса (число дальнейших ходов) для текущих кандидатов.
    /// Заполняется, только если useWarnsdorff = true. Иначе элементы = -1.
    /// </summary>
    public IReadOnlyList<int> CurrentCandidateWeights =>
        _candidateWeights.Count > 0 ? _candidateWeights[^1] : Array.Empty<int>();

    /// <summary>
    /// Человекочитаемое объяснение последнего выбора хода.
    /// null до первого хода.
    /// </summary>
    public string? LastDecisionInfo { get; private set; }

    // Источник случайности для режима без эвристики.
    // Random.Shared — потокобезопасный синглтон, доступен с .NET 6+.
    private readonly Random _rng = Random.Shared;

    private readonly Stack<UndoState> _undo = new();

    private static readonly int[] DR = { 2, 1, -1, -2, -2, -1, 1, 2 };
    private static readonly int[] DC = { 1, 2, 2, 1, -1, -2, -2, -1 };

    public int N => _n;
    public int CurrentRow { get; private set; }
    public int CurrentCol { get; private set; }
    public int Depth => _path.Count;
    public long Attempts { get; private set; }
    public long Backtracks { get; private set; }
    public bool IsSolved { get; private set; }
    public (int row, int col)? LastBacktrackedFrom { get; private set; }
    public (int row, int col)? LastMovedTo { get; private set; }
    public IReadOnlyList<(int row, int col)> Path => _path;
    public IReadOnlyList<(int row, int col)> CurrentCandidates =>
        _candidates.Count > 0 ? _candidates[^1] : Array.Empty<(int, int)>();
    public int CurrentCandidateIndex =>
        _candidateIdx.Count > 0 ? _candidateIdx[^1] : -1;

    /// <summary>
    /// Облегчённый статический solver для аналитических экспериментов:
    /// без undo-истории и без пошаговых событий, только результат и статистика.
    /// Возвращает (solved, attempts, backtracks, timedOut).
    /// </summary>
    public static (bool solved, long attempts, long backtracks) Solve(
    int n, int startRow, int startCol, bool useWarnsdorff, bool abortOnBacktrack = false)
    {
        int total = n * n;
        var board = new int[n, n];
        var pathR = new int[total];
        var pathC = new int[total];
        var cands = new List<(int r, int c)>[total];
        var idxs = new int[total];

        long attempts = 0, backtracks = 0;

        pathR[0] = startRow;
        pathC[0] = startCol;
        board[startRow, startCol] = 1;
        cands[0] = BuildCandidatesStatic(board, n, startRow, startCol, useWarnsdorff);
        idxs[0] = -1;

        int depth = 1;

        while (depth > 0)
        {
            if (depth == total)
                return (true, attempts, backtracks);

            int d = depth - 1;
            int ni = idxs[d] + 1;
            bool placed = false;

            while (ni < cands[d].Count)
            {
                attempts++;
                var (nr, nc) = cands[d][ni];
                if (board[nr, nc] == 0)
                {
                    idxs[d] = ni;
                    board[nr, nc] = depth + 1;
                    pathR[depth] = nr;
                    pathC[depth] = nc;
                    depth++;
                    cands[depth - 1] = BuildCandidatesStatic(board, n, nr, nc, useWarnsdorff);
                    idxs[depth - 1] = -1;
                    placed = true;
                    break;
                }
                ni++;
            }

            if (!placed)
            {
                board[pathR[depth - 1], pathC[depth - 1]] = 0;
                depth--;
                backtracks++;

                // Экспериментальный режим: как только произошёл первый откат —
                // прерываем поиск. Это защищает от экспоненциального взрыва
                // на «плохих» n, где Warnsdorff промахивается.
                if (abortOnBacktrack)
                    return (false, attempts, backtracks);
            }
        }

        return (false, attempts, backtracks);
    }

    private static List<(int r, int c)> BuildCandidatesStatic(
        int[,] board, int n, int r, int c, bool useWarnsdorff)
    {
        var raw = new List<(int r, int c)>(8);
        for (int i = 0; i < 8; i++)
        {
            int nr = r + DR[i];
            int nc = c + DC[i];
            if (nr >= 0 && nr < n && nc >= 0 && nc < n && board[nr, nc] == 0)
                raw.Add((nr, nc));
        }

        if (useWarnsdorff)
        {
            raw.Sort((a, b) =>
            {
                int ca = CountOnwardStatic(board, n, a.r, a.c);
                int cb = CountOnwardStatic(board, n, b.r, b.c);
                if (ca != cb) return ca.CompareTo(cb);
                if (a.r != b.r) return a.r.CompareTo(b.r);
                return a.c.CompareTo(b.c);
            });
        }
        return raw;
    }

    private static int CountOnwardStatic(int[,] board, int n, int r, int c)
    {
        int cnt = 0;
        for (int i = 0; i < 8; i++)
        {
            int nr = r + DR[i];
            int nc = c + DC[i];
            if (nr >= 0 && nr < n && nc >= 0 && nc < n && board[nr, nc] == 0)
                cnt++;
        }
        return cnt;
    }

    public KnightTourStepper(int n, bool useWarnsdorff)
    {
        _n = n;
        _useWarnsdorff = useWarnsdorff;
        _board = new int[n, n];
    }

    public void Start(int row, int col)
    {
        _board = new int[_n, _n];
        _path = new List<(int, int)> { (row, col) };
        _candidates = new List<List<(int, int)>>();
        _candidateIdx = new List<int>();
        _candidateWeights.Clear();
        _undo.Clear();
        LastDecisionInfo = null;

        _board[row, col] = 1;
        CurrentRow = row;
        CurrentCol = col;
        Attempts = 0;
        Backtracks = 0;
        IsSolved = false;
        LastBacktrackedFrom = null;
        LastMovedTo = null;

        EnsureCurrentCandidates();
    }

    public StepResult StepForward()
    {
        if (IsSolved) return StepResult.AlreadySolved;

        if (Depth == _n * _n)
        {
            IsSolved = true;
            return StepResult.SolutionFound;
        }

        PushUndo();

        var candidates = _candidates[^1];
        int idx = _candidateIdx[^1] + 1;

        while (idx < candidates.Count)
        {
            var (nr, nc) = candidates[idx];
            _candidateIdx[^1] = idx;
            Attempts++;

            if (_board[nr, nc] == 0)
            {
                // Собираем объяснение выбора ещё ДО изменения состояния
                LastDecisionInfo = BuildDecisionInfo(candidates, idx, nr, nc);

                _board[nr, nc] = Depth + 1;
                _path.Add((nr, nc));
                CurrentRow = nr;
                CurrentCol = nc;
                LastMovedTo = (nr, nc);
                LastBacktrackedFrom = null;

                if (Depth == _n * _n)
                {
                    IsSolved = true;
                    return StepResult.SolutionFound;
                }

                EnsureCurrentCandidates();
                return StepResult.Advanced;
            }
            idx++;
        }

        return DoBacktrack();
    }

    public bool StepBackward()
    {
        if (_undo.Count == 0) return false;
        RestoreUndo(_undo.Pop());
        return true;
    }

    private StepResult DoBacktrack()
    {
        var removed = _path[^1];
        _path.RemoveAt(_path.Count - 1);
        _board[removed.row, removed.col] = 0;
        CurrentRow = _path[^1].row;
        CurrentCol = _path[^1].col;

        _candidates.RemoveAt(_candidates.Count - 1);
        _candidateIdx.RemoveAt(_candidateIdx.Count - 1);

        _candidateWeights.RemoveAt(_candidateWeights.Count - 1);
        LastDecisionInfo = null;

        Backtracks++;
        LastBacktrackedFrom = removed;
        LastMovedTo = null;

        if (Depth == 1 &&
            _candidateIdx[^1] >= _candidates[^1].Count - 1)
        {
            return StepResult.NoSolution;
        }

        return StepResult.Backtracked;
    }

    private void EnsureCurrentCandidates()
    {
        if (_candidates.Count >= Depth) return;

        _candidates.Add(new List<(int, int)>());
        _candidateIdx.Add(-1);
        _candidateWeights.Add(new List<int>());

        var raw = new List<(int row, int col)>(8);
        for (int i = 0; i < 8; i++)
        {
            int nr = CurrentRow + DR[i];
            int nc = CurrentCol + DC[i];
            if (nr >= 0 && nr < _n && nc >= 0 && nc < _n && _board[nr, nc] == 0)
                raw.Add((nr, nc));
        }

        if (_useWarnsdorff)
        {
            // Считаем вес каждого кандидата и сортируем по возрастанию.
            var withWeight = raw
                .Select(m => (m, w: CountOnward(m.row, m.col)))
                .OrderBy(x => x.w)
                .ThenBy(x => x.m.row)
                .ThenBy(x => x.m.col)
                .ToList();

            _candidates[^1] = withWeight.Select(x => x.m).ToList();
            _candidateWeights[^1] = withWeight.Select(x => x.w).ToList();
        }
        else
        {
            _candidates[^1] = raw;
            _candidateWeights[^1] = raw.Select(_ => -1).ToList();
        }
        _candidateIdx[^1] = -1;
    }

    /// <summary>
    /// Формирует текстовое объяснение выбора хода для журнала.
    /// </summary>
    private string BuildDecisionInfo(
        List<(int row, int col)> candidates, int chosenIdx, int nr, int nc)
    {
        if (!_useWarnsdorff)
            return $"Из ({CurrentRow},{CurrentCol}) → ({nr},{nc}) " +
                   $"(случайный из {candidates.Count})";

        var weights = _candidateWeights[^1];
        var parts = new List<string>(candidates.Count);
        for (int k = 0; k < candidates.Count; k++)
        {
            string tag = k == chosenIdx ? "*" : "";
            parts.Add($"({candidates[k].row},{candidates[k].col})w={weights[k]}{tag}");
        }

        int w = weights[chosenIdx];
        return $"({CurrentRow},{CurrentCol}) → ({nr},{nc}) вес={w} (мин.) | " +
               $"кандидаты: {string.Join(" ", parts)}";
    }

    /// <summary>
    /// Перемешивание Фишера–Йетса. Работает in-place, O(n).
    /// </summary>
    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private int CountOnward(int row, int col)
    {
        int c = 0;
        for (int i = 0; i < 8; i++)
        {
            int nr = row + DR[i];
            int nc = col + DC[i];
            if (nr >= 0 && nr < _n && nc >= 0 && nc < _n && _board[nr, nc] == 0)
                c++;
        }
        return c;
    }

    // ============ История состояний ============
    private void PushUndo()
    {
        _undo.Push(new UndoState
        {
            Board = (int[,])_board.Clone(),
            Path = new List<(int, int)>(_path),
            Candidates = _candidates.Select(l => new List<(int, int)>(l)).ToList(),
            CandidateIdx = new List<int>(_candidateIdx),
            CandidateWeights = _candidateWeights.Select(l => new List<int>(l)).ToList(),
            LastDecisionInfo = LastDecisionInfo,
            CurrentRow = CurrentRow,
            CurrentCol = CurrentCol,
            IsSolved = IsSolved,
            LastBacktrackedFrom = LastBacktrackedFrom,
            LastMovedTo = LastMovedTo,
            Attempts = Attempts,
            Backtracks = Backtracks
        });
    }

    private void RestoreUndo(UndoState s)
    {
        _board = s.Board;
        _path = s.Path;
        _candidates = s.Candidates;
        _candidateIdx = s.CandidateIdx;
        _candidateWeights.Clear();
        _candidateWeights.AddRange(s.CandidateWeights.Select(l => new List<int>(l)));
        LastDecisionInfo = s.LastDecisionInfo;
        CurrentRow = s.CurrentRow;
        CurrentCol = s.CurrentCol;
        IsSolved = s.IsSolved;
        LastBacktrackedFrom = s.LastBacktrackedFrom;
        LastMovedTo = s.LastMovedTo;
        Attempts = s.Attempts;
        Backtracks = s.Backtracks;
    }

    private class UndoState
    {
        public int[,] Board = null!;
        public List<(int, int)> Path = null!;
        public List<List<(int, int)>> Candidates = null!;
        public List<int> CandidateIdx = null!;
        public int CurrentRow, CurrentCol;
        public bool IsSolved;
        public (int, int)? LastBacktrackedFrom;
        public (int, int)? LastMovedTo;
        public long Attempts, Backtracks;
        public List<List<int>> CandidateWeights = null!;
        public string? LastDecisionInfo;
    }
}