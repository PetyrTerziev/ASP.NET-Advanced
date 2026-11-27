using GuardPost.Common.Providers;

namespace GuardPost.Services.Tests.Infrastructure;

/// <summary>A fixed clock so the tests do not depend on the real date.</summary>
public class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }

    public DateTime Today => Now.Date;
}
