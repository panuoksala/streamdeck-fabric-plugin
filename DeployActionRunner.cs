using Microsoft.Extensions.Logging;
using StreamDeckLib;
using System.Net.Http;

namespace StreamDeckMicrosoftFabric
{
    /// <summary>
    /// Action to deploy Fabric deployment pipeline stages.
    /// </summary>
    [ActionUuid(Uuid = "net.fabricdeck.microsoftfabric.deployrunner")]
    public class DeployActionRunner(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        : BaseAction(clientFactory, loggerFactory)
    {
        protected override SupportedActions ResolveAction()
        {
            return SupportedActions.Deploy;
        }
    }
}
