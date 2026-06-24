using System;
using System.Collections.Generic;
using System.Linq;

namespace POE_Part3 
{
    // ── Sentiment result ─────────────────────────────────────────────────────────
    public enum Sentiment { Positive, Negative, Anxious, Angry, Curious, Neutral }

    public class SentimentResult
    {
        public Sentiment Sentiment { get; init; }
        public double Confidence { get; init; }  // 0.0 – 1.0
        public string Label { get; init; } = "";
    }

    // ── Delegate definitions used across the chatbot ─────────────────────────────
    public delegate void BotResponseDelegate(string message, MessageType type);
    public delegate void SentimentChangedDelegate(SentimentResult result);
    public delegate string KeywordMatchDelegate(string input);

    public enum MessageType { Bot, User, System, Warning, Tip }

    // ── Sentiment analyser ───────────────────────────────────────────────────────
    public static class SentimentAnalyser
    {
        private static readonly HashSet<string> PositiveWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "thanks","thank","great","awesome","cool","love","good","helpful",
            "wonderful","nice","fantastic","perfect","excellent","brilliant",
            "happy","pleased","glad","appreciate","amazing","superb","cheers"
        };

        private static readonly HashSet<string> NegativeWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "bad","terrible","awful","useless","hate","frustrated","annoyed",
            "stupid","broken","wrong","horrible","disappointing","poor","sad",
            "unhappy","not working","doesn't work","cant","can't"
        };

        private static readonly HashSet<string> AnxiousWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "worried","scared","afraid","nervous","anxious","hacked","compromised",
            "stolen","breached","attacked","virus","infected","help",
            "urgent","emergency","leak","exposed","danger","dangerous"
        };

        private static readonly HashSet<string> AngryWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "angry","furious","outraged","disgusting","pathetic","rubbish",
            "useless bot","idiot","ridiculous","unacceptable","worst","terrible"
        };

        private static readonly HashSet<string> CuriousWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "what","how","why","when","where","who","explain","tell me","curious",
            "wonder","question","learn","understand","know","teach","show"
        };

        /// <summary>Returns the dominant sentiment detected in the input string.</summary>
        public static SentimentResult Analyse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new SentimentResult { Sentiment = Sentiment.Neutral, Confidence = 1.0, Label = "😐 Neutral" };

            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int pos = words.Count(w => PositiveWords.Contains(w));
            int neg = words.Count(w => NegativeWords.Contains(w));
            int anxious = words.Count(w => AnxiousWords.Contains(w));
            int angry = words.Count(w => AngryWords.Contains(w));
            int curious = words.Count(w => CuriousWords.Contains(w));

            // Also scan full sentence for multi-word phrases
            if (input.Contains("help", StringComparison.OrdinalIgnoreCase)) anxious++;
            if (input.Contains("thank", StringComparison.OrdinalIgnoreCase)) pos++;

            int total = pos + neg + anxious + angry + curious;
            if (total == 0)
                return new SentimentResult { Sentiment = Sentiment.Neutral, Confidence = 0.9, Label = "😐 Neutral" };

            var scores = new Dictionary<Sentiment, int>
            {
                [Sentiment.Positive] = pos,
                [Sentiment.Negative] = neg,
                [Sentiment.Anxious] = anxious,
                [Sentiment.Angry] = angry,
                [Sentiment.Curious] = curious
            };

            var winner = scores.OrderByDescending(kv => kv.Value).First();
            double confidence = Math.Min(1.0, winner.Value / (double)Math.Max(1, total));

            string label = winner.Key switch
            {
                Sentiment.Positive => "😊 Positive",
                Sentiment.Negative => "😟 Negative",
                Sentiment.Anxious => "😰 Anxious",
                Sentiment.Angry => "😠 Frustrated",
                Sentiment.Curious => "🤔 Curious",
                _ => "😐 Neutral"
            };

            return new SentimentResult { Sentiment = winner.Key, Confidence = confidence, Label = label };
        }

        /// <summary>
        /// Returns a short mood-aware prefix the bot prepends to its normal response.
        /// </summary>
        public static string GetMoodPrefix(SentimentResult result) => result.Sentiment switch
        {
            Sentiment.Positive => "Glad you're feeling good! 😊 ",
            Sentiment.Negative => "I'm sorry you're having a tough time. Let me help — ",
            Sentiment.Anxious => "Don't worry, I've got you. Take a breath — ",
            Sentiment.Angry => "I hear your frustration. Let me address that properly — ",
            Sentiment.Curious => "Great question! ",
            _ => ""
        };
    }
}
