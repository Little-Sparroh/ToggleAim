using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Pigeon.Movement;  // Namespace from the code

[BepInPlugin("com.yourname.mycopunk.toggleaim", "ToggleAim", "1.0.0")]
[MycoMod(null, ModFlags.IsClientSide)]
public class AimTogglePlugin : BaseUnityPlugin
{
    internal static ConfigEntry<bool> enableToggle;
    internal static bool isAimToggled = false;
    internal static InputAction aimAction;

    private void Awake()
    {
        enableToggle = Config.Bind("General", "EnableAimToggle", true, "If true, aim becomes a toggle (press to enter/exit) instead of hold.");

        AimPatches.isAimInputHeldField = AccessTools.Field(typeof(Gun), "isAimInputHeld");
        if (AimPatches.isAimInputHeldField == null)
        {
            //Logger.LogError("isAimInputHeld field not found in Gun class. Mod may not work.");
        }

        AimPatches.lastPressedAimTimeField = AccessTools.Field(typeof(Gun), "lastPressedAimTime");
        if (AimPatches.lastPressedAimTimeField == null)
        {
            //Logger.LogError("lastPressedAimTime field not found in Gun class. Mod may not work.");
        }

        AimPatches.lastPressedFireTimeField = AccessTools.Field(typeof(Gun), "lastPressedFireTime");
        if (AimPatches.lastPressedFireTimeField == null)
        {
            //Logger.LogError("lastPressedFireTime field not found in Gun class. Mod may not work.");
        }

        AimPatches.playerField = AccessTools.Field(typeof(Gun), "player");
        if (AimPatches.playerField == null)
        {
            //Logger.LogError("player field not found in Gun class. Mod may not work.");
        }

        AimPatches.isAimingGetter = AccessTools.PropertyGetter(typeof(Gun), "IsAiming");
        if (AimPatches.isAimingGetter == null)
        {
            //Logger.LogError("IsAiming getter not found in Gun class. Mod may not work.");
        }

        AimPatches.wantsToFireGetter = AccessTools.PropertyGetter(typeof(Gun), "WantsToFire");
        if (AimPatches.wantsToFireGetter == null)
        {
            //Logger.LogError("WantsToFire getter not found in Gun class. Mod may not work.");
        }

        AimPatches.lastFireTimeGetter = AccessTools.PropertyGetter(typeof(Gun), "LastFireTime");
        if (AimPatches.lastFireTimeGetter == null)
        {
            //Logger.LogError("LastFireTime getter not found in Gun class. Mod may not work.");
        }

        var harmony = new Harmony("com.yourname.mycopunk.toggleaim");

        Logger.LogInfo($"{harmony.Id} loaded!");

        // Patch PlayerInput.Initialize to subscribe after init
        MethodInfo playerInputInit = AccessTools.Method(typeof(PlayerInput), "Initialize");
        if (playerInputInit != null)
        {
            harmony.Patch(playerInputInit, postfix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.PlayerInputInitializePostfix)));
        }

        // Patch Gun.OnAimInputPerformed to skip when toggle enabled
        MethodInfo onAimPerformedMethod = AccessTools.Method(typeof(Gun), "OnAimInputPerformed");
        if (onAimPerformedMethod != null)
        {
            harmony.Patch(onAimPerformedMethod, prefix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.SkipPrefix)));
        }
        else
        {
            //Logger.LogWarning("Could not find Gun.OnAimInputPerformed to patch.");
        }

        // Patch Gun.OnAimInputCancelled to skip when toggle enabled
        MethodInfo onAimCancelledMethod = AccessTools.Method(typeof(Gun), "OnAimInputCancelled");
        if (onAimCancelledMethod != null)
        {
            harmony.Patch(onAimCancelledMethod, prefix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.SkipPrefix)));
        }
        else
        {
            //Logger.LogWarning("Could not find Gun.OnAimInputCancelled to patch.");
        }

        // Patch Gun.HandleAim to force isAimInputHeld and update lastPressedAimTime when toggle enabled
        MethodInfo handleAimMethod = AccessTools.Method(typeof(Gun), "HandleAim");
        if (handleAimMethod != null)
        {
            harmony.Patch(handleAimMethod, prefix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.HandleAimPrefix)));
        }
        else
        {
            //Logger.LogError("Could not find Gun.HandleAim to patch. Mod may not work.");
        }

        // Patch Gun.Update to handle sprint/resume logic when toggle enabled
        MethodInfo updateMethod = AccessTools.Method(typeof(Gun), "Update");
        if (updateMethod != null)
        {
            harmony.Patch(updateMethod, postfix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.UpdatePostfix)));
        }
        else
        {
            //Logger.LogWarning("Could not find Gun.Update to patch. Sprint handling may not work.");
        }

        // Patch Player.Resurrect_ClientRpc to reset toggle on resurrect
        MethodInfo resurrectMethod = AccessTools.Method(typeof(Player), "Resurrect_ClientRpc");
        if (resurrectMethod != null)
        {
            harmony.Patch(resurrectMethod, postfix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.ResetTogglePostfix)));
        }
        else
        {
            //Logger.LogWarning("Could not find Player.Resurrect_ClientRpc to patch. Toggle may not reset on resurrect.");
        }
    }

    private void OnDestroy()
    {
        if (aimAction != null)
        {
            aimAction.started -= OnAimStarted;
        }
    }

    internal static void OnAimStarted(InputAction.CallbackContext context)
    {
        if (enableToggle.Value)
        {
            isAimToggled = !isAimToggled;
            //BepInEx.Logging.ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("AimToggle");
            //log.LogInfo($"Toggled aim: {isAimToggled}");
        }
    }
}

