using System;
using System.Collections.Generic;
using System.Linq;

namespace POE_Part3
{
    /// <summary>
    /// A single timestamped entry in the chatbot's activity log.
    /// </summary>
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }

        public ActivityLogEntry(string description)
        {
            Timestamp = DateTime.Now;
            Description = description;
        }

        public override string ToString() => $"[{Timestamp:dd MMM HH:mm}] {Description}";
    }

    /// <summary>
    /// In-memory record of the chatbot's recent actions — tasks added,
    /// completed, or deleted; quizzes started or finished — each timestamped.
    /// Keeps only the most recent <see cref="MaxEntries"/> items and is
    /// retrievable via chat command ("show activity log", "what have you
    /// done for me?").
    /// </summary>
    public class ActivityLog
    {
        private const int MaxEntries = 10;
        private readonly List<ActivityLogEntry> _entries = new();

        /// <summary>
        /// Records a new action. If the log is already at capacity, the
        /// oldest entry is dropped to make room.
        /// </summary>
        public void Log(string description)
        {
            _entries.Add(new ActivityLogEntry(description));
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
        }

        /// <summary>
        /// True if the input is asking to see the activity log, regardless
        /// of exact phrasing.
        /// </summary>
        public bool IsLogCommand(string lowerInput) =>
            lowerInput.Contains("activity log") || lowerInput.Contains("show log") ||
            lowerInput.Contains("view log") || lowerInput.Contains("what have you done") ||
            lowerInput.Contains("recent activity") || lowerInput.Contains("show activity") ||
            lowerInput.Contains("action history") || lowerInput.Contains("history of actions");

        /// <summary>
        /// Returns the most recent entries, newest first, formatted for chat.
        /// </summary>
        public string GetFormattedLog()
        {
            if (_entries.Count == 0)
                return "📜  No activity logged yet this session. Try adding a task or starting the quiz!";

            string list = string.Join("\n", _entries
                .OrderByDescending(e => e.Timestamp)
                .Select(e => e.ToString()));

            return
                "📜  RECENT ACTIVITY\n" +
                "──────────────────────────────────────────\n" +
                list;
        }
    }
}