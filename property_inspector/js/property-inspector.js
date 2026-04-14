// global websocket, used to communicate from/to Stream Deck software
// as well as some info about our plugin, as sent by Stream Deck software 
var websocket = null,
    uuid = null,
    inInfo = null,
    actionInfo = {},
    settingsModel = {};

const normalizeStageValue = (value) => {
    if (value === undefined || value === null) {
        return '';
    }

    return value.toString().trim();
};

const ACTION_IDS = {
    CONSUMPTION: 'net.fabricdeck.microsoftfabric.consumptionrunner',
    DEPLOY: 'net.fabricdeck.microsoftfabric.deployrunner',
    UDF: 'net.fabricdeck.microsoftfabric.userdatafunctionrunner'
};

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) {
    uuid = inUUID;
    actionInfo = JSON.parse(inActionInfo);
    inInfo = JSON.parse(inInfo);
    websocket = new WebSocket('ws://localhost:' + inPort);

    try {
        configureUiForAction(actionInfo.action);

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
            settingsModel.SourceStageId = normalizeStageValue(actionInfo.payload.settings.settingsModel.SourceStageId);
            settingsModel.TargetStageId = normalizeStageValue(actionInfo.payload.settings.settingsModel.TargetStageId);
            settingsModel.UdfParam1Name = actionInfo.payload.settings.settingsModel.UdfParam1Name || '';
            settingsModel.UdfParam1Type = actionInfo.payload.settings.settingsModel.UdfParam1Type || 0;
            settingsModel.UdfParam1Value = actionInfo.payload.settings.settingsModel.UdfParam1Value || '';
            settingsModel.UdfParam2Name = actionInfo.payload.settings.settingsModel.UdfParam2Name || '';
            settingsModel.UdfParam2Type = actionInfo.payload.settings.settingsModel.UdfParam2Type || 0;
            settingsModel.UdfParam2Value = actionInfo.payload.settings.settingsModel.UdfParam2Value || '';
            settingsModel.UdfParam3Name = actionInfo.payload.settings.settingsModel.UdfParam3Name || '';
            settingsModel.UdfParam3Type = actionInfo.payload.settings.settingsModel.UdfParam3Type || 0;
            settingsModel.UdfParam3Value = actionInfo.payload.settings.settingsModel.UdfParam3Value || '';
            settingsModel.UdfParam4Name = actionInfo.payload.settings.settingsModel.UdfParam4Name || '';
            settingsModel.UdfParam4Type = actionInfo.payload.settings.settingsModel.UdfParam4Type || 0;
            settingsModel.UdfParam4Value = actionInfo.payload.settings.settingsModel.UdfParam4Value || '';
            settingsModel.UdfParam5Name = actionInfo.payload.settings.settingsModel.UdfParam5Name || '';
            settingsModel.UdfParam5Type = actionInfo.payload.settings.settingsModel.UdfParam5Type || 0;
            settingsModel.UdfParam5Value = actionInfo.payload.settings.settingsModel.UdfParam5Value || '';
        } else {
            settingsModel.WorkspaceId = "";
            settingsModel.ResourceId = "";
            settingsModel.UpdateStatusEverySecond = 2; // option 2 from dropdown by default
            settingsModel.ErrorMessage = "";
            settingsModel.ClientId = "";
            settingsModel.TenantId = "";
            settingsModel.Secret = "";
            settingsModel.SourceStageId = "";
            settingsModel.TargetStageId = "";
            settingsModel.UdfParam1Name = ""; settingsModel.UdfParam1Type = 0; settingsModel.UdfParam1Value = "";
            settingsModel.UdfParam2Name = ""; settingsModel.UdfParam2Type = 0; settingsModel.UdfParam2Value = "";
            settingsModel.UdfParam3Name = ""; settingsModel.UdfParam3Type = 0; settingsModel.UdfParam3Value = "";
            settingsModel.UdfParam4Name = ""; settingsModel.UdfParam4Type = 0; settingsModel.UdfParam4Value = "";
            settingsModel.UdfParam5Name = ""; settingsModel.UdfParam5Type = 0; settingsModel.UdfParam5Value = "";
        }

        document.getElementById('txtWorkspaceId').value = settingsModel.WorkspaceId;
        document.getElementById('txtResourceId').value = settingsModel.ResourceId;
        document.getElementById('txtClientId').value = settingsModel.ClientId;
        document.getElementById('txtSecret').value = settingsModel.Secret;
        document.getElementById('txtTenantId').value = settingsModel.TenantId;
        document.getElementById('txtSourceStageId').value = settingsModel.SourceStageId;
        document.getElementById('txtTargetStageId').value = settingsModel.TargetStageId;
        document.getElementById('txtUdfParam1Name').value = settingsModel.UdfParam1Name;
        document.getElementById('selUdfParam1Type').value = settingsModel.UdfParam1Type;
        document.getElementById('txtUdfParam1Value').value = settingsModel.UdfParam1Value;
        document.getElementById('txtUdfParam2Name').value = settingsModel.UdfParam2Name;
        document.getElementById('selUdfParam2Type').value = settingsModel.UdfParam2Type;
        document.getElementById('txtUdfParam2Value').value = settingsModel.UdfParam2Value;
        document.getElementById('txtUdfParam3Name').value = settingsModel.UdfParam3Name;
        document.getElementById('selUdfParam3Type').value = settingsModel.UdfParam3Type;
        document.getElementById('txtUdfParam3Value').value = settingsModel.UdfParam3Value;
        document.getElementById('txtUdfParam4Name').value = settingsModel.UdfParam4Name;
        document.getElementById('selUdfParam4Type').value = settingsModel.UdfParam4Type;
        document.getElementById('txtUdfParam4Value').value = settingsModel.UdfParam4Value;
        document.getElementById('txtUdfParam5Name').value = settingsModel.UdfParam5Name;
        document.getElementById('selUdfParam5Type').value = settingsModel.UdfParam5Type;
        document.getElementById('txtUdfParam5Value').value = settingsModel.UdfParam5Value;
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
                    if (jsonObj.payload.settings.settingsModel.SourceStageId !== undefined) {
                        settingsModel.SourceStageId = normalizeStageValue(jsonObj.payload.settings.settingsModel.SourceStageId);
                        document.getElementById('txtSourceStageId').value = settingsModel.SourceStageId;
                    }
                    if (jsonObj.payload.settings.settingsModel.TargetStageId !== undefined) {
                        settingsModel.TargetStageId = normalizeStageValue(jsonObj.payload.settings.settingsModel.TargetStageId);
                        document.getElementById('txtTargetStageId').value = settingsModel.TargetStageId;
                    }
                    ['1','2','3','4','5'].forEach(n => {
                        const sm = jsonObj.payload.settings.settingsModel;
                        if (sm['UdfParam' + n + 'Name'] !== undefined) {
                            settingsModel['UdfParam' + n + 'Name'] = sm['UdfParam' + n + 'Name'];
                            document.getElementById('txtUdfParam' + n + 'Name').value = settingsModel['UdfParam' + n + 'Name'];
                        }
                        if (sm['UdfParam' + n + 'Type'] !== undefined) {
                            settingsModel['UdfParam' + n + 'Type'] = sm['UdfParam' + n + 'Type'];
                            document.getElementById('selUdfParam' + n + 'Type').value = settingsModel['UdfParam' + n + 'Type'];
                        }
                        if (sm['UdfParam' + n + 'Value'] !== undefined) {
                            settingsModel['UdfParam' + n + 'Value'] = sm['UdfParam' + n + 'Value'];
                            document.getElementById('txtUdfParam' + n + 'Value').value = settingsModel['UdfParam' + n + 'Value'];
                        }
                    });
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

function configureUiForAction(actionId) {
    const resourceLabel = document.getElementById('resource_id_label');
    const resourceInput = document.getElementById('txtResourceId');
    const workspaceSection = document.getElementById('workspace_id');
    const deploymentSection = document.getElementById('deployment_settings');
    const udfSection = document.getElementById('udf_settings');

    if (!resourceLabel || !resourceInput || !workspaceSection || !deploymentSection) {
        return;
    }

    if (actionId === ACTION_IDS.CONSUMPTION) {
        resourceLabel.innerText = 'Fabric Consumption Name';
        resourceInput.placeholder = 'Fabric Consumption Name';
        workspaceSection.style.display = 'block';
        deploymentSection.style.display = 'none';
        if (udfSection) udfSection.style.display = 'none';
    } else if (actionId === ACTION_IDS.DEPLOY) {
        resourceLabel.innerText = 'Deployment Pipeline Id';
        resourceInput.placeholder = 'Deployment Pipeline Id';
        workspaceSection.style.display = 'none';
        deploymentSection.style.display = 'block';
        if (udfSection) udfSection.style.display = 'none';
    } else if (actionId === ACTION_IDS.UDF) {
        resourceLabel.innerText = 'User Data Function Id';
        resourceInput.placeholder = 'User Data Function Id';
        workspaceSection.style.display = 'block';
        deploymentSection.style.display = 'none';
        if (udfSection) udfSection.style.display = 'block';
    } else {
        resourceLabel.innerText = 'Fabric Resource Id';
        resourceInput.placeholder = 'Fabric Resource Id.';
        workspaceSection.style.display = 'block';
        deploymentSection.style.display = 'none';
        if (udfSection) udfSection.style.display = 'none';
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
            if (param === 'SourceStageId' || param === 'TargetStageId') {
                value = normalizeStageValue(value);
            }

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

