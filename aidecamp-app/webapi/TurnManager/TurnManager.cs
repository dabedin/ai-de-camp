using Microsoft.SemanticKernel;
using OpenTelemetry.Exporter;
using System.ComponentModel;
public class TurnManagerPlugin
{
    private static readonly Random random = new Random();

    [KernelFunction("calculateOutcome")]
    [Description("Calculate the outcome of the wargame scenario, evaluating modifiers for the firing and target toy soldiers.")]
    [return: Description("The outcome of the wargame scenario.")]
    public ScenarioOutcome CalculateOutcome([Description("The wargame scenario")] ScenarioModel scenario) 
    {
        Console.WriteLine ("Calculating outcome of the wargame scenario");
        Console.WriteLine ("Firing: " + scenario.Firing.Pose + " " + scenario.Firing.Weapon);
        Console.WriteLine ("Target: " + scenario.Target.Pose + " " + scenario.Target.Weapon);
        Console.WriteLine ("Distance: " + scenario.Distance.Value + " " + scenario.Distance.Unit);

        int firingModifier = CalculateFiringModifier(scenario.Firing.Weapon);
        int targetModifier = CalculateTargetModifier(scenario.Target.Pose);
        int distanceModifier = CalculateDistanceModifier(scenario.Distance.Value);

        int rolledDice = random.Next(1, 20);
        int totalModifiers = firingModifier + targetModifier + distanceModifier;
        bool hitOrMiss = rolledDice > totalModifiers;

        return new ScenarioOutcome
        {
            FiringModifier = firingModifier,
            TargetModifier = targetModifier,
            DistanceModifier = distanceModifier,
            RolledDice = rolledDice,
            HitOrMiss = hitOrMiss
        };
    }

    private int CalculateFiringModifier(Weapon weapon)
    {
        return weapon switch
        {
            Weapon.rifle => 1,
            Weapon.machine_gun => 2,
            Weapon.SMG => 3,
            Weapon.pistol => 5,
            _ => 0
        };
    }

    private int CalculateTargetModifier(Pose pose)
    {
        return pose switch
        {
            Pose.crouched => 3,
            Pose.prone => 6,
            Pose.standing => 0,
            _ => 0
        };
    }

    private int CalculateDistanceModifier(int distance)
    {
        return distance >= 70 ? 3 : 0;
    }
}