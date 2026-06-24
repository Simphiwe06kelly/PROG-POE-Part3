using POE_Part3;
using System;
using System.Windows;
using System.Windows.Media;

namespace POE_Part3
{
    /// <summary>
    /// Represents a single chat message bubble in the conversation list.
    /// Visual properties (colours, alignment, prefix) are derived from
    /// the MessageType so the XAML DataTemplate needs no converters.
    /// </summary>
    public class ChatMessage
    {
        public string Text { get; set; } = "";
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // ── Visual properties derived from Type ──────────────────────────────

        public Brush Foreground => Type switch
        {
            MessageType.User => new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x9C)), // mint green
            MessageType.Bot => new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFD)), // near-white
            MessageType.System => new SolidColorBrush(Color.FromRgb(0x00, 0xD4, 0xFF)), // cyan
            MessageType.Warning => new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)), // red
            MessageType.Tip => new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)), // gold
            _ => Brushes.White
        };

        public Brush Background => Type switch
        {
            MessageType.User => new SolidColorBrush(Color.FromArgb(0x30, 0x00, 0xFF, 0x9C)),
            MessageType.Bot => new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0xD4, 0xFF)),
            MessageType.Warning => new SolidColorBrush(Color.FromArgb(0x25, 0xFF, 0x47, 0x57)),
            MessageType.Tip => new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xD7, 0x00)),
            _ => new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF))
        };

        public HorizontalAlignment Alignment =>
            Type == MessageType.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        public string Prefix => Type switch
        {
            MessageType.User => "YOU",
            MessageType.Bot => "CYBERBOT",
            MessageType.System => "SYSTEM",
            MessageType.Warning => "⚠ WARNING",
            MessageType.Tip => "💡 TIP",
            _ => "INFO"
        };

        public string TimeString => Timestamp.ToString("HH:mm");
    }
}
