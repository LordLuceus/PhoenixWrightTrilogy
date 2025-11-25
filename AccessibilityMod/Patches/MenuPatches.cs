using System;
using System.Collections.Generic;
using HarmonyLib;
using AccessibilityMod.Core;
using AccessibilityMod.Services;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class MenuPatches
    {
        private static int _lastTanteiCursor = -1;
        private static int _lastSelectCursor = -1;
        private static string[] _tanteiMenuOptions = new string[4];
        private static List<string> _selectOptions = new List<string>();

        // Detective menu setup
        [HarmonyPostfix]
        [HarmonyPatch(typeof(tanteiMenu), "setMenu")]
        public static void SetMenu_Postfix(tanteiMenu __instance, int in_type)
        {
            try
            {
                // Get menu options text
                _tanteiMenuOptions[0] = TextDataCtrl.GetText(TextDataCtrl.CommonTextID.INSPECT);
                _tanteiMenuOptions[1] = TextDataCtrl.GetText(TextDataCtrl.CommonTextID.ROOM_MOVE);
                _tanteiMenuOptions[2] = TextDataCtrl.GetText(TextDataCtrl.CommonTextID.TALK);
                _tanteiMenuOptions[3] = TextDataCtrl.GetText(TextDataCtrl.CommonTextID.TUKITUKE);

                int optionCount = (in_type == 0) ? 2 : 4;
                _lastTanteiCursor = __instance.cursor_no;

                string currentOption = GetTanteiOption(__instance.cursor_no, in_type);
                string message = $"Menu: {currentOption} ({__instance.cursor_no + 1} of {optionCount})";
                ClipboardManager.Announce(message, TextType.Menu);

                AccessibilityState.SetMode(AccessibilityState.GameMode.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in SetMenu patch: {ex.Message}");
            }
        }

        // Detective menu cursor movement
        [HarmonyPostfix]
        [HarmonyPatch(typeof(tanteiMenu), "cursor")]
        public static void Cursor_Postfix(tanteiMenu __instance, bool is_right)
        {
            try
            {
                if (__instance.cursor_no != _lastTanteiCursor)
                {
                    _lastTanteiCursor = __instance.cursor_no;
                    string option = GetTanteiOption(__instance.cursor_no, __instance.setting);
                    ClipboardManager.Announce(option, TextType.Menu);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Cursor patch: {ex.Message}");
            }
        }

        private static string GetTanteiOption(int cursorNo, int menuType)
        {
            if (menuType == 0)
            {
                // 2-option menu: Examine, Move
                return cursorNo == 0 ? _tanteiMenuOptions[0] : _tanteiMenuOptions[1];
            }
            else
            {
                // 4-option menu: Examine, Move, Talk, Present
                if (cursorNo >= 0 && cursorNo < 4)
                    return _tanteiMenuOptions[cursorNo];
            }
            return "Unknown";
        }

        // Selection plate text setting (choices/talk options)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(selectPlateCtrl), "setText")]
        public static void SetText_Postfix(int index, string text)
        {
            try
            {
                // Ensure list is big enough
                while (_selectOptions.Count <= index)
                {
                    _selectOptions.Add("");
                }
                _selectOptions[index] = text;
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in SetText patch: {ex.Message}");
            }
        }

        // Selection plate cursor activation
        [HarmonyPostfix]
        [HarmonyPatch(typeof(selectPlateCtrl), "playCursor")]
        public static void PlayCursor_Postfix(selectPlateCtrl __instance, int in_type)
        {
            try
            {
                string menuType = in_type == 0 ? "Choice" : "Talk";
                _lastSelectCursor = __instance.cursor_no;

                // Count active options
                int count = 0;
                for (int i = 0; i < _selectOptions.Count; i++)
                {
                    if (!Core.Net35Extensions.IsNullOrWhiteSpace(_selectOptions[i]))
                        count++;
                    else
                        break;
                }

                if (count > 0)
                {
                    string currentOption = __instance.cursor_no < _selectOptions.Count
                        ? _selectOptions[__instance.cursor_no]
                        : "Unknown";

                    string message = $"{menuType} menu: {count} options. {currentOption}";
                    ClipboardManager.Announce(message, TextType.MenuChoice);
                }

                AccessibilityState.SetMode(AccessibilityState.GameMode.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in PlayCursor patch: {ex.Message}");
            }
        }

        // Selection plate cursor position change
        [HarmonyPostfix]
        [HarmonyPatch(typeof(selectPlateCtrl), "SetCursorNo")]
        public static void SetCursorNo_Postfix(selectPlateCtrl __instance, int in_cursor_no)
        {
            try
            {
                if (in_cursor_no != _lastSelectCursor && __instance.body_active)
                {
                    _lastSelectCursor = in_cursor_no;
                    if (in_cursor_no >= 0 && in_cursor_no < _selectOptions.Count)
                    {
                        string option = _selectOptions[in_cursor_no];
                        if (!Core.Net35Extensions.IsNullOrWhiteSpace(option))
                        {
                            ClipboardManager.Announce(option, TextType.MenuChoice);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in SetCursorNo patch: {ex.Message}");
            }
        }

        // Selection end - clear stored options
        [HarmonyPostfix]
        [HarmonyPatch(typeof(selectPlateCtrl), "end")]
        public static void End_Postfix()
        {
            try
            {
                _selectOptions.Clear();
                _lastSelectCursor = -1;
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in End patch: {ex.Message}");
            }
        }
    }
}
