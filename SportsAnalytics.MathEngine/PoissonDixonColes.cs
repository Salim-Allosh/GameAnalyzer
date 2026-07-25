using MathNet.Numerics;
using MathNet.Numerics.Optimization;

namespace SportsAnalytics.MathEngine;

/// <summary>
/// نموذج Dixon-Coles (1997) — Poisson مزدوج مع تصحيح ارتباط النتائج المنخفضة.
/// يُقدّر معاملات هجوم/دفاع لكل فريق من بيانات المباريات التاريخية.
/// 
/// المرجع: Dixon, M.J. & Coles, S.G. (1997). "Modelling Association Football Scores 
/// and Inefficiencies in the Football Betting Market." Applied Statistics, 46(2), 265-280.
/// </summary>
public class PoissonDixonColes
{
    // ── معاملات النموذج المُقدَّرة بعد التدريب ──
    public Dictionary<string, double> AttackParams { get; private set; } = new();
    public Dictionary<string, double> DefenseParams { get; private set; } = new();
    public double HomeAdvantage { get; private set; } = 0.0;
    public double RhoCorrection { get; private set; } = 0.0; // معامل تصحيح Dixon-Coles
    public bool IsTrained { get; private set; } = false;

    // ── تدريب النموذج ──

    /// <summary>
    /// يُدرّب النموذج من قائمة مباريات تاريخية.
    /// </summary>
    public void Train(IEnumerable<MatchRecord> matches)
    {
        var matchList = matches.ToList();
        if (matchList.Count < 10)
            throw new InvalidOperationException("يحتاج النموذج على الأقل 10 مباريات للتدريب.");

        var teams = matchList
            .SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        // تهيئة المعاملات الابتدائية
        var initialParams = BuildInitialParams(teams);
        var paramNames = initialParams.Keys.ToList();
        var x0 = paramNames.Select(k => initialParams[k]).ToArray();

        // تعظيم Log-Likelihood باستخدام L-BFGS-B (تصغير السالب)
        double NegLogLikelihood(double[] p)
        {
            var pm = BuildParamMap(paramNames, p);
            return -ComputeLogLikelihood(matchList, pm, teams);
        }

        // تحسين بسيط باستخدام gradient descent يدوي (بدون مكتبة خارجية لهذه المرحلة)
        var optimized = SimpleGradientDescent(x0, NegLogLikelihood, maxIter: 2000, learningRate: 0.05);
        var finalMap = BuildParamMap(paramNames, optimized);

        // استخراج النتائج
        foreach (var team in teams)
        {
            AttackParams[team] = finalMap.TryGetValue($"atk_{team}", out var a) ? a : 0.0;
            DefenseParams[team] = finalMap.TryGetValue($"def_{team}", out var d) ? d : 0.0;
        }

        HomeAdvantage = finalMap.TryGetValue("home", out var h) ? h : 0.25;
        RhoCorrection = finalMap.TryGetValue("rho", out var r) ? r : -0.1;
        IsTrained = true;
    }

    // ── حساب Lambda ──

    /// <summary>
    /// يحسب λHome وλAway لمباراة بين فريقين محددين.
    /// </summary>
    public (double LambdaHome, double LambdaAway) ComputeLambdas(string homeTeam, string awayTeam)
    {
        if (!IsTrained)
            throw new InvalidOperationException("النموذج لم يُدرَّب بعد.");

        if (!AttackParams.ContainsKey(homeTeam))
            throw new KeyNotFoundException($"الفريق غير موجود: {homeTeam}");
        if (!AttackParams.ContainsKey(awayTeam))
            throw new KeyNotFoundException($"الفريق غير موجود: {awayTeam}");

        var atkHome = AttackParams[homeTeam];
        var defHome = DefenseParams[homeTeam];
        var atkAway = AttackParams[awayTeam];
        var defAway = DefenseParams[awayTeam];

        var lambdaHome = Math.Exp(atkHome + defAway + HomeAdvantage);
        var lambdaAway = Math.Exp(atkAway + defHome);

        return (lambdaHome, lambdaAway);
    }

    // ── احتمالات الفوز/التعادل/الخسارة ──

