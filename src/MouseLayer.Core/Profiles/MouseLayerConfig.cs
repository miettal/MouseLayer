namespace MouseLayer.Core.Profiles;

public sealed class MouseLayerConfig
{
    public List<AppProfile> Profiles { get; init; } = new();

    /// <summary>Picks the most specific matching profile: an exact process match wins over the "*" default.</summary>
    public AppProfile? FindProfile(string processName)
    {
        AppProfile? fallback = null;
        foreach (var profile in Profiles)
        {
            if (profile.IsDefault)
            {
                fallback ??= profile;
                continue;
            }

            if (profile.Matches(processName))
                return profile;
        }

        return fallback;
    }

    public static MouseLayerConfig CreateDefault() => new()
    {
        Profiles = new List<AppProfile>
        {
            new() { ProcessNamePattern = "*", Mappings = new List<ButtonMapping>() },
        },
    };
}
