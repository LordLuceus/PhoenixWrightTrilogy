using System;
using System.Text;
using HarmonyLib;
using AccessibilityMod.Core;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class CourtRecordPatches
    {
        private static int _lastRecordCursor = -1;
        private static int _lastRecordPage = -1;
        private static int _lastRecordType = -1;

        #region Court Record Open/Close

        // Court record opened - announce tab type and first item
        [HarmonyPostfix]
        [HarmonyPatch(typeof(recordListCtrl), "noteOpen")]
        public static void NoteOpen_Postfix(recordListCtrl __instance, int mode)
        {
            try
            {
                string tabName = mode == 0 ? "Evidence" : "Profiles";

                // Get item count for current tab
                int itemCount = 0;
                if (__instance.record_data_ != null && mode < __instance.record_data_.Count)
                {
                    itemCount = __instance.record_data_[mode].cursor_num_;
                }

                string message = $"Court Record: {tabName}. {itemCount} items.";
                ClipboardManager.Announce(message, TextType.Menu);

                // Reset tracking
                _lastRecordCursor = -1;
                _lastRecordPage = -1;
                _lastRecordType = mode;
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in NoteOpen patch: {ex.Message}");
            }
        }

        #endregion

        #region Tab Switching

        // Called when changing from Evidence to Profiles or vice versa
        [HarmonyPostfix]
        [HarmonyPatch(typeof(recordListCtrl), "ChangeRecord")]
        public static void ChangeRecord_Postfix(recordListCtrl __instance)
        {
            try
            {
                int recordType = __instance.record_type;

                // Only announce if we actually switched tabs (not initial open)
                if (_lastRecordType != -1 && _lastRecordType != recordType)
                {
                    string tabName = recordType == 0 ? "Evidence" : "Profiles";

                    int itemCount = 0;
                    if (__instance.record_data_ != null && recordType < __instance.record_data_.Count)
                    {
                        itemCount = __instance.record_data_[recordType].cursor_num_;
                    }

                    string message = $"{tabName}. {itemCount} items.";
                    ClipboardManager.Announce(message, TextType.Menu);
                }

                _lastRecordType = recordType;
                _lastRecordCursor = -1; // Reset cursor to force announcement of first item
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in ChangeRecord patch: {ex.Message}");
            }
        }

        #endregion

        #region Cursor Movement

        // Called when cursor moves or selection changes
        [HarmonyPostfix]
        [HarmonyPatch(typeof(recordListCtrl), "cursorRecord")]
        public static void CursorRecord_Postfix(recordListCtrl __instance, int in_type, int in_no)
        {
            try
            {
                // Avoid duplicate announcements from initial open
                if (_lastRecordCursor == in_no && _lastRecordType == in_type)
                    return;

                _lastRecordCursor = in_no;
                _lastRecordType = in_type;

                // Get current pice data
                piceData currentItem = __instance.current_pice_;
                if (currentItem == null) return;

                // Build announcement with name and description
                StringBuilder sb = new StringBuilder();
                sb.Append(currentItem.name);

                // Add description lines
                string comment0 = currentItem.comment00;
                string comment1 = currentItem.comment01;
                string comment2 = currentItem.comment02;

                if (!Net35Extensions.IsNullOrWhiteSpace(comment0))
                {
                    sb.Append(". ").Append(comment0);
                }
                if (!Net35Extensions.IsNullOrWhiteSpace(comment1))
                {
                    sb.Append(" ").Append(comment1);
                }
                if (!Net35Extensions.IsNullOrWhiteSpace(comment2))
                {
                    sb.Append(" ").Append(comment2);
                }

                // Add position info
                int pageNum = GetPageNum(__instance);
                int onePageMax = __instance.one_page_max;
                int totalIndex = in_no + (pageNum * onePageMax) + 1;
                int totalItems = __instance.record_data_[in_type].cursor_num_;

                sb.Append($" ({totalIndex} of {totalItems})");

                // Check if item has details available
                if (currentItem.detail_id != 0 || currentItem.obj_id != 0)
                {
                    sb.Append(" - Press A for details");
                }

                ClipboardManager.Announce(sb.ToString(), TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in CursorRecord patch: {ex.Message}");
            }
        }

        private static int GetPageNum(recordListCtrl instance)
        {
            try
            {
                // page_num_ is a property that reads from advCtrl
                if (instance.record_type == 0)
                {
                    return advCtrl.instance.sub_window_.note_.item_page;
                }
                return advCtrl.instance.sub_window_.note_.man_page;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Page Changes

        // Called when page changes (after animation)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(recordListCtrl), "recordPageChange")]
        public static void RecordPageChange_Postfix(recordListCtrl __instance)
        {
            try
            {
                int pageNum = GetPageNum(__instance);
                int pageCnt = GetPageCount(__instance);

                if (pageCnt > 1)
                {
                    string message = $"Page {pageNum + 1} of {pageCnt}";
                    ClipboardManager.Announce(message, TextType.Menu);
                }

                _lastRecordPage = pageNum;
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in RecordPageChange patch: {ex.Message}");
            }
        }

        private static int GetPageCount(recordListCtrl instance)
        {
            try
            {
                var field = typeof(recordListCtrl).GetField("page_cnt_",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return (int)field.GetValue(instance);
                }
            }
            catch { }
            return 1;
        }

        #endregion

        #region Detail View

        // Announce when viewing evidence/profile details
        [HarmonyPostfix]
        [HarmonyPatch(typeof(recordDetailCtrl), "CoroutineViewDetail")]
        public static void ViewDetail_Postfix(int in_id)
        {
            try
            {
                // Get the current item from the record list
                var recordList = recordListCtrl.instance;
                if (recordList == null) return;

                piceData currentItem = recordList.current_pice_;
                if (currentItem == null) return;

                string message = $"Viewing details: {currentItem.name}. Press B to close.";
                ClipboardManager.Announce(message, TextType.Menu);
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Error($"Error in ViewDetail patch: {ex.Message}");
            }
        }

        #endregion

        #region State Reset

        // Reset state when court record closes
        public static void ResetState()
        {
            _lastRecordCursor = -1;
            _lastRecordPage = -1;
            _lastRecordType = -1;
        }

        #endregion
    }
}
