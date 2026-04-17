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
using System.Net;

namespace StopWatch
{
    static class ReleaseHelper
    {
        public static GithubRelease GetLatestVersion()
        {
            var options = new RestClientOptions("https://api.github.com")
            {
                UserAgent = "jirastopwatch"
            };
            RestClient client = new RestClient(options);
            RestRequest request = new RestRequest("/repos/jirastopwatch/jirastopwatch/releases/latest");

            // RestSharp v112+ is async-only; use .GetAwaiter().GetResult() for sync context
            RestResponse<GithubRelease> response = client.ExecuteAsync<GithubRelease>(request).GetAwaiter().GetResult();
            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            return response.Data;
        }
    }
}
