"""Data models for the wargaming scenario."""

from enum import Enum
from typing import Optional
from pydantic import BaseModel, Field


class Pose(str, Enum):
    """Pose enumeration for toy soldiers."""
    STANDING = "standing"
    CROUCHED = "crouched"
    PRONE = "prone"


class Weapon(str, Enum):
    """Weapon enumeration for toy soldiers."""
    PISTOL = "pistol"
    MACHINE_GUN = "machine_gun"
    SMG = "SMG"
    RIFLE = "rifle"


class Coordinates(BaseModel):
    """Coordinates of a toy soldier in the image."""
    x: int = Field(description="X coordinate relative to upper-left corner")
    y: int = Field(description="Y coordinate relative to upper-left corner")


class Characteristics(BaseModel):
    """Characteristics of a toy soldier."""
    pose: Pose = Field(description="The pose of the toy soldier")
    weapon: Weapon = Field(description="The weapon of the toy soldier")
    coordinates: Optional[Coordinates] = Field(None, description="Position in the image")


class Distance(BaseModel):
    """Distance between firing and target toy soldiers."""
    value: int = Field(description="Distance value")
    unit: str = Field(description="Distance unit (typically 'cm')")
    estimated: bool = Field(description="Whether the distance is estimated")


class ScenarioModel(BaseModel):
    """The wargame scenario model."""
    firing: Characteristics = Field(description="The characteristics of the firing toy soldier")
    target: Characteristics = Field(description="The characteristics of the target toy soldier")
    distance: Distance = Field(description="The distance between the firing and target toy soldiers")


class ScenarioOutcome(BaseModel):
    """The outcome of a wargame scenario."""
    firing_modifier: int = Field(alias="firing-modifier", description="The modifier of the firing toy soldier")
    target_modifier: int = Field(alias="target-modifier", description="The modifier of the target toy soldier")
    distance_modifier: int = Field(alias="distance-modifier", description="The modifier of the distance")
    rolled_dice: int = Field(alias="rolled-dice", description="The virtual rolled dice result")
    hit_or_miss: bool = Field(alias="hit-or-miss", description="Whether the shot hit or missed")

    class Config:
        populate_by_name = True


class CombatResult(BaseModel):
    """Combined result containing both scenario and outcome."""
    scenario: ScenarioModel = Field(description="The identified wargame scenario")
    outcome: ScenarioOutcome = Field(description="The calculated outcome")