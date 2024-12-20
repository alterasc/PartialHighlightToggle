using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.PubSubSystem;
using Kingmaker.UI._ConsoleUI.Overtips;
using Kingmaker.View;

namespace PartialHighlightToggle;

/// <summary>
/// Manager of more complicated highlight state
/// </summary>
public static class HighlightManager
{
    public static bool isBasicHighlightToggledOn = false;
    private static bool isBasicHighlightSuppressed = false;
    private static bool isFullHighlightOn = false;

    private static bool BasicHiglightActive => isBasicHighlightToggledOn && !isBasicHighlightSuppressed;

    public static bool UnitHighlight(InteractionHighlightController _, UnitEntityView view)
    {
        return isFullHighlightOn || BasicHiglightActive && (view?.EntityData?.IsDeadAndHasLoot == true);
    }

    public static void FullHighlightOn()
    {
        isFullHighlightOn = true;
        RefreshHighlight();
    }

    public static void FullHighlightOff()
    {
        isFullHighlightOn = false;
        RefreshHighlight(true);
    }

    /// <summary>
    /// Updates current highlight level
    /// </summary>
    /// <param name="fullToggleOff">Indicates that full highlight is being toggled off</param>
    public static void RefreshHighlight(bool fullToggleOff = false)
    {
        var baseGameController = Game.Instance.InteractionHighlightController;
        if (baseGameController == null || baseGameController.m_Inactive)
        {
            return;
        }
        if (!isFullHighlightOn && !BasicHiglightActive)
        {
            HighlightPatches.OriginalHighlightOff(baseGameController);
            return;
        }
        // disable highlight in combat unless it's full highlight
        if (Game.Instance.Player.IsInCombat && !isFullHighlightOn)
        {
            HighlightPatches.OriginalHighlightOff(baseGameController);
            return;
        }
        if (fullToggleOff)
        {
            // resetting highlighting to hide all objects previously highlighted by full highlight
            HighlightPatches.OriginalHighlightOff(baseGameController);
        }
        baseGameController.m_IsHighlighting = true;
        foreach (var mapObjectEntity in Game.Instance.State.MapObjects)
        {
            mapObjectEntity.View?.UpdateHighlight();
        }
        foreach (var abstractUnitEntity in Game.Instance.State.Units)
        {
            if (isFullHighlightOn || abstractUnitEntity.IsDeadAndHasLoot)
            {
                abstractUnitEntity.View?.UpdateHighlight(false);
            }
        }
        EventBus.RaiseEvent(delegate (IInteractionHighlightUIHandler h)
        {
            if (BasicHiglightActive && !isFullHighlightOn && h is OvertipsVM)
            {

            }
            else
            {
                h.HandleHighlightChange(true);
            }
        }, true);

    }

    public static void AfterControllerActivate(InteractionHighlightController instance)
    {
        if (BasicHiglightActive)
        {
            RefreshHighlight();
        }
    }

    public static void TogglePassiveHighlight()
    {
        isBasicHighlightToggledOn = !isBasicHighlightToggledOn;
        if (Game.Instance.Player.IsInCombat) return;
        RefreshHighlight();
    }

    public static void SuppressPassiveHighlight()
    {
        isBasicHighlightSuppressed = true;
        RefreshHighlight();
    }

    public static void RestorePassiveHighlight()
    {
        isBasicHighlightSuppressed = false;
        RefreshHighlight();
    }
}
