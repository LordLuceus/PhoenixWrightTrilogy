using System;
using System.Text;
using System.Text.RegularExpressions;
using AccessibilityMod.Core;
using AccessibilityMod.Services;
using HarmonyLib;
using UnityEngine;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class DialoguePatches
    {
        private static string _lastAnnouncedText = "";
        private static int _lastSpeakerId = -1;

        // Regex for detecting button placeholders (multiple spaces or full-width spaces)
        private static readonly Regex SpacePlaceholderRegex = new Regex(
            @"[\u3000]+| {3,}",
            RegexOptions.Compiled
        );

        // Hook when arrow appears - this means the text is ready to be read
        [HarmonyPostfix]
        [HarmonyPatch(typeof(messageBoardCtrl), "arrow")]
        public static void Arrow_Postfix(messageBoardCtrl __instance, bool in_arrow, int in_type)
        {
            try
            {
                // Right arrow (type 0) appearing means text is complete and waiting for player input
                if (in_arrow && in_type == 0 && __instance.body_active)
                {
                    OutputCurrentDialogue(__instance);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error(
                    $"Error in Arrow patch: {ex.Message}"
                );
            }
        }

        // Hook when message board opens
        [HarmonyPostfix]
        [HarmonyPatch(typeof(messageBoardCtrl), "board")]
        public static void Board_Postfix(messageBoardCtrl __instance, bool in_board, bool in_mes)
        {
            try
            {
                if (!in_board)
                {
                    // Dialogue window closed - reset tracking
                    _lastAnnouncedText = "";
                    _lastSpeakerId = -1;
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error(
                    $"Error in Board patch: {ex.Message}"
                );
            }
        }

        // Hook when speaker name changes
        [HarmonyPostfix]
        [HarmonyPatch(typeof(messageBoardCtrl), "name_plate")]
        public static void NamePlate_Postfix(
            messageBoardCtrl __instance,
            bool in_name,
            int in_name_no,
            int in_pos
        )
        {
            try
            {
                // Debug: Log speaker ID to help identify correct mappings
                if (in_name && in_name_no > 0)
                {
                    AccessibilityMod.Core.AccessibilityMod.Logger?.Msg($"Speaker ID: {in_name_no}");
                }

                if (in_name && in_name_no != _lastSpeakerId)
                {
                    _lastSpeakerId = in_name_no;
                    // Don't announce name here - it will be combined with text in OutputCurrentDialogue
                }
                else if (!in_name)
                {
                    _lastSpeakerId = -1;
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error(
                    $"Error in NamePlate patch: {ex.Message}"
                );
            }
        }

        // Hook LoadMsgSet for saved message restoration
        [HarmonyPostfix]
        [HarmonyPatch(typeof(messageBoardCtrl), "LoadMsgSet")]
        public static void LoadMsgSet_Postfix(messageBoardCtrl __instance)
        {
            try
            {
                if (__instance.body_active)
                {
                    OutputCurrentDialogue(__instance);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error(
                    $"Error in LoadMsgSet patch: {ex.Message}"
                );
            }
        }

        private static void OutputCurrentDialogue(messageBoardCtrl ctrl)
        {
            try
            {
                // Get text from line_list
                string text = CombineLines(ctrl);

                if (Net35Extensions.IsNullOrWhiteSpace(text) || text == _lastAnnouncedText)
                    return;

                _lastAnnouncedText = text;

                // Replace button placeholders with actual key names
                text = ReplaceButtonPlaceholders(ctrl, text);

                // Get speaker name
                string speakerName = "";
                if (_lastSpeakerId > 0)
                {
                    speakerName = CharacterNameService.GetName(_lastSpeakerId);
                }

                // Also try to get from GSStatic if available
                if (Net35Extensions.IsNullOrWhiteSpace(speakerName))
                {
                    try
                    {
                        if (GSStatic.message_work_ != null && GSStatic.message_work_.speaker_id > 0)
                        {
                            speakerName = CharacterNameService.GetName(
                                GSStatic.message_work_.speaker_id
                            );
                        }
                    }
                    catch { }
                }

                ClipboardManager.Output(speakerName, text, TextType.Dialogue);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error(
                    $"Error outputting dialogue: {ex.Message}"
                );
            }
        }

        private static string ReplaceButtonPlaceholders(messageBoardCtrl ctrl, string text)
        {
            try
            {
                // Check if text contains placeholder patterns
                if (!SpacePlaceholderRegex.IsMatch(text))
                    return text;

                // Get the key icon from the message board
                var keyIcon = ctrl.msg_key_icon;
                if (keyIcon == null || keyIcon.key_icon == null)
                    return text;

                // Find the first active key icon and get its key type
                KeyType activeKeyType = KeyType.None;
                foreach (var icon in keyIcon.key_icon)
                {
                    if (icon != null && icon.icon_active_)
                    {
                        activeKeyType = icon.icon_key_type_;
                        break;
                    }
                }

                // Get the key name
                string keyName = null;
                if (activeKeyType != KeyType.None)
                {
                    keyName = GetKeyName(activeKeyType);
                }

                // Replace the placeholder
                if (!string.IsNullOrEmpty(keyName))
                {
                    text = SpacePlaceholderRegex.Replace(text, $" [{keyName}] ");
                }

                return text;
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error(
                    $"Error replacing button placeholders: {ex.Message}"
                );
                return text;
            }
        }

        private static string GetKeyName(KeyType keyType)
        {
            // Check if using controller or keyboard
            bool isController = keyGuideBase.keyguid_pad_;

            if (isController)
            {
                // Controller button names (Xbox style)
                switch (keyType)
                {
                    case KeyType.A:
                        return "A";
                    case KeyType.B:
                        return "B";
                    case KeyType.X:
                        return "X";
                    case KeyType.Y:
                        return "Y";
                    case KeyType.L:
                        return "LB";
                    case KeyType.R:
                        return "RB";
                    case KeyType.ZL:
                        return "LT";
                    case KeyType.ZR:
                        return "RT";
                    case KeyType.Start:
                        return "Menu";
                    case KeyType.Select:
                        return "View";
                    case KeyType.StickL:
                        return "Left Stick";
                    case KeyType.StickR:
                        return "Right Stick";
                    case KeyType.Record:
                        return "RB";
                    default:
                        return keyType.ToString();
                }
            }
            else
            {
                // Get keyboard binding
                try
                {
                    var keyCode = padCtrl.instance.GetKeyCode(keyType);
                    return GetKeyCodeName(keyCode);
                }
                catch
                {
                    return keyType.ToString();
                }
            }
        }

        private static string GetKeyCodeName(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.Space:
                    return "Space";
                case KeyCode.Return:
                    return "Enter";
                case KeyCode.Escape:
                    return "Escape";
                case KeyCode.Tab:
                    return "Tab";
                case KeyCode.Backspace:
                    return "Backspace";
                case KeyCode.Delete:
                    return "Delete";
                case KeyCode.Insert:
                    return "Insert";
                case KeyCode.Home:
                    return "Home";
                case KeyCode.End:
                    return "End";
                case KeyCode.PageUp:
                    return "Page Up";
                case KeyCode.PageDown:
                    return "Page Down";
                case KeyCode.UpArrow:
                    return "Up Arrow";
                case KeyCode.DownArrow:
                    return "Down Arrow";
                case KeyCode.LeftArrow:
                    return "Left Arrow";
                case KeyCode.RightArrow:
                    return "Right Arrow";
                case KeyCode.LeftShift:
                    return "Left Shift";
                case KeyCode.RightShift:
                    return "Right Shift";
                case KeyCode.LeftControl:
                    return "Left Ctrl";
                case KeyCode.RightControl:
                    return "Right Ctrl";
                case KeyCode.LeftAlt:
                    return "Left Alt";
                case KeyCode.RightAlt:
                    return "Right Alt";
                default:
                    string name = keyCode.ToString();
                    if (name.StartsWith("Alpha"))
                        return name.Substring(5);
                    if (name.StartsWith("Keypad"))
                        return "Numpad " + name.Substring(6);
                    return name;
            }
        }

        private static string CombineLines(messageBoardCtrl ctrl)
        {
            if (ctrl.line_list == null || ctrl.line_list.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            foreach (var line in ctrl.line_list)
            {
                if (line != null && !Net35Extensions.IsNullOrWhiteSpace(line.text))
                {
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(line.text);
                }
            }
            return sb.ToString();
        }
    }
}
