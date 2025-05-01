using Microsoft.Extensions.Logging;
using Serilog;
using StreamDeckMicrosoftFabric.Models;
using StreamDeckMicrosoftFabric.Services;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace StreamDeckMicrosoftFabric
{

    /// <summary>
    /// We need this class just to have own action. We cannot add two actionuuid for the same action
    /// and if we don't have separated actionuuid, the action name is shown wrong (duplicates).
    /// </summary>
    [ActionUuid(Uuid = "net.oksala.microsoftfabric.datapipelinerunner")]
    public class FabricRunnerDatapipeline(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory) : FabricActionRunner(clientFactory, loggerFactory)
    {
    }
}
