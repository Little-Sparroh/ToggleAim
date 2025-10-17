using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Pigeon.Movement;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class AimTogglePlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.toggleaim";
    public const string PluginName = "ToggleAim";
    public const string PluginVersion = "1.0.1";

    internal static ConfigEntry<bool> enableToggle;
    internal static bool isAimToggled = false;
    internal static InputAction aimAction;

    private void Awake()
    {
        enableToggle = Config.Bind("General", "EnableAimToggle", true, "If true, aim becomes a toggle (press to enter/exit) instead of hold.");

        AimPatches.isAimInputHeldField = AccessTools.Field(typeof(Gun), "isAimInputHeld");
        AimPatches.lastPressedAimTimeField = AccessTools.Field(typeof(Gun), "lastPressedAimTime");
        AimPatches.lastPressedFireTimeField = AccessTools.Field(typeof(Gun), "lastPressedFireTime");
        AimPatches.playerField = AccessTools.Field(typeof(Gun), "player");
        AimPatches.isAimingGetter = AccessTools.PropertyGetter(typeof(Gun), "IsAiming");
        AimPatches.wantsToFireGetter = AccessTools.PropertyGetter(typeof(Gun), "WantsToFire");
        AimPatches.lastFireTimeGetter = AccessTools.PropertyGetter(typeof(Gun), "LastFireTime");

        var harmony = new Harmony(PluginGUID);
        Logger.LogInfo($"{PluginName} loaded successfully.");

        MethodInfo playerInputInit = AccessTools.Method(typeof(PlayerInput), "Initialize");
        harmony.Patch(playerInputInit, postfix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.PlayerInputInitializePostfix)));

        MethodInfo onAimPerformedMethod = AccessTools.Method(typeof(Gun), "OnAimInputPerformed");
        harmony.Patch(onAimPerformedMethod, prefix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.SkipPrefix)));

        MethodInfo onAimCancelledMethod = AccessTools.Method(typeof(Gun), "OnAimInputCancelled");
        harmony.Patch(onAimCancelledMethod, prefix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.SkipPrefix)));

        MethodInfo handleAimMethod = AccessTools.Method(typeof(Gun), "HandleAim");
        harmony.Patch(handleAimMethod, prefix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.HandleAimPrefix)));

        MethodInfo updateMethod = AccessTools.Method(typeof(Gun), "Update");
        harmony.Patch(updateMethod, postfix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.UpdatePostfix)));

        MethodInfo resurrectMethod = AccessTools.Method(typeof(Player), "Resurrect_ClientRpc");
        harmony.Patch(resurrectMethod, postfix: new HarmonyMethod(typeof(AimPatches), nameof(AimPatches.ResetTogglePostfix)));
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
        if (AimTogglePlugin.enableToggle.Value)
        {
            bool prevHeld = (bool)isAimInputHeldField.GetValue(__instance);
            isAimInputHeldField.SetValue(__instance, AimTogglePlugin.isAimToggled);
            if (AimTogglePlugin.isAimToggled)
            {
                lastPressedAimTimeField.SetValue(__instance, Time.time);
            }
        }
    }

    public static void UpdatePostfix(Gun __instance)
    {
        if (AimTogglePlugin.enableToggle.Value)
        {
            bool isAiming = (bool)isAimingGetter.Invoke(__instance, null);
            bool wantsToFire = (bool)wantsToFireGetter.Invoke(__instance, null);
            float lastFireTime = (float)lastFireTimeGetter.Invoke(__instance, null);
            float lastPressedFireTime = (float)lastPressedFireTimeField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null && !isAiming && !wantsToFire && Time.time - Mathf.Max(lastFireTime, lastPressedFireTime) > 0.5f)
            {
                player.ResumeSprint();
            }
        }
    }

    public static void ResetTogglePostfix()
    {
        AimTogglePlugin.isAimToggled = false;
    }
}
