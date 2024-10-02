using YamlDotNet.Serialization;

namespace Simulacrum.Types;

public record struct EntitySheet()
{
    [YamlMember(Description = "The name of the entity as defined in EnemyType.")]
    public string EntityName { get; init; }

    [YamlMember(Description = "The health this entity has. If set to -1, the default value will be used.\nDefault: -1.")]
    public int Health { get; init; } = -1;

    [YamlMember(Description = "The difficulty rating of the entity in a scale from 1 to 10. More difficult entities will appear in later stages of the game.\nDefault: 1.")]
    public int DifficultyRating { get; init; } = 1;

    [YamlMember(Description = "The weight of this entity. The higher the weight, the higher the spawn chance.\nDefault: 100.")]
    public int Weight { get; init; } = 100;

    [YamlMember(Description = "Multiplier of the entity's damage to employees.\nDefault: 1.")]
    public int DamageMultiplier { get; init; } = 1;
}