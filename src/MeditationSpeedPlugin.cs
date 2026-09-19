using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MeditationSpeed
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class MeditationSpeedPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.gyk.meditationspeed";
        public const string PluginName = "Meditation Speed";
        public const string PluginVersion = "0.1.0";

        private Harmony _harmony;

        private void Awake()
        {
            try
            {
                RuntimeBindings.Initialize();
                MeditationSession.Initialize();

                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();

                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize " + PluginName + ": " + ex);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            try
            {
                MeditationSession.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Meditation timing cleanup failed: " + ex.Message);
            }
            finally
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
        }
    }

    internal static class MeditationSession
    {
        private const float NormalTimeScale = 1f;
        private const float NormalFixedDelta = 0.016666668f;

        private const float VanillaMeditationTimeScale = 10f;
        private const float VanillaMeditationFixedDelta = 0.083333336f;

        private const float TipScaleMultiplier = 1.25f;

        private static readonly float[] TimeScales = { 10f, 20f, 40f };
        private static readonly float[] FixedDeltas = { 0.083333336f, 0.16666667f, 0.33333334f };
        private static readonly string[] SpeedLabels = { "1×", "2×", "4×" };

        private static object _waitingGui;
        private static int _speedIndex;
        private static string _vanillaWakeText = string.Empty;
        private static Vector3 _originalTipScale;
        private static bool _hasOriginalTipScale;
        private static bool _stopInProgress;

        internal static void Initialize()
        {
            ClearSessionState();
        }

        internal static void OnWaitingTipPrinted(object buttonTips)
        {
            object activeGui = RuntimeBindings.GetActiveGui();
            if (!RuntimeBindings.IsWaitingGui(activeGui) ||
                !RuntimeBindings.IsWaitingState(activeGui) ||
                !ReferenceEquals(buttonTips, RuntimeBindings.GetButtonTips(activeGui)))
            {
                return;
            }

            string vanillaText = RuntimeBindings.GetButtonTipText(buttonTips) ?? string.Empty;

            if (ReferenceEquals(_waitingGui, activeGui))
            {
                // If vanilla refreshes its own tip during one waiting session,
                // retain the selected speed and only refresh the captured wake text.
                _vanillaWakeText = vanillaText;
                DrawTip();
                return;
            }

            RestoreTipPresentation();

            _waitingGui = activeGui;
            _speedIndex = 0;
            _stopInProgress = false;
            _vanillaWakeText = vanillaText;

            CaptureAndEnlargeTip(buttonTips);
            ApplySelectedTiming();
            DrawTip();
        }

        internal static bool HandleSlider(object instance, int direction)
        {
            if (!IsActiveWaitingGui(instance))
                return false;

            int next = Mathf.Clamp(_speedIndex + direction, 0, TimeScales.Length - 1);
            if (next != _speedIndex)
            {
                _speedIndex = next;
                ApplySelectedTiming();
                DrawTip();
            }

            // Consume the semantic slider key even at the bounds.
            return true;
        }

        internal static bool ShouldSuppressNavigation(object instance)
        {
            // SliderDec/SliderInc share the same D-pad physical bindings with
            // Left/Right. WaitingGUI has no useful vanilla directional navigation,
            // so suppress only its paired navigation action.
            return IsActiveWaitingGui(instance);
        }

        internal static void OnStopWaitingPrefix(object instance)
        {
            if (ReferenceEquals(instance, _waitingGui))
                _stopInProgress = true;
        }

        internal static void OnStopWaitingPostfix(object instance)
        {
            if (!ReferenceEquals(instance, _waitingGui))
                return;

            // Vanilla StopWaiting is the primary timing owner and has already
            // restored timeScale/fixedDeltaTime at this point.
            RestoreTipPresentation();
            ClearSessionState();
        }

        internal static void OnGuiHidden(object instance)
        {
            if (!ReferenceEquals(instance, _waitingGui) || _stopInProgress)
                return;

            // Defensive path only: if WaitingGUI is hidden without StopWaiting,
            // prevent this mod's accelerated global timing from leaking outside it.
            if (OwnsSelectedTiming())
            {
                Time.timeScale = NormalTimeScale;
                Time.fixedDeltaTime = NormalFixedDelta;
            }

            RestoreTipPresentation();
            ClearSessionState();
        }

        internal static void Shutdown()
        {
            if (_waitingGui == null)
                return;

            if (OwnsSelectedTiming())
            {
                if (RuntimeBindings.IsWaitingState(_waitingGui) &&
                    ReferenceEquals(_waitingGui, RuntimeBindings.GetActiveGui()))
                {
                    // Hand an active waiting session back to vanilla semantics.
                    Time.timeScale = VanillaMeditationTimeScale;
                    Time.fixedDeltaTime = VanillaMeditationFixedDelta;
                }
                else
                {
                    Time.timeScale = NormalTimeScale;
                    Time.fixedDeltaTime = NormalFixedDelta;
                }
            }

            RestoreVanillaWakeText();
            RestoreTipPresentation();
            ClearSessionState();
        }

        private static bool IsActiveWaitingGui(object instance)
        {
            return _waitingGui != null &&
                   ReferenceEquals(instance, _waitingGui) &&
                   ReferenceEquals(instance, RuntimeBindings.GetActiveGui()) &&
                   RuntimeBindings.IsWaitingState(instance);
        }

        private static void ApplySelectedTiming()
        {
            Time.timeScale = TimeScales[_speedIndex];
            Time.fixedDeltaTime = FixedDeltas[_speedIndex];
        }

        private static bool OwnsSelectedTiming()
        {
            return Approximately(Time.timeScale, TimeScales[_speedIndex]) &&
                   Approximately(Time.fixedDeltaTime, FixedDeltas[_speedIndex]);
        }

        private static void DrawTip()
        {
            if (_waitingGui == null)
                return;

            object tips = RuntimeBindings.GetButtonTips(_waitingGui);
            if (tips == null)
                return;

            string decIcon = RuntimeBindings.GetGameKeyIcon("SliderDec");
            string incIcon = RuntimeBindings.GetGameKeyIcon("SliderInc");

            string speedPart = decIcon + "  " + SpeedLabels[_speedIndex] + "  " + incIcon;
            string text = string.IsNullOrEmpty(_vanillaWakeText)
                ? speedPart
                : speedPart + "     " + _vanillaWakeText;

            RuntimeBindings.SetButtonTipText(tips, text);
        }

        private static void CaptureAndEnlargeTip(object buttonTips)
        {
            Component label = RuntimeBindings.GetButtonTipLabelComponent(buttonTips);
            if (label == null)
                return;

            _originalTipScale = label.transform.localScale;
            _hasOriginalTipScale = true;
            label.transform.localScale = _originalTipScale * TipScaleMultiplier;
        }

        private static void RestoreVanillaWakeText()
        {
            if (_waitingGui == null)
                return;

            object tips = RuntimeBindings.GetButtonTips(_waitingGui);
            if (tips != null)
                RuntimeBindings.SetButtonTipText(tips, _vanillaWakeText);
        }

        private static void RestoreTipPresentation()
        {
            if (!_hasOriginalTipScale || _waitingGui == null)
                return;

            object tips = RuntimeBindings.GetButtonTips(_waitingGui);
            Component label = RuntimeBindings.GetButtonTipLabelComponent(tips);
            if (label != null)
                label.transform.localScale = _originalTipScale;

            _hasOriginalTipScale = false;
        }

        private static void ClearSessionState()
        {
            _waitingGui = null;
            _speedIndex = 0;
            _vanillaWakeText = string.Empty;
            _originalTipScale = Vector3.one;
            _hasOriginalTipScale = false;
            _stopInProgress = false;
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.00001f;
        }
    }

    internal static class RuntimeBindings
    {
        internal static Type WaitingGuiType { get; private set; }
        internal static Type BaseGuiType { get; private set; }
        internal static Type ButtonTipsType { get; private set; }
        internal static Type GameKeyTipType { get; private set; }
        internal static Type GameKeyType { get; private set; }

        private static PropertyInfo _activeGuiProperty;
        private static PropertyInfo _buttonTipsProperty;
        private static FieldInfo _waitingStateField;
        private static FieldInfo _buttonTipsLabelField;
        private static PropertyInfo _labelTextProperty;
        private static MethodInfo _getIconMethod;

        internal static void Initialize()
        {
            WaitingGuiType = RequireType("WaitingGUI");
            BaseGuiType = RequireType("BaseGUI");
            ButtonTipsType = RequireType("ButtonTipsStr");
            GameKeyTipType = RequireType("GameKeyTip");
            GameKeyType = RequireType("GameKey");

            _activeGuiProperty = AccessTools.Property(BaseGuiType, "active_gui")
                ?? throw new MissingMemberException(BaseGuiType.FullName, "active_gui");

            _buttonTipsProperty = AccessTools.Property(BaseGuiType, "button_tips")
                ?? throw new MissingMemberException(BaseGuiType.FullName, "button_tips");

            _waitingStateField = AccessTools.Field(WaitingGuiType, "_state")
                ?? throw new MissingFieldException(WaitingGuiType.FullName, "_state");

            _buttonTipsLabelField = AccessTools.Field(ButtonTipsType, "label")
                ?? throw new MissingFieldException(ButtonTipsType.FullName, "label");

            Type uiLabelType = RequireType("UILabel");
            _labelTextProperty = AccessTools.Property(uiLabelType, "text")
                ?? throw new MissingMemberException(uiLabelType.FullName, "text");

            _getIconMethod = AccessTools.Method(GameKeyTipType, "GetIcon", new[] { GameKeyType })
                ?? throw new MissingMethodException(GameKeyTipType.FullName, "GetIcon");
        }

        internal static object GetActiveGui()
        {
            return _activeGuiProperty.GetValue(null, null);
        }

        internal static bool IsWaitingGui(object instance)
        {
            return instance != null && WaitingGuiType.IsInstanceOfType(instance);
        }

        internal static bool IsWaitingState(object instance)
        {
            if (!IsWaitingGui(instance))
                return false;

            object state = _waitingStateField.GetValue(instance);
            return state != null &&
                   string.Equals(state.ToString(), "Waiting", StringComparison.Ordinal);
        }

        internal static object GetButtonTips(object gui)
        {
            return gui == null ? null : _buttonTipsProperty.GetValue(gui, null);
        }

        internal static Component GetButtonTipLabelComponent(object buttonTips)
        {
            if (buttonTips == null)
                return null;

            return _buttonTipsLabelField.GetValue(buttonTips) as Component;
        }

        internal static string GetButtonTipText(object buttonTips)
        {
            Component label = GetButtonTipLabelComponent(buttonTips);
            if (label == null)
                return null;

            return _labelTextProperty.GetValue(label, null) as string;
        }

        internal static void SetButtonTipText(object buttonTips, string text)
        {
            Component label = GetButtonTipLabelComponent(buttonTips);
            if (label != null)
                _labelTextProperty.SetValue(label, text, null);
        }

        internal static string GetGameKeyIcon(string keyName)
        {
            try
            {
                object key = Enum.Parse(GameKeyType, keyName);
                return (_getIconMethod.Invoke(null, new[] { key }) as string) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static Type RequireType(string name)
        {
            return AccessTools.TypeByName(name)
                ?? throw new TypeLoadException("Could not find game type '" + name + "'.");
        }
    }

    [HarmonyPatch]
    internal static class WaitingTipPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                       RuntimeBindings.ButtonTipsType,
                       "Print",
                       new[] { RuntimeBindings.GameKeyTipType })
                   ?? throw new MissingMethodException(
                       RuntimeBindings.ButtonTipsType.FullName,
                       "Print(GameKeyTip)");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            MeditationSession.OnWaitingTipPrinted(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SliderDecPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "OnPressedSliderDec")
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "OnPressedSliderDec");
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (!MeditationSession.HandleSlider(__instance, -1))
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class SliderIncPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "OnPressedSliderInc")
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "OnPressedSliderInc");
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (!MeditationSession.HandleSlider(__instance, 1))
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class LeftNavigationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "OnPressedLeft")
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "OnPressedLeft");
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (!MeditationSession.ShouldSuppressNavigation(__instance))
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class RightNavigationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "OnPressedRight")
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "OnPressedRight");
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (!MeditationSession.ShouldSuppressNavigation(__instance))
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class StopWaitingPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.WaitingGuiType, "StopWaiting")
                ?? throw new MissingMethodException(RuntimeBindings.WaitingGuiType.FullName, "StopWaiting");
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance)
        {
            MeditationSession.OnStopWaitingPrefix(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            MeditationSession.OnStopWaitingPostfix(__instance);
        }
    }

    [HarmonyPatch]
    internal static class WaitingHideSafetyPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "Hide", new[] { typeof(bool) })
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "Hide(bool)");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            if (RuntimeBindings.IsWaitingGui(__instance))
                MeditationSession.OnGuiHidden(__instance);
        }
    }
}
