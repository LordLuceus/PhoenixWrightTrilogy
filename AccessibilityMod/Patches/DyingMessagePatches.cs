using System.Reflection;
using AccessibilityMod.Core;
using AccessibilityMod.Services;
using HarmonyLib;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Harmony patches for the Dying Message (connect the dots) minigame.
    /// </summary>
    [HarmonyPatch]
    public static class DyingMessagePatches
    {
        private static int _lastLineCount = 0;
        private static bool _wasInLineState = false;
        private static int _lastStartIndex = -1;

        /// <summary>
        /// Patch for _updateDyingMessageMain to track state changes.
        /// </summary>
        [HarmonyPatch(typeof(DyingMessageUtil), "_updateDyingMessageMain")]
        [HarmonyPostfix]
        public static void OnUpdateDyingMessageMain(DyingMessageUtil __instance)
        {
            try
            {
                // Check current state
                var stateField = typeof(DyingMessageUtil).GetField(
                    "stete_",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (stateField != null)
                {
                    var state = stateField.GetValue(__instance);
                    bool isInLineState = state.ToString() == "Line";

                    // Check if we just started a line
                    if (isInLineState && !_wasInLineState)
                    {
                        var startField = typeof(DyingMessageUtil).GetField(
                            "linepoint_start_index_",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );
                        if (startField != null)
                        {
                            int startIndex = (int)startField.GetValue(__instance);
                            _lastStartIndex = startIndex;
                            DyingMessageNavigator.OnLineStarted(startIndex);
                        }
                    }
                    // Check if line was cancelled
                    else if (!isInLineState && _wasInLineState)
                    {
                        // Line state ended - either connected or cancelled
                        // We'll detect connection via line count change
                    }

                    _wasInLineState = isInLineState;
                }

                // Check line count changes
                var lineListField = typeof(DyingMessageUtil).GetField(
                    "line_list_",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (lineListField != null)
                {
                    var lineList = lineListField.GetValue(__instance);
                    if (lineList != null)
                    {
                        var countProp = lineList.GetType().GetProperty("Count");
                        if (countProp != null)
                        {
                            int currentCount = (int)countProp.GetValue(lineList, null);

                            if (currentCount > _lastLineCount)
                            {
                                // A line was added - get the last line info
                                var itemMethod = lineList.GetType().GetProperty("Item");
                                if (itemMethod != null)
                                {
                                    var lastLine = itemMethod.GetValue(
                                        lineList,
                                        new object[] { currentCount - 1 }
                                    );
                                    var pt1Field = lastLine.GetType().GetField("pt1");
                                    var pt2Field = lastLine.GetType().GetField("pt2");

                                    if (pt1Field != null && pt2Field != null)
                                    {
                                        int pt1 = (int)pt1Field.GetValue(lastLine);
                                        int pt2 = (int)pt2Field.GetValue(lastLine);
                                        DyingMessageNavigator.OnLineCreated(pt1, pt2);
                                    }
                                }
                            }
                            else if (currentCount < _lastLineCount)
                            {
                                // A line was removed
                                DyingMessageNavigator.OnLineDeleted();
                            }

                            _lastLineCount = currentCount;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Reset state when minigame starts.
        /// </summary>
        [HarmonyPatch(typeof(DyingMessageUtil), "init_die_message")]
        [HarmonyPostfix]
        public static void OnInitDieMessage()
        {
            _lastLineCount = 0;
            _wasInLineState = false;
            _lastStartIndex = -1;
        }
    }
}
