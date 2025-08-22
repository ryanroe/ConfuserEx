using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Confuser.Core;
using Confuser.Core.Services;

namespace Confuser.Renamer {
    /// <summary>
    /// Word categories for meaningful renaming
    /// </summary>
    public enum WordCategory {
        Noun,
        Verb,
        Adjective,
        Adverb
    }

    /// <summary>
    /// Word pattern for name generation - a sequence of word categories
    /// </summary>
    public class WordPattern {
        public List<WordCategory> Categories { get; set; } = new List<WordCategory>();

        public WordPattern() { }

        public WordPattern(params WordCategory[] categories) {
            Categories.AddRange(categories);
        }

        /// <summary>
        /// Parse a pattern from string format like "Adjective,Noun" or "Verb+Adverb+Noun"
        /// </summary>
        public static WordPattern Parse(string pattern) {
            if (string.IsNullOrWhiteSpace(pattern))
                return new WordPattern();

            var result = new WordPattern();
            var parts = pattern.Split(new[] { ',', '+', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts) {
                var trimmedPart = part.Trim();
                if (Enum.TryParse<WordCategory>(trimmedPart, true, out WordCategory category)) {
                    result.Categories.Add(category);
                }
            }

            return result;
        }

        /// <summary>
        /// Convert to string representation for XML serialization
        /// </summary>
        public override string ToString() {
            return string.Join(",", Categories);
        }

        /// <summary>
        /// Get a display name for the pattern (e.g., "Adjective + Noun")
        /// </summary>
        public string GetDisplayName() {
            return string.Join(" + ", Categories);
        }

        public override bool Equals(object obj) {
            if (obj is WordPattern other) {
                return Categories.SequenceEqual(other.Categories);
            }
            return false;
        }

        public override int GetHashCode() {
            return Categories.Aggregate(0, (hash, cat) => hash ^ cat.GetHashCode());
        }
    }

    /// <summary>
    /// Represents a word with its category
    /// </summary>
    public class Word {
        public string Value { get; set; }
        public WordCategory Category { get; set; }

        public Word(string value, WordCategory category) {
            Value = value;
            Category = category;
        }
    }

    /// <summary>
    /// Configuration for meaningful words renaming
    /// </summary>
    public class MeaningfulWordsConfig {
        public List<Word> Words { get; } = new List<Word>();
        public List<WordPattern> Patterns { get; } = new List<WordPattern>();
        public bool UseNumbers { get; set; } = true;
        public int MaxLength { get; set; } = 50;
        public int MinLength { get; set; } = 3;

        /// <summary>
        /// Load configuration from XML element
        /// </summary>
        public void LoadFromXml(XmlElement configElement) {
            if (configElement == null) return;

            // Parse settings
            if (configElement.HasAttribute("useNumbers")) {
                bool.TryParse(configElement.GetAttribute("useNumbers"), out bool useNumbers);
                UseNumbers = useNumbers;
            }

            if (configElement.HasAttribute("maxLength")) {
                if (int.TryParse(configElement.GetAttribute("maxLength"), out int maxLength))
                    MaxLength = Math.Max(1, maxLength);
            }

            if (configElement.HasAttribute("minLength")) {
                if (int.TryParse(configElement.GetAttribute("minLength"), out int minLength))
                    MinLength = Math.Max(1, minLength);
            }

            // Parse patterns
            var patternsElement = configElement.SelectSingleNode("patterns") as XmlElement;
            if (patternsElement != null) {
                ParsePatterns(patternsElement);
            }
            else {
                // Default patterns if none specified
                SetDefaultPatterns();
            }

            // Parse word lists
            var wordsElement = configElement.SelectSingleNode("words") as XmlElement;
            if (wordsElement != null) {
                ParseWords(wordsElement);
            }

            // Ensure we have default words if none were provided
            if (Words.Count == 0) {
                SetDefaultWords();
            }
        }

        public void SetDefaultPatterns() {
            Patterns.AddRange(new[] {
                new WordPattern(WordCategory.Adjective, WordCategory.Noun),
                new WordPattern(WordCategory.Verb, WordCategory.Noun),
                new WordPattern(WordCategory.Noun, WordCategory.Verb),
                new WordPattern(WordCategory.Noun, WordCategory.Noun)
            });
        }

        void ParsePatterns(XmlElement patternsElement) {
            foreach (XmlNode patternNode in patternsElement.ChildNodes) {
                if (patternNode is XmlElement patternElement && patternElement.Name == "pattern") {
                    var patternValue = patternElement.GetAttribute("value");
                    if (!string.IsNullOrWhiteSpace(patternValue)) {
                        var pattern = WordPattern.Parse(patternValue);
                        if (pattern.Categories.Count > 0) {
                            // Check if pattern already exists to avoid duplicates
                            if (!Patterns.Any(p => p.Equals(pattern))) {
                                Patterns.Add(pattern);
                            }
                        }
                    }
                }
            }

            if (Patterns.Count == 0) {
                SetDefaultPatterns();
            }
        }

