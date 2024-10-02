using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using LethalLevelLoader;
using Simulacrum.Types;
using Simulacrum.Utils;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Simulacrum;

public class GameConfig
{
    internal readonly GeneralConfigs General;
    internal readonly PlayerConfigs Player;
    internal readonly EntityConfigs Entities;
    internal readonly EventConfigs Events;
    internal readonly PowerupConfigs Powerups;
    internal readonly EnvironmentConfigs Environment;

    internal readonly ConfigSheets Sheets;

    internal GameConfig(ConfigFile config)
    {
        General = new GeneralConfigs(config);
        Player = new PlayerConfigs(config);
        Entities = new EntityConfigs(config);
        Events = new EventConfigs(config);
        Powerups = new PowerupConfigs(config);
        Environment = new EnvironmentConfigs(config);

        Sheets = new ConfigSheets();
    }

    internal class GeneralConfigs(ConfigFile config)
    {
        internal readonly ConfigEntry<float> DifficultyMultiplier = config.Bind(
            "Scaling", "Difficulty Multiplier", 1f, "Global difficulty scaling multiplier."
        );

        internal readonly ConfigEntry<float> PreparationTime = config.Bind(
            "Scaling", "Preparation Time", 5f, "Preparation time between iterations."
        );

        internal readonly ConfigEntry<string> ConfigSheetsLocation = config.Bind(
            "Config", "Sheets Location", "sheets", "Location of the sheets relative to the assembly."
        );
    }

    internal class EnvironmentConfigs(ConfigFile config)
    {
        private readonly ConfigEntry<bool> Enhance = config.Bind(
            "Environment", "Enhance Environment", true,
            "Enhances certain aspects of the environment such as skybox and volumetrics."
        );

        private readonly ConfigEntry<string> DisabledLevels = config.Bind(
            "Environment", "Disabled Moons", "Gordion,Liquidation", "List of moons that are excluded from the moon pool."
        );

        internal readonly ConfigEntry<float> DaytimeDistributionMean = config.Bind(
            "Environment", "Daytime Mean", 0.45f, "Moon daytime normal distribution mean."
        );

        internal readonly ConfigEntry<float> DaytimeDistributionStdDev = config.Bind(
            "Environment", "Daytime Standard Deviation", 0.25f, "Moon daytime normal distribution standard deviation."
        );

        internal List<ExtendedLevel> FilterDisabledMoons(IEnumerable<ExtendedLevel> entities)
        {
            var blacklistedNames = DisabledLevels.Value.Split(",").ToHashSet();
            return entities
                .Where(level => !blacklistedNames.Contains(level.NumberlessPlanetName))
                .ToList();
        }
    }

    internal class PlayerConfigs(ConfigFile config)
    {
        internal readonly ConfigEntry<float> SprintTime = config.Bind(
            "Player", "Sprint Time", 10f, "How long the player can sprint."
        );

        internal readonly ConfigEntry<float> MovementSpeed = config.Bind(
            "Player", "Movement Speed", 8f, "Movement speed of the player."
        );

        internal readonly ConfigEntry<float> IncomingDamageMultiplier = config.Bind(
            "Player", "Incoming Damage Multiplier", 0.25f, "Multiplier of incoming damage."
        );

        internal readonly ConfigEntry<float> OneshotDamageMultiplier = config.Bind(
            "Player", "Oneshot Damage Multiplier", 0.25f,
            "Multiplier of incoming damage that would normally oneshot the employee."
        );
    }

    internal class EntityConfigs(ConfigFile config)
    {
        private readonly ConfigEntry<bool> EnableUnkillable = config.Bind(
            "Entities", "Enable Unkillable Entities", false,
            "Enables spawning of entities that are marked as unkillable in the game. These entities don't need to be killed in order to advance."
        );

        private readonly ConfigEntry<string> DisabledEntities = config.Bind(
            "Entities", "Disabled Entities",
            "Docile Locust Bees,Red Locust Bees,Manticoil,Red pill,Shiggy,ForestGiant,Lasso,Bush Wolf,Jester,Spring,Girl,RadMech,Earth Leviathan,Butler",
            "List of entities that are excluded from the spawn pool."
        );