internal class AimPatches
{
    internal static FieldInfo isAimInputHeldField;
    internal static FieldInfo lastPressedAimTimeField;
    internal static FieldInfo lastPressedFireTimeField;
    internal static FieldInfo playerField;
    internal static MethodInfo isAimingGetter;
    internal static MethodInfo wantsToFireGetter;
    internal static MethodInfo lastFireTimeGetter;

    public static void PlayerInputInitializePostfix()
    {
        // Alternative subscription point if coroutine fails
        AimTogglePlugin.aimAction = PlayerInput.Controls?.Player.Aim;
        if (AimTogglePlugin.aimAction != null && AimTogglePlugin.enableToggle.Value)
        {
            AimTogglePlugin.aimAction.started += AimTogglePlugin.OnAimStarted;
        }
    }

    public static bool SkipPrefix()
    {
        return !AimTogglePlugin.enableToggle.Value;
    }

    public static void HandleAimPrefix(Gun __instance)
    {
        if (AimTogglePlugin.enableToggle.Value && isAimInputHeldField != null)
        {
            bool prevHeld = (bool)isAimInputHeldField.GetValue(__instance);
            isAimInputHeldField.SetValue(__instance, AimTogglePlugin.isAimToggled);
            if (AimTogglePlugin.isAimToggled && lastPressedAimTimeField != null)
            {
                lastPressedAimTimeField.SetValue(__instance, Time.time);
            }
            //BepInEx.Logging.ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("AimToggle");
            //log.LogInfo($"HandleAimPrefix: set isAimInputHeld to {AimTogglePlugin.isAimToggled} (was {prevHeld}), lastPressedAimTime={Time.time}");
        }
    }

    public static void UpdatePostfix(Gun __instance)
    {
        if (AimTogglePlugin.enableToggle.Value && isAimingGetter != null && wantsToFireGetter != null && lastFireTimeGetter != null && lastPressedFireTimeField != null && playerField != null)
        {
            bool isAiming = (bool)isAimingGetter.Invoke(__instance, null);
            bool wantsToFire = (bool)wantsToFireGetter.Invoke(__instance, null);
            float lastFireTime = (float)lastFireTimeGetter.Invoke(__instance, null);
            float lastPressedFireTime = (float)lastPressedFireTimeField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null && !isAiming && !wantsToFire && Time.time - Mathf.Max(lastFireTime, lastPressedFireTime) > 0.5f)
            {
                player.ResumeSprint();  // Resume sprint if not aiming/firing
                //BepInEx.Logging.ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("AimToggle");
                //log.LogInfo("UpdatePostfix: Resumed sprint due to toggle.");
            }
        }
    }

    public static void ResetTogglePostfix()
    {
        AimTogglePlugin.isAimToggled = false;
    }
}