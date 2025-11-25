using System;
using HarmonyLib;
using AccessibilityMod.Core;
using AccessibilityMod.Services;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class InvestigationPatches
    {
        private static bool _wasInInvestigation = false;
        private static int _lastCursorSprite = -1;

        // Hook when investigation mode starts
        [HarmonyPostfix]
        [HarmonyPatch(typeof(inspectCtrl), "play")]
        public static void Play_Postfix(inspectCtrl __instance)
        {
            try
            {
                _wasInInvestigation = true;
                _lastCursorSprite = -1;

                AccessibilityState.SetMode(AccessibilityState.GameMode.Investigation);
                HotspotNavigator.OnInvestigationStart();
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Investigation Play patch: {ex.Message}");
            }
        }

        // Hook when investigation mode ends
        [HarmonyPostfix]
        [HarmonyPatch(typeof(inspectCtrl), "end")]
        public static void End_Postfix(inspectCtrl __instance)
        {
            try
            {
                if (_wasInInvestigation)
                {
                    _wasInInvestigation = false;
                    _lastCursorSprite = -1;
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Investigation End patch: {ex.Message}");
            }
        }

        // Hook cursor sprite changes to detect hovering over hotspots
        // The cursor sprite number indicates:
        // 0 = Normal cursor (no hotspot)
        // 1 = Hotspot detected (unexamined)
        // 3 = Hotspot detected (already examined)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(inspectCtrl), "Set_InspectCursor", MethodType.Normal)]
        public static void Set_InspectCursor_Postfix(inspectCtrl __instance)
        {
            try
            {
                if (!__instance.is_play)
                    return;

                // Get current cursor sprite number via reflection or by checking the sprite
                int currentSprite = GetCursorSpriteNumber(__instance);

                if (currentSprite != _lastCursorSprite)
                {
                    _lastCursorSprite = currentSprite;

                    switch (currentSprite)
                    {
                        case 1:
                            // Hovering over unexamined hotspot
                            ClipboardManager.Announce("Point of interest", TextType.Investigation);
                            break;
                        case 3:
                            // Hovering over already examined hotspot
                            ClipboardManager.Announce("Already examined", TextType.Investigation);
                            break;
                        // case 0: Normal cursor - don't announce
                    }
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Set_InspectCursor patch: {ex.Message}");
            }
        }

        private static int GetCursorSpriteNumber(inspectCtrl instance)
        {
            try
            {
                // Access the private cursor_ field to get its sprite number
                var cursorField = typeof(inspectCtrl).GetField("cursor_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (cursorField != null)
                {
                    var cursor = cursorField.GetValue(instance) as AssetBundleSprite;
                    if (cursor != null)
                    {
                        return cursor.sprite_no_;
                    }
                }
            }
            catch { }
            return 0;
        }
    }

    // Move menu patches
    [HarmonyPatch]
    public static class MovePatches
    {
        private static int _lastMoveCursor = -1;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(moveCtrl), "setting")]
        public static void Setting_Postfix(moveCtrl __instance, int in_num, int in_cursor_no)
        {
            try
            {
                _lastMoveCursor = in_cursor_no;

                string locationName = GetLocationName(__instance, in_cursor_no);
                string message = $"Move: {locationName} ({in_cursor_no + 1} of {in_num})";
                ClipboardManager.Announce(message, TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Move Setting patch: {ex.Message}");
            }
        }

        private static string GetLocationName(moveCtrl ctrl, int index)
        {
            try
            {
                // Access select_list to get location name
                var selectListField = typeof(moveCtrl).GetField("select_list_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (selectListField != null)
                {
                    var selectList = selectListField.GetValue(ctrl) as System.Collections.IList;
                    if (selectList != null && index >= 0 && index < selectList.Count)
                    {
                        var item = selectList[index];
                        var textField = item.GetType().GetField("text_");
                        if (textField != null)
                        {
                            var textComponent = textField.GetValue(item) as UnityEngine.UI.Text;
                            if (textComponent != null)
                            {
                                return textComponent.text;
                            }
                        }
                    }
                }
            }
            catch { }
            return $"Location {index + 1}";
        }
    }
}
