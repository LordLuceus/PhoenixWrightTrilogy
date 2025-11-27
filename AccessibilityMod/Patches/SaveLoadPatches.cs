using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine.UI;
using AccessibilityMod.Core;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class SaveLoadPatches
    {
        private static int _lastSlotCursor = -1;

        // Hook when save/load UI opens
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadUICtrl), "Open")]
        public static void Open_Postfix(SaveLoadUICtrl __instance)
        {
            try
            {
                var slotType = GetSlotType(__instance);
                string typeName = slotType == 0 ? "Save" : "Load";

                ClipboardManager.Announce($"{typeName} menu opened", TextType.Menu);
                _lastSlotCursor = -1;
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in SaveLoad Open patch: {ex.Message}");
            }
        }

        // Hook cursor changes in save/load via UpdateCursorPosition
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadUICtrl), "UpdateCursorPosition")]
        public static void UpdateCursorPosition_Postfix(SaveLoadUICtrl __instance)
        {
            try
            {
                int currentSlot = GetCurrentSlot(__instance);
                if (currentSlot != _lastSlotCursor && currentSlot >= 0)
                {
                    _lastSlotCursor = currentSlot;
                    AnnounceSlot(__instance, currentSlot);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in SaveLoad UpdateCursorPosition patch: {ex.Message}");
            }
        }

        private static int GetCurrentSlot(SaveLoadUICtrl ctrl)
        {
            try
            {
                var field = typeof(SaveLoadUICtrl).GetField("select_num_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return (int)field.GetValue(ctrl);
                }
            }
            catch { }
            return -1;
        }

        private static void AnnounceSlot(SaveLoadUICtrl ctrl, int slotNo)
        {
            try
            {
                // Simple slot announcement - slot number (1-10)
                string slotInfo = $"Slot {slotNo + 1}";

                // Try to get more info via reflection if available
                try
                {
                    var slotListField = typeof(SaveLoadUICtrl).GetField("slot_list_",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (slotListField != null)
                    {
                        var slotList = slotListField.GetValue(ctrl) as System.Collections.IList;
                        if (slotList != null && slotNo >= 0 && slotNo < slotList.Count)
                        {
                            var slot = slotList[slotNo];
                            // Try to get slot text or status
                            var textField = slot.GetType().GetField("text_");
                            if (textField != null)
                            {
                                var text = textField.GetValue(slot) as UnityEngine.UI.Text;
                                if (text != null && !string.IsNullOrEmpty(text.text))
                                {
                                    slotInfo = $"Slot {slotNo + 1}: {text.text}";
                                }
                            }
                        }
                    }
                }
                catch { }

                ClipboardManager.Announce(slotInfo, TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error announcing slot: {ex.Message}");
            }
        }

        private static int GetSlotType(SaveLoadUICtrl ctrl)
        {
            try
            {
                var field = typeof(SaveLoadUICtrl).GetField("slot_type_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return (int)field.GetValue(ctrl);
                }
            }
            catch { }
            return 0;
        }
    }

    [HarmonyPatch]
    public static class OptionPatches
    {
        private static int _lastCategory = -1;
        private static int _lastOptionIndex = -1;

        // Reflection cache
        private static FieldInfo _currentNumField;
        private static FieldInfo _availableOptionField;
        private static FieldInfo _optionTitleField;
        private static FieldInfo _titleBackTextField;

        private static readonly BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        // Tooltip delay in seconds
        private const float TooltipDelay = 2.0f;

        // Hook when options menu category changes
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionCtrl), "ChangeCategory")]
        public static void ChangeCategory_Postfix(optionCtrl __instance, optionCtrl.Category cat)
        {
            try
            {
                int categoryInt = (int)cat;
                if (categoryInt != _lastCategory)
                {
                    _lastCategory = categoryInt;
                    _lastOptionIndex = -1; // Reset option index when category changes

                    // Cancel any pending tooltip from previous category
                    CoroutineRunner.Instance?.CancelDelayedAnnouncement();

                    string categoryName = GetCategoryName(cat);
                    ClipboardManager.Announce($"Options: {categoryName}", TextType.Menu);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Options ChangeCategory patch: {ex.Message}");
            }
        }

        // Hook when navigating between options within a category
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionCtrl), "SelectItemSet")]
        public static void SelectItemSet_Postfix(optionCtrl __instance)
        {
            try
            {
                int currentIndex = GetCurrentOptionIndex(__instance);
                if (currentIndex == _lastOptionIndex || currentIndex < 0)
                    return;

                _lastOptionIndex = currentIndex;

                var options = GetAvailableOptions(__instance);
                if (options == null || currentIndex >= options.Count)
                    return;

                var currentOption = options[currentIndex];
                string optionName = GetOptionName(currentOption);
                string currentValue = GetOptionValue(currentOption);

                string message;
                if (!Net35Extensions.IsNullOrWhiteSpace(currentValue))
                    message = $"{optionName}: {currentValue}";
                else
                    message = optionName;

                ClipboardManager.Announce(message, TextType.Menu);

                // Schedule delayed tooltip announcement
                ScheduleTooltipAnnouncement(__instance);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Options SelectItemSet patch: {ex.Message}");
            }
        }

        private static void ScheduleTooltipAnnouncement(optionCtrl instance)
        {
            try
            {
                if (CoroutineRunner.Instance == null)
                    return;

                CoroutineRunner.Instance.ScheduleDelayedAnnouncement(
                    TooltipDelay,
                    () => GetTooltipText(instance),
                    TextType.Menu
                );
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error scheduling tooltip: {ex.Message}");
            }
        }

        private static string GetTooltipText(optionCtrl instance)
        {
            try
            {
                if (_titleBackTextField == null)
                    _titleBackTextField = typeof(optionCtrl).GetField("title_back_text_", NonPublicInstance);

                if (_titleBackTextField == null)
                    return null;

                var titleBackTextList = _titleBackTextField.GetValue(instance) as List<UnityEngine.UI.Text>;
                if (titleBackTextList == null || titleBackTextList.Count == 0)
                    return null;

                // Get the current key type for button prompts
                string keyName = GetCurrentKeyName(instance);

                // Combine both lines of tooltip text
                string line1 = titleBackTextList.Count > 0 ? titleBackTextList[0]?.text : null;
                string line2 = titleBackTextList.Count > 1 ? titleBackTextList[1]?.text : null;

                // Replace button icon placeholders (whitespace sequences) with key name
                if (!Net35Extensions.IsNullOrWhiteSpace(keyName))
                {
                    line1 = ReplaceButtonPlaceholder(line1, keyName);
                    line2 = ReplaceButtonPlaceholder(line2, keyName);
                }

                string tooltip = "";
                if (!Net35Extensions.IsNullOrWhiteSpace(line1))
                    tooltip = line1;
                if (!Net35Extensions.IsNullOrWhiteSpace(line2))
                    tooltip = Net35Extensions.IsNullOrWhiteSpace(tooltip) ? line2 : tooltip + " " + line2;

                return tooltip;
            }
            catch
            {
                return null;
            }
        }

        private static string ReplaceButtonPlaceholder(string text, string keyName)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // First check if the original placeholder is still there (【】)
            if (text.Contains("【】"))
            {
                if (!string.IsNullOrEmpty(keyName))
                    text = text.Replace("【】", $"[{keyName}]");
                else
                    text = text.Replace("【】", "");
                return text;
            }

            // The game replaces 【】 with multiple spaces to make room for an icon
            // Full-width space: \u3000, used 3 times for CJK languages
            // Half-width spaces used 9 times for other languages

            if (string.IsNullOrEmpty(keyName))
                return text;

            // Replace sequences of full-width ideographic spaces (\u3000)
            if (text.IndexOf('\u3000') >= 0)
            {
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    "[\u3000]+",
                    $" [{keyName}] "
                );
            }

            // Replace sequences of 3 or more regular spaces
            // Pattern: look for 3+ consecutive spaces
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                " {3,}",
                $" [{keyName}] "
            );

            return text;
        }

        private static string GetCurrentKeyName(optionCtrl instance)
        {
            try
            {
                // Get current option index
                int currentIndex = GetCurrentOptionIndex(instance);
                if (currentIndex < 0)
                    return null;

                // Get begin_num_ to calculate actual OptionItem
                var beginNumField = typeof(optionCtrl).GetField("begin_num_", NonPublicInstance);
                if (beginNumField == null)
                    return null;

                int beginNum = (int)beginNumField.GetValue(instance);
                int optionItemIndex = currentIndex + beginNum;

                // Get KeyType for this option based on game's mapping
                KeyType keyType = GetKeyTypeForOption(optionItemIndex);
                if (keyType == KeyType.None)
                    return null;

                return GetKeyTypeName(keyType);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"GetCurrentKeyName error: {ex.Message}");
                return null;
            }
        }

        // Maps OptionItem index to KeyType based on game's keyIconSet logic
        private static KeyType GetKeyTypeForOption(int optionItemIndex)
        {
            // OptionItem enum values:
            // SKIP = 3, LANGUAGE = 7, VOICE_LANG = 8
            switch (optionItemIndex)
            {
                case 3: // SKIP - has B button prompt
                    return KeyType.B;

                case 7:  // LANGUAGE - has A button prompt
                case 8:  // VOICE_LANG - has A button prompt
                    return KeyType.A;

                // Most options don't have button prompts
                default:
                    return KeyType.None;
            }
        }

        private static string GetKeyTypeName(KeyType keyType)
        {
            // Check if using controller or keyboard
            bool isController = keyGuideBase.keyguid_pad_;

            if (isController)
            {
                // Controller button names (Xbox style)
                switch (keyType)
                {
                    case KeyType.A: return "A";
                    case KeyType.B: return "B";
                    case KeyType.X: return "X";
                    case KeyType.Y: return "Y";
                    case KeyType.L: return "LB";
                    case KeyType.R: return "RB";
                    case KeyType.ZL: return "LT";
                    case KeyType.ZR: return "RT";
                    case KeyType.Start: return "Menu";
                    case KeyType.Select: return "View";
                    case KeyType.StickL: return "Left Stick";
                    case KeyType.StickR: return "Right Stick";
                    default: return keyType.ToString();
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

        private static string GetKeyCodeName(UnityEngine.KeyCode keyCode)
        {
            switch (keyCode)
            {
                case UnityEngine.KeyCode.Space: return "Space";
                case UnityEngine.KeyCode.Return: return "Enter";
                case UnityEngine.KeyCode.Escape: return "Escape";
                case UnityEngine.KeyCode.Tab: return "Tab";
                case UnityEngine.KeyCode.Backspace: return "Backspace";
                case UnityEngine.KeyCode.Delete: return "Delete";
                case UnityEngine.KeyCode.Insert: return "Insert";
                case UnityEngine.KeyCode.Home: return "Home";
                case UnityEngine.KeyCode.End: return "End";
                case UnityEngine.KeyCode.PageUp: return "Page Up";
                case UnityEngine.KeyCode.PageDown: return "Page Down";
                case UnityEngine.KeyCode.UpArrow: return "Up Arrow";
                case UnityEngine.KeyCode.DownArrow: return "Down Arrow";
                case UnityEngine.KeyCode.LeftArrow: return "Left Arrow";
                case UnityEngine.KeyCode.RightArrow: return "Right Arrow";
                case UnityEngine.KeyCode.LeftShift: return "Left Shift";
                case UnityEngine.KeyCode.RightShift: return "Right Shift";
                case UnityEngine.KeyCode.LeftControl: return "Left Ctrl";
                case UnityEngine.KeyCode.RightControl: return "Right Ctrl";
                case UnityEngine.KeyCode.LeftAlt: return "Left Alt";
                case UnityEngine.KeyCode.RightAlt: return "Right Alt";
                default:
                    // For letters and numbers, just return the key name
                    string name = keyCode.ToString();
                    // Handle "Alpha1" -> "1", etc.
                    if (name.StartsWith("Alpha"))
                        return name.Substring(5);
                    // Handle "Keypad1" -> "Numpad 1", etc.
                    if (name.StartsWith("Keypad"))
                        return "Numpad " + name.Substring(6);
                    return name;
            }
        }

        // Hook value changes for gauge-type options (BGM, SE)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionBgm), "ChangeValue")]
        public static void OptionBgm_ChangeValue_Postfix(optionItem __instance)
        {
            AnnounceOptionValue(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionSe), "ChangeValue")]
        public static void OptionSe_ChangeValue_Postfix(optionItem __instance)
        {
            AnnounceOptionValue(__instance);
        }

        // Hook value changes for toggle-type options
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionToggleItem), "ChangeValue")]
        public static void OptionToggle_ChangeValue_Postfix(optionItem __instance)
        {
            AnnounceOptionValue(__instance);
        }

        // Hook value changes for skip option
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionSkip), "ChangeValue")]
        public static void OptionSkip_ChangeValue_Postfix(optionItem __instance)
        {
            AnnounceOptionValue(__instance);
        }

        // Hook value changes for window transparency option
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionWindow), "ChangeValue")]
        public static void OptionWindow_ChangeValue_Postfix(optionItem __instance)
        {
            AnnounceOptionValue(__instance);
        }

        // Hook value changes for vibration option (extends optionItem directly)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionVibration), "ChangeValue")]
        public static void OptionVibration_ChangeValue_Postfix(optionItem __instance)
        {
            AnnounceOptionValue(__instance);
        }

        // Hook value changes for select-type options (language, resolution, window mode, auto-speed)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(optionSelectItem), "ChangeValue")]
        public static void OptionSelect_ChangeValue_Postfix(optionItem __instance)
        {
            AnnounceOptionValue(__instance);
        }

        private static void AnnounceOptionValue(optionItem item)
        {
            try
            {
                string value = GetOptionValue(item);
                if (!Net35Extensions.IsNullOrWhiteSpace(value))
                {
                    ClipboardManager.Announce(value, TextType.Menu);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error announcing option value: {ex.Message}");
            }
        }

        private static int GetCurrentOptionIndex(optionCtrl ctrl)
        {
            try
            {
                if (_currentNumField == null)
                    _currentNumField = typeof(optionCtrl).GetField("current_num_", NonPublicInstance);

                if (_currentNumField != null)
                    return (int)_currentNumField.GetValue(ctrl);
            }
            catch { }
            return -1;
        }

        private static List<optionItem> GetAvailableOptions(optionCtrl ctrl)
        {
            try
            {
                if (_availableOptionField == null)
                    _availableOptionField = typeof(optionCtrl).GetField("available_option_", NonPublicInstance);

                if (_availableOptionField != null)
                    return _availableOptionField.GetValue(ctrl) as List<optionItem>;
            }
            catch { }
            return null;
        }

        private static string GetOptionName(optionItem item)
        {
            try
            {
                if (_optionTitleField == null)
                    _optionTitleField = typeof(optionItem).GetField("option_title_", NonPublicInstance);

                if (_optionTitleField != null)
                {
                    var titleText = _optionTitleField.GetValue(item) as Text;
                    if (titleText != null && !Net35Extensions.IsNullOrWhiteSpace(titleText.text))
                        return titleText.text;
                }
            }
            catch { }
            return "Unknown option";
        }

        private static string GetOptionValue(optionItem item)
        {
            try
            {
                Type itemType = item.GetType();

                // Try gauge type pattern first (optionBgm, optionSe)
                // These have gauge_ field with count_text_.text
                var gaugeField = FindField(itemType, "gauge_");
                if (gaugeField != null)
                {
                    var gauge = gaugeField.GetValue(item);
                    if (gauge != null)
                    {
                        var countTextField = gauge.GetType().GetField("count_text_");
                        if (countTextField != null)
                        {
                            var countText = countTextField.GetValue(gauge) as Text;
                            if (countText != null && !Net35Extensions.IsNullOrWhiteSpace(countText.text))
                            {
                                return countText.text;
                            }
                        }
                    }
                }

                // Try toggle/select type pattern (optionToggleItem, optionSkip, optionShake, optionWindow, etc.)
                // These have select_text_[] array and setting_value_ index
                var selectTextField = FindField(itemType, "select_text_");
                var settingValueField = FindField(itemType, "setting_value_");

                if (selectTextField != null && settingValueField != null)
                {
                    var selectTexts = selectTextField.GetValue(item) as string[];
                    int settingValue = (int)settingValueField.GetValue(item);
                    if (selectTexts != null && settingValue >= 0 && settingValue < selectTexts.Length)
                    {
                        return selectTexts[settingValue];
                    }
                }

                // Fallback: try to get setting_value_ as a number
                if (settingValueField != null)
                {
                    int settingValue = (int)settingValueField.GetValue(item);
                    return settingValue.ToString();
                }
            }
            catch { }
            return null;
        }

        // Helper to find a field in a type or its base types
        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null && type != typeof(object))
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
        }

        private static string GetCategoryName(optionCtrl.Category category)
        {
            switch (category)
            {
                case optionCtrl.Category.SAVE_LOAD: return "Save/Load";
                case optionCtrl.Category.SOUND: return "Sound";
                case optionCtrl.Category.GAME: return "Game";
                case optionCtrl.Category.LANGUAGE: return "Language";
                case optionCtrl.Category.PC: return "Display";
                case optionCtrl.Category.KEYCONFIG: return "Key Config";
                case optionCtrl.Category.STORY: return "Story";
                case optionCtrl.Category.CREDIT: return "Credits";
                case optionCtrl.Category.PRIVACY: return "Privacy";
                default: return category.ToString();
            }
        }
    }

}
