using POE_Part3;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace POE_Part3

{
    /// <summary>
    /// Main ViewModel for the chat UI.
    /// Wires together ResponseEngine, MemoryStore, SentimentAnalyser,
    /// AudioPlayer, the MySQL-backed TaskService, the QuizEngine, and the
    /// ActivityLog. Uses delegate callbacks for bot responses and sentiment
    /// updates (Requirement 2 — delegate usage).
    /// </summary>
    public class ChatViewModel : INotifyPropertyChanged
    {
        // ── Delegates ────────────────────────────────────────────────────────────
        private readonly BotResponseDelegate _onBotResponse;
        private readonly SentimentChangedDelegate _onSentimentChanged;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly ResponseEngine _engine;
        private readonly MemoryStore _memory;
        private readonly ActivityLog _activityLog;
        private readonly TaskCommandHandler _taskCommandHandler;
        private readonly QuizEngine _quizEngine;

        private bool _nameCollected = false;
        private string _inputText = "";
        private string _sentimentLabel = "😐 Neutral";
        private string _statusText = "Initialising...";
        private bool _isTyping = false;

        // ── Public properties ─────────────────────────────────────────────────────
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSend)); }
        }

        public string SentimentLabel
        {
            get => _sentimentLabel;
            set { _sentimentLabel = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool IsTyping
        {
            get => _isTyping;
            set { _isTyping = value; OnPropertyChanged(); }
        }

        public bool CanSend => !string.IsNullOrWhiteSpace(InputText) && !IsTyping;

        // ── Constructor ───────────────────────────────────────────────────────────
        public ChatViewModel()
        {
            _memory = new MemoryStore();
            _engine = new ResponseEngine(_memory);
            _activityLog = new ActivityLog();
            _quizEngine = new QuizEngine(_activityLog);

            // Task assistant: attempt to connect to MySQL, but don't let a
            // missing/unreachable database take the whole chatbot down.
            // TaskCommandHandler reports the problem conversationally instead.
            TaskService? taskService = null;
            try
            {
                taskService = new TaskService();
            }
            catch
            {
                taskService = null;
            }
            _taskCommandHandler = new TaskCommandHandler(taskService, _activityLog);

            // Wire delegates
            _onBotResponse = HandleBotResponse;
            _onSentimentChanged = HandleSentimentChanged;

            // ── Delegate hook: keyword overrides ─────────────────────────────────
            _engine.OnKeywordMatched = input =>
            {
                string lowerInput = input.ToLower();

                // Activity log lookup — show what the bot has done recently.
                if (_activityLog.IsLogCommand(lowerInput))
                    return _activityLog.GetFormattedLog();

                // Quiz start command — only relevant when no quiz is already
                // running (mid-quiz answers are intercepted earlier, in
                // SendMessageAsync, so they never reach this delegate at all).
                if (_quizEngine.IsStartCommand(lowerInput))
                    return _quizEngine.StartQuiz();

                // Task assistant commands (add/view/complete/delete tasks,
                // reminders) take next priority among the overrides.
                if (_taskCommandHandler.IsTaskCommand(lowerInput))
                    return _taskCommandHandler.Handle(input);

                if (lowerInput.Contains("breach") || lowerInput.Contains("leaked"))
                    return
                        "🚨  DATA BREACH RESPONSE\n" +
                        "──────────────────────────────────────────\n" +
                        "If you think your data has been breached:\n" +
                        "• Change your passwords immediately\n" +
                        "• Enable 2FA on all important accounts\n" +
                        "• Check haveibeenpwned.com to confirm the breach\n" +
                        "• Monitor your bank statements for suspicious activity\n" +
                        "• Alert your bank if financial data may be involved\n" +
                        "• Consider a credit freeze if identity theft is a risk";

                if (lowerInput.Contains("update") || lowerInput.Contains("patch"))
                    return
                        "🔄  WHY UPDATES MATTER\n" +
                        "──────────────────────────────────────────\n" +
                        "Software updates patch known security vulnerabilities.\n" +
                        "Attackers actively exploit unpatched systems.\n\n" +
                        "• Enable automatic updates on your OS and apps\n" +
                        "• Update your router firmware regularly\n" +
                        "• Keep your antivirus definitions up to date\n" +
                        "• Don't ignore update prompts — act on them quickly";

                if (lowerInput.Contains("backup") || lowerInput.Contains("back up"))
                    return
                        "💾  BACKUP BEST PRACTICES\n" +
                        "──────────────────────────────────────────\n" +
                        "Follow the 3-2-1 rule:\n" +
                        "  ✦ 3 copies of your data\n" +
                        "  ✦ 2 different storage types (e.g. HDD + cloud)\n" +
                        "  ✦ 1 stored off-site or in the cloud\n\n" +
                        "Options: Google Drive, OneDrive, Backblaze, external HDD\n" +
                        "Test your backups regularly to ensure they restore correctly!";

                // Memory recall commands (Requirement 5)
                if (lowerInput.Contains("what do you remember") || lowerInput.Contains("what do you know about me"))
                    return _memory.GetMemorySummary();

                if (lowerInput.Contains("forget me") || lowerInput.Contains("clear memory"))
                    return "Memory clearing is not supported in this session. " +
                           "Your details are only stored for this conversation. 🔒";

                return string.Empty;
            };

            _ = BootSequenceAsync();
        }

        // ── Boot sequence ─────────────────────────────────────────────────────────
        private async Task BootSequenceAsync()
        {
            StatusText = "Starting CyberBot...";
            await Task.Delay(600);

            // Use the dedicated AudioPlayer class (ported from Part 1)
            AudioPlayer.PlayGreeting();
            await Task.Delay(400);

            _onBotResponse(
                "🛡️  Welcome to CyberBot — your cybersecurity awareness assistant.\n" +
                "Keeping you safe in the digital world.",
                MessageType.System);

            await Task.Delay(800);
            _onBotResponse("Before we begin, what's your name?", MessageType.Bot);
            StatusText = "Awaiting your name...";
        }

        // ── Send message ──────────────────────────────────────────────────────────
        public async Task SendMessageAsync()
        {
            string raw = InputText.Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            InputText = "";

            // Name collection phase
            if (!_nameCollected)
            {
                _memory.UserName = raw;
                _nameCollected = true;

                AddMessage(raw, MessageType.User);
                IsTyping = true;
                StatusText = "Typing...";
                await Task.Delay(700);

                _onBotResponse(
                    $"Nice to meet you, {_memory.UserName}! 🙂\n\n" +
                    "Here's what you can ask me about:\n" +
                    "  password • phishing • scam • safe browsing\n" +
                    "  malware • 2fa • privacy • vpn • ransomware • social\n\n" +
                    "You can also say things like:\n" +
                    "  'I'm interested in privacy' — I'll personalise tips for you!\n" +
                    "  'give me a tip'              — get a random security tip\n" +
                    "  'phishing tip'               — random phishing-specific tip\n" +
                    "  'what do you remember'       — see what I know about you\n" +
                    "  'add task - <title>'         — create a task or reminder\n" +
                    "  'show tasks'                 — see your pending tasks\n" +
                    "  'start quiz'                 — test your knowledge!\n" +
                    "  'show activity log'          — see what I've done recently\n\n" +
                    "Type 'help' for the full topic list.",
                    MessageType.Bot);

                IsTyping = false;
                StatusText = $"Chatting with {_memory.UserName}";
                return;
            }

            // Normal chat phase
            AddMessage($"{_memory.UserName ?? "You"}: {raw}", MessageType.User);

            var sentiment = SentimentAnalyser.Analyse(raw);
            _onSentimentChanged(sentiment);

            IsTyping = true;
            StatusText = "CyberBot is thinking...";

            await Task.Delay(400 + new Random().Next(200, 600));

            // ── Quiz in progress? ────────────────────────────────────────────────
            // While a quiz is active, the user's reply is their answer to the
            // current question — it must NOT be run through keyword/topic
            // matching, since an answer like "true" or "B" would otherwise get
            // swallowed by the normal response pipeline.
            if (_quizEngine.IsActive)
            {
                string quizResponse = _quizEngine.IsExitCommand(raw.ToLower())
                    ? _quizEngine.EndQuizEarly()
                    : _quizEngine.SubmitAnswer(raw);

                _onBotResponse(quizResponse, MessageType.Bot);

                IsTyping = false;
                StatusText = $"Chatting with {_memory.UserName ?? "you"}";
                return;
            }

            // Check for explicit interest declaration first (Requirement 5)
            string? interestAck = _memory.LearnFrom(raw);
            if (interestAck != null)
            {
                _onBotResponse(interestAck, MessageType.Bot);
                await Task.Delay(400);
            }

            string response = _engine.GetResponse(raw, sentiment);

            if (response == "__EXIT__")
            {
                _onBotResponse(
                    $"Goodbye, {_memory.UserName ?? "friend"}! Stay safe and vigilant online. 🔒\n" +
                    "Remember: Think Before You Click!",
                    MessageType.System);
                IsTyping = false;
                StatusText = "Session ended";
                await Task.Delay(2000);
                Application.Current.Shutdown();
                return;
            }

            _onBotResponse(response, MessageType.Bot);

            // Proactive memory callout (Requirement 5)
            await MaybeAppendMemoryCallout(raw, sentiment);

            IsTyping = false;
            StatusText = $"Chatting with {_memory.UserName ?? "you"}";
        }

        // ── Proactive interest reminder (Requirement 5) ───────────────────────────
        private async Task MaybeAppendMemoryCallout(string input, SentimentResult sentiment)
        {
            string? topInterest = _memory.GetTopInterest();
            if (topInterest == null) return;

            bool inputIsAboutInterest =
                input.Contains(topInterest, StringComparison.OrdinalIgnoreCase);
            if (inputIsAboutInterest) return;

            int topicCount = _memory.Topics.Count;
            if (topicCount > 0 && topicCount % 3 == 0)
            {
                await Task.Delay(300);
                _onBotResponse(
                    $"💡  As someone interested in {topInterest}, you might want to\n" +
                    $"review the security settings on your accounts regularly.\n" +
                    $"Type '{topInterest}' to dive deeper into that topic!",
                    MessageType.Tip);
            }
        }

        // ── Delegate handlers ─────────────────────────────────────────────────────
        private void HandleBotResponse(string message, MessageType type)
        {
            Application.Current.Dispatcher.Invoke(() => AddMessage(message, type));
        }

        private void HandleSentimentChanged(SentimentResult result)
        {
            Application.Current.Dispatcher.Invoke(() => SentimentLabel = result.Label);
        }

        private void AddMessage(string text, MessageType type)
        {
            Messages.Add(new ChatMessage { Text = text, Type = type, Timestamp = DateTime.Now });
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}