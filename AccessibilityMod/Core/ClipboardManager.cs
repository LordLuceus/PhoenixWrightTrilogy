using System;
using System.Collections.Generic;

namespace AccessibilityMod.Core
{
    public static class ClipboardManager
    {
        private static readonly Queue<string> MessageQueue = new Queue<string>();

        private static string _currentSpeaker = "";
        private static string _currentText = "";
        private static TextType _currentType = TextType.Dialogue;

        private static string _lastEnqueuedMessage = "";
        private static DateTime _lastEnqueueTime = DateTime.MinValue;
        private const double DuplicateWindowSeconds = 0.5;

        public static string DequeueMessage()
        {
            if (MessageQueue.Count > 0)
            {
                return MessageQueue.Dequeue();
            }
            return null;
        }

        private static void EnqueueText(string text)
        {
            if (Net35Extensions.IsNullOrWhiteSpace(text))
                return;

            DateTime now = DateTime.UtcNow;
            if (text == _lastEnqueuedMessage
                && (now - _lastEnqueueTime).TotalSeconds < DuplicateWindowSeconds)
            {
                return;
            }

            _lastEnqueuedMessage = text;
            _lastEnqueueTime = now;
            MessageQueue.Enqueue(text);
        }

        public static void ClearQueue()
        {
            MessageQueue.Clear();
        }

        public static void RepeatLast()
        {
            if (!Net35Extensions.IsNullOrWhiteSpace(_currentText))
            {
                string formattedText = FormatText(_currentSpeaker, _currentText, _currentType);
                EnqueueText(formattedText);
                AccessibilityMod.Logger?.Msg($"Repeating: '{formattedText}'");
            }
            else
            {
                AccessibilityMod.Logger?.Msg("Nothing to repeat");
            }
        }

        public static void Output(string speaker, string text, TextType textType = TextType.Dialogue)
        {
            if (Net35Extensions.IsNullOrWhiteSpace(text))
                return;

            string formattedText = FormatText(speaker, text, textType);

            // Store for repeat functionality
            if (textType == TextType.Dialogue || textType == TextType.Narrator)
            {
                _currentSpeaker = speaker ?? "";
                _currentText = text;
                _currentType = textType;
            }

            EnqueueText(formattedText);
            AccessibilityMod.Logger?.Msg($"[{textType}] {formattedText}");
        }

        public static void Announce(string text, TextType textType = TextType.SystemMessage)
        {
            Output("", text, textType);
        }

        private static string FormatText(string speaker, string text, TextType textType)
        {
            text = TextCleaner.Clean(text);

            switch (textType)
            {
                case TextType.Dialogue:
                    if (!Net35Extensions.IsNullOrWhiteSpace(speaker))
                        return $"{speaker}: {text}";
                    return text;
                case TextType.Menu:
                case TextType.MenuChoice:
                    return text;
                default:
                    return text;
            }
        }
    }

    public enum TextType
    {
        Dialogue,
        Narrator,
        Menu,
        MenuChoice,
        Investigation,
        Evidence,
        SystemMessage,
        Trial,
        PsycheLock
    }
}
