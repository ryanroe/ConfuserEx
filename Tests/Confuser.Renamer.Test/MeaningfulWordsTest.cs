using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using Confuser.Core;
using Confuser.Core.Services;
using Confuser.Renamer;
using Xunit;

namespace Confuser.Renamer.Test {
    public class MeaningfulWordsTest {
        [Fact]
        public void MeaningfulWordsConfig_LoadFromXml_DefaultPatterns() {
            var config = new MeaningfulWordsConfig();
            var xml = @"<meaningfulWords useNumbers='true' maxLength='50' minLength='3'>
							<words>
								<noun>
									<word>Car</word>
									<word>House</word>
								</noun>
								<verb>
									<word>Run</word>
									<word>Jump</word>
								</verb>
								<adjective>
									<word>Red</word>
									<word>Blue</word>
								</adjective>
							</words>
						</meaningfulWords>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            config.LoadFromXml(doc.DocumentElement);

            Assert.True(config.UseNumbers);
            Assert.Equal(50, config.MaxLength);
            Assert.Equal(3, config.MinLength);
            Assert.True(config.Words.Count >= 6); // At least 2 nouns, 2 verbs, 2 adjectives
            Assert.True(config.Patterns.Count > 0); // Default patterns should be set
        }

        [Fact]
        public void MeaningfulWordsConfig_SetDefaultWords_HasAllCategories() {
            var config = new MeaningfulWordsConfig();
            config.SetDefaultWords();

            var nouns = config.Words.FindAll(w => w.Category == WordCategory.Noun);
            var verbs = config.Words.FindAll(w => w.Category == WordCategory.Verb);
            var adjectives = config.Words.FindAll(w => w.Category == WordCategory.Adjective);
            var adverbs = config.Words.FindAll(w => w.Category == WordCategory.Adverb);

            Assert.True(nouns.Count > 0, "Should have nouns");
            Assert.True(verbs.Count > 0, "Should have verbs");
            Assert.True(adjectives.Count > 0, "Should have adjectives");
            Assert.True(adverbs.Count > 0, "Should have adverbs");
        }

        [Fact]
        public void MeaningfulWordsGenerator_GenerateName_ReturnsValidName() {
            var config = new MeaningfulWordsConfig();
            config.SetDefaultWords();
            config.SetDefaultPatterns();

            // Create RandomGenerator using reflection to access internal constructor
            var testSeed = Utils.SHA256(Encoding.UTF8.GetBytes("test-seed"));
            var randomGeneratorType = typeof(RandomGenerator);
            var constructor = randomGeneratorType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var randomGenerator = (RandomGenerator)constructor.Invoke(new object[] { testSeed });
            var generator = new MeaningfulWordsGenerator(config, randomGenerator);

            var name = generator.GenerateName();

            Assert.NotNull(name);
            Assert.True(name.Length >= config.MinLength, "Name should meet minimum length");
            Assert.True(name.Length <= config.MaxLength, "Name should not exceed maximum length");
            Assert.True(char.IsLetter(name[0]), "Name should start with a letter");
        }

        [Fact]
        public void MeaningfulWordsGenerator_GenerateName_ProducesVariation() {
            var config = new MeaningfulWordsConfig();
            config.SetDefaultWords();
            config.SetDefaultPatterns();

            // Create RandomGenerator using reflection to access internal constructor
            var testSeed = Utils.SHA256(Encoding.UTF8.GetBytes("test-seed"));
            var randomGeneratorType = typeof(RandomGenerator);
            var constructor = randomGeneratorType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var randomGenerator = (RandomGenerator)constructor.Invoke(new object[] { testSeed });
            var generator = new MeaningfulWordsGenerator(config, randomGenerator);

            var names = new HashSet<string>();

            // Generate 20 names and ensure we get some variation
            for (int i = 0; i < 20; i++) {
                var name = generator.GenerateName();
                names.Add(name);
            }

            // We should have some variation (not all the same name)
            Assert.True(names.Count > 1, "Should generate varied names");
        }

