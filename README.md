# CyberBot — Cybersecurity Awareness Chatbot
### PROG POE Part 3 | The Independent Institute of Education

---

## Overview

CyberBot is a WPF-based cybersecurity awareness chatbot built in C# as the final submission
for the Programming module POE. It extends the console-based Part 1 and the GUI-based Part 2
into a fully featured application with four advanced capabilities:

1. **Task Assistant** — manage cybersecurity tasks and reminders with MySQL storage
2. **Cybersecurity Quiz** — an interactive mini-game testing cybersecurity knowledge
3. **NLP Simulation** — keyword detection and varied phrasing recognition
4. **Activity Log** — a timestamped record of all bot actions during the session

---

## Features

### Task Assistant (MySQL)
- Add tasks with a title, optional description, and optional reminder date
- View pending tasks or all tasks
- Mark tasks as complete or delete them
- All data persisted to a MySQL database (cyberbot_db)
- Supports natural language commands:
  - `add task - Enable two factor authentication`
  - `remind me to update my password tomorrow`
  - `show tasks` / `all tasks`
  - `complete task 1` / `delete task 2`
  - `due reminders`

### Cybersecurity Quiz
- 12 questions: 6 multiple choice and 6 true/false
- Topics: phishing, password safety, public Wi-Fi, HTTPS, ransomware, social engineering, VPN, backups
- One question at a time with immediate feedback and explanation
- Flexible answer parsing: accepts A/B/C/D, 1/2/3/4, True/False, or full option text
- Score tracking with final percentage and verdict
- Start with: `start quiz`

### NLP Simulation
- Keyword detection using `string.Contains()` throughout all command handlers
- Regex-based date extraction from phrases like `due tomorrow`, `due next week`, `due 2026/07/01`
- Longest-key-wins dictionary matching for more specific topic responses
- Recognises varied phrasings for all commands without requiring exact wording
- Minimises "I didn't understand" responses

### Activity Log
- Records every significant action with a timestamp
- Tracks: tasks added, completed, deleted; quiz started/completed; reminders set
- Displays the last 10 actions
- Triggered by:
  - `show activity log`
  - `what have you done for me`
  - `recent activity`
  - `action history`

### From Parts 1 & 2
- **Keyword response engine** with 15+ cybersecurity topics
- **Random tip pools** for phishing, passwords, privacy, browsing, and scams
- **Sentiment analysis** — detects positive, negative, anxious, angry, and curious moods
- **Memory store** — learns user name, job, device, and declared interests
- **Conversation flow** — handles "tell me more", "another tip", and topic continuity
- **Audio greeting** on startup (greeting.wav)
- **Delegate system** for bot responses
