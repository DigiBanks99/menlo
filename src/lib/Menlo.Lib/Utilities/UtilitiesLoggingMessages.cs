using Menlo.Utilities.Handlers.Electricity;
using Microsoft.Extensions.Logging;

namespace Menlo.Utilities;

public static partial class UtilitiesLoggingMessages
{
    [LoggerMessage(LogLevel.Information, "Handling electricity capture command: {Command}")]
    public static partial void HandlingRequestToCaptureElectricityUsage(this ILogger logger, CaptureElectricityUsageRequest command);

    [LoggerMessage(LogLevel.Information, "Handling electricity usage query: {Query}")]
    public static partial void HandlingElectricityUsageQuery(this ILogger logger, ElectricityUsageQuery query);
}
