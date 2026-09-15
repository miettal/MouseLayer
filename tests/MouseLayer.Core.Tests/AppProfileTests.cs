using MouseLayer.Core;
using MouseLayer.Core.Actions;
using MouseLayer.Core.Profiles;

namespace MouseLayer.Core.Tests;

public class AppProfileTests
{
    [Fact]
    public void Matches_IsCaseInsensitive_ForExactProcessName()
    {
        var profile = new AppProfile { ProcessNamePattern = "chrome.exe" };

        Assert.True(profile.Matches("CHROME.EXE"));
        Assert.False(profile.Matches("notepad.exe"));
    }

    [Fact]
    public void Matches_DefaultProfile_MatchesAnyProcess()
    {
        var profile = new AppProfile { ProcessNamePattern = "*" };

        Assert.True(profile.Matches("anything.exe"));
    }

    [Fact]
    public void GetAction_ReturnsMappedAction_WhenButtonIsConfigured()
    {
        var profile = new AppProfile
        {
            Mappings = { new ButtonMapping { Source = MouseButton.X1, Action = ButtonAction.ToKey(0x41) } },
        };

        var action = profile.GetAction(MouseButton.X1);

        Assert.Equal(ActionType.KeyPress, action.Type);
        Assert.Equal((ushort)0x41, action.VirtualKeyCode);
    }

    [Fact]
    public void GetAction_ReturnsPassThrough_WhenButtonIsNotConfigured()
    {
        var profile = new AppProfile();

        var action = profile.GetAction(MouseButton.Middle);

        Assert.Equal(ActionType.PassThrough, action.Type);
    }
}
