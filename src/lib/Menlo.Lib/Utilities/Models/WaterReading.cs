using Menlo.Common;
using Microsoft.Azure.CosmosRepository;
using System.Diagnostics;

namespace Menlo.Utilities.Models;

[DebuggerDisplay("{Date.ToString(\"O\")} - {Usage} [{Cost}]")]
public class WaterReading : Item
{
    private WaterReading(Reading ownReading, Reading municipalReading, DateOnly date)
    {
        OwnReading = ownReading;
        MunicipalReading = municipalReading;
        Date = date;
    }

    public WaterReading(Reading ownReading, DateOnly date)
        : this(ownReading, Reading.Empty, date)
    {
    }

    public Reading OwnReading { get; private set; }
    public Reading MunicipalReading { get; private set; }
    public Money Cost => HasMunicipalReading ? MunicipalReading.Cost : OwnReading.Cost;
    public LiterMeasurement Usage => HasMunicipalReading ? MunicipalReading.Usage : OwnReading.Usage;
    public DateOnly Date { get; private set; }

    private bool HasMunicipalReading => MunicipalReading != Reading.Empty;

    public void UpdateOwnReading(Reading ownReading)
    {
        OwnReading = ownReading;
    }

    public void TakeMunicipalReading(Reading municipalReading)
    {
        MunicipalReading = municipalReading;
    }

    public record Reading(LiterMeasurement Opening, LiterMeasurement Closing, Money Cost)
    {
        public static Reading Empty { get; } = new(
            LiterMeasurement.Zero,
            LiterMeasurement.Zero,
            new Money(decimal.Zero, Currency.Zar));

        public LiterMeasurement Usage => Closing - Opening;
    }
}
