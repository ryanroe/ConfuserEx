using System;
using System.Xml;
using Confuser.Renamer;
using Xunit;

namespace Confuser.Renamer.Test {
    public class MinimalConfigTest {
        [Fact]
        public void MeaningfulWords_MinimalConfig_DoesNotThrowNRE() {
            // Test that providing only min/max length doesn't cause NRE
            var config = new MeaningfulWordsConfig();
            var xml = @"<meaningfulWords minLength='5' maxLength='15' />";

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            // This should not throw any exception
            Exception caughtException = null;
            try {
                config.LoadFromXml(doc.DocumentElement);
            }
            catch (Exception ex) {
                caughtException = ex;
            }

            Assert.Null(caughtException);

            // Verify the configuration is properly loaded
            Assert.Equal(15, config.MaxLength);
            Assert.Equal(5, config.MinLength);

            // Verify default words and patterns are set
            Assert.True(config.Words.Count > 0, "Should have default words");
            Assert.True(config.Patterns.Count > 0, "Should have default patterns");
        }
    }
}
