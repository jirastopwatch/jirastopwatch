/**
 * Copyright 2023 Y. Meyer-Norwood
 * Copyright 2020 Dan Tulloh
 * Copyright 2016 Carsten Gehling
 *
 * For a full list of contributing authors, see:
 *
 *     https://jirastopwatch.com/contributors
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
    using System;
    using System.Globalization;
    using System.Threading;

    [TestFixture]
    public class JiraTimeHelpersTest
    {
        [SetUp]
        public void Setup()
        {
            JiraTimeHelpers.Configuration = null;
        }

        [Test]
        public void DateTimeToJiraDateTime_HandlesTimeZones()
        {
            Assert.That(JiraTimeHelpers.DateTimeToJiraDateTime(new DateTimeOffset(2015, 09, 20, 16, 40, 51, TimeSpan.Zero)), Is.EqualTo("2015-09-20T16:40:51.000+0000"));
            Assert.That(JiraTimeHelpers.DateTimeToJiraDateTime(new DateTimeOffset(2015, 09, 20, 16, 40, 51, TimeSpan.FromHours(1))), Is.EqualTo("2015-09-20T16:40:51.000+0100"));
            Assert.That(JiraTimeHelpers.DateTimeToJiraDateTime(new DateTimeOffset(2015, 09, 20, 16, 40, 51, TimeSpan.FromMinutes(9 * 60 + 30))), Is.EqualTo("2015-09-20T16:40:51.000+0930"));
        }

        [Test]
        public void DateTimeToJiraDateTime_IgnoreRegionalSettings()
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            var currentUICulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("bn-BD");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("bn-BD");
                var s = JiraTimeHelpers.DateTimeToJiraDateTime(DateTimeOffset.Now);
                Assert.That(JiraTimeHelpers.DateTimeToJiraDateTime(new DateTimeOffset(2015, 09, 20, 16, 40, 51, TimeSpan.Zero)), Is.EqualTo("2015-09-20T16:40:51.000+0000"));
                Assert.That(JiraTimeHelpers.DateTimeToJiraDateTime(new DateTimeOffset(2015, 09, 20, 16, 40, 51, TimeSpan.FromHours(1))), Is.EqualTo("2015-09-20T16:40:51.000+0100"));
                Assert.That(JiraTimeHelpers.DateTimeToJiraDateTime(new DateTimeOffset(2015, 09, 20, 16, 40, 51, TimeSpan.FromMinutes(9 * 60 + 30))), Is.EqualTo("2015-09-20T16:40:51.000+0930"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                Thread.CurrentThread.CurrentUICulture = currentUICulture;
            }
        }

        [Test]
        public void TimeSpanToJira_FormatsDaysHoursMinutes()
        {
            Assert.That(JiraTimeHelpers.TimeSpanToJiraTime(new TimeSpan(12, 7, 0)), Is.EqualTo("12h 7m"));
            Assert.That(JiraTimeHelpers.TimeSpanToJiraTime(new TimeSpan(9, 15, 0)), Is.EqualTo("9h 15m"));
            Assert.That(JiraTimeHelpers.TimeSpanToJiraTime(new TimeSpan(1, 2, 5, 0)), Is.EqualTo("1d 2h 5m"));
            Assert.That(JiraTimeHelpers.TimeSpanToJiraTime(new TimeSpan(21, 4, 0, 0)), Is.EqualTo("21d 4h 0m"));
        }


        [Test]
        public void JiraTimeToTimeSpan_InvalidMinutesFails()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("m"), Is.Null);
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("2 m"), Is.Null);
        }

        [Test]
        public void JiraTimeToTimeSpan_InvalidHoursFails()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("h"), Is.Null);
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("8 h"), Is.Null);
        }

        /*
        [Test]
        public void JiraTimeToTimeSpan_ValidHoursWithInvalidMinutesFails()
        {
            Assert.IsNull(JiraTimeHelpers.JiraTimeToTimeSpan("2h 5"));
            Assert.IsNull(JiraTimeHelpers.JiraTimeToTimeSpan("2h m"));
        }

        [Test]
        public void JiraTimeToTimeSpan_InvalidHoursWithValidMinutesFails()
        {
            Assert.IsNull(JiraTimeHelpers.JiraTimeToTimeSpan("2 5m"));
            Assert.IsNull(JiraTimeHelpers.JiraTimeToTimeSpan("h 5m"));
        }
        */

        [Test]
        public void JiraTimeToTimeSpan_ParsesJiraStyleTimespan()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("2h").Value.TotalMinutes, Is.EqualTo(120));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("2h 5m").Value.TotalMinutes, Is.EqualTo(125));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("5m").Value.TotalMinutes, Is.EqualTo(5));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("0").Value.TotalMinutes, Is.EqualTo(0));
        }

        [Test]
        public void JiraTimeToTimeSpan_ParsesDecimalHours()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("2.5h").Value.TotalMinutes, Is.EqualTo(150));
        }

        [Test]
        public void JiraTimeToTimeSpan_IgnoresDecimalValueForMinutes()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("10.5m").Value.TotalSeconds, Is.EqualTo(600));
        }

        [Test]
        public void JiraTimeToTimeSpan_AllowsMinutesBeforeHours()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("5m 2h").Value.TotalMinutes, Is.EqualTo(125));
        }

        [Test]
        public void JiraTimeToTimeSpan_AllowsSillyValues()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("2h 0m").Value.TotalMinutes, Is.EqualTo(120));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("0h 5m").Value.TotalMinutes, Is.EqualTo(5));
        }

        [Test]
        public void JiraTimeToTimeSpan_AllowsMultipleWhitespace()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("1h      5m").Value.TotalMinutes, Is.EqualTo(65));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("    2h   5m    ").Value.TotalMinutes, Is.EqualTo(125));
        }

        [Test]
        public void JiraTimeToTimeSpan_AllowsNoWhitespace()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("2h5m").Value.TotalMinutes, Is.EqualTo(125));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("1d2h5m").Value.TotalMinutes, Is.EqualTo(1565));
        }

        [Test]
        public void JiraTimeToTimeSpan_AllowsDays()
        {
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("1d 2h 5m").Value.TotalMinutes, Is.EqualTo(1565));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("1d 2h").Value.TotalMinutes, Is.EqualTo(1560));
            Assert.That(JiraTimeHelpers.JiraTimeToTimeSpan("1d 5m").Value.TotalMinutes, Is.EqualTo(1445));
        }
    }
}
