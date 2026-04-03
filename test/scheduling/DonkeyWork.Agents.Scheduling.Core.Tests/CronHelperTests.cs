using DonkeyWork.Agents.Scheduling.Core.Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Tests;

public class CronHelperTests
{
    #region NormalizeToQuartzCron Tests

    [Fact]
    public void NormalizeToQuartzCron_ValidQuartz7Field_ReturnsUnchanged()
    {
        var result = CronHelper.NormalizeToQuartzCron("0 0 8 ? * MON-FRI");
        Assert.Equal("0 0 8 ? * MON-FRI", result);
    }

    [Fact]
    public void NormalizeToQuartzCron_ValidQuartz6Field_ReturnsUnchanged()
    {
        var result = CronHelper.NormalizeToQuartzCron("0 0 8 * * ?");
        Assert.Equal("0 0 8 * * ?", result);
    }

    [Fact]
    public void NormalizeToQuartzCron_Linux5Field_PrependSeconds()
    {
        var result = CronHelper.NormalizeToQuartzCron("0 8 * * 1-5");
        Assert.StartsWith("0 ", result);
    }

    [Fact]
    public void NormalizeToQuartzCron_Linux5Field_HandlesWildcardDow()
    {
        var result = CronHelper.NormalizeToQuartzCron("0 8 * * *");
        Assert.Contains("?", result);
    }

    [Fact]
    public void NormalizeToQuartzCron_InvalidFieldCount_Throws()
    {
        Assert.Throws<ArgumentException>(() => CronHelper.NormalizeToQuartzCron("0 8 *"));
    }

    [Fact]
    public void NormalizeToQuartzCron_TrimsWhitespace()
    {
        var result = CronHelper.NormalizeToQuartzCron("  0 0 8 ? * MON-FRI  ");
        Assert.Equal("0 0 8 ? * MON-FRI", result);
    }

    #endregion

    #region IsValid Tests

    [Theory]
    [InlineData("0 0 8 ? * MON-FRI")]
    [InlineData("0 0/5 * * * ?")]
    [InlineData("0 0 2 * * ?")]
    public void IsValid_ValidExpressions_ReturnsTrue(string cron)
    {
        Assert.True(CronHelper.IsValid(cron));
    }

    [Theory]
    [InlineData("not a cron")]
    [InlineData("")]
    [InlineData("0 0 25 * * ?")]
    public void IsValid_InvalidExpressions_ReturnsFalse(string cron)
    {
        Assert.False(CronHelper.IsValid(cron));
    }

    #endregion

    #region MeetsMinimumInterval Tests

    [Fact]
    public void MeetsMinimumInterval_EveryDay_Passes4Hours()
    {
        Assert.True(CronHelper.MeetsMinimumInterval("0 0 8 * * ?", 4));
    }

    [Fact]
    public void MeetsMinimumInterval_Every5Minutes_Fails4Hours()
    {
        Assert.False(CronHelper.MeetsMinimumInterval("0 0/5 * * * ?", 4));
    }

    [Fact]
    public void MeetsMinimumInterval_EveryHour_Fails4Hours()
    {
        Assert.False(CronHelper.MeetsMinimumInterval("0 0 * * * ?", 4));
    }

    [Fact]
    public void MeetsMinimumInterval_Every4Hours_Passes4Hours()
    {
        Assert.True(CronHelper.MeetsMinimumInterval("0 0 0/4 * * ?", 4));
    }

    [Fact]
    public void MeetsMinimumInterval_InvalidCron_ReturnsFalse()
    {
        Assert.False(CronHelper.MeetsMinimumInterval("not valid", 4));
    }

    #endregion
}
