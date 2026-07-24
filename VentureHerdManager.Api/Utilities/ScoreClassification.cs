namespace VentureHerdManager.Api.Utilities;

public static class ScoreClassification
{
    public enum Grade
    {
        GP,  // Good Plus: 84 and below
        VG,  // Very Good: 85-89
        EX   // Excellent: 90+
    }

    public static Grade GetGrade(decimal score)
    {
        if (score >= 90)
            return Grade.EX;
        if (score >= 85)
            return Grade.VG;
        return Grade.GP;
    }

    public static string GetGradeLabel(decimal? score)
    {
        if (!score.HasValue)
            return "—";

        var grade = GetGrade(score.Value);
        return $"{grade} {score:F2}";
    }

    public static string GetBaaLabel(decimal? baa)
    {
        if (!baa.HasValue)
            return "—";

        return $"BAA {baa:F2}";
    }
}
