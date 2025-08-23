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
            return (StatusUpdateFrequency)UpdateStatusEverySecond switch
            {
                StatusUpdateFrequency.Every30seconds => 30,
                StatusUpdateFrequency.Every60seconds => 60,
                StatusUpdateFrequency.Every180seconds => 180,
                StatusUpdateFrequency.Every300seconds => 300,
                _ => 60, // Default to 60 to avoid issues if something goes wrong
            };
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
