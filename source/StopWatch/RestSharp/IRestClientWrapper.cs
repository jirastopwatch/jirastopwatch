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
using System.Threading.Tasks;

namespace StopWatch
{
    /// <summary>
    /// Wrapper interface for RestClient since RestSharp v107+ removed IRestClient.
    /// This enables mocking in unit tests (Moq cannot mock concrete classes).
    /// </summary>
    internal interface IRestClientWrapper
    {
        Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request);
        void AddDefaultHeader(string name, string value);
    }
}
