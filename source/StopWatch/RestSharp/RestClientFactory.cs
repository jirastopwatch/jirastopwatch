/**************************************************************************
Copyright 2016 Carsten Gehling

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**************************************************************************/
using RestSharp;
using System;
using System.Net;

namespace StopWatch
{
    internal class RestClientFactory : IRestClientFactory
    {
        private string _baseUrl = "";

        public string BaseUrl
        {
            get { return _baseUrl; }
            set
            {
                // Normalize Jira Cloud URLs: strip path components like /jira/ so that
                // API resource paths (e.g. /rest/api/2/myself) resolve correctly against
                // the host root. RestSharp v112 concatenates base URL path + resource path
                // instead of resolving the resource as an absolute path from the host.
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        var uri = new Uri(value.TrimEnd('/'));
                        _baseUrl = uri.GetLeftPart(UriPartial.Authority);
                    }
                    catch (UriFormatException)
                    {
                        _baseUrl = value;
                    }
                }
                else
                {
                    _baseUrl = value ?? "";
                }
            }
        }

        public RestClientFactory()
        {
            BaseUrl = "";
            this.cookieContainer = new CookieContainer();
        }


        public IRestClientWrapper Create(bool invalidateCookies = false)
        {
            if (invalidateCookies)
                cookieContainer = new CookieContainer();

            var options = new RestClientOptions(BaseUrl)
            {
                CookieContainer = cookieContainer,
                // Disable automatic redirect following so that 302 redirects to the
                // Jira Cloud login page are surfaced as non-200 status codes instead of
                // being silently followed and returning 200 + HTML.
                FollowRedirects = false
            };
            RestClient client = new RestClient(options);
            return new RestClientWrapper(client);
        }

        private CookieContainer cookieContainer;
    }
}
