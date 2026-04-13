namespace StopWatchTest
{
    using Moq;
    using NUnit.Framework;
    using RestSharp;
    using StopWatch;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    internal class TestPocoClass
    {
        public string foo { get; set; }
        public string bar { get; set; }
    }

    [TestFixture]
    public class JiraApiRequesterTest
    {
        private Mock<IRestClientWrapper> clientMock;
        private Mock<IRestClientFactory> clientFactoryMock;

        private Mock<IJiraApiRequestFactory> jiraApiRequestFactoryMock;

        private JiraApiRequester jiraApiRequester;

        [SetUp]
        public void Setup()
        {
            clientMock = new Mock<IRestClientWrapper>();

            clientFactoryMock = new Mock<IRestClientFactory>();
            clientFactoryMock.Setup(c => c.Create(It.IsAny<bool>())).Returns(clientMock.Object);

            jiraApiRequestFactoryMock = new Mock<IJiraApiRequestFactory>();

            jiraApiRequester = new JiraApiRequester(clientFactoryMock.Object, jiraApiRequestFactoryMock.Object);
        }

        private static RestResponse<TestPocoClass> TestAuth(RestRequest requestMock, string valid_username, string valid_apitoken)
        {
            var authParam = requestMock.Parameters.FirstOrDefault(p => p.Type == ParameterType.HttpHeader && p.Name == "Authorization");
            const string prefix = "Basic ";
            if (authParam != null)
            {
                if (authParam.Value is string && ((string)authParam.Value).StartsWith(prefix))
                {
                    var base64 = ((string)authParam.Value).Substring(prefix.Length);
                    try
                    {
                        string authString = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64));
                        var comps = authString.Split(':');
                        if (comps.Length == 2 && comps[0] == valid_username && comps[1] == valid_apitoken)
                        {
                            return new RestResponse<TestPocoClass>(requestMock)
                            {
                                StatusCode = HttpStatusCode.OK,
                                Data = new TestPocoClass() { foo = "foo", bar = "bar" },
                            };
                        }
                    }
                    catch (System.Exception)
                    { }
                }
            }
            return new RestResponse<TestPocoClass>(requestMock)
            {
                StatusCode = HttpStatusCode.Unauthorized
            };
        }

        [Test, Description("DoAuthenticatedRequest: with correct credentials return data without error message")]
        public void DoAuthenticatedRequest_WithValidCredentials()
        {
            var valid_username = "validusername";
            var valid_apitoken = "validapitoken";

            var requestMock = new RestRequest();

            clientMock.Setup(c => c.ExecuteAsync<TestPocoClass>(It.IsAny<RestRequest>())).Returns(() => Task.FromResult(TestAuth(requestMock, valid_username, valid_apitoken)));

            jiraApiRequester.SetAuthentication(valid_username, valid_apitoken);

            var response = jiraApiRequester.DoAuthenticatedRequest<TestPocoClass>(requestMock);

            Assert.That(response, Is.Not.Null);
            Assert.That(jiraApiRequester.ErrorMessage, Is.Empty);
        }

        [Test, Description("DoAuthenticatedRequest: with wrong credentials it throws an exception")]
        public void DoAuthenticatedRequest_WithInvalidCredentials()
        {
            var valid_username = "validusername";
            var valid_apitoken = "validapitoken";

            var requestMock = new RestRequest();

            clientMock.Setup(c => c.ExecuteAsync<TestPocoClass>(It.IsAny<RestRequest>())).Returns(() => Task.FromResult(TestAuth(requestMock, valid_username, valid_apitoken)));

            jiraApiRequester.SetAuthentication("invalidUsername", "invalidApiToken");

            Assert.Throws<RequestDeniedException>(() =>
            {
                var response = jiraApiRequester.DoAuthenticatedRequest<TestPocoClass>(requestMock);
            });
        }

    }
}