    /// <summary>
    /// يحسب احتمالات فوز المضيف، التعادل، وفوز الضيف
    /// من خلال جمع احتمالات Poisson لكل نتيجة (0-0 حتى 10-10).
    /// </summary>


    public static double[,] ComputeOutcomesFromLambdas(
        double lambdaHome, double lambdaAway, double rho, int maxGoals = 14)
    {
        var grid = new double[maxGoals + 1, maxGoals + 1];
        double total = 0;

        for (int h = 0; h <= maxGoals; h++)
        {
            for (int a = 0; a <= maxGoals; a++)
            {
                var p = PoissonPmf(lambdaHome, h) * PoissonPmf(lambdaAway, a) * CalculateTau(h, a, lambdaHome, lambdaAway, rho);
                grid[h, a] = p;
                total += p;
            }
        }

        // تطبيع للتأكد من أن المجموع = 1
        if (total > 0)
        {
            for (int h = 0; h <= maxGoals; h++)
                for (int a = 0; a <= maxGoals; a++)
                    grid[h, a] /= total;
        }

        return grid;
    }

    public static double CalculateTau(int h, int a, double lambdaHome, double lambdaAway, double rho)
    {
        if (h == 0 && a == 0) return 1.0 - (lambdaHome * lambdaAway * rho);
        if (h == 0 && a == 1) return 1.0 + (lambdaHome * rho);
        if (h == 1 && a == 0) return 1.0 + (lambdaAway * rho);
        if (h == 1 && a == 1) return 1.0 - rho;
        return 1.0;
    }

    public (double HomeWin, double Draw, double AwayWin) ComputeOutcomeProbabilities(
        string homeTeam, string awayTeam)
    {
        var grid = ComputeExactGrid(homeTeam, awayTeam);
        
        double homeWin = 0, draw = 0, awayWin = 0;
        int maxGoals = grid.GetLength(0) - 1;

        for (int h = 0; h <= maxGoals; h++)
        {
            for (int a = 0; a <= maxGoals; a++)
            {
                if (h > a) homeWin += grid[h, a];
                else if (h == a) draw += grid[h, a];
                else awayWin += grid[h, a];
            }
        }

        return (homeWin, draw, awayWin);
    }

    public double[,] ComputeExactGrid(string homeTeam, string awayTeam)
    {
        var (lambdaHome, lambdaAway) = ComputeLambdas(homeTeam, awayTeam);
        return ComputeOutcomesFromLambdas(lambdaHome, lambdaAway, RhoCorrection);
    }

    // ── Brier Score ──

    /// <summary>
    /// يحسب Brier Score على مجموعة مباريات اختبار.
    /// Brier Score = متوسط (P_predicted - Actual)² 
    /// كلما كان أصغر كان النموذج أدق. الحد الأقصى النظري = 0.667 (عشوائي).
    /// </summary>
    public double ComputeBrierScore(IEnumerable<MatchRecord> testMatches)
    {
        var list = testMatches.ToList();
        if (list.Count == 0) return double.NaN;

        double totalScore = 0;
        int count = 0;

        foreach (var match in list)
        {
            if (!AttackParams.ContainsKey(match.HomeTeam) ||
                !AttackParams.ContainsKey(match.AwayTeam))
                continue;

            var (pHome, pDraw, pAway) = ComputeOutcomeProbabilities(match.HomeTeam, match.AwayTeam);

            // النتيجة الفعلية كـ one-hot
            double actualHome = match.HomeGoals > match.AwayGoals ? 1.0 : 0.0;
            double actualDraw = match.HomeGoals == match.AwayGoals ? 1.0 : 0.0;
            double actualAway = match.HomeGoals < match.AwayGoals ? 1.0 : 0.0;

            totalScore += Math.Pow(pHome - actualHome, 2)
                        + Math.Pow(pDraw - actualDraw, 2)
                        + Math.Pow(pAway - actualAway, 2);
            count++;
        }

        return count > 0 ? totalScore / count : double.NaN;
    }

    // ── دوال مساعدة ──

    private static double PoissonPmf(double lambda, int k)
    {
        if (lambda <= 0) return k == 0 ? 1.0 : 0.0;
        return Math.Exp(-lambda) * Math.Pow(lambda, k) / Factorial(k);
    }

