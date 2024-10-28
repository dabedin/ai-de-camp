using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class ScenarioModel
{
    [JsonPropertyName("firing")]
    [Description("The characteristics of the firing toy soldier.")]
    public Characteristics? Firing { get; set; }

    [JsonPropertyName("target")]
    [Description("The characteristics of the target toy soldier.")]
    public Characteristics? Target { get; set; }

    [JsonPropertyName("distance")]
    [Description("The distance between the firing and target toy soldiers.")]
    public Distance? Distance { get; set; }
}

public class Characteristics
{
    [JsonPropertyName("pose")]
    public Pose Pose { get; set; }

    [JsonPropertyName("weapon")]
    public Weapon Weapon { get; set; }
}

public class Distance
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("estimated")]
    public bool Estimated { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Pose
{
    standing,
    crouched,
    prone
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Weapon
{
    pistol,
    machine_gun,
    SMG,
    rifle
}

public class ScenarioOutcome
{
    [JsonPropertyName("firing-modifier")]
    [Description("The modifier of the firing toy soldier.")]
    public int FiringModifier { get; set; }

    [JsonPropertyName("target-modifier")]
    [Description("The modifier of the target toy soldier.")]
    public int TargetModifier { get; set; }

    [JsonPropertyName("distance-modifier")]
    [Description("The modifier of the distance between firing and target toy soldiers.")]
    public int DistanceModifier { get; set; }
    
    [JsonPropertyName("rolled-dice")]
    [Description("The virtual rolled dice: a random number that is used to compare it with the overall modifiers.")]
    public int RolledDice { get; set; }

    [JsonPropertyName("hit-or-miss")]
    [Description("The scenario outcome: true if the firing toy soldier hit or false if it missed the target toy soldier.")]
    public bool HitOrMiss { get; set; }
}
