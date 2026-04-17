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

using RestSharp;
using StopWatch.Logging;
using System;
using System.Linq;
using System.Net;

namespace StopWatch
{
    internal class JiraApiRequester : IJiraApiRequester
    {
        public string ErrorMessage { get; private set; }

        public JiraApiRequester(IRestClientFactory restClientFactory, IJiraApiRequestFactory jiraApiRequestFactory)
        {
            this.restClientFactory = restClientFactory;
            this.jiraApiRequestFactory = jiraApiRequestFactory;
            ErrorMessage = "";
        }

        public T DoAuthenticatedRequest<T>(RestRequest request)
            where T : new()
        {
            AddAuthHeader(request);

            IRestClientWrapper client = restClientFactory.Create();

            _logger.Log(string.Format("Request: {0}", request.Resource));
            // RestSharp v112+ is async-only; use .GetAwaiter().GetResult() for sync WinForms context
            RestResponse<T> response = client.ExecuteAsync<T>(request).GetAwaiter().GetResult();
            _logger.Log(string.Format("Response: {0} - {1} (URL: {2})",
                response.StatusCode,
                StringHelpers.Truncate(response.Content, 100),
                response.ResponseUri));

            // Detect redirects (now that FollowRedirects is disabled).
            // Jira Cloud may return 302 to its login page when auth fails instead of 401.
            if (response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.MovedPermanently ||
                response.StatusCode == HttpStatusCode.TemporaryRedirect ||
                (int)response.StatusCode == 308)
            {
                _logger.Log(string.Format("ERROR: Jira redirected {0} to {1}. This usually means authentication failed.",
                    request.Resource, response.Headers?.FirstOrDefault(h => h.Name == "Location")?.Value ?? "(unknown)"));
                ErrorMessage = "Jira redirected to a login page. Your API token may be invalid or expired. Please check your credentials in Settings.";
                throw new RequestDeniedException();
            }

            // If login session has expired, try to login, and then re-execute the original request
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Preserve full response content for diagnostics
                ErrorMessage = FormatErrorResponse(response);
                throw new RequestDeniedException();
            }

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
            {
                // Preserve full response content for diagnostics instead of just ErrorMessage
                ErrorMessage = FormatErrorResponse(response);
                throw new RequestDeniedException();
            }

            // Detect when Jira returns an HTML login/landing page instead of JSON.
            // This happens when Basic auth credentials are invalid or the API token has expired —
            // some Jira Cloud instances return HTTP 200 with text/html instead of a 401.
            if (response.ContentType != null && response.ContentType.Contains("text/html"))
            {
                _logger.Log(string.Format("ERROR: Jira returned HTML instead of JSON for {0}. This typically means authentication failed silently. Content-Type: {1}",
                    request.Resource, response.ContentType));
                ErrorMessage = "Jira returned an HTML page instead of JSON. Your API token may be invalid or expired. Please check your credentials in Settings.";
                throw new RequestDeniedException();
            }

            // Detect deserialization failures: HTTP 200 but Data is null
            if (response.Data == null)
            {
                _logger.Log(string.Format("WARNING deserialization returned null for {0}. Content-Type: {1}, ContentLength: {2}, Content: {3}",
                    request.Resource,
                    response.ContentType ?? "(null)",
                    response.Content?.Length ?? 0,
                    StringHelpers.Truncate(response.Content, 500)));
            }

            ErrorMessage = "";
            return response.Data;
        }


        private string FormatErrorResponse(RestResponse response)
        {
            // Prefer the response content as it contains JIRA's error details
            if (!string.IsNullOrEmpty(response.Content))
            {
                return string.Format("HTTP {0}: {1}", (int)response.StatusCode, response.Content);
            }
            // Fall back to ErrorMessage if Content is empty
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                return string.Format("HTTP {0}: {1}", (int)response.StatusCode, response.ErrorMessage);
            }
            // Last resort: just the status
            return string.Format("HTTP {0}: {1}", (int)response.StatusCode, response.StatusDescription);
        }

        public void SetAuthentication(string username, string apiToken)
        {
            _username = username;
            _apiToken = apiToken;
        }

        private void AddAuthHeader(RestRequest request)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_apiToken))
            {
                throw new UsernameAndApiTokenNotSetException();
            }
            request.AddHeader("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_username}:{_apiToken}")));
            // Explicitly request JSON to prevent Jira Cloud from returning HTML
            // when content negotiation defaults to text/html
            request.AddHeader("Accept", "application/json");
        }

        private Logger _logger = Logger.Instance;

        private IRestClientFactory restClientFactory;
        private IJiraApiRequestFactory jiraApiRequestFactory;
        private string _username;
        private string _apiToken;
    }

    internal class RequestDeniedException : Exception
    {
        public RequestDeniedException() : base()
        {
        }

        public RequestDeniedException(string message) : base(message)
        {
        }

        public RequestDeniedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    internal class UsernameAndApiTokenNotSetException : Exception
    {
        public UsernameAndApiTokenNotSetException() : base()
        {
        }

        public UsernameAndApiTokenNotSetException(string message) : base(message)
        {
        }

        public UsernameAndApiTokenNotSetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
