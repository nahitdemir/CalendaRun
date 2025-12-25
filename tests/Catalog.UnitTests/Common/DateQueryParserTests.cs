using Calendarun.Common.Time;
using FluentAssertions;

namespace Catalog.UnitTests.Common;

public class DateQueryParserTests
{
    [Fact]
    public void TryParseDateFilter_ReturnsNullForEmptyInput()
    {
        var ok = DateQueryParser.TryParseDateFilter(null, "from", false, out var value, out var error);

        ok.Should().BeTrue();
        value.Should().BeNull();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("2025-01-02", false, 2025, 1, 2)]
    [InlineData("2025-01-02", true, 2025, 1, 3)]
    public void TryParseDateFilter_ParsesDateOnlyAsUtcBoundary(
        string input,
        bool endExclusive,
        int year,
        int month,
        int day)
    {
        var ok = DateQueryParser.TryParseDateFilter(input, "from", endExclusive, out var value, out var error);

        ok.Should().BeTrue();
        error.Should().BeNull();
        value.Should().Be(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void TryParseDateFilter_ParsesIsoTimestampAsUtc()
    {
        var ok = DateQueryParser.TryParseDateFilter(
            "2025-01-02T05:30:00+02:00",
            "from",
            false,
            out var value,
            out var error);

        ok.Should().BeTrue();
        error.Should().BeNull();
        value.Should().Be(new DateTimeOffset(2025, 1, 2, 3, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void TryParseDateFilter_RejectsInvalidInput()
    {
        var ok = DateQueryParser.TryParseDateFilter(
            "02-01-2025",
            "from",
            false,
            out var value,
            out var error);

        ok.Should().BeFalse();
        value.Should().BeNull();
        error.Should().Contain("from");
    }
}