        void ParseWords(XmlElement wordsElement) {
            foreach (XmlNode categoryNode in wordsElement.ChildNodes) {
                if (!(categoryNode is XmlElement categoryElement)) continue;

                if (Enum.TryParse<WordCategory>(categoryElement.Name, true, out WordCategory category)) {
                    foreach (XmlNode wordNode in categoryElement.ChildNodes) {
                        if (wordNode is XmlElement wordElement && wordElement.Name == "word") {
                            var wordValue = wordElement.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(wordValue)) {
                                Words.Add(new Word(wordValue, category));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get default word lists if no configuration is provided
        /// </summary>
        public void SetDefaultWords() {
            if (Words.Count > 0) return;

            // Default nouns
            var defaultNouns = new[] {
                "Value", "Data", "Name", "Type", "Item", "User", "File", "Text", "List", "Object",
                "String", "Number", "Node", "Key", "Map", "Table", "Row", "Column", "Record", "Entry",
                "Count", "Index", "Size", "Length", "Buffer", "Stream", "State", "Status", "Result", "Error",
                "Exception", "Message", "Event", "Request", "Response", "Service", "Client", "Server", "Config", "Option",
                "Param", "Command", "Path", "Url", "Query", "Token", "Cache", "Session", "Log", "Time",
                "Date"

            };

            // Default verbs
            var defaultVerbs = new[] {
               "Get", "Set", "Add", "Remove", "Update", "Create", "Delete", "Load", "Save", "Open",
                "Close", "Read", "Write", "Send", "Receive", "Start", "Stop", "Begin", "End", "Reset",
                "Clear", "Build", "Check", "Find", "Search", "Select", "Insert", "Replace", "Sort", "Filter",
                "Convert", "Format", "Parse", "Validate", "Calculate", "Compute", "Generate", "Render", "Draw", "Print",
                "Execute", "Run", "Call", "Apply", "Bind", "Attach", "Detach", "Connect", "Disconnect", "Deploy"

            };

            // Default adjectives
            var defaultAdjectives = new[] {
                "Active", "Inactive", "Valid", "Invalid", "Enabled", "Disabled", "Visible", "Hidden", "Public", "Private",
                "Internal", "External", "Secure", "Unsafe", "Safe", "Busy", "Idle", "Empty", "Full", "Available",
                "Unavailable", "Online", "Offline", "Open", "Closed", "Max", "Min", "First", "Last", "Next",
                "Previous", "Current", "New", "Old", "Primary", "Secondary", "Main", "Alternate", "Temporary", "Permanent",
                "Successful", "Failed", "True", "False", "High", "Low", "Upper", "Lower", "Left", "Right"

            };

            // Default adverbs
            var defaultAdverbs = new[] {
                "Automatically", "Manually", "Synchronously", "Asynchronously", "Concurrently", "Sequentially", "Simultaneously", "Independently", "Together", "Separately",
                "Globally", "Locally", "Internally", "Externally", "Directly", "Indirectly", "Dynamically", "Statically", "Explicitly", "Implicitly",
                "Continuously", "Periodically", "Occasionally", "Frequently", "Rarely", "Always", "Never", "Sometimes", "Usually", "Normally",
                "Temporarily", "Permanently", "Safely", "Unsafely", "Correctly", "Incorrectly", "Properly", "Improperly", "Efficiently", "Inefficiently",
                "Quickly", "Slowly", "Clearly", "Easily", "Hardly", "Approximately", "Exactly", "Partially", "Fully", "Completely"

            };

            foreach (var noun in defaultNouns)
                Words.Add(new Word(noun, WordCategory.Noun));

            foreach (var verb in defaultVerbs)
                Words.Add(new Word(verb, WordCategory.Verb));

            foreach (var adjective in defaultAdjectives)
                Words.Add(new Word(adjective, WordCategory.Adjective));

            foreach (var adverb in defaultAdverbs)
                Words.Add(new Word(adverb, WordCategory.Adverb));
        }
    }

    /// <summary>
    /// Generates meaningful names using word combinations
    /// </summary>
    public class MeaningfulWordsGenerator {
        readonly MeaningfulWordsConfig config;
        readonly RandomGenerator random;
        readonly List<Word> nouns;
        readonly List<Word> verbs;
        readonly List<Word> adjectives;
        readonly List<Word> adverbs;
        readonly HashSet<string> usedNames;

        public MeaningfulWordsGenerator(MeaningfulWordsConfig config, RandomGenerator random) {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // If no words configured, use defaults
            if (config.Words.Count == 0) {
                config.SetDefaultWords();
            }

            // Group words by category for efficient lookup
            nouns = config.Words.Where(w => w.Category == WordCategory.Noun).ToList();
            verbs = config.Words.Where(w => w.Category == WordCategory.Verb).ToList();
            adjectives = config.Words.Where(w => w.Category == WordCategory.Adjective).ToList();
            adverbs = config.Words.Where(w => w.Category == WordCategory.Adverb).ToList();

            // Ensure we have words for each category, or fall back to defaults
            if (nouns.Count == 0 || verbs.Count == 0 || adjectives.Count == 0) {
                config.SetDefaultWords();
                nouns = config.Words.Where(w => w.Category == WordCategory.Noun).ToList();
                verbs = config.Words.Where(w => w.Category == WordCategory.Verb).ToList();
                adjectives = config.Words.Where(w => w.Category == WordCategory.Adjective).ToList();
                adverbs = config.Words.Where(w => w.Category == WordCategory.Adverb).ToList();
            }
        }

        /// <summary>
        /// Generate a meaningful name using configured patterns
        /// </summary>
        public string GenerateName() {
            return GenerateUniqueName(null);
        }

        /// <summary>
        /// Generate a unique meaningful name, avoiding conflicts with existing names
        /// </summary>
        /// <param name="existingNames">Set of existing names to avoid conflicts with</param>
        /// <returns>A unique meaningful name</returns>
        public string GenerateUniqueName(HashSet<string> existingNames) {
            if (config.Patterns.Count == 0) {
                config.SetDefaultPatterns();
            }

            string name;
            int attempts = 0;
            const int maxAttempts = 100; // Prevent infinite loops

            do {
                var pattern = config.Patterns[random.NextInt32(config.Patterns.Count)];
                name = GenerateNameForPattern(pattern);

                // Add number suffix if enabled and name is too short or to ensure uniqueness
                if (config.UseNumbers && (name.Length < config.MinLength || random.NextInt32(3) == 0)) {
                    name += random.NextInt32(100, 999);
                }

                // If still having conflicts, add random numbers to ensure uniqueness
                if (attempts > maxAttempts / 2 && (IsNameUsed(name, existingNames) || usedNames.Contains(name))) {
                    name += random.NextInt32(1000, 9999);
                }

                attempts++;

                // Fallback to sequential numbering if we can't find a unique name
                if (attempts >= maxAttempts) {
                    name = GenerateNameForPattern(config.Patterns[0]) + attempts;
                    break;
                }

            } while (IsNameUsed(name, existingNames) || usedNames.Contains(name));

            // Ensure length constraints
            if (name.Length > config.MaxLength) {
                name = name.Substring(0, config.MaxLength);
                // Re-check uniqueness after truncation
                if (IsNameUsed(name, existingNames) || usedNames.Contains(name)) {
                    name = name.Substring(0, Math.Max(1, config.MaxLength - 4)) + random.NextInt32(1000, 9999);
                }
            }

            // Track this name as used
            usedNames.Add(name);

            return name;
        }

        /// <summary>
        /// Check if a name is already used
        /// </summary>
        /// <param name="name">Name to check</param>
        /// <param name="existingNames">Additional set of existing names to check against</param>
        /// <returns>True if the name is already used</returns>
        bool IsNameUsed(string name, HashSet<string> existingNames) {
            return existingNames?.Contains(name) == true;
        }

        /// <summary>
        /// Clear the internal cache of used names
        /// </summary>
        public void ClearUsedNames() {
            usedNames.Clear();
        }

        string GenerateNameForPattern(WordPattern pattern) {
            if (pattern == null || pattern.Categories.Count == 0) {
                // Fallback to default pattern
                return GetRandomWord(adjectives) + GetRandomWord(nouns);
            }

            var nameBuilder = new System.Text.StringBuilder();

            foreach (var category in pattern.Categories) {
                var word = GetWordForCategory(category);
                nameBuilder.Append(word);
            }

            return nameBuilder.ToString();
        }

        string GetWordForCategory(WordCategory category) {
            switch (category) {
                case WordCategory.Noun:
                    return GetRandomWord(nouns);
                case WordCategory.Verb:
                    return GetRandomWord(verbs);
                case WordCategory.Adjective:
                    return GetRandomWord(adjectives);
                case WordCategory.Adverb:
                    if (adverbs.Count > 0) {
                        return GetRandomWord(adverbs);
                    }
                    // Fallback to noun if no adverbs available
                    return GetRandomWord(nouns);
                default:
                    return GetRandomWord(nouns);
            }
        }

        string GetRandomWord(List<Word> words) {
            if (words.Count == 0) {
                return "Default";
            }
            return words[random.NextInt32(words.Count)].Value;
        }
    }
}
