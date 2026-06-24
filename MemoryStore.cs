using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace POE_Part3
{
    /// <summary>
    /// Stores facts, interests, and context the bot learns about the user
    /// during the session.  Supports personalised responses (Requirement 5).
    /// </summary>
    public class MemoryStore
    {
        // ── Core storage ─────────────────────────────────────────────────────────
        private readonly Dictionary<string, string> _facts = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _topicHistory = new();

        // Maps topic name → engagement count
        private readonly Dictionary<string, int> _interestScores = new(StringComparer.OrdinalIgnoreCase);

        // Explicitly declared interests ("I'm interested in privacy")
        private readonly List<string> _declaredInterests = new();

        // ── Public ───────────────────────────────────────────────────────────────
        public string? UserName { get; set; }

        // ════════════════════════════════════════════════════════════════════════
        // FACT MANAGEMENT
        // ════════════════════════════════════════════════════════════════════════

        public void Remember(string key, string value) => _facts[key] = value;
        public string? Recall(string key) => _facts.TryGetValue(key, out var v) ? v : null;
        public bool Has(string key) => _facts.ContainsKey(key);

        // ════════════════════════════════════════════════════════════════════════
        // TOPIC HISTORY
        // ════════════════════════════════════════════════════════════════════════

        public void AddTopic(string topic)
        {
            if (!_topicHistory.Contains(topic, StringComparer.OrdinalIgnoreCase))
                _topicHistory.Add(topic);

            if (_interestScores.ContainsKey(topic))
                _interestScores[topic]++;
            else
                _interestScores[topic] = 1;
        }

        public IReadOnlyList<string> Topics => _topicHistory.AsReadOnly();
        public bool HasAskedAbout(string topic) =>
            _topicHistory.Exists(t => t.Contains(topic, StringComparison.OrdinalIgnoreCase));

        // ════════════════════════════════════════════════════════════════════════
        // INTEREST MANAGEMENT (Requirement 5)
        // ════════════════════════════════════════════════════════════════════════

        public void RememberInterest(string topic)
        {
            if (!_declaredInterests.Contains(topic, StringComparer.OrdinalIgnoreCase))
                _declaredInterests.Add(topic);
            AddTopic(topic);
        }

        public string? GetTopInterest()
        {
            if (_declaredInterests.Count > 0)
                return _declaredInterests.Last();

            if (_interestScores.Count == 0) return null;

            return _interestScores.OrderByDescending(kv => kv.Value).First().Key;
        }

        public string BuildInterestAcknowledgement(string interest) =>
            $"Great! I'll remember that you're interested in {interest}. " +
            $"It's a crucial part of staying safe online. " +
            $"I'll personalise my tips for you accordingly! 🎯";

        public string GetInterestCallout(string currentTopic)
        {
            foreach (var interest in _declaredInterests)
            {
                if (currentTopic.Contains(interest, StringComparison.OrdinalIgnoreCase) ||
                    interest.Contains(currentTopic, StringComparison.OrdinalIgnoreCase))
                {
                    return $"As someone interested in {interest}, this is especially relevant — ";
                }
            }

            var topInterest = GetTopInterest();
            if (topInterest != null &&
                !topInterest.Equals(currentTopic, StringComparison.OrdinalIgnoreCase))
            {
                return $"Given your interest in {topInterest}, you might also explore this — ";
            }

            return string.Empty;
        }

        // ════════════════════════════════════════════════════════════════════════
        // RECALL HINTS
        // ════════════════════════════════════════════════════════════════════════

        public string GetRecallHint(string currentInput)
        {
            foreach (var topic in _topicHistory)
            {
                if (currentInput.Contains(topic, StringComparison.OrdinalIgnoreCase))
                    return $"(You asked about {topic} earlier — building on that)";
            }
            return string.Empty;
        }

        // ════════════════════════════════════════════════════════════════════════
        // PASSIVE LEARNING
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Scans user message for learnable facts and declared interests.
        /// Returns an acknowledgement string if an interest was detected,
        /// or null otherwise.
        /// </summary>
        public string? LearnFrom(string input)
        {
            string? acknowledgement = null;

            // "my name is X" / "I am X" / "call me X"
            var nameMatch = Regex.Match(input,
                @"(?:my name is|i am|call me)\s+([A-Za-z]+)",
                RegexOptions.IgnoreCase);
            if (nameMatch.Success && UserName == null)
            {
                Remember("name", nameMatch.Groups[1].Value);
                UserName = nameMatch.Groups[1].Value;
            }

            // "I work as X" / "I'm a X"
            var jobMatch = Regex.Match(input,
                @"(?:i work as|i'm a|i am a)\s+(.+?)(?:\s*$|\.)",
                RegexOptions.IgnoreCase);
            if (jobMatch.Success)
                Remember("job", jobMatch.Groups[1].Value.Trim());

            // "I use Windows/Mac/Linux/Android/iPhone/iOS"
            var deviceMatch = Regex.Match(input,
                @"i use\s+(windows|mac|linux|android|iphone|ios)",
                RegexOptions.IgnoreCase);
            if (deviceMatch.Success)
                Remember("device", deviceMatch.Groups[1].Value);

            // Explicit interest declarations
            var interestPatterns = new[]
            {
                @"(?:i'm interested in|i am interested in|interested in)\s+([a-z\s]+?)(?:\s*$|[.,!?])",
                @"(?:i care about|care about)\s+([a-z\s]+?)(?:\s*$|[.,!?])",
                @"(?:i want to learn about|tell me about)\s+([a-z\s]+?)(?:\s*$|[.,!?])",
                @"(?:i'm worried about|worried about|concerned about)\s+([a-z\s]+?)(?:\s*$|[.,!?])"
            };

            foreach (var pattern in interestPatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string topic = match.Groups[1].Value.Trim().ToLower();
                    if (topic.Length > 3 &&
                        !_declaredInterests.Contains(topic, StringComparer.OrdinalIgnoreCase))
                    {
                        _declaredInterests.Add(topic);
                        RememberInterest(topic);
                        acknowledgement = BuildInterestAcknowledgement(topic);
                    }
                    break;
                }
            }

            return acknowledgement;
        }

        // ════════════════════════════════════════════════════════════════════════
        // PERSONALISED PREFIX BUILDERS
        // ════════════════════════════════════════════════════════════════════════

        public string BuildPersonalPrefix() =>
            !string.IsNullOrEmpty(UserName) ? $"{UserName}, " : string.Empty;

        public string BuildInterestPrefix(string currentTopic) =>
            GetInterestCallout(currentTopic);

        // ════════════════════════════════════════════════════════════════════════
        // MEMORY SUMMARY (for "what do you remember" command)
        // ════════════════════════════════════════════════════════════════════════

        public string GetMemorySummary()
        {
            var lines = new List<string>
            {
                "🧠  WHAT I REMEMBER ABOUT YOU\n──────────────────────────────────────────"
            };

            if (!string.IsNullOrEmpty(UserName))
                lines.Add($"• Name:    {UserName}");

            if (_facts.TryGetValue("job", out var job))
                lines.Add($"• Job:     {job}");

            if (_facts.TryGetValue("device", out var device))
                lines.Add($"• Device:  {device}");

            if (_declaredInterests.Count > 0)
                lines.Add($"• Interests:       {string.Join(", ", _declaredInterests)}");

            if (_topicHistory.Count > 0)
                lines.Add($"• Topics explored: {string.Join(", ", _topicHistory)}");

            if (lines.Count == 1)
                lines.Add("Nothing stored yet — keep chatting!");

            return string.Join("\n", lines);
        }
    }
}
