using System;
using System.IO;
using System.Xml;
using Confuser.Core.Project;
using Xunit;

namespace Confuser.Core.Test {
    public class ConfuserProjectTest {
        [Fact]
        public void ConfuserProject_LoadXml_HandlesInlineXmlArguments() {
            // Test XML with inline meaningfulWords configuration (no value attribute)
            var xml = @"<?xml version='1.0' encoding='utf-8'?>
                        <project baseDir='.' outputDir='.\Confused' xmlns='http://confuser.codeplex.com'>
                          <rule pattern='*' inherit='false'>
                            <protection id='rename'>
                              <argument name='mode' value='MeaningfulWords' />
                              <argument name='meaningfulWords'>
                                <meaningfulWords useNumbers='true' maxLength='20' minLength='3' />
                              </argument>
                            </protection>
                          </rule>
                          <module path='test.dll' />
                        </project>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var project = new ConfuserProject();

            // This should not throw NRE at line 349
            Exception caughtException = null;
            try {
                project.Load(doc);
            }
            catch (Exception ex) {
                caughtException = ex;
            }

            // Verify no exception was thrown
            Assert.Null(caughtException);

            // Verify the project loaded correctly
            Assert.Single(project.Rules);
            Assert.Single(project.Rules[0]);

            var protection = project.Rules[0][0];
            Assert.Equal("rename", protection.Id);
            Assert.Equal("MeaningfulWords", protection["mode"]);
            Assert.True(protection.ContainsKey("meaningfulWords"));
            Assert.Contains("meaningfulWords", protection["meaningfulWords"]);
        }
    }
}
