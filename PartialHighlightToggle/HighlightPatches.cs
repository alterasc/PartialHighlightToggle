using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.GameModes;
using Kingmaker.View;
using Owlcat.Runtime.UniRx;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PartialHighlightToggle;

/// <summary>
/// Harmony patches required for partial highlighting to work
/// </summary>
[HarmonyPatch]
public static class HighlightPatches
{
    /// <summary>
    /// After activating InteractionHighlightController
    /// maybe restores highlight state
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionHighlightController), nameof(InteractionHighlightController.Activate))]
    private static void AfterActivate(InteractionHighlightController __instance)
    {
        HighlightManager.AfterControllerActivate(__instance);
    }

    /// <summary>
    /// Redirects call to implementation that takes into account highlight levels
    /// </summary>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionHighlightController), nameof(InteractionHighlightController.HighlightOn))]
    private static bool HighlightOnReplacer()
    {
        HighlightManager.FullHighlightOn();
        return false;
    }

    /// <summary>
    /// Redirects call to implementation that takes into account highlight levels
    /// </summary>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionHighlightController), nameof(InteractionHighlightController.HighlightOff))]
    private static bool HighlightOffReplacer()
    {
        HighlightManager.FullHighlightOff();
        return false;
    }

    /// <summary>
    /// Disables hightlight in cutscenes and dialogues
    /// Has to be prefix, because in process of starting these mods highlight controller
    /// is set to be inactive and Game.Instace field is null
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.DoStartMode))]
    private static void After_Game_DoStartMode(Game __instance, GameModeType type)
    {
        if (type == GameModeType.Cutscene
            || type == GameModeType.Dialog
            || type == GameModeType.CutsceneGlobalMap)
        {
            if (!(__instance.CurrentMode == GameModeType.Cutscene
                  || __instance.CurrentMode == GameModeType.Dialog
                  || __instance.CurrentMode == GameModeType.CutsceneGlobalMap))
            {
                HighlightManager.SuppressPassiveHighlight();
            }
        }
    }

    /// <summary>
    /// Restores highlight when cutscenes and dialogues end
    /// </summary>
    /// <param name="oldMode"></param>
    /// <param name="newMode"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Game), nameof(Game.HandleGameModeChanged))]
    private static void After_Game_HandleGameModeChanged(GameModeType oldMode, GameModeType newMode)
    {
        if (!(newMode == GameModeType.Cutscene
              || newMode == GameModeType.Dialog
              || newMode == GameModeType.CutsceneGlobalMap))
        {
            HighlightManager.RestorePassiveHighlight();
        }
    }

    /// <summary>
    /// Original method that disables all highlighting.
    /// </summary>
    /// <param name="instance"></param>
    /// <exception cref="NotImplementedException"></exception>
    [HarmonyReversePatch(HarmonyReversePatchType.Original)]
    [HarmonyPatch(typeof(InteractionHighlightController), nameof(InteractionHighlightController.HighlightOff))]
    public static void OriginalHighlightOff(InteractionHighlightController instance)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException("It's a stub");
    }

    /// <summary>
    /// Disables highlight on combat start, enables back on combat end
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameHistoryLog), nameof(GameHistoryLog.HandlePartyCombatStateChanged))]
    private static void DisableOnCombatStart(bool inCombat)
    {
        if (inCombat)
        {
            HighlightManager.SuppressPassiveHighlight();
        }
        else
        {
            HighlightManager.RestorePassiveHighlight();
        }
    }

    /// <summary>
    /// Support for highlight being on by default.
    /// 
    /// Delay is to make action run when game actually loaded 
    /// and controller can do highlighting.
    /// Minimum that worked for me is 2 seconds, so 4 is chosen 
    /// to have some safety margin.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.OnAreaLoaded))]
    private static void AfterAreaLoad()
    {
        DelayedInvoker.InvokeInTime(delegate
        {
            HighlightManager.RefreshHighlight();
        }, 4);
    }

    /// <summary>
    /// Replaces normal check for highlight status with check that accounts
    /// for basic/full level of highlight and unit status
    /// </summary>
    /// <param name="instructions"></param>
    /// <returns></returns>
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UnitEntityView), nameof(UnitEntityView.UpdateHighlight))]
    private static IEnumerable<CodeInstruction> ReplaceHighlightCheckForUnit(IEnumerable<CodeInstruction> instructions)
    {
        var highlightGetter = AccessTools.PropertyGetter(typeof(InteractionHighlightController), nameof(InteractionHighlightController.IsHighlighting));
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(highlightGetter))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(HighlightManager), nameof(HighlightManager.UnitHighlight));
            }
            else
            {
                yield return instruction;
            }
        }
    }
}

