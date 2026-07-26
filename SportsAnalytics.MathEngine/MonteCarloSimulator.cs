namespace SportsAnalytics.MathEngine;

/// <summary>
/// محرك Monte Carlo — يولّد آلاف المباريات الافتراضية من توزيع بواسون
/// لإنتاج توزيع احتمالات كامل (كل نتيجة ممكنة).
///
/// الخوارزمية:
/// 1. سحب عينات عشوائية من Poisson(λHome) وPoisson(λAway)
/// 2. حساب نسبة كل نتيجة (goals_h : goals_a)
/// 3. مراقبة الخطأ المعياري — نتوقف عند الاستقرار أو بلوغ الحد الأقصى
/// </summary>
/// <summary>
/// يمثل نتيجة مباراة واحدة تم محاكاتها.
/// </summary>
public class MatchSimulationResult
{
    public int HomeGoalsFH { get; set; }
    public int AwayGoalsFH { get; set; }
    public int HomeGoalsSH { get; set; }
    public int AwayGoalsSH { get; set; }
    
    public int HomeGoalsFT => HomeGoalsFH + HomeGoalsSH;
    public int AwayGoalsFT => AwayGoalsFH + AwayGoalsSH;
}

public class MonteCarloSimulator
{
    private readonly Random _rng = new(42); // Seed ثابت للاستنساخ

    // إعدادات افتراضية
    public int MinIterations { get; set; } = 5_000;
    public int MaxIterations { get; set; } = 20_000;
    public double TargetStdError { get; set; } = 0.003; // 0.3% — هدف الاستقرار
    public int MaxGoals { get; set; } = 10;            // أقصى أهداف تُسجَّل في جدول النتائج

    /// <summary>
    /// يُشغّل المحاكاة ويُرجع <see cref="SimulationResult"/> كاملاً.
    /// </summary>
    public SimulationResult Simulate(double lambdaHome, double lambdaAway)
    {
        if (lambdaHome <= 0 || lambdaAway <= 0)
            throw new ArgumentException("λ يجب أن يكون > 0.");

        // جدول النتائج: [goals_home, goals_away] → عدد مرات الحدوث
        var scoreGrid = new int[MaxGoals + 1, MaxGoals + 1];
        int homeWins = 0, draws = 0, awayWins = 0;

        // قائمة لحفظ كل المباريات المحاكية بالتفصيل (لصالح أسواق المراهنات)
        var rawSimulations = new List<MatchSimulationResult>(MaxIterations);

        // ── مرحلة 1: تشغيل الحد الأدنى ──
        for (int i = 0; i < MinIterations; i++)
            RunOneSample(lambdaHome, lambdaAway, scoreGrid, ref homeWins, ref draws, ref awayWins, rawSimulations);

        int totalSamples = MinIterations;

        // ── مرحلة 2: مراقبة الخطأ المعياري ديناميكياً ──
        const int batchSize = 1000;
        while (totalSamples < MaxIterations)
        {
            for (int i = 0; i < batchSize; i++)
                RunOneSample(lambdaHome, lambdaAway, scoreGrid, ref homeWins, ref draws, ref awayWins, rawSimulations);
            totalSamples += batchSize;

            // حساب الخطأ المعياري لاحتمال الفوز المنزلي (الأكثر تغيراً)
            double pHome = (double)homeWins / totalSamples;
            double stdErr = Math.Sqrt(pHome * (1 - pHome) / totalSamples);
            if (stdErr <= TargetStdError) break; // استقرار ✅
        }

        // ── بناء النتيجة ──
        double n = totalSamples;
        var probGrid = new double[MaxGoals + 1, MaxGoals + 1];
        for (int h = 0; h <= MaxGoals; h++)
            for (int a = 0; a <= MaxGoals; a++)
                probGrid[h, a] = scoreGrid[h, a] / n;

        // أعلى 10 نتائج محتملة
        var topScores = new List<(int Home, int Away, double Prob)>();
        for (int h = 0; h <= MaxGoals; h++)
            for (int a = 0; a <= MaxGoals; a++)
                topScores.Add((h, a, probGrid[h, a]));

        return new SimulationResult(
            HomeWinProbability: homeWins / n,
            DrawProbability: draws / n,
            AwayWinProbability: awayWins / n,
            ScoreProbabilityGrid: probGrid,
            TopScores: topScores.OrderByDescending(s => s.Prob).Take(10).ToList(),
            TotalIterations: totalSamples,
            StandardError: Math.Sqrt((homeWins / n) * (1 - homeWins / n) / n),
            LambdaHome: lambdaHome,
            LambdaAway: lambdaAway,
            RawSimulations: rawSimulations);
    }

