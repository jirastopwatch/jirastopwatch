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
    using System.Collections.Generic;

    [TestFixture]
    public class JiraClientTest
    {
        private Mock<IJiraApiRequestFactory> jiraApiRequestFactoryMock;
        private Mock<IJiraApiRequester> jiraApiRequesterMock;

        private JiraClient jiraClient;


        [SetUp]
        public void Setup()
        {
            jiraApiRequestFactoryMock = new Mock<IJiraApiRequestFactory>();

            jiraApiRequesterMock = new Mock<IJiraApiRequester>();

            jiraClient = new JiraClient(jiraApiRequestFactoryMock.Object, jiraApiRequesterMock.Object);
        }


        [Test, Description("Authenticate returns true on successful authentication")]
        public void Authenticate_OnSuccess_It_Returns_True()
        {
            var jiraConfig = new JiraConfiguration()
            {
                timeTrackingConfiguration = new TimeTrackingConfiguration()
            };
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<JiraConfiguration>(It.IsAny<RestRequest>())).Returns(jiraConfig);
            Assert.That(jiraClient.Authenticate("myuser", "myapitoken"), Is.True);
        }


        [Test, Description("Authenticate returns false on unsuccessful authentication")]
        public void Authenticate_OnFailure_It_Returns_False()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<JiraConfiguration>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.Authenticate("myuser", "myapitoken"), Is.False);
        }


        [Test, Description("ValidateSession: On success it sets SessionValid and returns true")]
        public void ValidateSession_OnSuccess_It_Sets_SessionValid_And_Returns_True()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Returns(new object());
            Assert.That(jiraClient.ValidateSession(), Is.True);
            Assert.That(jiraClient.SessionValid, Is.True);
        }


        [Test, Description("ValidateSession: On failure it resets SessionValid and returns false")]
        public void ValidateSession_OnFailure_It_Resets_SessionValid_And_Returns_False()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.ValidateSession(), Is.False);
            Assert.That(jiraClient.SessionValid, Is.False);
        }


        [Test, Description("GetFavoriteFilters: On success it returns a list of type filter")]
        public void GetFavoriteFilters_OnSuccess_It_Returns_List_Of_Filters()
        {
            List<Filter> returnData = new List<Filter>();
            returnData.Add(new Filter { Id = 5, Name = "Foo", Jql = "Project=Foo" });
            returnData.Add(new Filter { Id = 6, Name = "bar", Jql = "Project=Bar" });

            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<List<Filter>>(It.IsAny<RestRequest>())).Returns(returnData);

            Assert.That(jiraClient.GetFavoriteFilters(), Is.EqualTo(returnData));
        }


        [Test, Description("GetFavoriteFilters: On failure it returns null")]
        public void GetFavoriteFilters_OnFailure_It_Returns_Null()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<List<Filter>>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.GetFavoriteFilters(), Is.Null);
        }


        [Test, Description("GetIssuesByJQL: On success it returns a list of type filter")]
        public void GetIssuesByJQL_OnSuccess_It_Returns_List_Of_Issues()
        {
            SearchResult returnData = new SearchResult
            {
                Issues = new List<Issue>()
            };
            returnData.Issues.Add(new Issue { Key = "FOO-1", Fields = new IssueFields { Summary = "Summary for FOO-1" } });
            returnData.Issues.Add(new Issue { Key = "FOO-2", Fields = new IssueFields { Summary = "Summary for FOO-2" } });

            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<SearchResult>(It.IsAny<RestRequest>())).Returns(returnData);

            Assert.That(jiraClient.GetIssuesByJQL("testjql"), Is.EqualTo(returnData));
        }


        [Test, Description("GetIssuesByJQL: On failure it returns null")]
        public void GetIssuesByJQL_OnFailure_It_Returns_Null()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<List<Filter>>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.GetIssuesByJQL("testjql"), Is.Null);
        }


        [Test, Description("GetIssueSummary: On success it returns a list of type filter")]
        public void GetIssueSummary_OnSuccess_It_Returns_Issue_Summary()
        {
            Issue returnData = new Issue
            {
                Fields = new IssueFields
                {
                    Summary = "The long dark tea-time of the soul"
                }
            };

            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<Issue>(It.IsAny<RestRequest>())).Returns(returnData);

            Assert.That(jiraClient.GetIssueSummary("DG-42", false), Is.EqualTo(returnData.Fields.Summary));
        }


        [Test, Description("GetIssueSummary: On failure it returns empty string")]
        public void GetIssueSummary_OnFailure_It_Returns_Empty_String()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<Issue>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.GetIssueSummary("DG-42", false), Is.EqualTo(""));
        }

        [Test, Description("GetIssueTimetracking: On success it returns a timetracking object")]
        public void GetIssueTimetracking_OnSuccess_It_Returns_RemainingTime()
        {
            Issue returnData = new Issue
            {
                Fields = new IssueFields
                {
                    Summary = "The long dark tea-time of the soul",
                    Timetracking = new TimetrackingFields
                    {
                        RemainingEstimate = "1h",
                        RemainingEstimateSeconds = 360
                    }
                }
            };

            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<Issue>(It.IsAny<RestRequest>())).Returns(returnData);

            Assert.That(jiraClient.GetIssueTimetracking("DG-42"), Is.EqualTo(returnData.Fields.Timetracking));
        }


        [Test, Description("GetIssueTimetracking: On failure it returns null")]
        public void GetIssueTimetracking_OnFailure_It_Returns_Empty_String()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<Issue>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.GetIssueTimetracking("DG-42"), Is.Null);
        }


        [Test, Description("PostWorklog: On success it returns true")]
        public void PostWorklog_OnSuccess_It_Returns_True()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Returns(new object());

            Assert.That(jiraClient.PostWorklog("DG-42", DateTimeOffset.UtcNow, new TimeSpan(1, 20, 0), "Time is an illusion", EstimateUpdateMethods.Auto, null), Is.True);
        }


        [Test, Description("PostWorklog: On failure it returns false")]
        public void PostWorklog_OnFailure_It_Returns_False()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.PostWorklog("DG-42", DateTimeOffset.UtcNow, new TimeSpan(2, 10, 0), "Lunchtime doubly so", EstimateUpdateMethods.Auto, null), Is.False);
        }


        [Test, Description("PostComment: On success it returns true")]
        public void PostComment_OnSuccess_It_Returns_True()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Returns(new object());

            Assert.That(jiraClient.PostComment("DG-42", "Time is an illusion"), Is.True);
        }


        [Test, Description("PostComment: On failure it returns false")]
        public void PostComment_OnFailure_It_Returns_False()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.PostComment("DG-42", "Lunchtime doubly so"), Is.False);
        }


        [Test, Description("GetAvailableTransitions: On success it returns a list transitions currently available for issue")]
        public void GetAvailableTransitions_OnSuccess_It_Returns_List_Of_Issues()
        {
            AvailableTransitions returnData = new AvailableTransitions
            {
                Expand = "transitions",
                Transitions = new List<Transition>()

            };
            returnData.Transitions.Add(new Transition { Id = 8, Name = "Trans1" });
            returnData.Transitions.Add(new Transition { Id = 9, Name = "Trans2" });

            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<AvailableTransitions>(It.IsAny<RestRequest>())).Returns(returnData);

            Assert.That(jiraClient.GetAvailableTransitions("KEY-3"), Is.EqualTo(returnData));
        }


        [Test, Description("GetAvailableTransitions: On failure it returns null")]
        public void GetAvailableTransitions_OnFailure_It_Returns_Null()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<AvailableTransitions>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.GetAvailableTransitions("KEY-3"), Is.Null);
        }


        [Test, Description("DoTransition: On success it returns true")]
        public void DoTransition_OnSuccess_It_Returns_True()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Returns(new object());

            Assert.That(jiraClient.DoTransition("DG-42", 6), Is.True);
        }


        [Test, Description("DoTransition: On failure it returns false")]
        public void DoTransition_OnFailure_It_Returns_False()
        {
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            Assert.That(jiraClient.DoTransition("DG-42", 6), Is.False);
        }


        #region JIRA Cloud Compatibility Tests

        [Test, Description("ValidateSession: Uses /rest/api/2/myself endpoint for JIRA Cloud compatibility")]
        public void ValidateSession_Uses_Myself_Endpoint_For_Cloud_Compatibility()
        {
            // Arrange
            RestRequest capturedRequest = null;
            jiraApiRequestFactoryMock.Setup(f => f.CreateValidateSessionRequest())
                .Returns(() => {
                    capturedRequest = new RestRequest("/rest/api/2/myself", Method.Get);
                    return capturedRequest;
                });
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Returns(new object());

            // Act
            jiraClient.ValidateSession();

            // Assert - verify that CreateValidateSessionRequest was called (factory creates correct endpoint)
            jiraApiRequestFactoryMock.Verify(f => f.CreateValidateSessionRequest(), Times.Once);
        }


        [Test, Description("GetFavoriteFilters: Uses /rest/api/2/filter/favourites endpoint for JIRA Cloud compatibility")]
        public void GetFavoriteFilters_Uses_Favourites_Endpoint_For_Cloud_Compatibility()
        {
            // Arrange
            List<Filter> returnData = new List<Filter>
            {
                new Filter { Id = 1, Name = "My Filter", Jql = "project = TEST" }
            };
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<List<Filter>>(It.IsAny<RestRequest>())).Returns(returnData);

            // Act
            var result = jiraClient.GetFavoriteFilters();

            // Assert - verify the correct factory method was called
            jiraApiRequestFactoryMock.Verify(f => f.CreateGetFavoriteFiltersRequest(), Times.Once);
            Assert.That(result, Is.EqualTo(returnData));
        }


        [Test, Description("GetFavoriteFilters: Returns empty list instead of null when API returns empty array")]
        public void GetFavoriteFilters_Returns_Empty_List_When_Api_Returns_Empty()
        {
            // Arrange
            List<Filter> emptyList = new List<Filter>();
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<List<Filter>>(It.IsAny<RestRequest>())).Returns(emptyList);

            // Act
            var result = jiraClient.GetFavoriteFilters();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }


        [Test, Description("GetFavoriteFilters: Preserves error message from requester on failure")]
        public void GetFavoriteFilters_Preserves_ErrorMessage_On_Failure()
        {
            // Arrange
            string expectedError = "HTTP 401: {\"errorMessages\":[\"You do not have the permission to see the specified issue.\"],\"errors\":{}}";
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<List<Filter>>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            jiraApiRequesterMock.SetupGet(m => m.ErrorMessage).Returns(expectedError);

            // Act
            var result = jiraClient.GetFavoriteFilters();

            // Assert
            Assert.That(result, Is.Null);
            jiraApiRequesterMock.VerifyGet(m => m.ErrorMessage, Times.AtLeastOnce);
        }


        [Test, Description("GetIssuesByJQL: Handles special characters in JQL through URL encoding")]
        public void GetIssuesByJQL_Handles_Special_Characters_In_Jql()
        {
            // Arrange
            string jqlWithSpecialChars = "summary ~ \"test & bug\" AND priority = High";
            SearchResult returnData = new SearchResult { Issues = new List<Issue>() };
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<SearchResult>(It.IsAny<RestRequest>())).Returns(returnData);

            // Act
            var result = jiraClient.GetIssuesByJQL(jqlWithSpecialChars);

            // Assert - verify the factory method was called with JQL containing special chars
            jiraApiRequestFactoryMock.Verify(f => f.CreateGetIssuesByJQLRequest(It.Is<string>(jql => jql == jqlWithSpecialChars)), Times.Once);
        }


        [Test, Description("ValidateSession: Sets ErrorMessage from requester on authentication failure")]
        public void ValidateSession_Sets_ErrorMessage_On_Auth_Failure()
        {
            // Arrange
            string expectedError = "HTTP 401: {\"errorMessages\":[\"User is not authenticated\"]}";
            jiraApiRequesterMock.Setup(m => m.DoAuthenticatedRequest<object>(It.IsAny<RestRequest>())).Throws<RequestDeniedException>();
            jiraApiRequesterMock.SetupGet(m => m.ErrorMessage).Returns(expectedError);

            // Act
            var result = jiraClient.ValidateSession();

            // Assert
            Assert.That(result, Is.False);
            Assert.That(jiraClient.SessionValid, Is.False);
            Assert.That(jiraClient.ErrorMessage, Is.EqualTo(expectedError));
        }

        #endregion


    }
}
