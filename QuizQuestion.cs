namespace POE_Part3
{
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse
    }

    /// <summary>
    /// Represents a single cybersecurity quiz question — either multiple-choice
    /// (4 options) or true/false (2 options) — with the correct answer's index
    /// and an explanation shown as feedback once the user answers.
    /// </summary>
    public class QuizQuestion
    {
        public string QuestionText { get; set; }
        public QuestionType Type { get; set; }
        public string[] Options { get; set; }
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; }

        public QuizQuestion(string questionText, QuestionType type, string[] options, int correctIndex, string explanation)
        {
            QuestionText = questionText;
            Type = type;
            Options = options;
            CorrectIndex = correctIndex;
            Explanation = explanation;
        }
    }
}