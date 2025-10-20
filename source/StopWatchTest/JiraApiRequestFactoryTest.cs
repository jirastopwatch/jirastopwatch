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
    using Moq;
    using NUnit.Framework;
    using RestSharp;
    using StopWatch;
    using System;

    [TestFixture]
    public class JiraApiRequestFactoryTest
    {
        private Mock<RestRequest> requestMock;
        private Mock<IRestRequestFactory> requestFactoryMock;

        private JiraApiRequestFactory jiraApiRequestFactory;

        [SetUp]
        public void Setup()
        {
            requestMock = new Mock<RestRequest>();

            requestFactoryMock = new Mock<IRestRequestFactory>();
            requestFactoryMock.Setup(m => m.Create(It.IsAny<string>(), It.IsAny<Method>())).Returns(requestMock.Object);

            jiraApiRequestFactory = new JiraApiRequestFactory(requestFactoryMock.Object);
        }



        [Test]
        public void CreateValidateSessionRequest_CreatesValidRequest()
        {
            var request = jiraApiRequestFactory.CreateValidateSessionRequest();
            requestFactoryMock.Verify(m => m.Create("/rest/auth/1/session", Method.Get));
        }


        [Test]
        public void CreateGetFavoriteFiltersRequest_CreatesValidRequest()
        {
            var request = jiraApiRequestFactory.CreateGetFavoriteFiltersRequest();
            requestFactoryMock.Verify(m => m.Create("/rest/api/3/filter/favourite", Method.Get));
        }
        

        [Test]
        public void CreateGetIssuesByJQLRequest_CreatesValidRequest()
        {
            string jql = "status%3Dopen";
            var request = jiraApiRequestFactory.CreateGetIssuesByJQLRequest(jql);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/search?jql={0}&maxResults=200", jql), Method.Get));
        }


        [Test]
        public void CreateGetIssueSummaryRequest_CreatesValidRequest()
        {
            string key = "FOO-42";
            var request = jiraApiRequestFactory.CreateGetIssueSummaryRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}", key), Method.Get));
        }


        [Test]
        public void CreateGetIssueSummaryRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            var request = jiraApiRequestFactory.CreateGetIssueSummaryRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}", key.Trim()), Method.Get));
        }

        [Test]
        public void CreateGetIssueTimetrackingRequestt_CreatesValidRequest()
        {
            string key = "FOO-42";
            var request = jiraApiRequestFactory.CreateGetIssueTimetrackingRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}?fields=timetracking", key), Method.Get));
        }


        [Test]
        public void CreateGetIssueTimetrackingRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            var request = jiraApiRequestFactory.CreateGetIssueTimetrackingRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}?fields=timetracking", key.Trim()), Method.Get));
        }


        [Test]
        [Ignore("Moq problem")]
        public void CreatePostWorklogRequest_CreatesValidRequest()
        {
            string key = "FOO-42";
            var started = new DateTimeOffset(2016, 07, 26, 1, 44, 15, TimeSpan.Zero);
            TimeSpan time = new TimeSpan(1, 2, 0);
            string comment = "Sorry for the inconvenience...";
            StopWatch.EstimateUpdateMethods adjusmentMethod = EstimateUpdateMethods.Auto;
            string adjustmentValue = "";
            var request = jiraApiRequestFactory.CreatePostWorklogRequest(key, started, time, comment, adjusmentMethod, adjustmentValue);

            requestFactoryMock.Verify(m => m.Create(string.Format("/rest/api/3/issue/{0}/worklog", key), Method.Post));

            requestMock.VerifySet(m => m.RequestFormat = DataFormat.Json);

            requestMock.Verify(m => m.AddBody(It.Is<object>(o =>
                o.GetHashCode() == (new {
                    timeSpent = JiraTimeHelpers.TimeSpanToJiraTime(time),
                    started = "2016-07-26T01:44:15.000+0000",
                    comment = comment
                }).GetHashCode()
            ), ContentType.Json)
            );
        }


        [Test]
        public void CreatePostWorklogRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            var started = new DateTimeOffset(2016, 07, 26, 1, 44, 15, TimeSpan.Zero);
            TimeSpan time = new TimeSpan(1, 2, 0);
            string comment = "Sorry for the inconvenience...";
            StopWatch.EstimateUpdateMethods adjusmentMethod = EstimateUpdateMethods.Auto;
            string adjustmentValue = "";
            var request = jiraApiRequestFactory.CreatePostWorklogRequest(key, started, time, comment, adjusmentMethod, adjustmentValue);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}/worklog", key.Trim()), Method.Post));
        }

        [Test]
        [Ignore("Moq problem")]
        public void CreatePostCommentRequest_CreatesValidRequest()
        {
            string key = "FOO-42";
            string comment = "Sorry for the inconvenience...";
            var request = jiraApiRequestFactory.CreatePostCommentRequest(key, comment);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}/comment", key), Method.Post));

            requestMock.VerifySet(m => m.RequestFormat = DataFormat.Json);

            requestMock.Verify(m => m.AddBody(It.Is<object>(o =>
                o.GetHashCode() == (new {
                    body = comment
                }).GetHashCode()
            ), ContentType.Json));
        }


        [Test]
        public void CreatePostCommentRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            string comment = "Sorry for the inconvenience...";
            var request = jiraApiRequestFactory.CreatePostCommentRequest(key, comment);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}/comment", key.Trim()), Method.Post));
        }


        [Test]
        public void CreateGetAvailableTransitions_CreatesValidRequest()
        {
            string key = "TST-1";

            var request = jiraApiRequestFactory.CreateGetAvailableTransitions(key);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}/transitions", key), Method.Get));
        }


        [Test]
        [Ignore("Moq problem")]
        public void CreateDoTransition_CreatesValidRequest()
        {
            string key = "TST-1";
            int transitionId = 5;

            var request = jiraApiRequestFactory.CreateDoTransition(key, transitionId);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/3/issue/{0}/transitions", key), Method.Post));

            //todo verify test, verifySet The next member after the last one shown above is non-virtual, sealed, or not visible to the proxy factory.
            requestMock.VerifySet(m => m.RequestFormat = DataFormat.Json);

            requestMock.Verify(m => m.AddBody(It.Is<object>(o =>
                o.GetHashCode() == (new {
                    transition = new
                    {
                        id = transitionId
                    }
                }).GetHashCode()
            ), ContentType.Json));
        }

    }
}
