"""Turn manager for calculating wargame outcomes."""

import random
from typing import Annotated

from semantic_kernel.functions import kernel_function

from models import ScenarioModel, ScenarioOutcome, Weapon, Pose


class TurnManagerPlugin:
    """Plugin for calculating wargame scenario outcomes."""

    def __init__(self):
        self._random = random.Random()

    @kernel_function(
        name="calculate_outcome",
        description="Calculate the outcome of the wargame scenario, evaluating modifiers for the firing and target toy soldiers.",
    )
    def calculate_outcome(
        self, scenario: Annotated[ScenarioModel, "The wargame scenario"]
    ) -> Annotated[ScenarioOutcome, "The outcome of the wargame scenario"]:
        """Calculate the outcome of a wargame scenario."""
        print("Calculating outcome of the wargame scenario")
        print(f"Firing: {scenario.firing.pose} {scenario.firing.weapon}")
        print(f"Target: {scenario.target.pose} {scenario.target.weapon}")
        print(f"Distance: {scenario.distance.value} {scenario.distance.unit}")

        firing_modifier = self._calculate_firing_modifier(scenario.firing.weapon)
        target_modifier = self._calculate_target_modifier(scenario.target.pose)
        distance_modifier = self._calculate_distance_modifier(scenario.distance.value)

        rolled_dice = self._random.randint(1, 20)
        total_modifiers = firing_modifier + target_modifier + distance_modifier
        hit_or_miss = rolled_dice > total_modifiers

        return ScenarioOutcome(
            firing_modifier=firing_modifier,
            target_modifier=target_modifier,
            distance_modifier=distance_modifier,
            rolled_dice=rolled_dice,
            hit_or_miss=hit_or_miss,
        )

    def _calculate_firing_modifier(self, weapon: Weapon) -> int:
        """Calculate the firing modifier based on weapon type."""
        weapon_modifiers = {
            Weapon.RIFLE: 1,
            Weapon.MACHINE_GUN: 2,
            Weapon.SMG: 3,
            Weapon.PISTOL: 5,
        }
        return weapon_modifiers.get(weapon, 0)

    def _calculate_target_modifier(self, pose: Pose) -> int:
        """Calculate the target modifier based on pose."""
        pose_modifiers = {
            Pose.CROUCHED: 3,
            Pose.PRONE: 6,
            Pose.STANDING: 0,
        }
        return pose_modifiers.get(pose, 0)

    def _calculate_distance_modifier(self, distance: int) -> int:
        """Calculate the distance modifier based on distance in cm."""
        return 3 if distance >= 70 else 0