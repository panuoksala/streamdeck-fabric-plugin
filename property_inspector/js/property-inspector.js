// global websocket, used to communicate from/to Stream Deck software
// as well as some info about our plugin, as sent by Stream Deck software 
var websocket = null,
    uuid = null,
    inInfo = null,
    actionInfo = {},
    settingsModel = {};

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) {
    uuid = inUUID;
    actionInfo = JSON.parse(inActionInfo);
    inInfo = JSON.parse(inInfo);
    websocket = new WebSocket('ws://localhost:' + inPort);

    try {
        // initialize values
        if (actionInfo.payload.settings.settingsModel) {
            settingsModel.WorkspaceId = actionInfo.payload.settings.settingsModel.WorkspaceId;
            settingsModel.ResourceId = actionInfo.payload.settings.settingsModel.ResourceId;
            settingsModel.ClientId = actionInfo.payload.settings.settingsModel.ClientId;
            settingsModel.Secret = actionInfo.payload.settings.settingsModel.Secret;
            settingsModel.TenantId = actionInfo.payload.settings.settingsModel.TenantId;
            settingsModel.UpdateStatusEverySecond = actionInfo.payload.settings.settingsModel.UpdateStatusEverySecond;
            settingsModel.ErrorMessage = actionInfo.payload.settings.settingsModel.ErrorMessage;
            settingsModel.LoginMethod = actionInfo.payload.settings.settingsModel.LoginMethod;
        } else {
            settingsModel.WorkspaceId = "";
            settingsModel.ResourceId = "";
            settingsModel.UpdateStatusEverySecond = 2; // option 2 from dropdown by default
            settingsModel.ErrorMessage = "";
            settingsModel.ClientId = "";
            settingsModel.TenantId = "";
            settingsModel.Secret = "";
        }

        document.getElementById('txtWorkspaceId').value = settingsModel.WorkspaceId;
        document.getElementById('txtResourceId').value = settingsModel.ResourceId;
        document.getElementById('txtClientId').value = settingsModel.ClientId;
        document.getElementById('txtSecret').value = settingsModel.Secret;
        document.getElementById('txtTenantId').value = settingsModel.TenantId;
        document.getElementById('update_status_every_second').value = settingsModel.UpdateStatusEverySecond;
        document.getElementById('loginmethod').value = settingsModel.LoginMethod;
        document.getElementById('error_message').innerHTML = settingsModel.ErrorMessage;

        websocket.onopen = function () {
            var json = { event: inRegisterEvent, uuid: inUUID };
            // register property inspector to Stream Deck
            websocket.send(JSON.stringify(json));
        };

        websocket.onmessage = function (evt) {
            // Received message from Stream Deck
            var jsonObj = JSON.parse(evt.data);
            var sdEvent = jsonObj['event'];
            switch (sdEvent) {
                case "didReceiveSettings":
                    if (jsonObj.payload.settings.settingsModel.WorkspaceId) {
                        settingsModel.WorkspaceId = jsonObj.payload.settings.settingsModel.WorkspaceId;
                        document.getElementById('txtWorkspaceId').value = settingsModel.WorkspaceId;
                    }
                    if (jsonObj.payload.settings.settingsModel.ResourceId) {
                        settingsModel.ResourceId = jsonObj.payload.settings.settingsModel.ResourceId;
                        document.getElementById('txtResourceId').value = settingsModel.ResourceId;
                    }
                    if (jsonObj.payload.settings.settingsModel.ClientId) {
                        settingsModel.ClientId = jsonObj.payload.settings.settingsModel.ClientId;
                        document.getElementById('txtClientId').value = settingsModel.ClientId;
                    }
                    if (jsonObj.payload.settings.settingsModel.Secret) {
                        settingsModel.Secret = jsonObj.payload.settings.settingsModel.Secret;
                        document.getElementById('txtSecret').value = settingsModel.Secret;
                    }
                    if (jsonObj.payload.settings.settingsModel.TenantId) {
                        settingsModel.TenantId = jsonObj.payload.settings.settingsModel.TenantId;
                        document.getElementById('txtTenantId').value = settingsModel.TenantId;
                    }
                    if (jsonObj.payload.settings.settingsModel.LoginMethod) {
                        settingsModel.LoginMethod = jsonObj.payload.settings.settingsModel.LoginMethod;
                        document.getElementById('loginmethod').value = settingsModel.LoginMethod;
                    }
                    if (jsonObj.payload.settings.settingsModel.UpdateStatusEverySecond) {
                        settingsModel.UpdateStatusEverySecond = jsonObj.payload.settings.settingsModel.UpdateStatusEverySecond;
                        document.getElementById('update_status_every_second').value = settingsModel.UpdateStatusEverySecond;
                    }
                    break;
                default:
                    break;
            }
        };
    } catch (err) {
        // Enable for debugging, but don't leave on in production
        // alert(err);
    }
}

const setSettings = (value, param) => {
    try {
        // If param is LoginMethod and value is 1, then set appregistration div to visible,
        // else hide it and on hide set values of ClientId, Secret and TenantId to empty
        if (param == "LoginMethod") {
            if (value == 2) {
                document.getElementById('appregistration').style.display = "block";
            } else {
                document.getElementById('appregistration').style.display = "none";
                document.getElementById('txtClientId').value = "";
                document.getElementById('txtSecret').value = "";
            }
        }

        if (websocket) {
            settingsModel[param] = value;
            var json = {
                "event": "setSettings",
                "context": uuid,
                "payload": {
                    "settingsModel": settingsModel
                }
            };
            websocket.send(JSON.stringify(json));
        }
    } catch (error) {
        alert(error);
    }
};

