using ERPSystem.Application.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Tests.Common;

public sealed class ChinaImportLengthDisplayTests
{
    [Fact]
    public void FromStoredLength_Yards_converts_from_meters()
    {
        var yards = ChinaImportLengthDisplay.FromStoredLength(109.728m, DplQuantityUnit.Yards);
        Assert.Equal(120m, yards);
    }

    [Fact]
    public void FromStoredRate_Yards_multiplies_by_factor()
    {
        var perYard = ChinaImportLengthDisplay.FromStoredRate(1.41m, DplQuantityUnit.Yards);
        Assert.Equal(1.289304m, perYard);
    }

    [Fact]
    public void ToStoredRate_Yards_divides_by_factor()
    {
        var perMeter = ChinaImportLengthDisplay.ToStoredRate(1.289304m, DplQuantityUnit.Yards);
        Assert.Equal(1.41m, perMeter);
    }

    [Theory]
    [InlineData(DplQuantityUnit.Yards, "يارد", "تكلفة/ي")]
    [InlineData(DplQuantityUnit.Meters, "أمتار", "تكلفة/م")]
    public void Labels_follow_unit(DplQuantityUnit unit, string lengthHeader, string costLabel)
    {
        Assert.Equal(lengthHeader, ChinaImportLengthDisplay.LengthColumnHeader(unit));
        Assert.Equal(costLabel, ChinaImportLengthDisplay.CostPerUnitLabel(unit));
    }
}
