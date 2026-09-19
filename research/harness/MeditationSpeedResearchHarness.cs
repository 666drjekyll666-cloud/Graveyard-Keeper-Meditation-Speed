using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MeditationSpeedResearchHarness
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class MeditationSpeedResearchHarnessPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.gyk.meditationspeed.research";
        public const string PluginName = "Meditation Speed Research Harness";
        public const string PluginVersion = "0.0.1";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            try
            {
                RuntimeBindings.Initialize();
                HarnessState.Initialize();

                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();

                Logger.LogInfo(
                    "MEDITATION_HARNESS status=loaded version=" + PluginVersion +
                    " target=GraveyardKeeper-1.407 save_mutation=false");
            }
            catch (Exception ex)
            {
                Logger.LogError("MEDITATION_HARNESS status=init_failed error=" + ex);
                enabled = false;
            }
        }

        private void Update()
        {
            HarnessState.OnFrame();
        }

        private void OnDestroy()
        {
            try
            {
                HarnessState.OnHarnessDestroy();
            }
            finally
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
        }
    }

    internal enum FixedPolicy
    {
        Proportional,
        VanillaMeditationStep,
        BoundedIntermediate
    }

    internal static class HarnessState
    {
        private const float NormalFixedDelta = 0.016666668f;
        private const float VanillaMeditationFixedDelta = 0.083333336f;
        private const float TwoXFixedDelta = 0.16666667f;
        private const float FourXFixedDelta = 0.33333334f;
        private const float SampleDurationSeconds = 5f;

        private static object _waitingGui;
        private static bool _pendingWaitingStart;
        private static bool _waitingActive;
        private static bool _stopWaitingInProgress;
        private static string _vanillaWakeTip = string.Empty;

        private static int _speedIndex;
        private static FixedPolicy _fixedPolicy = FixedPolicy.Proportional;

        private static bool _sampleActive;
        private static string _sampleName = string.Empty;
        private static float _sampleStartReal;
        private static int _sampleFrames;
        private static int _sampleFixedTicks;
        private static float _sampleWorldStart;
        private static int _sampleDayStart;

        private static string _sampleStatus = "idle";

        internal static void Initialize()
        {
            _waitingGui = null;
            _pendingWaitingStart = false;
            _waitingActive = false;
            _stopWaitingInProgress = false;
            _sampleActive = false;
            _sampleStatus = "idle";
        }

        internal static void OnWaitingOpen(object instance)
        {
            _waitingGui = instance;
            _pendingWaitingStart = true;
            _waitingActive = false;
            _stopWaitingInProgress = false;
            _sampleActive = false;
            _sampleStatus = "waiting-for-vanilla-start";

            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=open" +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime));
        }

        internal static void OnButtonTipsPrinted(object buttonTips)
        {
            if (!_pendingWaitingStart || _waitingGui == null)
                return;

            object expectedTips = RuntimeBindings.GetButtonTips(_waitingGui);
            if (!ReferenceEquals(buttonTips, expectedTips))
                return;

            if (!RuntimeBindings.IsWaitingState(_waitingGui))
                return;

            _pendingWaitingStart = false;
            _waitingActive = true;
            _speedIndex = 0;
            _fixedPolicy = FixedPolicy.Proportional;
            _vanillaWakeTip = RuntimeBindings.GetButtonTipText(buttonTips) ?? string.Empty;

            ApplyCurrentTiming("vanilla-start");
            StartSample("1x-baseline");
            RedrawTip();

            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=waiting_started" +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime) +
                " input=" + InputSource());
        }

        internal static bool HandleSlider(object instance, int direction, string semantic)
        {
            if (!IsOurActiveWaitingGui(instance))
                return false;

            int oldIndex = _speedIndex;
            int next = Mathf.Clamp(_speedIndex + direction, 0, 2);
            _speedIndex = next;

            if (_speedIndex == 2 && oldIndex != 2)
                _fixedPolicy = FixedPolicy.Proportional;

            Log(
                "MEDITATION_INPUT_DIAGNOSTIC semantic=" + semantic +
                " source=" + InputSource() +
                " old_speed=" + SpeedLabel(oldIndex) +
                " new_speed=" + SpeedLabel(_speedIndex) +
                " handled=true");

            if (_speedIndex != oldIndex)
            {
                FinishSample("interrupted-by-speed-change");
                ApplyCurrentTiming("slider");
                StartSample(CurrentSampleName());
            }

            RedrawTip();
            return true;
        }

        internal static void OnNavigationHandler(object instance, string semantic, bool result)
        {
            if (!IsOurActiveWaitingGui(instance))
                return;

            Log(
                "MEDITATION_NAV_DIAGNOSTIC semantic=" + semantic +
                " source=" + InputSource() +
                " result=" + result.ToString().ToLowerInvariant() +
                " speed=" + SpeedLabel(_speedIndex));
        }

        internal static void OnFixedTick()
        {
            if (_sampleActive)
                _sampleFixedTicks++;
        }

        internal static void OnFrame()
        {
            if (!_waitingActive)
                return;

            if (_sampleActive)
            {
                _sampleFrames++;
                float elapsed = Time.realtimeSinceStartup - _sampleStartReal;
                if (elapsed >= SampleDurationSeconds)
                {
                    FinishSample("complete");
                    RedrawTip();
                }
            }

            if (_speedIndex == 2 && Input.GetKeyDown(KeyCode.F8))
            {
                FinishSample("interrupted-by-policy-change");

                if (_fixedPolicy == FixedPolicy.Proportional)
                    _fixedPolicy = FixedPolicy.VanillaMeditationStep;
                else if (_fixedPolicy == FixedPolicy.VanillaMeditationStep)
                    _fixedPolicy = FixedPolicy.BoundedIntermediate;
                else
                    _fixedPolicy = FixedPolicy.Proportional;

                ApplyCurrentTiming("F8-policy-cycle");
                StartSample(CurrentSampleName());
                RedrawTip();

                Log(
                    "MEDITATION_POLICY_DIAGNOSTIC event=cycle" +
                    " policy=" + PolicyLabel() +
                    " timescale=" + F(Time.timeScale) +
                    " fixed=" + F(Time.fixedDeltaTime));
            }
        }

        internal static void OnStopWaitingPrefix(object instance)
        {
            if (!IsOurActiveWaitingGui(instance))
                return;

            _stopWaitingInProgress = true;
            FinishSample("wake");
            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=stop_prefix" +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime));
        }

        internal static void OnStopWaitingPostfix(object instance)
        {
            if (!ReferenceEquals(instance, _waitingGui) || !_stopWaitingInProgress)
                return;

            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=stop_postfix" +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime) +
                " restore_ok=" +
                (Approximately(Time.timeScale, 1f) && Approximately(Time.fixedDeltaTime, NormalFixedDelta))
                    .ToString().ToLowerInvariant());

            _waitingActive = false;
            _pendingWaitingStart = false;
            _stopWaitingInProgress = false;
            _sampleActive = false;
            _sampleStatus = "stopped";
        }

        internal static void OnWaitingHidePrefix(object instance)
        {
            if (!ReferenceEquals(instance, _waitingGui) || !_waitingActive || _stopWaitingInProgress)
                return;

            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=unexpected_hide_prefix" +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime));
        }

        internal static void OnWaitingHidePostfix(object instance)
        {
            if (!ReferenceEquals(instance, _waitingGui) || !_waitingActive || _stopWaitingInProgress)
                return;

            FinishSample("unexpected-hide");

            bool stuck = !Approximately(Time.timeScale, 1f) ||
                         !Approximately(Time.fixedDeltaTime, NormalFixedDelta);

            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=unexpected_hide_postfix" +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime) +
                " would_leave_modified=" + stuck.ToString().ToLowerInvariant());

            // Research-harness safety cleanup. The diagnostic above records the native
            // values before cleanup, so this does not hide a lifecycle gap.
            if (stuck)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = NormalFixedDelta;
                Log(
                    "MEDITATION_LIFECYCLE_DIAGNOSTIC event=harness_safety_cleanup" +
                    " timescale=" + F(Time.timeScale) +
                    " fixed=" + F(Time.fixedDeltaTime));
            }

            _waitingActive = false;
            _pendingWaitingStart = false;
            _sampleActive = false;
            _sampleStatus = "hidden";
        }

        internal static void OnHarnessDestroy()
        {
            if (!_waitingActive && !_sampleActive)
                return;

            FinishSample("harness-destroy");

            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=harness_destroy" +
                " pre_timescale=" + F(Time.timeScale) +
                " pre_fixed=" + F(Time.fixedDeltaTime));

            Time.timeScale = 1f;
            Time.fixedDeltaTime = NormalFixedDelta;

            Log(
                "MEDITATION_LIFECYCLE_DIAGNOSTIC event=harness_destroy_cleanup" +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime));
        }

        private static bool IsOurActiveWaitingGui(object instance)
        {
            return _waitingActive &&
                   ReferenceEquals(instance, _waitingGui) &&
                   RuntimeBindings.IsWaitingState(instance);
        }

        private static void ApplyCurrentTiming(string reason)
        {
            float scale = SpeedTimeScale();
            float fixedDelta = FixedDeltaForCurrentMode();

            Time.timeScale = scale;
            Time.fixedDeltaTime = fixedDelta;

            Log(
                "MEDITATION_TIMING_DIAGNOSTIC event=apply" +
                " reason=" + reason +
                " speed=" + SpeedLabel(_speedIndex) +
                " policy=" + PolicyLabel() +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime));
        }

        private static float SpeedTimeScale()
        {
            if (_speedIndex == 0)
                return 10f;
            if (_speedIndex == 1)
                return 20f;
            return 40f;
        }

        private static float FixedDeltaForCurrentMode()
        {
            if (_speedIndex == 0)
                return VanillaMeditationFixedDelta;

            if (_speedIndex == 1)
                return TwoXFixedDelta;

            if (_fixedPolicy == FixedPolicy.VanillaMeditationStep)
                return VanillaMeditationFixedDelta;

            if (_fixedPolicy == FixedPolicy.BoundedIntermediate)
                return TwoXFixedDelta;

            return FourXFixedDelta;
        }

        private static void StartSample(string sampleName)
        {
            _sampleName = sampleName;
            _sampleStartReal = Time.realtimeSinceStartup;
            _sampleFrames = 0;
            _sampleFixedTicks = 0;
            _sampleWorldStart = RuntimeBindings.GetWorldTime();
            _sampleDayStart = RuntimeBindings.GetDay();
            _sampleActive = true;
            _sampleStatus = "measuring";

            Log(
                "MEDITATION_SAMPLE_DIAGNOSTIC event=start" +
                " sample=" + sampleName +
                " speed=" + SpeedLabel(_speedIndex) +
                " policy=" + PolicyLabel() +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime) +
                " world=" + F(_sampleWorldStart) +
                " day=" + _sampleDayStart);
        }

        private static void FinishSample(string reason)
        {
            if (!_sampleActive)
                return;

            float endReal = Time.realtimeSinceStartup;
            float elapsed = Mathf.Max(0.0001f, endReal - _sampleStartReal);
            float endWorld = RuntimeBindings.GetWorldTime();
            int endDay = RuntimeBindings.GetDay();

            float worldUnits = endWorld - _sampleWorldStart +
                               2f * (endDay - _sampleDayStart);
            float worldScaledSeconds = worldUnits * 225f;
            float worldRate = worldScaledSeconds / elapsed;
            float fps = _sampleFrames / elapsed;
            float fixedRate = _sampleFixedTicks / elapsed;

            Log(
                "MEDITATION_SAMPLE_DIAGNOSTIC event=end" +
                " sample=" + _sampleName +
                " reason=" + reason +
                " elapsed_real=" + F(elapsed) +
                " frames=" + _sampleFrames +
                " fps=" + F(fps) +
                " fixed_ticks=" + _sampleFixedTicks +
                " fixed_per_real_sec=" + F(fixedRate) +
                " world_start=" + F(_sampleWorldStart) +
                " world_end=" + F(endWorld) +
                " day_start=" + _sampleDayStart +
                " day_end=" + endDay +
                " world_scaled_seconds=" + F(worldScaledSeconds) +
                " observed_world_rate=" + F(worldRate) +
                " expected_world_rate=" + F(SpeedTimeScale()) +
                " timescale=" + F(Time.timeScale) +
                " fixed=" + F(Time.fixedDeltaTime));

            _sampleActive = false;
            _sampleStatus = reason == "complete" ? "done" : reason;
        }

        private static string CurrentSampleName()
        {
            return SpeedLabel(_speedIndex) + "-" + PolicyLabel();
        }

        private static string SpeedLabel(int index)
        {
            if (index <= 0)
                return "1x";
            if (index == 1)
                return "2x";
            return "4x";
        }

        private static string PolicyLabel()
        {
            if (_speedIndex < 2)
                return "proportional";

            if (_fixedPolicy == FixedPolicy.VanillaMeditationStep)
                return "vanilla-step";

            if (_fixedPolicy == FixedPolicy.BoundedIntermediate)
                return "bounded-step";

            return "proportional";
        }

        private static string InputSource()
        {
            return RuntimeBindings.GetGamepadActive() ? "gamepad" : "keyboard";
        }

        private static void RedrawTip()
        {
            if (_waitingGui == null)
                return;

            object tips = RuntimeBindings.GetButtonTips(_waitingGui);
            if (tips == null)
                return;

            string decIcon = RuntimeBindings.GetGameKeyIcon("SliderDec");
            string incIcon = RuntimeBindings.GetGameKeyIcon("SliderInc");

            string text =
                "Speed " + decIcon + SpeedLabel(_speedIndex) + " " + incIcon +
                "   " + _vanillaWakeTip +
                "   sample:" + _sampleStatus;

            if (_speedIndex == 2)
                text += "   [F8] fixed:" + PolicyLabel();

            RuntimeBindings.SetButtonTipText(tips, text);
        }

        private static void Log(string message)
        {
            if (MeditationSpeedResearchHarnessPlugin.Log != null)
                MeditationSpeedResearchHarnessPlugin.Log.LogInfo(message);
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.00001f;
        }

        private static string F(float value)
        {
            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    internal static class RuntimeBindings
    {
        internal static Type WaitingGuiType { get; private set; }
        internal static Type BaseGuiType { get; private set; }
        internal static Type ButtonTipsType { get; private set; }
        internal static Type GameKeyTipType { get; private set; }
        internal static Type GameKeyType { get; private set; }
        internal static Type CustomUpdateManagerType { get; private set; }

        private static PropertyInfo _buttonTipsProperty;
        private static FieldInfo _waitingStateField;
        private static FieldInfo _buttonTipsLabelField;
        private static PropertyInfo _labelTextProperty;

        private static PropertyInfo _environmentMeProperty;
        private static FieldInfo _environmentCurTimeField;

        private static MemberInfo _mainGameMeMember;
        private static MemberInfo _mainGameSaveMember;
        private static MemberInfo _saveDayMember;

        private static PropertyInfo _gamepadActiveProperty;
        private static MethodInfo _getIconMethod;

        internal static void Initialize()
        {
            WaitingGuiType = RequireType("WaitingGUI");
            BaseGuiType = RequireType("BaseGUI");
            ButtonTipsType = RequireType("ButtonTipsStr");
            GameKeyTipType = RequireType("GameKeyTip");
            GameKeyType = RequireType("GameKey");
            CustomUpdateManagerType = RequireType("CustomUpdateManager");

            _buttonTipsProperty = AccessTools.Property(BaseGuiType, "button_tips")
                ?? throw new MissingMemberException(BaseGuiType.FullName, "button_tips");

            _waitingStateField = AccessTools.Field(WaitingGuiType, "_state")
                ?? throw new MissingFieldException(WaitingGuiType.FullName, "_state");

            _buttonTipsLabelField = AccessTools.Field(ButtonTipsType, "label")
                ?? throw new MissingFieldException(ButtonTipsType.FullName, "label");

            Type uiLabelType = RequireType("UILabel");
            _labelTextProperty = AccessTools.Property(uiLabelType, "text")
                ?? throw new MissingMemberException(uiLabelType.FullName, "text");

            Type environmentType = RequireType("EnvironmentEngine");
            _environmentMeProperty = AccessTools.Property(environmentType, "me")
                ?? throw new MissingMemberException(environmentType.FullName, "me");
            _environmentCurTimeField = AccessTools.Field(environmentType, "_cur_time")
                ?? throw new MissingFieldException(environmentType.FullName, "_cur_time");

            Type mainGameType = RequireType("MainGame");
            _mainGameMeMember = FindStaticMember(mainGameType, "me");
            _mainGameSaveMember = FindInstanceMember(mainGameType, "save");

            Type saveType = GetMemberType(_mainGameSaveMember);
            _saveDayMember = FindInstanceMember(saveType, "day");

            Type lazyInputType = RequireType("LazyInput");
            _gamepadActiveProperty = AccessTools.Property(lazyInputType, "gamepad_active")
                ?? throw new MissingMemberException(lazyInputType.FullName, "gamepad_active");

            _getIconMethod = AccessTools.Method(GameKeyTipType, "GetIcon", new[] { GameKeyType })
                ?? throw new MissingMethodException(GameKeyTipType.FullName, "GetIcon");
        }

        internal static object GetButtonTips(object gui)
        {
            return gui == null ? null : _buttonTipsProperty.GetValue(gui, null);
        }

        internal static bool IsWaitingState(object gui)
        {
            if (gui == null || !WaitingGuiType.IsInstanceOfType(gui))
                return false;

            object state = _waitingStateField.GetValue(gui);
            return state != null && string.Equals(state.ToString(), "Waiting", StringComparison.Ordinal);
        }

        internal static string GetButtonTipText(object buttonTips)
        {
            if (buttonTips == null)
                return null;

            object label = _buttonTipsLabelField.GetValue(buttonTips);
            if (label == null)
                return null;

            return _labelTextProperty.GetValue(label, null) as string;
        }

        internal static void SetButtonTipText(object buttonTips, string text)
        {
            if (buttonTips == null)
                return;

            object label = _buttonTipsLabelField.GetValue(buttonTips);
            if (label == null)
                return;

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

        internal static float GetWorldTime()
        {
            object environment = _environmentMeProperty.GetValue(null, null);
            if (environment == null)
                return float.NaN;

            return Convert.ToSingle(
                _environmentCurTimeField.GetValue(environment),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static int GetDay()
        {
            object main = ReadMember(null, _mainGameMeMember);
            if (main == null)
                return -1;

            object save = ReadMember(main, _mainGameSaveMember);
            if (save == null)
                return -1;

            object day = ReadMember(save, _saveDayMember);
            return day == null ? -1 : Convert.ToInt32(day);
        }

        internal static bool GetGamepadActive()
        {
            object value = _gamepadActiveProperty.GetValue(null, null);
            return value is bool && (bool)value;
        }

        private static Type RequireType(string name)
        {
            return AccessTools.TypeByName(name)
                ?? throw new TypeLoadException("Could not find game type '" + name + "'.");
        }

        private static MemberInfo FindStaticMember(Type type, string name)
        {
            PropertyInfo property = AccessTools.Property(type, name);
            if (property != null && property.GetGetMethod(true) != null &&
                property.GetGetMethod(true).IsStatic)
                return property;

            FieldInfo field = AccessTools.Field(type, name);
            if (field != null && field.IsStatic)
                return field;

            throw new MissingMemberException(type.FullName, name);
        }

        private static MemberInfo FindInstanceMember(Type type, string name)
        {
            PropertyInfo property = AccessTools.Property(type, name);
            if (property != null)
                return property;

            FieldInfo field = AccessTools.Field(type, name);
            if (field != null)
                return field;

            throw new MissingMemberException(type.FullName, name);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null)
                return property.PropertyType;

            FieldInfo field = member as FieldInfo;
            if (field != null)
                return field.FieldType;

            throw new NotSupportedException("Unsupported member: " + member.MemberType);
        }

        private static object ReadMember(object instance, MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null)
                return property.GetValue(instance, null);

            FieldInfo field = member as FieldInfo;
            if (field != null)
                return field.GetValue(instance);

            return null;
        }
    }

    [HarmonyPatch]
    internal static class WaitingGuiOpenPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.WaitingGuiType, "Open")
                ?? throw new MissingMethodException(RuntimeBindings.WaitingGuiType.FullName, "Open");
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance)
        {
            HarnessState.OnWaitingOpen(__instance);
        }
    }

    [HarmonyPatch]
    internal static class WaitingGuiStartTipPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                       RuntimeBindings.ButtonTipsType,
                       "Print",
                       new[] { RuntimeBindings.GameKeyTipType })
                   ?? throw new MissingMethodException(RuntimeBindings.ButtonTipsType.FullName, "Print(GameKeyTip)");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            HarnessState.OnButtonTipsPrinted(__instance);
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
            if (!HarnessState.HandleSlider(__instance, -1, "SliderDec"))
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
            if (!HarnessState.HandleSlider(__instance, 1, "SliderInc"))
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class LeftNavigationObservationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "OnPressedLeft")
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "OnPressedLeft");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            HarnessState.OnNavigationHandler(__instance, "Left", __result);
        }
    }

    [HarmonyPatch]
    internal static class RightNavigationObservationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "OnPressedRight")
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "OnPressedRight");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            HarnessState.OnNavigationHandler(__instance, "Right", __result);
        }
    }

    [HarmonyPatch]
    internal static class CustomFixedUpdateObservationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.CustomUpdateManagerType, "FixedUpdate")
                ?? throw new MissingMethodException(RuntimeBindings.CustomUpdateManagerType.FullName, "FixedUpdate");
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            HarnessState.OnFixedTick();
        }
    }

    [HarmonyPatch]
    internal static class WaitingGuiStopPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.WaitingGuiType, "StopWaiting")
                ?? throw new MissingMethodException(RuntimeBindings.WaitingGuiType.FullName, "StopWaiting");
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance)
        {
            HarnessState.OnStopWaitingPrefix(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            HarnessState.OnStopWaitingPostfix(__instance);
        }
    }

    [HarmonyPatch]
    internal static class BaseGuiHideObservationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(RuntimeBindings.BaseGuiType, "Hide", new[] { typeof(bool) })
                ?? throw new MissingMethodException(RuntimeBindings.BaseGuiType.FullName, "Hide(bool)");
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance)
        {
            if (RuntimeBindings.WaitingGuiType.IsInstanceOfType(__instance))
                HarnessState.OnWaitingHidePrefix(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            if (RuntimeBindings.WaitingGuiType.IsInstanceOfType(__instance))
                HarnessState.OnWaitingHidePostfix(__instance);
        }
    }
}
