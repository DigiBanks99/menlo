using System.Globalization;

namespace Menlo.Ui.Charts;

/// <summary>
/// The arithmetic behind the SVG charts. Pure functions, so chart geometry can be unit tested without rendering.
/// </summary>
public static class ChartMath
{
    /// <summary>
    /// Returns evenly spaced axis ticks from zero up to a round number at or above <paramref name="max"/>.
    /// Steps are 1, 2, 2.5 or 5 times a power of ten, so labels read cleanly.
    /// </summary>
    public static IReadOnlyList<decimal> Ticks(decimal max, int target = 4)
    {
        if (max <= 0m)
        {
            return [0m, 1m];
        }

        double rough = (double)max / Math.Max(target, 1);
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        double normalized = rough / magnitude;
        double step = normalized switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 2.5 => 2.5,
            <= 5 => 5,
            _ => 10
        } * magnitude;

        List<decimal> ticks = [];
        decimal stepValue = (decimal)step;
        for (decimal tick = 0m; tick < max + stepValue; tick += stepValue)
        {
            ticks.Add(tick);
            if (tick >= max)
            {
                break;
            }
        }

        return ticks;
    }

    /// <summary>
    /// Maps a value in <c>[0, top]</c> to a length in <c>[0, size]</c>. Values outside the range are clamped.
    /// </summary>
    public static double Scale(decimal value, decimal top, double size) =>
        top <= 0m ? 0d : (double)(Math.Clamp(value, 0m, top) / top) * size;

    /// <summary>
    /// Returns each value's share of the total as a fraction between 0 and 1. Negative values count as zero.
    /// </summary>
    public static IReadOnlyList<double> Shares(IReadOnlyList<decimal> values)
    {
        decimal total = values.Sum(value => Math.Max(value, 0m));
        return total <= 0m
            ? [.. values.Select(_ => 0d)]
            : [.. values.Select(value => (double)(Math.Max(value, 0m) / total))];
    }

    /// <summary>
    /// Formats a coordinate for an SVG attribute: invariant culture, at most two decimals.
    /// </summary>
    public static string Coordinate(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the 1-based colour slot for a series index. There are eight colours; further series reuse them in order.
    /// </summary>
    public static int ColourSlot(int index) => (index % 8) + 1;
}

/// <summary>
/// A named set of values, one per category, drawn as one colour in a bar or line chart.
/// </summary>
/// <param name="Name">The series name, shown in the legend.</param>
/// <param name="Values">One value per category, in category order.</param>
public sealed record MnlChartSeries(string Name, IReadOnlyList<decimal> Values);

/// <summary>
/// One segment of a donut chart.
/// </summary>
/// <param name="Label">The segment's name, shown in the legend.</param>
/// <param name="Value">The segment's size. Only its share of the total matters.</param>
public sealed record MnlChartSlice(string Label, decimal Value);