    // ── دوال مساعدة ──

    private void RunOneSample(
        double lambdaHome, double lambdaAway,
        int[,] grid, ref int hw, ref int d, ref int aw, List<MatchSimulationResult> rawSimulations)
    {
        // تقسيم بسيط للأهداف بين الشوطين: الشوط الأول عادة أهدافه أقل بقليل (نفترض 45% للأول و 55% للثاني كمعدل)
        // لتجنب التعقيد الرياضي، سنقسم Lambda بنسبة 0.45 و 0.55
        int homeGoalsFH = SamplePoisson(lambdaHome * 0.45);
        int awayGoalsFH = SamplePoisson(lambdaAway * 0.45);
        int homeGoalsSH = SamplePoisson(lambdaHome * 0.55);
        int awayGoalsSH = SamplePoisson(lambdaAway * 0.55);

        var matchResult = new MatchSimulationResult
        {
            HomeGoalsFH = homeGoalsFH,
            AwayGoalsFH = awayGoalsFH,
            HomeGoalsSH = homeGoalsSH,
            AwayGoalsSH = awayGoalsSH
        };

        rawSimulations.Add(matchResult);

        int goalsHome = matchResult.HomeGoalsFT;
        int goalsAway = matchResult.AwayGoalsFT;

        int gh = Math.Min(goalsHome, MaxGoals);
        int ga = Math.Min(goalsAway, MaxGoals);
        grid[gh, ga]++;

        if (goalsHome > goalsAway) hw++;
        else if (goalsHome == goalsAway) d++;
        else aw++;
    }

    /// <summary>
    /// توليد عدد عشوائي من توزيع Poisson(λ) باستخدام خوارزمية Knuth.
    /// </summary>
    private int SamplePoisson(double lambda)
    {
        // خوارزمية Knuth — دقيقة لـ λ صغيرة
        if (lambda < 30)
        {
            double L = Math.Exp(-lambda);
            double p = 1.0;
            int k = 0;
            do { k++; p *= _rng.NextDouble(); } while (p > L);
            return k - 1;
        }

        // تقريب Normal لـ λ كبيرة (لكرة القدم نادراً نصل هنا)
        var normal = lambda + Math.Sqrt(lambda) * NormalSample();
        return Math.Max(0, (int)Math.Round(normal));
    }

    /// <summary>Box-Muller لتوليد عينة N(0,1).</summary>
    private double NormalSample()
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}

/// <summary>نتيجة المحاكاة الكاملة.</summary>
public record SimulationResult(
    double HomeWinProbability,
    double DrawProbability,
    double AwayWinProbability,
    double[,] ScoreProbabilityGrid,
    List<(int Home, int Away, double Prob)> TopScores,
    int TotalIterations,
    double StandardError,
    double LambdaHome,
    double LambdaAway,
    List<MatchSimulationResult> RawSimulations)
{
    /// <summary>يُعيد احتمال نتيجة بعينها (e.g. 2-1).</summary>
    public double GetScoreProbability(int homeGoals, int awayGoals)
    {
        int maxG = ScoreProbabilityGrid.GetLength(0) - 1;
        if (homeGoals < 0 || homeGoals > maxG || awayGoals < 0 || awayGoals > maxG)
            return 0;
        return ScoreProbabilityGrid[homeGoals, awayGoals];
    }
}
