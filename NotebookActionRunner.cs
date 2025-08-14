using Microsoft.Extensions.Logging;
using StreamDeckLib;
using System.Net.Http;

namespace StreamDeckMicrosoftFabric
{
    public enum SupportedActions
    {
        RunNotebook = 0,
        RunDatapipeline = 1,
        RunDataFlow = 2,
    }

    /// <summary>
    /// The notebook runner action
    /// </summary>
    [ActionUuid(Uuid = "net.oksala.microsoftfabric.notebookrunner")]
    public class NotebookActionRunner(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory) 
        : BaseAction(clientFactory, loggerFactory)
    {
        protected override SupportedActions ResolveAction()
        {
            return SupportedActions.RunNotebook;
        }
    }
}
