using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2TurnTimer.UI;

namespace STS2TurnTimer.Patches;

/// <summary>
/// Patches NEndTurnButton to inject the turn timer bar and drive its state
/// based on CombatManager events.
/// </summary>
public static class EndTurnButtonPatches
{
    /// <summary>
    /// Checks if all non-local alive players have ended their turn.
    /// Dead players are skipped since they can't act.
    /// </summary>
    private static bool AreAllOtherPlayersReady(CombatState combatState)
    {
        var localPlayer = LocalContext.GetMe(combatState);
        if (localPlayer == null) return false;

        foreach (var player in combatState.Players)
        {
            if (LocalContext.IsMe(player)) continue;
            if (player.Creature.IsDead) continue;
            if (!CombatManager.Instance.IsPlayerReadyToEndTurn(player))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true if the game is in multiplayer (more than 1 player).
    /// </summary>
    private static bool IsMultiplayer(CombatState combatState)
    {
        return combatState.Players.Count > 1;
    }

    /// <summary>
    /// Returns true if the local player is the only alive player.
    /// No point running a timer if there's nobody to wait for.
    /// </summary>
    private static bool IsOnlyAlivePlayer(CombatState combatState)
    {
        var localPlayer = LocalContext.GetMe(combatState);
        if (localPlayer == null) return false;

        foreach (var player in combatState.Players)
        {
            if (LocalContext.IsMe(player)) continue;
            if (player.Creature.IsAlive) return false;
        }
        return true;
    }

    /// <summary>
    /// Adds the timer bar as a child of NEndTurnButton when combat initializes.
    /// </summary>
    [HarmonyPatch(typeof(NEndTurnButton), "Initialize")]
    public static class InitializePatch
    {
        [HarmonyPostfix]
        public static void Postfix(NEndTurnButton __instance, CombatState state)
        {
            try
            {
                // Remove any existing timer bar (in case of re-initialization)
                var existing = __instance.GetNodeOrNull("TurnTimerBar");
                if (existing != null)
                    existing.QueueFree();

                // Only add in multiplayer
                if (!IsMultiplayer(state)) return;

                var timerBar = new NTurnTimerBar();
                timerBar.Name = "TurnTimerBar";

                // Center the bar below the End Turn button.
                // The button's Visuals/Image is ~280px wide, centered around x≈140.
                // We position the bar so its center aligns with the button center.
                var visuals = __instance.GetNodeOrNull<Control>("Visuals");
                float btnCenterX = visuals != null
                    ? visuals.Position.X + visuals.Size.X / 2f
                    : 140f;
                float barWidth = 260f; // matches NTurnTimerBar.BarWidth
                timerBar.Position = new Vector2(btnCenterX - barWidth / 2f, 105f);
                __instance.AddChild(timerBar);

                MainFile.Logger.Info("[EndTurnButtonPatches] Timer bar added to NEndTurnButton");
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Error($"[EndTurnButtonPatches] Failed to add timer bar: {e}");
            }
        }
    }

    /// <summary>
    /// After a player ends their turn, check if the timer should start.
    /// </summary>
    [HarmonyPatch(typeof(NEndTurnButton), "AfterPlayerEndedTurn")]
    public static class PlayerEndedTurnPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player player, bool canBackOut)
        {
            try
            {
                var timerBar = NTurnTimerBar.Instance;
                if (timerBar == null) return;

                var combatState = player.Creature.CombatState;
                if (combatState == null) return;

                var localPlayer = LocalContext.GetMe(combatState);
                if (localPlayer == null) return;

                // If local player just ended their turn, stop timer (they're done)
                if (LocalContext.IsMe(player))
                {
                    timerBar.StopTimer();
                    return;
                }

                // If local player has already ended turn, no timer needed
                if (CombatManager.Instance.IsPlayerReadyToEndTurn(localPlayer))
                    return;

                // If all players are ready (shouldn't happen since local hasn't ended),
                // don't start timer
                if (CombatManager.Instance.AllPlayersReadyToEndTurn())
                    return;

                // Don't start timer if we're the only one alive
                if (IsOnlyAlivePlayer(combatState)) return;

                // Check if all OTHER alive players have ended their turn
                if (TimerConfig.StartTimerFromTurnStart || AreAllOtherPlayersReady(combatState))
                {
                    if (!timerBar.IsActive)
                        timerBar.StartTimer();
                }
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Error($"[EndTurnButtonPatches] PlayerEndedTurn error: {e}");
            }
        }
    }

    /// <summary>
    /// After a player un-ends their turn, check if the timer should stop.
    /// </summary>
    [HarmonyPatch(typeof(NEndTurnButton), "AfterPlayerUnendedTurn")]
    public static class PlayerUnendedTurnPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player player)
        {
            try
            {
                var timerBar = NTurnTimerBar.Instance;
                if (timerBar == null) return;

                // If local player un-ended their turn, they're back in action —
                // re-check if others are all still ready
                if (LocalContext.IsMe(player))
                {
                    var combatState = player.Creature.CombatState;
                    if (combatState != null && IsOnlyAlivePlayer(combatState)) return;

                    if (combatState != null && !TimerConfig.StartTimerFromTurnStart)
                    {
                        if (AreAllOtherPlayersReady(combatState))
                        {
                            if (!timerBar.IsActive)
                                timerBar.StartTimer();
                        }
                    }
                    else if (TimerConfig.StartTimerFromTurnStart)
                    {
                        if (!timerBar.IsActive)
                            timerBar.StartTimer();
                    }
                    return;
                }

                // A non-local player un-ended their turn
                if (!TimerConfig.StartTimerFromTurnStart)
                {
                    // Not all others are waiting anymore — stop timer
                    var combatState = player.Creature.CombatState;
                    if (combatState == null || !AreAllOtherPlayersReady(combatState))
                    {
                        timerBar.StopTimer();
                    }
                }
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Error($"[EndTurnButtonPatches] PlayerUnendedTurn error: {e}");
            }
        }
    }

    /// <summary>
    /// When a new turn starts, reset the timer.
    /// </summary>
    [HarmonyPatch(typeof(NEndTurnButton), "OnTurnStarted")]
    public static class TurnStartedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CombatState state)
        {
            try
            {
                var timerBar = NTurnTimerBar.Instance;
                if (timerBar == null) return;

                // Stop any running timer from previous turn
                timerBar.StopTimer();

                // If StartTimerFromTurnStart is on, start immediately on player turn
                // (but not if we're the only one alive)
                if (TimerConfig.StartTimerFromTurnStart && state.CurrentSide == CombatSide.Player)
                {
                    if (IsOnlyAlivePlayer(state)) return;

                    var localPlayer = LocalContext.GetMe(state);
                    if (localPlayer != null && !CombatManager.Instance.IsPlayerReadyToEndTurn(localPlayer))
                    {
                        timerBar.StartTimer();
                    }
                }
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Error($"[EndTurnButtonPatches] TurnStarted error: {e}");
            }
        }
    }

    /// <summary>
    /// When switching to enemy turn, stop the timer.
    /// </summary>
    [HarmonyPatch(typeof(NEndTurnButton), "OnAboutToSwitchToEnemyTurn")]
    public static class AboutToSwitchPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                NTurnTimerBar.Instance?.StopTimer();
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Error($"[EndTurnButtonPatches] AboutToSwitch error: {e}");
            }
        }
    }
}
