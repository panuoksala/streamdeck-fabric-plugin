using System;

namespace StreamDeckMicrosoftFabric.Models
{
    /// <summary>
    /// Dont use Enums in this class because they won't work in BaseAction class.
    /// </summary>
    public class FabricSettingsModel
    {
        /// <summary>
        /// The Microsoft Fabric workspace id
        /// </summary>
        public string WorkspaceId { get; set; } = "";

        /// <summary>
        /// The Microsoft Fabric notebook id
        /// </summary>
        public string ResourceId { get; set; } = "";

        /// <summary>
        /// Client id from app registration that has access to Fabric.
        /// Plugin currently uses interactive authentication as Fabric does not support anything else at the moment.
        /// So no need to provide client secret.
        /// </summary>
        public string ClientId { get; set; } = "";

        /// <summary>
        /// Tenant id from app registration that has access to Fabric
        /// </summary>
        public string TenantId { get; set; } = "";

        /// <summary>
        /// Secret from app registration that has access to Fabric
        /// </summary>
        public string Secret { get; set; } = "";

        /// <summary>
        /// How often build/release status is fethced from Fabric
        /// </summary>
        public int UpdateStatusEverySecond { get; set; } = 0;

        /// <summary>
        /// Fabric has limited number of login methods that is supports, so 
        /// offer user to select which one to use.
        /// </summary>
        public int LoginMethod { get; set; } = 1;

        /// <summary>
        /// Possible error message that occures
        /// </summary>
        public string ErrorMessage { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(WorkspaceId);
        }

        public int GetUpdateStatusInSeconds()
        {
            switch((StatusUpdateFrequency)UpdateStatusEverySecond)
            {
                case StatusUpdateFrequency.Every30seconds:
                    return 30;
                case StatusUpdateFrequency.Every60seconds:
                    return 60;
                case StatusUpdateFrequency.Every180seconds:
                    return 180;
                case StatusUpdateFrequency.Every300seconds:
                    return 300;
                default:
                    return 0;
            }
        }
    }

    public enum StatusUpdateFrequency
    {
        Never = 0,
        Every30seconds = 1,
        Every60seconds = 2,
        Every180seconds = 3,
        Every300seconds = 4,
    }

    public enum LoginMethod
    {
        Interactive = 1,
        AppRegistration = 2
    }
}
