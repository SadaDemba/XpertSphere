using FluentAssertions;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;

namespace XpertSphere.MonolithApi.Tests.Extensions;

/// <summary>
/// Covers .claude/specifications/configurable-salary-currency.md, criterion 1: <see cref="Currency"/>
/// conversion follows the same convention (and the same strictness on unknown values) as the other
/// enums stored as strings by EF Core (<c>OrganizationSize</c>, <c>WorkMode</c>, ...).
/// </summary>
public class EnumExtensionsCurrencyTests
{
    [Theory]
    [InlineData(Currency.EUR, "EUR")]
    [InlineData(Currency.XOF, "XOF")]
    public void ToStringValue_ShouldReturnExpectedString(Currency currency, string expected)
    {
        currency.ToStringValue().Should().Be(expected);
    }

    [Theory]
    [InlineData("EUR", Currency.EUR)]
    [InlineData("XOF", Currency.XOF)]
    [InlineData("eur", Currency.EUR)]
    [InlineData("xof", Currency.XOF)]
    public void ToCurrency_ShouldParseCaseInsensitively(string value, Currency expected)
    {
        value.ToCurrency().Should().Be(expected);
    }

    [Fact]
    public void ToCurrency_WithUnknownValue_ShouldThrow()
    {
        var act = () => "USD".ToCurrency();
        act.Should().Throw<ArgumentException>();
    }
}
