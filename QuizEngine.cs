using POE_Part3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace POE_Part3
{
    /// <summary>
    /// Drives the cybersecurity quiz mini-game: holds the question bank,
    /// tracks progress through a single play-through, evaluates answers given
    /// in varied formats (letter, number, or free text), produces the
    /// chat-formatted question/feedback/results strings, and records quiz
    /// start/finish events to the ActivityLog.
    /// </summary>
    public class QuizEngine
    {
        private readonly List<QuizQuestion> _allQuestions;
        private readonly ActivityLog _activityLog;
        private List<QuizQuestion> _activeQuestions = new();
        private readonly Random _rng = new();

        private int _currentIndex;
        private int _score;
        private bool _isActive;

        public bool IsActive => _isActive;

        public QuizEngine(ActivityLog activityLog)
        {
            _activityLog = activityLog;
            _allQuestions = BuildQuestionBank();
        }

        // ════════════════════════════════════════════════════════════════
        // DETECTION
        // ════════════════════════════════════════════════════════════════

        public bool IsStartCommand(string lowerInput) =>
            lowerInput.Contains("start quiz") || lowerInput.Contains("take quiz") ||
            lowerInput.Contains("take a quiz") || lowerInput.Contains("quiz me") ||
            lowerInput.Contains("cybersecurity quiz") || lowerInput.Contains("begin quiz") ||
            lowerInput.Contains("play quiz") || lowerInput.Contains("start the quiz") ||
            (lowerInput.Contains("quiz") && (lowerInput.Contains("start") || lowerInput.Contains("play")));

        public bool IsExitCommand(string lowerInput) =>
            lowerInput is "exit" or "quit" or "stop quiz" or "cancel quiz" or "end quiz";

        // ════════════════════════════════════════════════════════════════
        // GAMEPLAY
        // ════════════════════════════════════════════════════════════════

        public string StartQuiz()
        {
            _activeQuestions = _allQuestions.OrderBy(_ => _rng.Next()).ToList();
            _currentIndex = 0;
            _score = 0;
            _isActive = true;

            _activityLog.Log("Started cybersecurity quiz");

            return
                "🎮  CYBERSECURITY QUIZ TIME!\n" +
                "──────────────────────────────────────────\n" +
                $"Let's test your knowledge with {_activeQuestions.Count} questions —\n" +
                "a mix of multiple choice and true/false. Here goes!\n\n" +
                FormatQuestion();
        }

        public string EndQuizEarly()
        {
            int answered = _currentIndex;
            _isActive = false;

            _activityLog.Log($"Ended quiz early — scored {_score}/{answered}");

            return
                $"🛑  Quiz ended early. You scored {_score}/{answered} on the\n" +
                "questions you answered. Say 'start quiz' anytime to try again!";
        }

        public string SubmitAnswer(string rawAnswer)
        {
            if (!_isActive || _currentIndex >= _activeQuestions.Count)
                return "There's no quiz in progress right now. Say 'start quiz' to begin one!";

            var question = _activeQuestions[_currentIndex];
            string lowerAnswer = rawAnswer.ToLower().Trim();

            int? parsedIndex = ParseAnswerIndex(question, lowerAnswer);
            bool isCorrect = parsedIndex.HasValue && parsedIndex.Value == question.CorrectIndex;

            string feedback;
            if (parsedIndex == null)
            {
                feedback =
                    "🤔  I couldn't quite tell what your answer was.\n" +
                    $"The correct answer was: {question.Options[question.CorrectIndex]}\n\n" +
                    question.Explanation;
            }
            else if (isCorrect)
            {
                _score++;
                feedback = "✅  Correct!\n\n" + question.Explanation;
            }
            else
            {
                feedback =
                    $"❌  Not quite. The correct answer was: {question.Options[question.CorrectIndex]}\n\n" +
                    question.Explanation;
            }

            _currentIndex++;

            if (_currentIndex >= _activeQuestions.Count)
            {
                _isActive = false;
                _activityLog.Log($"Completed cybersecurity quiz — scored {_score}/{_activeQuestions.Count}");
                return feedback + "\n\n" + BuildFinalResults();
            }

            return feedback + "\n\n" + FormatQuestion();
        }

        // ════════════════════════════════════════════════════════════════
        // FORMATTING
        // ════════════════════════════════════════════════════════════════

        private string FormatQuestion()
        {
            var q = _activeQuestions[_currentIndex];
            int displayNumber = _currentIndex + 1;
            int total = _activeQuestions.Count;

            if (q.Type == QuestionType.TrueFalse)
            {
                return
                    $"❓  Question {displayNumber}/{total} (True or False)\n" +
                    "──────────────────────────────────────────\n" +
                    $"{q.QuestionText}\n\n" +
                    "Reply 'True' or 'False'.";
            }

            string options = string.Join("\n", q.Options.Select((opt, i) => $"  {(char)('A' + i)}) {opt}"));

            return
                $"❓  Question {displayNumber}/{total} (Multiple Choice)\n" +
                "──────────────────────────────────────────\n" +
                $"{q.QuestionText}\n\n" +
                options +
                "\n\nReply with the letter (A-D) of your answer.";
        }

        private string BuildFinalResults()
        {
            int total = _activeQuestions.Count;
            double percentage = total == 0 ? 0 : (_score * 100.0 / total);

            string verdict = percentage switch
            {
                >= 90 => "Outstanding! You're a cybersecurity pro! 🏆",
                >= 70 => "Great job! You've got a solid grasp of the basics. 👍",
                >= 50 => "Not bad! A bit more practice and you'll be golden. 💪",
                _ => "Worth another look — review the topics above and try again! 📚"
            };

            return
                "🏁  QUIZ COMPLETE!\n" +
                "──────────────────────────────────────────\n" +
                $"You scored {_score}/{total} ({percentage:0}%)\n\n" +
                verdict + "\n\n" +
                "Say 'start quiz' anytime to play again!";
        }

        // ════════════════════════════════════════════════════════════════
        // ANSWER PARSING
        // ════════════════════════════════════════════════════════════════

        private int? ParseAnswerIndex(QuizQuestion q, string lowerAnswer)
        {
            lowerAnswer = lowerAnswer.TrimEnd('.', ')', '!', ' ');

            if (q.Type == QuestionType.TrueFalse)
            {
                if (lowerAnswer is "true" or "t" or "yes" or "correct") return 0;
                if (lowerAnswer is "false" or "f" or "no" or "incorrect") return 1;
            }

            if (lowerAnswer.Length == 1 && lowerAnswer[0] >= 'a' && lowerAnswer[0] <= 'd')
                return lowerAnswer[0] - 'a';

            if (lowerAnswer.Length > 0 && (lowerAnswer.StartsWith("option") || lowerAnswer.StartsWith("answer")))
            {
                char lastChar = lowerAnswer[lowerAnswer.Length - 1];
                if (lastChar >= 'a' && lastChar <= 'd') return lastChar - 'a';
            }

            if (int.TryParse(lowerAnswer, out int num) && num >= 1 && num <= q.Options.Length)
                return num - 1;

            for (int i = 0; i < q.Options.Length; i++)
            {
                string optionLower = q.Options[i].ToLower();
                if (optionLower.Contains(lowerAnswer) || lowerAnswer.Contains(optionLower))
                    return i;
            }

            return null;
        }

        // ════════════════════════════════════════════════════════════════
        // QUESTION BANK  (12 questions: 6 multiple-choice, 6 true/false)
        // ════════════════════════════════════════════════════════════════

        private List<QuizQuestion> BuildQuestionBank()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion(
                    "What should you do if you receive an email urgently asking you to verify your bank details?",
                    QuestionType.MultipleChoice,
                    new[]
                    {
                        "Click the link and enter your details immediately",
                        "Ignore it and delete it without checking",
                        "Contact your bank directly using a number you already know to verify",
                        "Reply to the email asking if it's legitimate"
                    },
                    2,
                    "Always verify suspicious requests through a channel you already trust — never through the email itself."),

                new QuizQuestion(
                    "Using the same strong password across multiple accounts is safe, as long as the password itself is strong.",
                    QuestionType.TrueFalse,
                    new[] { "True", "False" },
                    1,
                    "If one site is breached, attackers will try that same password everywhere else (credential stuffing) — always use unique passwords."),

                new QuizQuestion(
                    "Which of the following is the most secure form of two-factor authentication?",
                    QuestionType.MultipleChoice,
                    new[]
                    {
                        "SMS text message codes",
                        "Authenticator app codes",
                        "Email verification codes",
                        "Security questions"
                    },
                    1,
                    "Authenticator apps generate codes locally on your device, making them far harder to intercept than SMS, which is vulnerable to SIM-swap attacks."),

                new QuizQuestion(
                    "Public Wi-Fi networks are generally safe for online banking.",
                    QuestionType.TrueFalse,
                    new[] { "True", "False" },
                    1,
                    "Public Wi-Fi can be intercepted by attackers on the same network. Use mobile data or a VPN for anything sensitive."),

                new QuizQuestion(
                    "What does 'HTTPS' in a website's address indicate?",
                    QuestionType.MultipleChoice,
                    new[]
                    {
                        "The site is hosted in the United States",
                        "The connection between you and the site is encrypted",
                        "The site is free to use",
                        "The site doesn't show ads"
                    },
                    1,
                    "HTTPS encrypts data travelling between your browser and the server, protecting it from interception."),

                new QuizQuestion(
                    "Ransomware encrypts your files and demands payment for their release.",
                    QuestionType.TrueFalse,
                    new[] { "True", "False" },
                    0,
                    "That's correct — and security experts recommend never paying, since it doesn't guarantee you'll get your files back."),

                new QuizQuestion(
                    "Which of these is a common warning sign of a phishing email?",
                    QuestionType.MultipleChoice,
                    new[]
                    {
                        "A personalised greeting using your full name",
                        "Urgent language demanding immediate action",
                        "An email from someone you've corresponded with before",
                        "Correct spelling and grammar throughout"
                    },
                    1,
                    "Urgency and threats ('act now or your account will be closed') are classic pressure tactics used to stop you thinking it through."),

                new QuizQuestion(
                    "You should regularly update your software and operating system.",
                    QuestionType.TrueFalse,
                    new[] { "True", "False" },
                    0,
                    "Updates often patch known security vulnerabilities — delaying them leaves you exposed to attacks that are already public knowledge."),

                new QuizQuestion(
                    "What is the recommended minimum length for a strong password?",
                    QuestionType.MultipleChoice,
                    new[] { "4 characters", "8 characters", "12 characters", "20 characters" },
                    2,
                    "Security guidance generally recommends at least 12 characters, ideally as a longer passphrase that's easy for you to remember."),

                new QuizQuestion(
                    "A VPN hides your IP address and encrypts your internet traffic.",
                    QuestionType.TrueFalse,
                    new[] { "True", "False" },
                    0,
                    "Correct — a VPN routes and encrypts your traffic through a remote server, masking your real IP address and location."),

                new QuizQuestion(
                    "Following the 3-2-1 backup rule, how many total copies of your data should you keep?",
                    QuestionType.MultipleChoice,
                    new[] { "1", "2", "3", "4" },
                    2,
                    "The rule is 3 copies of your data, on 2 different storage types, with 1 copy stored off-site or in the cloud."),

                new QuizQuestion(
                    "Social engineering attacks target human psychology rather than technical vulnerabilities.",
                    QuestionType.TrueFalse,
                    new[] { "True", "False" },
                    0,
                    "Correct — social engineering exploits trust, urgency, and habit rather than exploiting code or systems directly.")
            };
        }
    }
}