        private readonly ConfigEntry<string> UnkillableEntities = config.Bind(
            "Entities", "Unkillable Entities",
            "Blob",
            "List of entities that aren't classified as unkillable by the game but should be. These entities don't have to be killed for the game to advance but will still be spwned, if not in the blacklist."
        );

        internal List<EnemyType> FilterDisabledEntities(List<EnemyType> entities)
        {
            var blacklistedNames = DisabledEntities.Value.Split(",").ToHashSet();
            foreach (var blacklistedName in blacklistedNames)
            {
                Simulacrum.Log.LogInfo($"DISABLED: '{blacklistedName}'");
            }

            foreach (var enemyType in entities)
            {
                Simulacrum.Log.LogInfo(
                    $"Entity name: '{enemyType.enemyName}', is blacklist: {blacklistedNames.Contains(enemyType.enemyName)}");
            }

            return entities
                .Where(entity => (entity.canDie || EnableUnkillable.Value) && !blacklistedNames.Contains(entity.enemyName))
                .ToList();
        }

        internal bool IsUnkillable(string entityName)
        {
            return UnkillableEntities.Value.Split(",").ToHashSet().Contains(entityName);
        }
    }

    internal class EventConfigs(ConfigFile config)
    {
        internal readonly ConfigEntry<float> Chance = config.Bind(
            "Events", "Chance", 0.75f,
            "This chance is rolled for every event. If it hits, the event is executed, otherwise its skipped."
        );

        internal readonly ConfigEntry<Distribution> AmountDistribution = config.Bind(
            "Events", "Amount Distribution", Distribution.Linear,
            "Type of distribution used to determine the amount of event rolls per iteration."
        );

        internal readonly ConfigEntry<float> AmountNormalDistributionMean = config.Bind(
            "Events", "Normal Mean", 0.5f,
            "The mean of the event amount distribution if the normal distribution is used."
        );

        internal readonly ConfigEntry<float> AmountNormalDistributionStdDev = config.Bind(
            "Events", "Normal Standard Deviation", 0.1f,
            "The stardard deviation of the event amount distribution if the normal distribution is used."
        );

        internal readonly ConfigEntry<int> RollLowerBound = config.Bind(
            "Events", "Lower Bound", 4,
            "Lower bound of the amount of event rolls per iteration. This value is multiplied by the gloabl difficulty multiplier."
        );

        internal readonly ConfigEntry<int> RollUpperBound = config.Bind(
            "Events", "Upper Bound", 8,
            "Upper bound of the amount of event rolls per iteration. This value is multiplied by the global difficulty multiplier."
        );
    }

    internal class PowerupConfigs(ConfigFile config)
    {
        internal readonly ConfigEntry<float> SpawnChance = config.Bind(
            "Powerups", "Chance", 0.75f, "Chance of a powerup spawn per roll."
        );

        internal readonly ConfigEntry<Distribution> AmountDistribution = config.Bind(
            "Powerups", "Amount Distribution", Distribution.Linear,
            "Type of distribution used to determine the amount of powerups rolls per iteration."
        );

        internal readonly ConfigEntry<float> AmountNormalDistributionMean = config.Bind(
            "Powerups", "Normal Mean", 0.5f,
            "The mean of the powerup amount distribution if the normal distribution is used."
        );

        internal readonly ConfigEntry<float> AmountNormalDistributionStdDev = config.Bind(
            "Powerups", "Normal Stdandard Deviation", 0.1f,
            "The stardard deviation of the powerup amount distribution if the normal distribution is used."
        );

        internal readonly ConfigEntry<int> RollLowerBound = config.Bind(
            "Powerups", "Lower Bound", 0,
            "Lower bound of the amount of powerup rolls per iteration. This value is multiplied by the difficulty multiplier."
        );

