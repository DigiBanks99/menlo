using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class OnboardingStateIdValueConverter()
    : ValueConverter<OnboardingStateId, Guid>(id => id.Value, guid => new OnboardingStateId(guid));
