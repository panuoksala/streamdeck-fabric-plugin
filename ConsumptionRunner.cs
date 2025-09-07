using Microsoft.Extensions.Logging;
using StreamDeckLib;
using System.Net.Http;

namespace StreamDeckMicrosoftFabric
{
    /// <summary>
    /// Action to show Microsoft Fabric capacity CU usage.
    /// ResourceId should contain the capacity name.
    /// </summary>
    [ActionUuid(Uuid = "net.fabricdeck.microsoftfabric.consumptionrunner")]
    public class ConsumptionRunner(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        : BaseAction(clientFactory, loggerFactory)
    {
        protected override SupportedActions ResolveAction()
        {
            return SupportedActions.GetCapacityUsage;
        }
    }
}
