/**
 * Copyright 2023 Y. Meyer-Norwood
 * Copyright 2020 Dan Tulloh
 * Copyright 2016 Carsten Gehling
 *
 * For a full list of contributing authors, see:
 *
 *     https://jirastopwatch.github.io/contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at:
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace StopWatchTest
{
    using NUnit.Framework;
    using StopWatch;


    [TestFixture]
    public class JiraKeyHelpersTest
    {
        [Test]
        public void ParseUrlToKey_ReturnsKeyOnFirstMatch()
        {
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"KEY-123"), Is.EqualTo("KEY-123"));
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"http://jira.test.local/browse/KEY-123"), Is.EqualTo("KEY-123"));
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"browse/KEY-123"), Is.EqualTo("KEY-123"));
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"http://jira.test.local/browse/KEY-123?foo=bar&key=FOO-555"), Is.EqualTo("KEY-123"));
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"http://jira.test.local/browse/KEY-123/somefoo/qwe?foo=bar&key=FOO-555"), Is.EqualTo("KEY-123"));
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"http://jira.test.local/bwse/KEY-123"), Is.EqualTo("KEY-123"));
        }

        [Test]
        public void ParseUrlToKey_ReturnsOriginalTextOnNoMatch()
        {
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"ABC"), Is.EqualTo("ABC"));
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"http://jira.test.local/bwse/"), Is.EqualTo("http://jira.test.local/bwse/"));
            Assert.That(JiraKeyHelpers.ParseUrlToKey(@"http://jira.test.local/browse/KEY-"), Is.EqualTo("http://jira.test.local/browse/KEY-"));
        }
    }
}
