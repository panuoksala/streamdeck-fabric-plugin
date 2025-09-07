using Microsoft.Extensions.Logging;
using StreamDeckLib;
using System.Net.Http;

namespace StreamDeckMicrosoftFabric
{

    /// <summary>
    /// We need this class just to have own action. We cannot add two actionuuid for the same action
    /// and if we don't have separated actionuuid, the action name is shown wrong (duplicates).
    /// </summary>
    [ActionUuid(Uuid = "net.fabricdeck.microsoftfabric.datapipelinerunner")]
    public class DatapipelineActionRunner(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        : BaseAction(clientFactory, loggerFactory)
    {
        protected override SupportedActions ResolveAction()
        {
            return SupportedActions.RunDatapipeline;
        }
    }
}
