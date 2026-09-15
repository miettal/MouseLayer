using MouseLayer.Core;
using MouseLayer.Core.Actions;
using MouseLayer.Core.Profiles;

namespace MouseLayer.Core.Tests;

public class MouseLayerConfigTests
{
    [Fact]
    public void FindProfile_PrefersExactProcessMatch_OverDefault()
    {
        var chromeProfile = new AppProfile { ProcessNamePattern = "chrome.exe" };
        var defaultProfile = new AppProfile { ProcessNamePattern = "*" };
        var config = new MouseLayerConfig { Profiles = { defaultProfile, chromeProfile } };

        var found = config.FindProfile("chrome.exe");

        Assert.Same(chromeProfile, found);
    }

    [Fact]
    public void FindProfile_FallsBackToDefault_WhenNoExactMatch()
    {
        var defaultProfile = new AppProfile { ProcessNamePattern = "*" };
        var config = new MouseLayerConfig
        {
            Profiles = { new AppProfile { ProcessNamePattern = "chrome.exe" }, defaultProfile },
        };

        var found = config.FindProfile("notepad.exe");

        Assert.Same(defaultProfile, found);
    }

    [Fact]
    public void FindProfile_ReturnsNull_WhenNoProfilesConfigured()
    {
        var config = new MouseLayerConfig();

        Assert.Null(config.FindProfile("anything.exe"));
    }

    [Fact]
    public void CreateDefault_ProducesSingleEmptyFallbackProfile()
    {
        var config = MouseLayerConfig.CreateDefault();

        var profile = Assert.Single(config.Profiles);
        Assert.True(profile.IsDefault);
        Assert.Empty(profile.Mappings);
    }
}