    private static double Factorial(int n)
    {
        double result = 1;
        for (int i = 2; i <= n; i++) result *= i;
        return result;
    }

    private static Dictionary<string, double> BuildInitialParams(List<string> teams)
    {
        var p = new Dictionary<string, double>();
        foreach (var t in teams)
        {
            p[$"atk_{t}"] = 0.2;
            p[$"def_{t}"] = -0.2;
        }
        p["home"] = 0.25;
        p["rho"] = -0.1;
        return p;
    }

    private static Dictionary<string, double> BuildParamMap(List<string> names, double[] values)
    {
        var map = new Dictionary<string, double>();
        for (int i = 0; i < names.Count; i++)
            map[names[i]] = values[i];
        return map;
    }

    private static double ComputeLogLikelihood(
        List<MatchRecord> matches,
        Dictionary<string, double> pm,
        List<string> teams)
    {
        double ll = 0;
        // Clamp rho to prevent unbounded divergence in unconstrained GD
        double rho = pm.ContainsKey("rho") ? Math.Max(-0.2, Math.Min(0.2, pm["rho"])) : -0.1;
        
        foreach (var m in matches)
        {
            if (!pm.ContainsKey($"atk_{m.HomeTeam}") || !pm.ContainsKey($"atk_{m.AwayTeam}"))
                continue;

            var lambdaHome = Math.Exp(pm[$"atk_{m.HomeTeam}"] + pm[$"def_{m.AwayTeam}"] + pm["home"]);
            var lambdaAway = Math.Exp(pm[$"atk_{m.AwayTeam}"] + pm[$"def_{m.HomeTeam}"]);

            var tau = CalculateTau(m.HomeGoals, m.AwayGoals, lambdaHome, lambdaAway, rho);
            var pH = PoissonPmf(lambdaHome, m.HomeGoals);
            var pA = PoissonPmf(lambdaAway, m.AwayGoals);

            if (tau <= 0 || pH <= 0 || pA <= 0) continue;
            ll += Math.Log(tau) + Math.Log(pH) + Math.Log(pA);
        }
        return ll;
    }

    /// <summary>
    /// Gradient Descent بسيط لتحسين المعاملات.
    /// يُستبدل بـ L-BFGS في نسخة أكثر دقة (مستقبلاً).
    /// </summary>
    private static double[] SimpleGradientDescent(
        double[] x0, Func<double[], double> f,
        int maxIter, double learningRate)
    {
        var x = (double[])x0.Clone();
        const double epsilon = 1e-5;
        double prevLoss = f(x);
        if (double.IsNaN(prevLoss) || double.IsInfinity(prevLoss)) prevLoss = 1000.0;

        for (int iter = 0; iter < maxIter; iter++)
        {
            var grad = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                var xPlus = (double[])x.Clone();
                xPlus[i] += epsilon;
                var lossPlus = f(xPlus);
                if (double.IsNaN(lossPlus) || double.IsInfinity(lossPlus)) lossPlus = prevLoss;
                grad[i] = (lossPlus - prevLoss) / epsilon;
                if (double.IsNaN(grad[i]) || double.IsInfinity(grad[i])) grad[i] = 0;
            }

            // تحديث المعاملات
            for (int i = 0; i < x.Length; i++)
            {
                x[i] -= learningRate * grad[i];
                // سقف المعاملات لمنع الانفجار
                x[i] = Math.Clamp(x[i], -3.0, 3.0);
            }

            var loss = f(x);
            if (double.IsNaN(loss) || double.IsInfinity(loss)) loss = prevLoss;
            
            if (Math.Abs(prevLoss - loss) < 1e-8) break; // convergence
            prevLoss = loss;

            // تقليل learning rate تدريجياً
            if (iter > 0 && iter % 200 == 0) learningRate *= 0.9;
        }

        return x;
    }
}

/// <summary>سجل مباراة مُبسَّط لاستخدام MathEngine (بدون اعتمادية على EF Core).</summary>
public record MatchRecord(
    string HomeTeam,
    string AwayTeam,
    DateTime Date,
    int HomeGoals,
    int AwayGoals);
