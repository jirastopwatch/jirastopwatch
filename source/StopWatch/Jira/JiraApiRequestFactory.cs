/**
 * Copyright © 2026 Marco Leonor
 * Copyright © 2023 Y. Meyer-Norwood
 * Copyright © 2020 Dan Tulloh
 * Copyright © 2016 Carsten Gehling
 * 
 * For a full list of contributing authors, see:
 *
 *     https://jirastopwatch.com/humans
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

using RestSharp;
using System;

namespace StopWatch
{
    internal class JiraApiRequestFactory : IJiraApiRequestFactory
    {
        #region public methods
        public JiraApiRequestFactory(IRestRequestFactory restRequestFactory)
        {
            this.restRequestFactory = restRequestFactory;
        }


        public RestRequest CreateValidateSessionRequest()
        {
            // Use /rest/api/2/myself endpoint which is compatible with JIRA Cloud API token auth
            // The legacy /rest/auth/1/session endpoint doesn't work with API tokens on JIRA Cloud
            var request = restRequestFactory.Create("/rest/api/2/myself", Method.Get);
            return request;
        }


        public RestRequest CreateGetFavoriteFiltersRequest()
        {
            // Use the singular /filter/favourite endpoint — the plural /filter/favourites
            // is treated as a filter ID lookup by Jira Cloud and returns HTTP 400.
            var request = restRequestFactory.Create("/rest/api/2/filter/favourite", Method.Get);
            return request;
        }
        

        public RestRequest CreateGetIssuesByJQLRequest(string jql)
        {
            // URL-encode the JQL query to handle special characters properly on JIRA Cloud
            var encodedJql = Uri.EscapeDataString(jql);
            // Jira Cloud has removed /rest/api/2/search (returns HTTP 410 Gone).
            // Use the v3 replacement endpoint /rest/api/3/search/jql instead.
            // Unlike v2, v3 returns only issue IDs by default — we must explicitly
            // request the fields the app needs (key, summary, project, timetracking).
            var request = restRequestFactory.Create(String.Format("/rest/api/3/search/jql?jql={0}&maxResults=200&fields=key,summary,project,timetracking", encodedJql), Method.Get);
            return request;
        }


        public RestRequest CreateGetIssueSummaryRequest(string key)
        {
            var request = restRequestFactory.Create(String.Format("/rest/api/2/issue/{0}", key.Trim()), Method.Get);
            return request;
        }

        public RestRequest CreateGetIssueTimetrackingRequest(string key)
        {
            var request = restRequestFactory.Create(String.Format("/rest/api/2/issue/{0}?fields=timetracking", key.Trim()), Method.Get);
            return request;
        }


        public RestRequest CreatePostWorklogRequest(string key, DateTimeOffset started, TimeSpan time, string comment, EstimateUpdateMethods adjustmentMethod, string adjustmentValue)
        {
            var request = restRequestFactory.Create(String.Format("/rest/api/2/issue/{0}/worklog", key.Trim()), Method.Post);
            request.AddJsonBody(new
                {
                    timeSpent = JiraTimeHelpers.TimeSpanToJiraTime(time),
                    started = JiraTimeHelpers.DateTimeToJiraDateTime(started),
                    comment = comment
                }
            );
            switch(adjustmentMethod) {
                case EstimateUpdateMethods.Leave:
                    request.AddQueryParameter("adjustEstimate", "leave");
                    break;
                case EstimateUpdateMethods.SetTo:
                    request.AddQueryParameter("adjustEstimate", "new");
                    request.AddQueryParameter("newEstimate", adjustmentValue);
                    break;
                case EstimateUpdateMethods.ManualDecrease:
                    request.AddQueryParameter("adjustEstimate", "manual");
                    request.AddQueryParameter("reduceBy", adjustmentValue);
                    break;
                case EstimateUpdateMethods.Auto:
                    request.AddQueryParameter("adjustEstimate", "auto");
                    break;
            }
            return request;
        }

        public RestRequest CreateGetConfigurationRequest()
        {
            return restRequestFactory.Create("/rest/api/2/configuration", Method.Get);
        }


        public RestRequest CreatePostCommentRequest(string key, string comment)
        {
            var request = restRequestFactory.Create(String.Format("/rest/api/2/issue/{0}/comment", key.Trim()), Method.Post);
            request.AddJsonBody(new
                {
                    body = comment
                }
            );
            return request;
        }

        public RestRequest CreateGetAvailableTransitions(string key)
        {
            var request = restRequestFactory.Create(String.Format("/rest/api/2/issue/{0}/transitions", key.Trim()), Method.Get);
            return request;
        }

        public RestRequest CreateDoTransition(string key, int transitionId)
        {
            var request = restRequestFactory.Create(String.Format("/rest/api/2/issue/{0}/transitions", key.Trim()), Method.Post);
            request.AddJsonBody(new
                {
                    transition = new
                    {
                        id = transitionId
                    }
                }
            );
            return request;
        }
        #endregion


        #region private members
        private IRestRequestFactory restRequestFactory;
        #endregion
    }
}
