using System;
using MelonLoader;
using UnityEngine;
using AccessibilityMod.Services;

namespace AccessibilityMod.Core
{
    public class AccessibilityMod : MelonMod
    {
        public static MelonLogger.Instance Logger { get; private set; }
        public static AccessibilityMod Instance { get; private set; }

        private static bool _isInitialized = false;

        public override void OnInitializeMelon()
        {
            Instance = this;
            Logger = LoggerInstance;
            Logger.Msg("Phoenix Wright Accessibility Mod initializing...");
        }

        private void InitializeAccessibility()
        {
            if (_isInitialized)
                return;

            try
            {
                // Create the coroutine runner for clipboard processing
                GameObject managerObject = new GameObject("AccessibilityMod_CoroutineRunner");
                managerObject.AddComponent<CoroutineRunner>();

                // Initialize the hotspot navigator for investigation mode
                HotspotNavigator.Initialize();

                Logger.Msg("Accessibility systems initialized successfully");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize accessibility systems: {ex.Message}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Logger.Msg($"Scene loaded: {sceneName} (Index: {buildIndex})");

            // Initialize on first scene load when Unity is ready
            InitializeAccessibility();

            // Announce scene changes
            if (!string.IsNullOrEmpty(sceneName))
            {
                ClipboardManager.Announce($"Scene: {sceneName}", TextType.SystemMessage);
            }
        }

        public override void OnUpdate()
        {
            try
            {
                HandleInput();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnUpdate: {ex.Message}");
            }
        }

        private void HandleInput()
        {
            // R - Repeat last output
            if (Input.GetKeyDown(KeyCode.R))
            {
                ClipboardManager.RepeatLast();
            }

            // I - Announce current context/state
            if (Input.GetKeyDown(KeyCode.I))
            {
                AccessibilityState.AnnounceCurrentState();
            }

            // Investigation mode hotspot navigation
            if (AccessibilityState.IsInInvestigationMode())
            {
                // [ - Next hotspot
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    HotspotNavigator.NavigateNext();
                }

                // ] - Previous hotspot
                if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    HotspotNavigator.NavigatePrevious();
                }

                // U - Next unexamined hotspot
                if (Input.GetKeyDown(KeyCode.U))
                {
                    HotspotNavigator.NavigateToNextUnexamined();
                }

                // H - List all hotspots
                if (Input.GetKeyDown(KeyCode.H))
                {
                    HotspotNavigator.AnnounceAllHotspots();
                }
            }

            // G - Announce life gauge (in trial)
            if (Input.GetKeyDown(KeyCode.G) && AccessibilityState.IsInTrialMode())
            {
                AccessibilityState.AnnounceLifeGauge();
            }
        }

        public override void OnDeinitializeMelon()
        {
            if (CoroutineRunner.Instance != null)
            {
                CoroutineRunner.Instance.StopClipboardProcessor();
                UnityEngine.Object.Destroy(CoroutineRunner.Instance.gameObject);
            }

            Logger.Msg("Phoenix Wright Accessibility Mod deinitialized.");
        }
    }
}
