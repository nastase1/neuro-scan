using NeuroScan.Application.Helpers;

namespace NeuroScan.Tests.Helpers;

public class DateHelperTests
{
    [Fact]
    public void CalculateAge_WhenBirthdayAlreadyPassedThisYear_ReturnsCorrectAge()
    {
        var today = DateTime.Today;
        var birthDate = today.AddYears(-30).AddDays(-1); // birthday was yesterday

        var age = DateHelper.CalculateAge(birthDate);

        Assert.Equal(30, age);
    }

    [Fact]
    public void CalculateAge_WhenBirthdayIsToday_ReturnsCorrectAge()
    {
        var today = DateTime.Today;
        var birthDate = today.AddYears(-25); // birthday is exactly today

        var age = DateHelper.CalculateAge(birthDate);

        Assert.Equal(25, age);
    }

    [Fact]
    public void CalculateAge_WhenBirthdayNotYetPassedThisYear_ReturnsOneYearLess()
    {
        var today = DateTime.Today;
        var birthDate = today.AddYears(-20).AddDays(1); // birthday is tomorrow

        var age = DateHelper.CalculateAge(birthDate);

        Assert.Equal(19, age);
    }

    [Fact]
    public void CalculateAge_WithNewbornBirthDate_ReturnsZero()
    {
        var age = DateHelper.CalculateAge(DateTime.Today);

        Assert.Equal(0, age);
    }
}
