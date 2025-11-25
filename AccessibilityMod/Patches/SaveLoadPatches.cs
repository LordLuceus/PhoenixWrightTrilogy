using System;
using HarmonyLib;
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

                    string categoryName = GetCategoryName(cat);
                    ClipboardManager.Announce($"Options: {categoryName}", TextType.Menu);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Options ChangeCategory patch: {ex.Message}");
            }
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
