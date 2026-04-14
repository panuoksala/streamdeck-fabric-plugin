using Microsoft.Extensions.Logging;
using StreamDeckLib;
using System.Net.Http;

namespace StreamDeckMicrosoftFabric
{
    [ActionUuid(Uuid = "net.fabricdeck.microsoftfabric.userdatafunctionrunner")]
    public class UserDataFunctionRunner(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        : BaseAction(clientFactory, loggerFactory)
    {
        protected override SupportedActions ResolveAction()
        {
            return SupportedActions.RunUserDataFunction;
        }
    }
}
