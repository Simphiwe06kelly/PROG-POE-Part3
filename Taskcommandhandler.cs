using POE_Part3;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace POE_Part3
{
    /// <summary>
    /// Interprets free-form chat input for task-assistant commands — add, view,
    /// complete, delete tasks, and reminders — using keyword/phrase detection
    /// (NLP simulation: understanding varied phrasings via string.Contains()),
    /// then delegates persistence to TaskService and records each successful
    /// action to the ActivityLog.
    /// </summary>
    public class TaskCommandHandler
    {
        private readonly TaskService? _taskService;
        private readonly ActivityLog _activityLog;
        private readonly bool _isAvailable;

        public TaskCommandHandler(TaskService? taskService, ActivityLog activityLog)
        {
            _taskService = taskService;
            _activityLog = activityLog;
            _isAvailable = taskService != null;
        }

        /// <summary>
        /// True if the input looks like it's asking for task-assistant
        /// functionality, regardless of exact phrasing.
        /// </summary>
        public bool IsTaskCommand(string input)
        {
            string lower = input.ToLower();

            return lower.Contains("task")
                || lower.Contains("remind me to")
                || lower.Contains("reminder")
                || lower.Contains("to-do")
                || lower.Contains("todo");
        }

        /// <summary>
        /// Handles a recognised task command and returns the chatbot's response.
        /// Pass the original (non-lowercased) input so task titles keep their casing.
        /// </summary>
        public string Handle(string input)
        {
            string lower = input.ToLower().Trim();

            if (!IsTaskCommand(lower))
                return string.Empty;

            if (!_isAvailable)
            {
                return "⚠️  The task assistant database isn't reachable right now.\n" +
                       "Check that MySQL is running and the connection details in\n" +
                       "TaskService.cs match your setup, then restart CyberBot.";
            }

            try
            {
                if (IsAddCommand(lower))
                    return HandleAddTask(input.Trim());

                if (IsCompleteCommand(lower))
                    return HandleCompleteTask(lower);

                if (IsDeleteCommand(lower))
                    return HandleDeleteTask(lower);

                if (lower.Contains("due") || lower.Contains("overdue") || lower.Contains("upcoming"))
                    return HandleDueReminders();

                if (IsViewCommand(lower))
                    return HandleViewTasks(lower);

                // "task" was mentioned but no clear action matched
                return
                    "📋  TASK ASSISTANT\n" +
                    "──────────────────────────────────────────\n" +
                    "I can help manage tasks and reminders! Try:\n" +
                    "  'add task - <title>'              → create a task\n" +
                    "  'add task - <title> - <details>'  → with a description\n" +
                    "  ...due <date>                     → with a reminder date\n" +
                    "  'show tasks' / 'view tasks'        → list pending tasks\n" +
                    "  'all tasks'                        → list everything\n" +
                    "  'complete task 3'                  → mark task 3 done\n" +
                    "  'delete task 3'                    → remove task 3\n" +
                    "  'due reminders'                    → see overdue items";
            }
            catch (Exception ex)
            {
                return $"⚠️  Something went wrong talking to the task database: {ex.Message}";
            }
        }

        // ════════════════════════════════════════════════════════════════
        // DETECTION HELPERS (varied phrasing via Contains)
        // ════════════════════════════════════════════════════════════════

        private bool IsAddCommand(string lower) =>
            lower.Contains("add task") || lower.Contains("add a task") ||
            lower.Contains("new task") || lower.Contains("create task") ||
            lower.Contains("remind me to");

        private bool IsCompleteCommand(string lower) =>
            lower.Contains("complete task") || lower.Contains("finish task") ||
            lower.Contains("mark task") || lower.Contains("done with task") ||
            lower.Contains("i finished task") || lower.Contains("i completed task");

        private bool IsDeleteCommand(string lower) =>
            lower.Contains("delete task") || lower.Contains("remove task") ||
            lower.Contains("cancel task");

        private bool IsViewCommand(string lower) =>
            lower.Contains("show task") || lower.Contains("view task") ||
            lower.Contains("list task") || lower.Contains("my tasks") ||
            lower.Contains("what tasks") || lower.Contains("to-do list") ||
            lower.Contains("todo list") || lower.Contains("all tasks");

        // ════════════════════════════════════════════════════════════════
        // ADD
        // ════════════════════════════════════════════════════════════════

        private string HandleAddTask(string rawInput)
        {
            string lower = rawInput.ToLower();

            // Pull a reminder date out first, so it doesn't end up in the title.
            DateTime? reminderDate = ExtractReminderDate(rawInput, out string withoutDatePart);

            string title;
            string description = "";

            if (lower.Contains("remind me to"))
            {
                int idx = withoutDatePart.ToLower().IndexOf("remind me to");
                title = idx >= 0
                    ? withoutDatePart.Substring(idx + "remind me to".Length).Trim()
                    : withoutDatePart.Trim();
            }
            else
            {
                int dashIndex = withoutDatePart.IndexOf(" - ");
                if (dashIndex >= 0)
                {
                    string afterTrigger = withoutDatePart.Substring(dashIndex + 3).Trim();
                    int secondDash = afterTrigger.IndexOf(" - ");
                    if (secondDash >= 0)
                    {
                        title = afterTrigger.Substring(0, secondDash).Trim();
                        description = afterTrigger.Substring(secondDash + 3).Trim();
                    }
                    else
                    {
                        title = afterTrigger;
                    }
                }
                else
                {
                    int taskIdx = withoutDatePart.ToLower().IndexOf("task");
                    title = taskIdx >= 0
                        ? withoutDatePart.Substring(taskIdx + 4).Trim()
                        : withoutDatePart.Trim();
                }
            }

            title = title.Trim().TrimStart('-', ':', ' ').TrimEnd('?', '.', '!', ' ');

            if (string.IsNullOrWhiteSpace(title))
            {
                return
                    "I'd like to add that task, but I didn't catch a title.\n" +
                    "Try: 'add task - Review privacy settings'";
            }

            var task = new CyberTask(title, description, reminderDate);
            var saved = _taskService!.AddTask(task);

            _activityLog.Log($"Added task #{saved.Id}: {saved.Title}");

            string reminderNote = reminderDate.HasValue
                ? $"\n📅  Reminder set for {reminderDate.Value:dd MMM yyyy}"
                : "";

            return
                "✅  Task added!\n" +
                "──────────────────────────────────────────\n" +
                $"[{saved.Id}] {saved.Title}" +
                (string.IsNullOrWhiteSpace(description) ? "" : $" — {description}") +
                reminderNote;
        }

        /// <summary>
        /// Looks for "due &lt;date&gt;" / "remind [me] [on] &lt;date&gt;" phrases and
        /// tries to parse a date from what follows. Returns the input with that
        /// phrase stripped out via the out parameter, so the remainder can be
        /// used cleanly as the task title.
        /// </summary>
        private DateTime? ExtractReminderDate(string input, out string remainder)
        {
            remainder = input;

            var match = Regex.Match(
                input,
                @"(due|remind(?:er)?(?:\s+me)?(?:\s+on)?)\s+([0-9]{1,4}[\/\-\.][0-9]{1,2}[\/\-\.][0-9]{1,4}|tomorrow|today|next week)",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            string dateText = match.Groups[2].Value.ToLower();
            DateTime? parsed = dateText switch
            {
                "today" => DateTime.Today,
                "tomorrow" => DateTime.Today.AddDays(1),
                "next week" => DateTime.Today.AddDays(7),
                _ => DateTime.TryParse(dateText, out var d) ? d : (DateTime?)null
            };

            remainder = input.Remove(match.Index, match.Length).Trim();
            return parsed;
        }

        // ════════════════════════════════════════════════════════════════
        // COMPLETE / DELETE
        // ════════════════════════════════════════════════════════════════

        private string HandleCompleteTask(string lower)
        {
            int? id = ExtractTaskId(lower);
            if (id == null)
                return "Which task? Try 'complete task 3' using the task's number from 'show tasks'.";

            bool success = _taskService!.CompleteTask(id.Value);

            if (success)
                _activityLog.Log($"Completed task #{id}");

            return success
                ? $"✅  Task {id} marked as complete. Nice work staying on top of it!"
                : $"I couldn't find a task with the number {id}. Try 'show tasks' to see current numbers.";
        }

        private string HandleDeleteTask(string lower)
        {
            int? id = ExtractTaskId(lower);
            if (id == null)
                return "Which task? Try 'delete task 3' using the task's number from 'show tasks'.";

            bool success = _taskService!.DeleteTask(id.Value);

            if (success)
                _activityLog.Log($"Deleted task #{id}");

            return success
                ? $"🗑️  Task {id} deleted."
                : $"I couldn't find a task with the number {id}. Try 'show tasks' to see current numbers.";
        }

        private int? ExtractTaskId(string lower)
        {
            var match = Regex.Match(lower, @"\d+");
            return match.Success ? int.Parse(match.Value) : (int?)null;
        }

        // ════════════════════════════════════════════════════════════════
        // VIEW
        // ════════════════════════════════════════════════════════════════

        private string HandleViewTasks(string lower)
        {
            var tasks = lower.Contains("all")
                ? _taskService!.GetAllTasks()
                : _taskService!.GetPendingTasks();

            if (tasks.Count == 0)
            {
                return lower.Contains("all")
                    ? "You don't have any tasks yet. Say 'add task - <title>' to create one!"
                    : "No pending tasks — you're all caught up! 🎉";
            }

            string header = lower.Contains("all")
                ? "📋  ALL TASKS\n"
                : "📋  PENDING TASKS\n";

            string list = string.Join("\n", tasks.Select(t => t.ToString()));

            return header + "──────────────────────────────────────────\n" + list;
        }

        private string HandleDueReminders()
        {
            var due = _taskService!.GetDueReminders();

            if (due.Count == 0)
                return "No overdue reminders right now. You're on track! ✅";

            string list = string.Join("\n", due.Select(t => t.ToString()));
            return
                "⏰  DUE / OVERDUE REMINDERS\n" +
                "──────────────────────────────────────────\n" +
                list;
        }
    }
}