        internal readonly ConfigEntry<int> RollUpperBound = config.Bind(
            "Powerups", "Upper Bound", 4,
            "Upper bound of the amount of powerup rolls per iteration. This value is multiplied by the difficulty multiplier."
        );
    }

    internal class ConfigSheets
    {
        private Dictionary<string, EntitySheet> entities { get; init; } = GetEntities();
        private Dictionary<string, MoonSheet> moons { get; init; } = GetMoons();

        internal EntitySheet GetEntity(string entityName) => entities[entityName];
        internal MoonSheet GetMoon(string planetName) => moons[planetName];

        private const string entitySheetFolder = "entities";
        private const string moonSheetFolder = "moons";

        internal void CheckEntitySheets(IEnumerable<EnemyType> registeredEntities)
        {
            foreach (var entityName in registeredEntities.Select(entity => entity.enemyName))
            {
                if (entities.ContainsKey(entityName))
                {
                    Simulacrum.Log.LogInfo($"[CFG] Found entity sheet for entity '{entityName}'.");
                    return;
                }

                Simulacrum.Log.LogInfo($"[CFG] Created default entity sheet for entity '{entityName}'.");

                var entitySheet = new EntitySheet { EntityName = entityName };

                entities[entityName] = entitySheet;
                SerializeSheet(entityName, entitySheetFolder, entitySheet);
            }
        }

        internal void CheckMoonSheets(IEnumerable<ExtendedLevel> registeredMoons)
        {
            foreach (var moonName in registeredMoons.Select(moon => moon.NumberlessPlanetName))
            {
                if (moons.ContainsKey(moonName))
                {
                    Simulacrum.Log.LogInfo($"[CFG] Found moon sheet for moon '{moonName}'.");
                    return;
                }

                Simulacrum.Log.LogInfo($"[CFG] Created default moon sheet for moon '{moonName}'.");

                var moonSheet = new MoonSheet { PlanetName = moonName };

                moons[moonName] = moonSheet;
                SerializeSheet(moonName, moonSheetFolder, moonSheet);
            }
        }

        private void SerializeSheet<T>(string sheetName, string sheetFolder, T sheet)
        {
            var sheetPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "sheets",
                sheetFolder,
                SanitizeFileName(sheetName) + ".yaml"
            );

            var sheetSerialized = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Serialize(sheet);

            File.WriteAllText(sheetPath, sheetSerialized);
        }

        private string SanitizeFileName(string fileName)
        {
            return string.Concat(fileName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_").ToLower();
        }

        private static Dictionary<string, MoonSheet> GetMoons()
        {
            var sheetFiles = GetSheetFiles(moonSheetFolder);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var objects = new Dictionary<string, MoonSheet>();

            foreach (var file in sheetFiles)
            {
                using var input = File.OpenText(file);
                var sheet = deserializer.Deserialize<MoonSheet>(input);

                if (string.IsNullOrEmpty(sheet.PlanetName))
                {
                    Simulacrum.Log.LogError($"[CFG] Invalid moon sheet found: {file}. Skipping.");
                    continue;
                }

                objects[sheet.PlanetName] = sheet;
            }

            return objects;
        }

        private static Dictionary<string, EntitySheet> GetEntities()
        {
            var sheetFiles = GetSheetFiles(entitySheetFolder);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var objects = new Dictionary<string, EntitySheet>();

            foreach (var file in sheetFiles)
            {
                using var input = File.OpenText(file);
                var sheet = deserializer.Deserialize<EntitySheet>(input);

                if (string.IsNullOrEmpty(sheet.EntityName))
                {
                    Simulacrum.Log.LogError($"[CFG] Invalid entity sheet found: {file}. Skipping.");
                    continue;
                }

                objects[sheet.EntityName] = sheet;
            }

            return objects;
        }

        private static string[] GetSheetFiles(string folderName)
        {
            var sheetsPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "sheets",
                folderName
            );

            if (!Directory.Exists(sheetsPath)) Directory.CreateDirectory(sheetsPath);

            return Directory.GetFiles(sheetsPath);
        }
    }
}