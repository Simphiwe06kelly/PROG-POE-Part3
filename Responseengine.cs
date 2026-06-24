using System;
using System.Collections.Generic;
using System.Linq;

namespace POE_Part3
{
    /// <summary>
    /// Core response engine.  Merges the keyword dictionary from Part 1's
    /// ResponseHandler with Part 2's random-tip pools, sentiment handling,
    /// conversation-flow detection, and memory-personalisation.
    /// </summary>
    public class ResponseEngine
    {
        // ── Delegate wired up by ChatViewModel ──────────────────────────────────
        public KeywordMatchDelegate? OnKeywordMatched { get; set; }

        private readonly Dictionary<string, string[]> _randomResponses;
        private readonly Dictionary<string, string> _responses;
        private readonly MemoryStore _memory;
        private readonly Random _rng = new();

        private string _lastTopic = string.Empty;
        private int _lastTipIndex = -1;

        public ResponseEngine(MemoryStore memory)
        {
            _memory = memory;
            _responses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _randomResponses = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            InitialiseResponses();
            InitialiseRandomResponses();
        }

        // ════════════════════════════════════════════════════════════════════════
        // RANDOM RESPONSE POOLS  (Requirement 3)
        // ════════════════════════════════════════════════════════════════════════

        private void InitialiseRandomResponses()
        {
            // ── Phishing tips ────────────────────────────────────────────────────
            _randomResponses["phishing tip"] = new[]
            {
                "🎣  PHISHING TIP #1\n" +
                "──────────────────────────────────────────\n" +
                "Be cautious of emails asking for personal information.\n" +
                "Scammers often disguise themselves as trusted organisations\n" +
                "like your bank, SARS, or a courier service.\n\n" +
                "✅  Always go directly to the company's official website\n" +
                "    rather than clicking a link in an email.",

                "🎣  PHISHING TIP #2\n" +
                "──────────────────────────────────────────\n" +
                "Hover over links before clicking — the real URL\n" +
                "often reveals itself in the bottom bar of your browser.\n\n" +
                "⚠️  Watch for subtle misspellings:\n" +
                "    paypa1.com vs paypal.com\n" +
                "    amaz0n.co.za vs amazon.co.za\n\n" +
                "When in doubt, type the address manually!",

                "🎣  PHISHING TIP #3\n" +
                "──────────────────────────────────────────\n" +
                "Urgency is a red flag! Phishing messages often say:\n" +
                "  'Your account will be suspended in 24 hours!'\n" +
                "  'Immediate action required!'\n\n" +
                "Legitimate companies almost NEVER demand instant action\n" +
                "via email. Slow down and verify through official channels.",

                "🎣  PHISHING TIP #4\n" +
                "──────────────────────────────────────────\n" +
                "Check the sender's actual email address carefully.\n" +
                "A scam email might show 'FNB Support' as the name\n" +
                "but the address is: support@fnb-secure-login.xyz\n\n" +
                "✅  Real company emails come from their official domain\n" +
                "    e.g. noreply@fnb.co.za — not a random third-party domain.",

                "🎣  PHISHING TIP #5\n" +
                "──────────────────────────────────────────\n" +
                "Never enter credentials on a site you reached via email.\n" +
                "Instead: open a new browser tab → type the URL yourself.\n\n" +
                "This one habit prevents the majority of phishing attacks! 🛡️\n\n" +
                "Tip: Use a password manager — it auto-fills only on the\n" +
                "real domain, acting as a phishing safety net."
            };

            // ── Password tips ────────────────────────────────────────────────────
            _randomResponses["password tip"] = new[]
            {
                "🔑  PASSWORD TIP #1\n" +
                "──────────────────────────────────────────\n" +
                "Use a PASSPHRASE instead of a single word.\n" +
                "Example: 'BlueMonkey!Runs42km'\n\n" +
                "• Long (20 chars)\n• Easy to remember\n• Hard to crack\n\n" +
                "Add a number and symbol to meet most site requirements.",

                "🔑  PASSWORD TIP #2\n" +
                "──────────────────────────────────────────\n" +
                "Never reuse passwords across different sites!\n\n" +
                "If one site gets breached, attackers will try your\n" +
                "credentials on every other site (credential stuffing).\n\n" +
                "✅  Use a password manager like Bitwarden (free!) to\n" +
                "    generate and store unique passwords automatically.",

                "🔑  PASSWORD TIP #3\n" +
                "──────────────────────────────────────────\n" +
                "Avoid these TERRIBLE password choices:\n" +
                "  ✗  Your name + birth year   (e.g. Name1998)\n" +
                "  ✗  'password', 'qwerty', '123456'\n" +
                "  ✗  Your pet's or child's name\n" +
                "  ✗  Your ID or phone number\n\n" +
                "Attackers use personal info from social media to guess!",

                "🔑  PASSWORD TIP #4\n" +
                "──────────────────────────────────────────\n" +
                "Enable Two-Factor Authentication (2FA) everywhere!\n" +
                "Even if someone steals your password, they still\n" +
                "can't log in without your second factor.\n\n" +
                "Use an authenticator app (Google Authenticator, Authy)\n" +
                "rather than SMS codes — SMS can be intercepted via SIM-swap.",

                "🔑  PASSWORD TIP #5\n" +
                "──────────────────────────────────────────\n" +
                "Check if your passwords have already been leaked!\n\n" +
                "Visit: https://haveibeenpwned.com\n" +
                "Enter your email — it shows if your data appeared\n" +
                "in any known data breaches.\n\n" +
                "If it has → change that password IMMEDIATELY!"
            };

            // ── Privacy tips ─────────────────────────────────────────────────────
            _randomResponses["privacy tip"] = new[]
            {
                "🕵️  PRIVACY TIP #1\n" +
                "──────────────────────────────────────────\n" +
                "Audit your social media privacy settings right now.\n" +
                "Default settings on most platforms are set to PUBLIC.\n\n" +
                "• Set posts to 'Friends only'\n" +
                "• Hide your phone number and email address\n" +
                "• Disable facial recognition if offered\n" +
                "• Turn off location tagging on photos",

                "🕵️  PRIVACY TIP #2\n" +
                "──────────────────────────────────────────\n" +
                "Use a separate email for sign-ups and newsletters.\n" +
                "This keeps your primary inbox cleaner AND safer.\n\n" +
                "Better yet — use email aliases:\n" +
                "• SimpleLogin (free tier)\n" +
                "• AnonAddy\n\n" +
                "Forward to your real address and delete alias if spammed!",

                "🕵️  PRIVACY TIP #3\n" +
                "──────────────────────────────────────────\n" +
                "Review app permissions on your phone regularly.\n\n" +
                "Does that flashlight app really need your contacts?\n" +
                "Does a game need your location?\n\n" +
                "Go to Settings → Apps → Permissions and revoke any\n" +
                "access that doesn't make sense for the app's function.",

                "🕵️  PRIVACY TIP #4\n" +
                "──────────────────────────────────────────\n" +
                "Use end-to-end encrypted messaging apps.\n\n" +
                "✅  Signal — best-in-class privacy, open-source\n" +
                "✅  WhatsApp — E2E encrypted (metadata collected though)\n" +
                "✗  Regular SMS — NOT encrypted, avoid for sensitive info\n\n" +
                "Signal is the gold standard for private communication."
            };

            // ── Safe browsing tips ───────────────────────────────────────────────
            _randomResponses["browsing tip"] = new[]
            {
                "🌐  BROWSING TIP #1\n" +
                "──────────────────────────────────────────\n" +
                "Always look for HTTPS and the padlock icon 🔒\n" +
                "before entering any personal or payment information.\n\n" +
                "If a site shows 'Not Secure' in the address bar → LEAVE!\n" +
                "Your data would be transmitted in plain text.",

                "🌐  BROWSING TIP #2\n" +
                "──────────────────────────────────────────\n" +
                "Install uBlock Origin in your browser.\n" +
                "It's a free, powerful ad/malware blocker.\n\n" +
                "Malicious ads (malvertising) can infect your device\n" +
                "even WITHOUT clicking — just by loading the page!\n\n" +
                "Available for Chrome, Firefox, Edge, and Brave.",

                "🌐  BROWSING TIP #3\n" +
                "──────────────────────────────────────────\n" +
                "Keep your browser updated! Most browser updates\n" +
                "contain critical security patches.\n\n" +
                "Also consider switching to:\n" +
                "• Brave — built-in ad & tracker blocking\n" +
                "• Firefox — privacy-focused, open-source\n\n" +
                "Both are significant upgrades over default Chrome."
            };

            // ── General security tips ────────────────────────────────────────────
            _randomResponses["general tip"] = new[]
            {
                "💡  SECURITY TIP\n" +
                "──────────────────────────────────────────\n" +
                "Log out of accounts when using shared or public computers.\n" +
                "Browsers can save sessions — the next person can access\n" +
                "your account if you don't explicitly sign out.",

                "💡  SECURITY TIP\n" +
                "──────────────────────────────────────────\n" +
                "Be wary of USB drives you find lying around.\n" +
                "Attackers deliberately leave infected USB drives\n" +
                "in car parks and offices (a 'baiting' attack).\n\n" +
                "Never plug in a USB drive you didn't buy yourself.",

                "💡  SECURITY TIP\n" +
                "──────────────────────────────────────────\n" +
                "Screen lock your devices with strong PINs or biometrics.\n" +
                "A 6-digit PIN is vastly better than a 4-digit one.\n" +
                "Fingerprint/Face ID adds convenience without sacrificing\n" +
                "security for most threat models.",

                "💡  SECURITY TIP\n" +
                "──────────────────────────────────────────\n" +
                "Think before you share online. Oversharing on social\n" +
                "media gives attackers personal details to craft\n" +
                "convincing phishing messages targeting YOU specifically.\n\n" +
                "That 'fun quiz' asking your mother's maiden name? Social engineering.",

                "💡  SECURITY TIP\n" +
                "──────────────────────────────────────────\n" +
                "Set up account breach alerts. Services like\n" +
                "haveibeenpwned.com can notify you by email the\n" +
                "moment your address appears in a new data breach.\n\n" +
                "Early warning = faster response = less damage.",

                "💡  SECURITY TIP\n" +
                "──────────────────────────────────────────\n" +
                "Don't ignore software update prompts!\n" +
                "The WannaCry ransomware attack in 2017 infected\n" +
                "200,000+ computers across 150 countries — all running\n" +
                "Windows systems that hadn't applied a security patch\n" +
                "released two months earlier."
            };

            // ── Scam / social engineering tips ──────────────────────────────────
            _randomResponses["scam tip"] = new[]
            {
                "🎭  SCAM AWARENESS TIP\n" +
                "──────────────────────────────────────────\n" +
                "If someone calls claiming to be from Microsoft, SARS,\n" +
                "your bank, or the police — and asks for remote access\n" +
                "or payment → HANG UP IMMEDIATELY.\n\n" +
                "These organisations NEVER cold-call asking for access.",

                "🎭  SCAM AWARENESS TIP\n" +
                "──────────────────────────────────────────\n" +
                "The 'too good to be true' rule applies online too.\n" +
                "Prize winnings, investment returns >30% p/a, job offers\n" +
                "with unusually high salaries for little work — these\n" +
                "are common bait in advance-fee and romance scams.",

                "🎭  SCAM AWARENESS TIP\n" +
                "──────────────────────────────────────────\n" +
                "Verify before you trust. If a 'friend' messages asking\n" +
                "for money urgently, call them on a number you already\n" +
                "have saved. Accounts get hacked and impersonated.\n\n" +
                "One phone call can save you thousands of rands."
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        // KEYWORD RESPONSE DICTIONARY
        // Merged from Part 1 (ResponseHandler) and Part 2 (ResponseEngine).
        // Part 2 entries take priority; Part 1-only entries are added below.
        // ════════════════════════════════════════════════════════════════════════

        private void InitialiseResponses()
        {
            // ── General ──────────────────────────────────────────────────────────
            _responses["how are you"] =
                "I'm running at full capacity and ready to help keep you safe online! 🔒";

            _responses["who are you"] =
                "I'm CyberBot — your personal cybersecurity awareness assistant. " +
                "I can help you with passwords, phishing, privacy, malware, and much more!";

            _responses["what's your purpose"] =
                "My purpose is to educate you on cybersecurity best practices.\n" +
                "I cover topics like password safety, phishing, safe browsing,\n" +
                "two-factor authentication, malware, data privacy, and more.";

            _responses["what can i ask"] =
                "You can ask me about:\n" +
                "• Password Safety\n• Phishing & Scams\n• Safe Browsing\n" +
                "• Two-Factor Authentication (2FA)\n• Malware & Viruses\n" +
                "• Data Privacy\n• Social Engineering\n• VPN & Public Wi-Fi\n" +
                "• Ransomware\n• HTTPS\n\n" +
                "Or say 'give me a tip' for a random security tip!";

            _responses["hello"] =
                "Hello! Great to chat with you. How can I help you stay safe online today? 👋";
            _responses["hi"] = _responses["hello"];
            _responses["hey"] = _responses["hello"];

            _responses["help"] =
                "🛡️  TOPICS I CAN HELP WITH\n" +
                "──────────────────────────────────────────\n" +
                "  password         → Password safety tips\n" +
                "  phishing / scam  → Spot phishing attacks\n" +
                "  safe browsing    → Browse securely\n" +
                "  malware / virus  → Malware protection\n" +
                "  2fa              → Two-factor authentication\n" +
                "  privacy / data   → Protect personal data\n" +
                "  social           → Social engineering tactics\n" +
                "  vpn              → VPN usage guide\n" +
                "  ransomware       → Ransomware prevention\n" +
                "  https            → Secure connections\n" +
                "  public wifi      → Wi-Fi safety\n" +
                "  task             → Manage tasks & reminders\n" +
                "  quiz             → Test your knowledge\n" +
                "  log              → View recent activity\n" +
                "──────────────────────────────────────────\n" +
                "Say 'give me a tip' or 'phishing tip' for random tips!\n" +
                "Say 'add task - <title>' to create a task or reminder!\n" +
                "Say 'start quiz' to play the cybersecurity quiz!\n" +
                "Say 'show activity log' to see what I've done recently!\n" +
                "Type 'exit' or click ✕ to close.";

            // ── Password Safety ───────────────────────────────────────────────────
            _responses["password safety"] =
                "🔑  STRONG PASSWORD TIPS\n" +
                "──────────────────────────────────────────\n" +
                "• Use at least 12 characters (longer is better)\n" +
                "• Mix UPPERCASE, lowercase, numbers & symbols (!@#$)\n" +
                "• Avoid personal info: birthdays, pet names, ID numbers\n" +
                "• Never reuse the same password across multiple sites\n" +
                "• Use a password manager (Bitwarden, 1Password)\n" +
                "• Enable Two-Factor Authentication wherever possible\n\n" +
                "💡 Tip: Try a passphrase like 'BlueSky#Rain42!' — easy to remember, hard to crack.\n\n" +
                "Say 'password tip' for a random specific tip!";

            _responses["password"] = _responses["password safety"];
            _responses["password manager"] =
                "🗄️  PASSWORD MANAGERS\n" +
                "──────────────────────────────────────────\n" +
                "Password managers (Bitwarden, LastPass, 1Password) securely\n" +
                "store and generate strong unique passwords.\n" +
                "You only need to remember ONE master password.\n\n" +
                "• Bitwarden — free & open-source (highly recommended)\n" +
                "• 1Password  — great family/team plans\n" +
                "• KeePass    — offline, self-hosted option\n\n" +
                "This is one of the single best security steps you can take!";

            // ── Phishing ─────────────────────────────────────────────────────────
            _responses["phishing"] =
                "🎣  PHISHING AWARENESS\n" +
                "──────────────────────────────────────────\n" +
                "Phishing = attackers impersonating trusted sources to steal your info.\n\n" +
                "⚠️  WARNING SIGNS:\n" +
                "• Urgent / threatening language ('Your account will be closed!')\n" +
                "• Suspicious sender email addresses\n" +
                "• Unexpected links or attachments\n" +
                "• Requests for passwords or financial info\n" +
                "• Poor spelling and grammar\n\n" +
                "✅  WHAT TO DO:\n" +
                "• Do NOT click any links — hover to preview the URL first\n" +
                "• Do NOT open unexpected attachments\n" +
                "• Verify by calling the company directly\n" +
                "• Report to your IT department or email provider\n" +
                "• Delete the email immediately\n\n" +
                "Say 'phishing tip' for a random targeted tip!";

            _responses["phishing email"] = _responses["phishing"];
            _responses["scam"] = _responses["phishing"];
            _responses["email scam"] = _responses["phishing"];
            _responses["spam"] = _responses["phishing"];

            // ── Safe Browsing ─────────────────────────────────────────────────────
            _responses["safe browsing"] =
                "🌐  SAFE BROWSING HABITS\n" +
                "──────────────────────────────────────────\n" +
                "• Check for HTTPS (padlock icon 🔒) in the address bar\n" +
                "• Avoid public Wi-Fi for sensitive transactions\n" +
                "• Keep your browser and OS fully updated\n" +
                "• Install reputable antivirus/anti-malware software\n" +
                "• Only download software from official, trusted sources\n" +
                "• Use an ad blocker (uBlock Origin) to block malicious ads\n" +
                "• Use a privacy-focused browser: Firefox or Brave\n\n" +
                "Say 'browsing tip' for a random specific tip!";

            _responses["safe"] = _responses["safe browsing"];
            _responses["browsing"] = _responses["safe browsing"];
            _responses["browse"] = _responses["safe browsing"];

            _responses["https"] =
                "🔒  HTTPS EXPLAINED\n" +
                "──────────────────────────────────────────\n" +
                "HTTPS encrypts data travelling between your browser and the server.\n\n" +
                "Always look for the padlock icon before entering:\n" +
                "• Passwords\n• Banking details\n• Personal information\n\n" +
                "⚠️  If a site uses only HTTP — leave immediately!\n" +
                "HTTP data is sent in plain text and can be intercepted.";

            _responses["public wifi"] =
                "📶  PUBLIC WI-FI SECURITY\n" +
                "──────────────────────────────────────────\n" +
                "Public Wi-Fi is a MAJOR security risk!\n\n" +
                "• Hackers can intercept unencrypted data on open networks\n" +
                "• Avoid logging into banking, email, or work accounts\n" +
                "• Use a VPN if you must connect to public Wi-Fi\n" +
                "• Disable auto-connect to open/public networks\n" +
                "• Prefer mobile data over unknown Wi-Fi for sensitive tasks";

            _responses["wifi"] = _responses["public wifi"];

            _responses["vpn"] =
                "🛡️  VPN (VIRTUAL PRIVATE NETWORK)\n" +
                "──────────────────────────────────────────\n" +
                "A VPN encrypts all your internet traffic.\n\n" +
                "Benefits:\n" +
                "• Hides your IP address and physical location\n" +
                "• Secures your data on public Wi-Fi networks\n" +
                "• Helps bypass geographic content restrictions\n\n" +
                "Recommended options:\n" +
                "• ProtonVPN — strong privacy, free tier available\n" +
                "• Mullvad    — no-logs, anonymous payment\n" +
                "• NordVPN    — user-friendly, wide server network";

            // ── Two-Factor Authentication ─────────────────────────────────────────
            _responses["two-factor"] =
                "🔐  TWO-FACTOR AUTHENTICATION (2FA)\n" +
                "──────────────────────────────────────────\n" +
                "2FA adds a critical second security layer to your accounts.\n" +
                "Even if your password is stolen, attackers still need:\n" +
                "• A one-time code from your authenticator app\n" +
                "• Your fingerprint or face scan\n" +
                "• A physical security key (e.g. YubiKey)\n\n" +
                "✅ Enable 2FA on ALL important accounts — especially email & banking!\n" +
                "Authenticator apps are more secure than SMS codes.";

            _responses["2fa"] = _responses["two-factor"];
            _responses["two factor"] = _responses["two-factor"];
            _responses["authentication"] = _responses["two-factor"];
            _responses["authenticator"] = _responses["two-factor"];

            // ── Malware ───────────────────────────────────────────────────────────
            _responses["malware"] =
                "🦠  MALWARE PROTECTION\n" +
                "──────────────────────────────────────────\n" +
                "Malware = malicious software designed to harm or steal.\n" +
                "Types: viruses, ransomware, spyware, trojans, adware, worms.\n\n" +
                "PROTECT YOURSELF:\n" +
                "• Install reputable antivirus (Windows Defender, Malwarebytes)\n" +
                "• Keep your OS and all apps fully updated\n" +
                "• Do NOT download software from unknown sources\n" +
                "• Avoid pirated/cracked software — it often contains hidden malware\n" +
                "• Scan USB drives before opening files";

            _responses["virus"] = _responses["malware"];
            _responses["trojan"] = _responses["malware"];
            _responses["spyware"] = _responses["malware"];

            _responses["ransomware"] =
                "💀  RANSOMWARE PREVENTION\n" +
                "──────────────────────────────────────────\n" +
                "Ransomware encrypts your files and demands payment to restore them.\n\n" +
                "PREVENTION TIPS:\n" +
                "• Back up data regularly — follow the 3-2-1 rule!\n" +
                "• Do not open suspicious email attachments\n" +
                "• Keep all software and OS fully patched\n" +
                "• Never pay the ransom — it does NOT guarantee recovery\n\n" +
                "📦  3-2-1 Backup Rule:\n" +
                "  3 copies of data\n  2 different storage media\n  1 stored off-site";

            // ── Data Privacy ──────────────────────────────────────────────────────
            _responses["privacy"] =
                "🕵️  DIGITAL PRIVACY TIPS\n" +
                "──────────────────────────────────────────\n" +
                "• Review app permissions — revoke any you don't need\n" +
                "• Use privacy-focused browsers: Firefox, Brave\n" +
                "• Limit personal info shared on social media\n" +
                "• Read privacy policies before registering on new sites\n" +
                "• Use end-to-end encrypted messaging: Signal, WhatsApp\n" +
                "• Regularly check haveibeenpwned.com for data breaches\n\n" +
                "Say 'privacy tip' for a random specific tip!";

            _responses["data"] =
                "📊  PROTECTING YOUR PERSONAL DATA\n" +
                "──────────────────────────────────────────\n" +
                "Your personal data is extremely valuable to attackers.\n\n" +
                "• Be selective about what personal info you share online\n" +
                "• Regularly audit which apps have access to your accounts\n" +
                "• Use disposable/alias emails for sign-ups (SimpleLogin, AnonAddy)\n" +
                "• Monitor for breaches at: haveibeenpwned.com\n" +
                "• Request data deletion from services you no longer use";

            // ── Social Engineering ────────────────────────────────────────────────
            _responses["social engineering"] =
                "🎭  SOCIAL ENGINEERING AWARENESS\n" +
                "──────────────────────────────────────────\n" +
                "Social engineering = manipulating PEOPLE, not systems.\n" +
                "It exploits trust, urgency, and human psychology.\n\n" +
                "COMMON TACTICS:\n" +
                "• Pretexting — creating a believable false scenario\n" +
                "• Baiting    — enticing offers (free USB, gift cards)\n" +
                "• Phishing   — deceptive emails / messages\n" +
                "• Vishing    — voice/phone call scams\n" +
                "• Tailgating — following someone into a restricted area\n\n" +
                "🛑 Always verify identities before sharing ANY sensitive info!\n\n" +
                "Say 'scam tip' for a random targeted tip!";

            _responses["social"] = _responses["social engineering"];

            // ── Appreciation ──────────────────────────────────────────────────────
            _responses["thank you"] = "You're very welcome! Stay safe out there. 🔒";
            _responses["thanks"] = _responses["thank you"];
            _responses["cheers"] = _responses["thank you"];
        }

        // ════════════════════════════════════════════════════════════════════════
        // MAIN RESPONSE METHOD
        // ════════════════════════════════════════════════════════════════════════

        public string GetResponse(string rawInput, SentimentResult sentiment)
        {
            _memory.LearnFrom(rawInput);
            string lower = rawInput.ToLower().Trim();

            // Exit shortcuts
            if (lower is "exit" or "quit" or "bye" or "goodbye")
                return "__EXIT__";

            // Conversation flow (Requirement 4)
            string? flowResponse = HandleConversationFlow(lower, sentiment);
            if (flowResponse != null) return flowResponse;

            // Random tip requests (Requirement 3)
            string? randomResponse = HandleRandomTipRequest(lower, sentiment);
            if (randomResponse != null) return randomResponse;

            // Delegate override hook — pass the ORIGINAL casing (trimmed) so
            // things like task titles aren't forced to lowercase. Handlers
            // that need case-insensitive matching lower it themselves.
            if (OnKeywordMatched != null)
            {
                string? overrideResponse = OnKeywordMatched(rawInput.Trim());
                if (!string.IsNullOrEmpty(overrideResponse))
                    return BuildResponse(overrideResponse, lower, sentiment);
            }

            // Sentiment-driven auto-tips (Requirement 6)
            string? sentimentResponse = HandleSentimentDrivenResponse(lower, sentiment);
            if (sentimentResponse != null) return sentimentResponse;

            // Dictionary match — longest key wins for specificity
            string? matched = null;
            int bestLen = 0;

            foreach (var key in _responses.Keys)
            {
                if (lower.Contains(key) && key.Length > bestLen)
                {
                    matched = _responses[key];
                    bestLen = key.Length;
                    _lastTopic = key;
                    _memory.AddTopic(key);
                    _memory.RememberInterest(key);   // Requirement 5
                }
            }

            if (matched != null)
                return BuildResponse(matched, lower, sentiment);

            return BuildFallback(sentiment);
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONVERSATION FLOW  (Requirement 4)
        // ════════════════════════════════════════════════════════════════════════

        private string? HandleConversationFlow(string lower, SentimentResult sentiment)
        {
            bool isMore = lower.Contains("more") || lower.Contains("elaborate") ||
                          lower.Contains("explain") || lower.Contains("detail") ||
                          lower.Contains("tell me more") || lower.Contains("go on") ||
                          lower.Contains("continue");

            bool isAnother = lower.Contains("another") || lower.Contains("different") ||
                             lower.Contains("new tip") || lower.Contains("again");

            if (isAnother && lower.Contains("tip"))
            {
                string pool = MapTopicToPool(_lastTopic);
                return BuildResponse(GetRandom(pool), lower, sentiment);
            }

            if (isMore && !string.IsNullOrEmpty(_lastTopic))
            {
                if (_responses.TryGetValue(_lastTopic, out var followUp))
                {
                    string header = $"📖  MORE ON: {_lastTopic.ToUpper()}\n" +
                                    "──────────────────────────────────────────\n";
                    return BuildResponse(header + followUp, lower, sentiment);
                }

                string poolKey = MapTopicToPool(_lastTopic);
                if (_randomResponses.ContainsKey(poolKey))
                {
                    return BuildResponse(
                        $"Here's another angle on {_lastTopic}:\n\n{GetRandom(poolKey)}",
                        lower, sentiment);
                }
            }

            return null;
        }

        // ════════════════════════════════════════════════════════════════════════
        // RANDOM TIP REQUEST HANDLER  (Requirement 3)
        // ════════════════════════════════════════════════════════════════════════

        private string? HandleRandomTipRequest(string lower, SentimentResult sentiment)
        {
            if (lower.Contains("phishing tip") || lower.Contains("tip about phishing"))
            {
                _lastTopic = "phishing"; _memory.AddTopic("phishing");
                return BuildResponse(GetRandom("phishing tip"), lower, sentiment);
            }
            if (lower.Contains("password tip") || lower.Contains("tip about password"))
            {
                _lastTopic = "password"; _memory.AddTopic("password");
                return BuildResponse(GetRandom("password tip"), lower, sentiment);
            }
            if (lower.Contains("privacy tip") || lower.Contains("tip about privacy"))
            {
                _lastTopic = "privacy"; _memory.AddTopic("privacy");
                return BuildResponse(GetRandom("privacy tip"), lower, sentiment);
            }
            if (lower.Contains("browsing tip") || lower.Contains("tip about browsing"))
            {
                _lastTopic = "browsing"; _memory.AddTopic("browsing");
                return BuildResponse(GetRandom("browsing tip"), lower, sentiment);
            }
            if (lower.Contains("scam tip") || lower.Contains("tip about scam"))
            {
                _lastTopic = "scam"; _memory.AddTopic("scam");
                return BuildResponse(GetRandom("scam tip"), lower, sentiment);
            }

            // Generic tip request
            if (lower is "tip" or "give me a tip" or "security tip" or "random tip" ||
                (lower.Contains("give") && lower.Contains("tip")) ||
                (lower.Contains("share") && lower.Contains("tip")))
            {
                string? interest = _memory.GetTopInterest();
                string poolKey = interest != null ? MapTopicToPool(interest) : "general tip";
                string note = interest != null ? $"(Based on your interest in {interest})\n\n" : "";
                _lastTopic = interest ?? "security";
                return BuildResponse(note + GetRandom(poolKey), lower, sentiment);
            }

            return null;
        }

        // ════════════════════════════════════════════════════════════════════════
        // SENTIMENT-DRIVEN RESPONSES  (Requirement 6)
        // ════════════════════════════════════════════════════════════════════════

        private string? HandleSentimentDrivenResponse(string lower, SentimentResult sentiment)
        {
            if (sentiment.Sentiment == Sentiment.Anxious)
            {
                if (lower.Contains("scam") || lower.Contains("fraud"))
                {
                    _lastTopic = "scam"; _memory.AddTopic("scam");
                    return
                        "😰  It's completely understandable to feel that way.\n" +
                        "Scammers can be very convincing — but knowledge is your\n" +
                        "best defence. Here's what you need to know:\n\n" +
                        GetRandom("scam tip") + "\n\n" +
                        "💬  You're already taking the right step by learning.\n" +
                        "Ask me anything — I'm here to help!";
                }
                if (lower.Contains("hack") || lower.Contains("compromised") || lower.Contains("breach"))
                {
                    _lastTopic = "password"; _memory.AddTopic("password");
                    return
                        "😰  Don't panic — let's handle this step by step.\n\n" +
                        "🚨  IMMEDIATE ACTIONS:\n" +
                        "──────────────────────────────────────────\n" +
                        "1️⃣   Change your passwords immediately — start with email\n" +
                        "2️⃣   Enable 2FA on all important accounts\n" +
                        "3️⃣   Check haveibeenpwned.com to see what was exposed\n" +
                        "4️⃣   Alert your bank if financial accounts may be affected\n" +
                        "5️⃣   Review recent account activity for unauthorised actions\n\n" +
                        "💬  Take a breath — you can recover from this.";
                }
                if (lower.Contains("phishing") || lower.Contains("email"))
                {
                    _lastTopic = "phishing"; _memory.AddTopic("phishing");
                    return
                        "😰  It's completely understandable to feel worried.\n" +
                        "Phishing attacks are increasingly sophisticated.\n" +
                        "Here's a targeted tip to protect yourself:\n\n" +
                        GetRandom("phishing tip") + "\n\n" +
                        "💬  Remember: when in doubt, don't click. Verify first!";
                }
                if (lower.Contains("privacy") || lower.Contains("data"))
                {
                    _lastTopic = "privacy"; _memory.AddTopic("privacy");
                    return
                        "😰  Privacy concerns are very valid in today's world.\n" +
                        "Here's a practical step you can take right now:\n\n" +
                        GetRandom("privacy tip") + "\n\n" +
                        "💬  Small actions add up to significantly better privacy.";
                }

                return
                    "😰  I can hear that you're concerned — and that's okay.\n" +
                    "Cybersecurity can feel overwhelming, but I'm here to\n" +
                    "help you take it one step at a time.\n\n" +
                    GetRandom("general tip") + "\n\n" +
                    "💬  What specific area would you like to focus on?\n" +
                    "Type 'help' to see all topics.";
            }

            if (sentiment.Sentiment == Sentiment.Angry)
            {
                return
                    "😠  I hear your frustration — let me address that properly.\n\n" +
                    "Cybersecurity issues can be incredibly stressful,\n" +
                    "especially when systems fail you or you've been targeted.\n\n" +
                    "Tell me specifically what happened and I'll give you\n" +
                    "the most relevant guidance. What's going on?";
            }

            return null;
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════════════

        private string GetRandom(string poolKey)
        {
            if (!_randomResponses.TryGetValue(poolKey, out var pool) || pool.Length == 0)
                return GetRandom("general tip");

            int idx;
            do { idx = _rng.Next(pool.Length); }
            while (pool.Length > 1 && idx == _lastTipIndex);

            _lastTipIndex = idx;
            return pool[idx];
        }

        private string MapTopicToPool(string topic) => topic.ToLower() switch
        {
            var t when t.Contains("phish") || t.Contains("scam") || t.Contains("email") => "phishing tip",
            var t when t.Contains("password") => "password tip",
            var t when t.Contains("privacy") || t.Contains("data") => "privacy tip",
            var t when t.Contains("brows") || t.Contains("safe") || t.Contains("https") => "browsing tip",
            var t when t.Contains("social") => "scam tip",
            _ => "general tip"
        };

        private string BuildResponse(string core, string input, SentimentResult sentiment)
        {
            string moodPrefix = SentimentAnalyser.GetMoodPrefix(sentiment);
            string recallHint = _memory.GetRecallHint(input);
            string namePrefix = _memory.BuildPersonalPrefix();

            string prefix = !string.IsNullOrEmpty(moodPrefix) ? moodPrefix
                          : !string.IsNullOrEmpty(namePrefix) ? namePrefix
                          : string.Empty;

            return string.IsNullOrEmpty(recallHint)
                ? prefix + core
                : prefix + recallHint + "\n\n" + core;
        }

        private string BuildFallback(SentimentResult sentiment)
        {
            string[] fallbacks =
            {
                "I didn't quite catch that. Could you rephrase?\nTry: 'password', 'phishing', 'malware', '2fa', 'privacy', or type 'help'.",
                "Hmm, I'm not sure about that one. Type 'help' to see all topics I can assist with.\nOr say 'give me a tip' for a random security tip!",
                "That one stumped me! I specialise in cybersecurity — try asking about 'scams', 'safe browsing', or 'vpn'."
            };

            return SentimentAnalyser.GetMoodPrefix(sentiment) + fallbacks[_rng.Next(fallbacks.Length)];
        }
    }
}