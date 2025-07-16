using System;

namespace Utilla.Attributes
{
    /// <summary>
    /// This attribute marks a method to be called when a modded gamemode starts.
    /// </summary>
    /// <remarks>
    /// The method must either take no arguments, or a string for the gamemode.
    /// Use <c>String.Contains</c> to test if a lobby is of a specific gamemode.
    /// This is called after the gamemode has been initialized and is ready to play.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodeStartAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute marks a method to be called when a modded gamemode ends.
    /// </summary>
    /// <remarks>
    /// The method must either take no arguments, or a string for the gamemode.
    /// Use <c>String.Contains</c> to test if a lobby is of a specific gamemode.
    /// This is called when the gamemode is stopping or transitioning to another gamemode.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodeEndAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute marks a method to be called when a player joins a modded gamemode.
    /// </summary>
    /// <remarks>
    /// The method must either take no arguments, a string for the gamemode, or both a string for the gamemode and a Player object.
    /// Use <c>String.Contains</c> to test if a lobby is of a specific gamemode.
    /// The Player object represents the player who joined the gamemode.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodePlayerJoinAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute marks a method to be called when a player leaves a modded gamemode.
    /// </summary>
    /// <remarks>
    /// The method must either take no arguments, a string for the gamemode, or both a string for the gamemode and a Player object.
    /// Use <c>String.Contains</c> to test if a lobby is of a specific gamemode.
    /// The Player object represents the player who left the gamemode.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodePlayerLeaveAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute marks a method to be called every frame while in a modded gamemode.
    /// </summary>
    /// <remarks>
    /// The method must either take no arguments, or a string for the gamemode.
    /// Use <c>String.Contains</c> to test if a lobby is of a specific gamemode.
    /// This is called every frame while the gamemode is active, similar to Unity's Update method.
    /// Be careful with performance-intensive operations in this method.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodeUpdateAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute marks a method to be called when a player tags another player in a modded gamemode.
    /// </summary>
    /// <remarks>
    /// The method must either take no arguments, a string for the gamemode, or both a string for the gamemode and two Player objects.
    /// Use <c>String.Contains</c> to test if a lobby is of a specific gamemode.
    /// The first Player object represents the player who did the tagging, the second represents the player who was tagged.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodePlayerTagAttribute : Attribute
    {
    }
} 