using System;
using System.Text;
using HarmonyLib;
using AccessibilityMod.Core;
using AccessibilityMod.Services;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class DialoguePatches
    {
        private static string _lastAnnouncedText = "";
        private static int _lastSpeakerId = -1;

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
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Arrow patch: {ex.Message}");
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
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in Board patch: {ex.Message}");
            }
        }

        // Hook when speaker name changes
        [HarmonyPostfix]
        [HarmonyPatch(typeof(messageBoardCtrl), "name_plate")]
        public static void NamePlate_Postfix(messageBoardCtrl __instance, bool in_name, int in_name_no, int in_pos)
        {
            try
            {
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
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in NamePlate patch: {ex.Message}");
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
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in LoadMsgSet patch: {ex.Message}");
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
                            speakerName = CharacterNameService.GetName(GSStatic.message_work_.speaker_id);
                        }
                    }
                    catch { }
                }

                ClipboardManager.Output(speakerName, text, TextType.Dialogue);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error outputting dialogue: {ex.Message}");
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