        [Fact]
        public void MeaningfulWordsConfig_LoadFromXml_MinimalConfig_NoNRE() {
            var config = new MeaningfulWordsConfig();
            var xml = @"<meaningfulWords minLength='5' maxLength='15' />";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            config.LoadFromXml(doc.DocumentElement);

            // Should not throw NRE and should have defaults
            Assert.Equal(15, config.MaxLength);
            Assert.Equal(5, config.MinLength);
            Assert.True(config.Words.Count > 0, "Should have default words");
            Assert.True(config.Patterns.Count > 0, "Should have default patterns");
        }

        [Fact]
        public void MeaningfulWordsGenerator_MinimalConfig_GeneratesValidNames() {
            var config = new MeaningfulWordsConfig();
            var xml = @"<meaningfulWords minLength='5' maxLength='15' />";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            config.LoadFromXml(doc.DocumentElement);

            // Create RandomGenerator using reflection to access internal constructor
            var testSeed = Utils.SHA256(Encoding.UTF8.GetBytes("test-seed"));
            var randomGeneratorType = typeof(RandomGenerator);
            var constructor = randomGeneratorType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var randomGenerator = (RandomGenerator)constructor.Invoke(new object[] { testSeed });
            var generator = new MeaningfulWordsGenerator(config, randomGenerator);

            // Should not throw NRE
            var name = generator.GenerateName();

            Assert.NotNull(name);
            Assert.True(name.Length >= 5, "Name should meet minimum length");
            Assert.True(name.Length <= 15, "Name should not exceed maximum length");
        }

        [Fact]
        public void MeaningfulWordsGenerator_GenerateUniqueName_AvoidsConflicts() {
            var config = new MeaningfulWordsConfig();
            config.SetDefaultWords();
            config.SetDefaultPatterns();

            // Create RandomGenerator using reflection to access internal constructor
            var testSeed = Utils.SHA256(Encoding.UTF8.GetBytes("test-seed"));
            var randomGeneratorType = typeof(RandomGenerator);
            var constructor = randomGeneratorType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var randomGenerator = (RandomGenerator)constructor.Invoke(new object[] { testSeed });
            var generator = new MeaningfulWordsGenerator(config, randomGenerator);

            // Create a set of existing names to avoid conflicts with
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var generatedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Generate multiple unique names
            for (int i = 0; i < 50; i++) {
                var name = generator.GenerateUniqueName(existingNames);

                Assert.NotNull(name);
                Assert.False(existingNames.Contains(name), $"Generated name '{name}' should not conflict with existing names");
                Assert.False(generatedNames.Contains(name), $"Generated name '{name}' should be unique among generated names");

                generatedNames.Add(name);
                existingNames.Add(name); // Add to existing to test future conflicts
            }

            // All names should be unique
            Assert.Equal(50, generatedNames.Count);
        }

        [Fact]
        public void MeaningfulWordsGenerator_ClearUsedNames_ResetsInternalState() {
            var config = new MeaningfulWordsConfig();
            config.SetDefaultWords();
            config.SetDefaultPatterns();

            // Create RandomGenerator using reflection to access internal constructor
            var testSeed = Utils.SHA256(Encoding.UTF8.GetBytes("test-seed"));
            var randomGeneratorType = typeof(RandomGenerator);
            var constructor = randomGeneratorType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var randomGenerator = (RandomGenerator)constructor.Invoke(new object[] { testSeed });
            var generator = new MeaningfulWordsGenerator(config, randomGenerator);

            // Generate some names
            for (int i = 0; i < 10; i++) {
                generator.GenerateName();
            }

            // Clear used names
            generator.ClearUsedNames();

            // Should be able to generate names again without internal conflicts
            var name = generator.GenerateName();
            Assert.NotNull(name);
            Assert.True(name.Length > 0);
        }
    }
}
