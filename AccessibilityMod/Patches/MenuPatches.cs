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

        // Main menu tracking
        private static int _lastSeriesTitle = -1;

        #region Main Title Menu Patches

        // Main title menu initialization - announce when main menu appears
        [HarmonyPostfix]
        [HarmonyPatch(typeof(titleSelectPlate), "playCursor")]
        public static void TitleSelectPlate_PlayCursor_Postfix(titleSelectPlate __instance, int in_type)
        {
            try
            {
                // Check if this is the main menu (from mainTitleCtrl)
                var mainTitle = mainTitleCtrl.instance;
                if (mainTitle == null) return;

                var field = typeof(mainTitleCtrl).GetField("select_plate_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) return;

                var selectPlate = field.GetValue(mainTitle) as titleSelectPlate;
                if (selectPlate != __instance) return;

                // Get the menu options and announce
                string optionText = GetMainMenuOptionText(mainTitle, __instance.cursor_no);
                if (!Core.Net35Extensions.IsNullOrWhiteSpace(optionText))
                {
                    int totalOptions = GetMainMenuOptionCount(mainTitle);
                    string message = $"Main menu: {optionText} ({__instance.cursor_no + 1} of {totalOptions})";
                    ClipboardManager.Announce(message, TextType.Menu);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in TitleSelectPlate_PlayCursor patch: {ex.Message}");
            }
        }

        private static string GetMainMenuOptionText(mainTitleCtrl mainTitle, int index)
        {
            try
            {
                var field = typeof(mainTitleCtrl).GetField("select_text_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) return null;

                var selectText = field.GetValue(mainTitle) as titleSelectPlate.ButtonParam[][];
                if (selectText == null) return null;

                var typeField = typeof(mainTitleCtrl).GetField("select_type_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (typeField == null) return null;

                int selectType = (int)typeField.GetValue(mainTitle);
                if (selectType < 0 || selectType >= selectText.Length) return null;

                var options = selectText[selectType];
                if (options == null || index < 0 || index >= options.Length) return null;

                return options[index].message_;
            }
            catch
            {
                return null;
            }
        }

        private static int GetMainMenuOptionCount(mainTitleCtrl mainTitle)
        {
            try
            {
                var field = typeof(mainTitleCtrl).GetField("select_text_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) return 0;

                var selectText = field.GetValue(mainTitle) as titleSelectPlate.ButtonParam[][];
                if (selectText == null) return 0;

                var typeField = typeof(mainTitleCtrl).GetField("select_type_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (typeField == null) return 0;

                int selectType = (int)typeField.GetValue(mainTitle);
                if (selectType < 0 || selectType >= selectText.Length) return 0;

                return selectText[selectType]?.Length ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Series Selection Patches

        // Series title selection - announce when entering series selection
        [HarmonyPostfix]
        [HarmonyPatch(typeof(seriesTitleSelectCtrl), "Play")]
        public static void SeriesSelect_Play_Postfix(seriesTitleSelectCtrl __instance)
        {
            try
            {
                _lastSeriesTitle = -1;
                string message = "Select game. Use left and right to choose game, then select Play Title or Select Episode.";
                ClipboardManager.Announce(message, TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in SeriesSelect_Play patch: {ex.Message}");
            }
        }

        // Series title change - announce when changing between GS1, GS2, GS3
        [HarmonyPostfix]
        [HarmonyPatch(typeof(seriesTitleSelectCtrl), "changeTitle")]
        public static void SeriesSelect_ChangeTitle_Postfix(seriesTitleSelectCtrl __instance, TitleId title_id)
        {
            try
            {
                int titleIndex = (int)title_id;
                if (titleIndex == _lastSeriesTitle) return;

                _lastSeriesTitle = titleIndex;
                string titleName = GetGameTitleName(title_id);
                ClipboardManager.Announce(titleName, TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in SeriesSelect_ChangeTitle patch: {ex.Message}");
            }
        }

        private static string GetGameTitleName(TitleId titleId)
        {
            switch (titleId)
            {
                case TitleId.GS1: return "Phoenix Wright: Ace Attorney";
                case TitleId.GS2: return "Phoenix Wright: Ace Attorney - Justice for All";
                case TitleId.GS3: return "Phoenix Wright: Ace Attorney - Trials and Tribulations";
                default: return $"Game {(int)titleId + 1}";
            }
        }

        #endregion

        #region Chapter Jump Patches

        // Chapter jump menu - announce when entering chapter selection
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChapterJumpCtrl), "Play")]
        public static void ChapterJump_Play_Postfix()
        {
            try
            {
                string message = "Chapter selection. Use left and right to choose episode, up and down to choose chapter.";
                ClipboardManager.Announce(message, TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in ChapterJump_Play patch: {ex.Message}");
            }
        }

        // Chapter jump episode change - announce episode name
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChapterJumpInMenuCtrl), "EpisodeDispRefresh")]
        public static void ChapterJump_EpisodeRefresh_Postfix(ChapterJumpInMenuCtrl __instance, int position)
        {
            try
            {
                // Get episode cursor from instance
                var nameTableField = typeof(ChapterJumpInMenuCtrl).GetField("name_table",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (nameTableField == null) return;

                var nameTable = nameTableField.GetValue(__instance) as System.Collections.Generic.List<System.Collections.Generic.List<string>>;
                if (nameTable == null || position < 0 || position >= nameTable.Count) return;

                string episodeName = $"Episode {position + 1}";
                ClipboardManager.Announce(episodeName, TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in ChapterJump_EpisodeRefresh patch: {ex.Message}");
            }
        }

        // GeneralSelectPlateCtrl playCursor - announce initial chapter selection
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GeneralSelectPlateCtrl), "playCursor")]
        public static void GeneralSelectPlate_PlayCursor_Postfix(GeneralSelectPlateCtrl __instance)
        {
            try
            {
                int cursorNo = __instance.cursor_no;
                string optionText = GetGeneralSelectOptionText(__instance, cursorNo);
                if (!Core.Net35Extensions.IsNullOrWhiteSpace(optionText))
                {
                    // Get total count
                    var cursorNumField = typeof(GeneralSelectPlateCtrl).GetField("cursor_num_",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int total = cursorNumField != null ? (int)cursorNumField.GetValue(__instance) : 0;

                    string message = total > 0
                        ? $"{optionText} ({cursorNo + 1} of {total})"
                        : optionText;
                    ClipboardManager.Announce(message, TextType.Menu);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in GeneralSelectPlate_PlayCursor patch: {ex.Message}");
            }
        }

        private static string GetGeneralSelectOptionText(GeneralSelectPlateCtrl selectPlate, int index)
        {
            try
            {
                var field = typeof(GeneralSelectPlateCtrl).GetField("select_list_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) return null;

                var selectList = field.GetValue(selectPlate) as System.Collections.IList;
                if (selectList == null || index < 0 || index >= selectList.Count) return null;

                var item = selectList[index];
                var textField = item.GetType().GetField("text_");
                if (textField == null) return null;

                var textComponent = textField.GetValue(item) as UnityEngine.UI.Text;
                return textComponent?.text;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Detective Menu Patches

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

        #endregion

        #region Selection Plate Patches (Dialogue Choices)

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

        #endregion
    }
}
