using System;
using System.Collections.Generic;

namespace AccessibilityMod.Services
{
    /// <summary>
    /// Service for providing accessibility descriptions for evidence detail views.
    /// The game displays evidence details as images with no text, so this service
    /// provides hand-written descriptions keyed by game and detail_id.
    /// </summary>
    public static class EvidenceDetailService
    {
        /// <summary>
        /// Represents accessibility descriptions for an evidence detail view.
        /// </summary>
        public class DetailDescription
        {
            public string[] Pages { get; private set; }

            public DetailDescription(params string[] pages)
            {
                Pages = pages ?? new string[0];
            }

            public string GetPage(int pageIndex)
            {
                if (Pages == null || pageIndex < 0 || pageIndex >= Pages.Length)
                    return null;
                return Pages[pageIndex];
            }

            public int PageCount
            {
                get { return Pages != null ? Pages.Length : 0; }
            }
        }

        // GS1 evidence detail descriptions, keyed by detail_id (index into status_ext_bg_tbl)
        private static readonly Dictionary<int, DetailDescription> GS1_DETAILS = new Dictionary<
            int,
            DetailDescription
        >
        {
            {
                2,
                new DetailDescription(
                    @"[September 5, 9:27 AM]
Maya: Mia! What's up? You haven't called in a while.
Mia: Well, actually there's something I want you to hold on to for me.
Maya: Again? What is it this time?",
                    @"Mia: It's... a clock. It's made to look like that statue, ""The Thinker."" And it tells you the time! Ah... I should probably tell you, the clock isn't talking right now.
Maya: Huh? It's not working? That's lame!",
                    @"Mia: I had to take the clockwork out, sorry. I put some papers inside it instead.
Maya: Papers? Is that the evidence, then?
Mia: I'll leave that one up to your imagination. See you tonight at nine."
                )
            },
            {
                9,
                new DetailDescription(
                    @"Case Summary:
12/28, 2001
Elevator, District Court.
Air in elevator was oxygen depleted at time of incident.
No clues found on the scene.",
                    @"Victim Data:
Gregory Edgeworth (Age 35)
Defense attorney. Trapped in elevator returning from a lost trial with son Miles (Age 9).
One bullet found in heart. The murder weapon was fired twice.",
                    @"Suspect Data:
Yanni Yogi (Age 37)
Court bailiff, trapped with the Edgeworths. Memory loss due to oxygen deprivation.
After his arrest, fiancee Polly Jenkins committed suicide."
                )
            },
        };

        // GS2 evidence detail descriptions
        private static readonly Dictionary<int, DetailDescription> GS2_DETAILS = new Dictionary<
            int,
            DetailDescription
        >
        { };

        // GS3 evidence detail descriptions
        private static readonly Dictionary<int, DetailDescription> GS3_DETAILS = new Dictionary<
            int,
            DetailDescription
        >
        { };

        /// <summary>
        /// Get the description for an evidence detail view.
        /// </summary>
        /// <param name="detailId">The detail_id from piceData (index into status_ext_bg_tbl)</param>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <returns>Description text, or null if not available</returns>
        public static string GetDescription(int detailId, int pageIndex = 0)
        {
            try
            {
                TitleId currentGame = TitleId.GS1;
                try
                {
                    if (GSStatic.global_work_ != null)
                    {
                        currentGame = GSStatic.global_work_.title;
                    }
                }
                catch { }

                Dictionary<int, DetailDescription> detailDict = null;
                switch (currentGame)
                {
                    case TitleId.GS1:
                        detailDict = GS1_DETAILS;
                        break;
                    case TitleId.GS2:
                        detailDict = GS2_DETAILS;
                        break;
                    case TitleId.GS3:
                        detailDict = GS3_DETAILS;
                        break;
                    default:
                        detailDict = GS1_DETAILS;
                        break;
                }

                if (detailDict != null && detailDict.ContainsKey(detailId))
                {
                    return detailDict[detailId].GetPage(pageIndex);
                }
            }
            catch (Exception ex)
            {
                AccessibilityMod.Core.AccessibilityMod.Logger?.Warning(
                    $"Error getting evidence detail description: {ex.Message}"
                );
            }

            return null;
        }

        /// <summary>
        /// Check if a description exists for the given detail.
        /// </summary>
        public static bool HasDescription(int detailId)
        {
            try
            {
                TitleId currentGame = TitleId.GS1;
                try
                {
                    if (GSStatic.global_work_ != null)
                    {
                        currentGame = GSStatic.global_work_.title;
                    }
                }
                catch { }

                Dictionary<int, DetailDescription> detailDict = null;
                switch (currentGame)
                {
                    case TitleId.GS1:
                        detailDict = GS1_DETAILS;
                        break;
                    case TitleId.GS2:
                        detailDict = GS2_DETAILS;
                        break;
                    case TitleId.GS3:
                        detailDict = GS3_DETAILS;
                        break;
                    default:
                        detailDict = GS1_DETAILS;
                        break;
                }

                return detailDict != null && detailDict.ContainsKey(detailId);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the number of pages with descriptions for a detail.
        /// </summary>
        public static int GetDescriptionPageCount(int detailId)
        {
            try
            {
                TitleId currentGame = TitleId.GS1;
                try
                {
                    if (GSStatic.global_work_ != null)
                    {
                        currentGame = GSStatic.global_work_.title;
                    }
                }
                catch { }

                Dictionary<int, DetailDescription> detailDict = null;
                switch (currentGame)
                {
                    case TitleId.GS1:
                        detailDict = GS1_DETAILS;
                        break;
                    case TitleId.GS2:
                        detailDict = GS2_DETAILS;
                        break;
                    case TitleId.GS3:
                        detailDict = GS3_DETAILS;
                        break;
                    default:
                        detailDict = GS1_DETAILS;
                        break;
                }

                if (detailDict != null && detailDict.ContainsKey(detailId))
                {
                    return detailDict[detailId].PageCount;
                }
            }
            catch { }

            return 0;
        }
    }
}
