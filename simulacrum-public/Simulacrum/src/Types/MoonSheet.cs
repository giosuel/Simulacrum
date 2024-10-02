using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Simulacrum.Types;

public record struct MoonSheet()
{
    [YamlMember(Description = "The moon's name as defined in the 'PlanetName' field.")]
    public string PlanetName { get; init; }

    [YamlMember(Description = "The weight of the moon that determines the chance of it being used.\nDefault: 100.")]
    public float Weight { get; init; } = 100;

    [YamlMember(Description = "Blacklist of weathers on the moon.\nDefault: []")]
    public List<int> WeatherBlacklist { get; init; } = [];
}