var vJsonCategoryData = null;
var vJsonControlData = null;
var childTargetControlID;
var setContextFlag = false;
var IsDiscardFromEquipment = false;
var deviceStatus = 'DISCONNECTED';
var serviceStatus = 'OFFLINE';
var serverId = '';
var createFileFlag = false;
var deviceRawDataOID = 0;
var lastVisibleControl;
//Timer 
var ticker;
var timeInSecs;
var SessiontimeInSecs;
var contextRefreshTimerId;

var SessionTimeout = '0';
var SessionAlertWindow = '0';
var SessionTimeoutTimerId;
var SessionAlertTimeoutTimerId;
var SessionCountDownTimerId;
var controlLevelVisibility = {};

$(document).ready(function () {

    //CSS Apply
    var vJsonCSSData = null;
    $.getJSON('Json/Style.json', function (data) {

        var vTextColor = '#000000'; //Default
        var vAppLogo = 'Images/GTLogo.png'; //default
        var vTitle = 'Login'; //default
        var vAppName = '';
        var vAppNameTextColor = '#000000';
        var vHeaderTextColor = '#000000';
        var vHeaderBgColor = '#000000';
        var vFooterText = '';
        var vFooterTextColor = '#000000';

        vJsonCommonStyle = data.CommonStyle;
        vJsonMessageData = data.MessageText;
        if (vJsonCommonStyle != null) {
            vTitle = vJsonCommonStyle[0].Title;
            vAppName = vJsonCommonStyle[0].AppName;
            vAppNameTextColor = vJsonCommonStyle[0].AppNameTextColor;
            vTextColor = vJsonCommonStyle[0].TextColor;
            vAppLogo = vJsonCommonStyle[0].AppLogo;
            vHeaderTextColor = vJsonCommonStyle[0].HeaderTextColor;
            vHeaderBgColor = vJsonCommonStyle[0].HeaderBgColor;
            vFooterText = vJsonCommonStyle[0].FooterText;
            vFooterTextColor = vJsonCommonStyle[0].FooterTextColor;
        }
        document.title = vTitle;
        document.getElementById('imgLogo').setAttribute('src', vAppLogo);
        $('#lblHeader')[0].innerText = vAppName;
        $('#lblFooter')[0].innerText = vFooterText;
        document.documentElement.style.setProperty('--Textcolor', vTextColor);
        document.documentElement.style.setProperty('--AppNameTextColor', vAppNameTextColor);
        document.documentElement.style.setProperty('--HeaderBgColor', vHeaderBgColor);
        document.documentElement.style.setProperty('--HeaderTextColor', vHeaderTextColor);
        document.documentElement.style.setProperty('--FooterTextColor', vFooterTextColor);
        //old
        //startTimer(0);
        TimeoutCountdownTimer(0);

    });
    $.getJSON('Json/ControlsConfig.json', function (data) {
        vJsonControlData = data.Config.Control;
        childTargetControlID = vJsonControlData[0]["ControlName"];
        LoadDefaultControl(vJsonControlData[0]);
    });
    buttonDisabled(true, true, true, true);

    LoadServiceStatus();

    var ServiceRefreshInterval = document.getElementById("hdnServiceRefreshInterval").value;
    setInterval(function () {
        LoadServiceStatus(); // this will run after every 5 mins (300000)
    }, ServiceRefreshInterval);

});

function buttonDisabled(IsbtnSetContextDisabled, IsbtnCreateFileDisabled, IsbtnReconnectDisabled, IsbtnDiscardDisabled) {
    document.getElementById("btnSetContext").disabled = IsbtnSetContextDisabled;
    document.getElementById("btnCreateFile").disabled = IsbtnCreateFileDisabled;
    document.getElementById("btnReconnect").disabled = IsbtnReconnectDisabled;
    document.getElementById("btnDiscard").disabled = IsbtnDiscardDisabled;
    if (IsbtnReconnectDisabled)
        document.getElementById('btnReconnect').src = 'Images/Reconnect-Button_Gray.png'
    else
        document.getElementById('btnReconnect').src = 'Images/Reconnect-Button.png';
    if (IsbtnDiscardDisabled)
        document.getElementById('btnDiscard').src = 'Images/Discard-Button_Gray.png'
    else
        document.getElementById('btnDiscard').src = 'Images/Discard-Button.png';
}



function setVisibility(vSelDropDownControlID, currentSelectedID, masterDiv) {

    var vArraySelDropdownControlID = vSelDropDownControlID.split(',');
    // var targetControlsDetails;
    //Check if more than 1 controls in Visibility section to get min. To find immediate next visible control to bind
    //if (vArraySelDropdownControlID.length > 1) {
    //    var minControlId = Math.min(...vArraySelDropdownControlID);
    //    targetControlsDetails = getTargetControlDetails(vJsonControlData, minControlId);
    //}
    //else {
    //    var minControlId = vArraySelDropdownControlID;
    //    targetControlsDetails = getTargetControlDetails(vJsonControlData, minControlId);
    //}

    vArraySelDropdownControlID.forEach(function (ControlID, index) {
        vArraySelDropdownControlID[index] = "#Controldiv" + ControlID;
    });

    for (var j = 1; j <= masterDiv.children.length - 1; j++) {
        var controlid = '#' + masterDiv.children[j].id;
        var foundrows = vArraySelDropdownControlID.includes(controlid);;
        if (foundrows) {
            $(controlid).show();
        }
        else {
            if (j >= currentSelectedID)
                $(controlid).hide();
        }
    }
    // return targetControlsDetails;
}



function DropDownOnChangeEvent(element) {
    ResetSessionTimeout();

    buttonDisabled(true, true, true, true);
    //old
    //document.getElementById("txtRawData").value = '';
    SetRawDataPacket('');

    //document.getElementById("txtRawData").style.backgroundColor = '#DFDFDF'; // Gray Color.

    SetRawDataPanelStyle('gray');

    //old
    //deviceStatus = 'DISCONNECTED';
    SetDeviceStatus('DISCONNECTED');

    SetDeviceStatusStyle();
    setContextFlag = false;
    //old
    //startTimer(0);
    TimeoutCountdownTimer(0);
    //old
    //startEquipmentTimer(false);
    ContextRefreshTimer(false);
    document.getElementById("hdnSelectedEquipment").value = '';
    document.getElementById("hdnSampleContextJSON").value = '';

    var objSelectedIdConfig = vJsonControlData.filter(function (el) {
        return el.ControlName == element.name
    });

    var arrVisibleIds = objSelectedIdConfig[0].Visibility;
    //if (arrVisibleIds != undefined) {
    //    var drpSelectedValue = element.options[element.selectedIndex].text;
    //    var visibleControlIds;

    //    if (arrVisibleIds[0][drpSelectedValue] != undefined) {
    //        visibleControlIds = arrVisibleIds[0][drpSelectedValue].split(',');
    //        lastVisibleControl = Math.max(...visibleControlIds);
    //    }
    //    else {
    //        lastVisibleControl = vJsonControlData.length;
    //    }
    //}


    var masterDiv = document.getElementById("divDynamicControl");
    var lasVisible;
    for (var j = 1; j <= masterDiv.children.length - 1; j++) {
        var controlid = '#' + masterDiv.children[j].id;
        var isVisible = true;

        //if control is hidden from parent, it will be in same state for child combo
        if (objSelectedIdConfig[0].ControlID > 1) {
            isVisible = $(controlid).is(":visible")
        }
        if (isVisible == true) {
            lastVisibleControl = j + 1; 
            $(controlid).show();
        }
    }

    //lastVisibleControl = lstV + 1;

    var currentSelectedID = objSelectedIdConfig[0].ControlID;

    //var drpSelectedValue = element.value
    var drpSelectedValue = element.options[element.selectedIndex].text;

    //get the visible ids from the json.

    //next target control to bind on change event 
    var targetControlName;


    if (arrVisibleIds != undefined) {
        var vSelDropDownControlID = arrVisibleIds[0][drpSelectedValue];
        var visibleIDs;

        if (vSelDropDownControlID != null || typeof vSelDropDownControlID !== "undefined") {
            var vArraySelDropdownControlID1 = vSelDropDownControlID.split(',');

            for (var i = 0; i < vArraySelDropdownControlID1.length; i++) {
                controlLevelVisibility[vArraySelDropdownControlID1[i]] = vSelDropDownControlID;
            }
            visibleIDs = vSelDropDownControlID;
        }
        else {
            visibleIDs = controlLevelVisibility[currentSelectedID];
        }

        // targetControlName = getVisileControlDetails(visibleIDs, currentSelectedID, masterDiv);
        if (visibleIDs != undefined)
            setVisibility(visibleIDs, currentSelectedID, masterDiv);
        for (var j = currentSelectedID; j <= masterDiv.children.length - 1; j++) {
            var controlid = '#' + masterDiv.children[j].id;
            isVisible = $(controlid).is(":visible");
            if (isVisible == true) {
                var targetControl = j + 1;
                targetControlName = getTargetControlDetails(vJsonControlData, targetControl);
                break;
            }
        }
    }
    else {
        if (currentSelectedID == 1) {
            //If visibility is not defined for first control, then all controls will be visible
            var allConfigControls;
            for (var c = 1; c <= masterDiv.children.length; c++) {
                allConfigControls = allConfigControls != undefined ? allConfigControls + ',' + c : c;
            }
            for (var c = 1; c <= masterDiv.children.length - 1; c++) {
                controlLevelVisibility[c] = allConfigControls;
            }
        }
        //If Visibility not assigned in config. Consider next control of selected one as target to bind
        for (var j = currentSelectedID; j <= masterDiv.children.length - 1; j++) {
            var controlid = '#' + masterDiv.children[j].id;
            isVisible = $(controlid).is(":visible")
            if (isVisible == true) {
                var targetControl = j + 1;
                targetControlName = getTargetControlDetails(vJsonControlData, targetControl);
                break;
            }
        }
    }





    var IsLastDropdown;
    if (currentSelectedID == lastVisibleControl)
        IsLastDropdown = true;

    var childControl;
    var strCurrentDataAsParam = '';
    for (var i = currentSelectedID; i < vJsonControlData.length; i++) {
        var controlName = '#' + masterDiv.children[i].id;
        var isVisible = $(controlName).is(":visible");
        //Reset all child dropdown to default.
        if ($(controlName)[0].childNodes[1].type == 'text')
            $(controlName)[0].childNodes[1].value = '';
        else {

            $(controlName).find("option:not(:first)").remove();
            var _controlName = vJsonControlData[i].ControlName;
            var labelAdditionalInfoID = "#lblAI_" + _controlName;
            if ($(labelAdditionalInfoID)[0] != undefined) {
                $(labelAdditionalInfoID)[0].style.display = "none";
                $(labelAdditionalInfoID)[0].innerHTML = '';
            }
        }
    }
    $('#ddlEquipment').find("option:not(:first)").remove();
    // Only for valid selection of current dropdown.
    if (element.selectedIndex != 0) {
        // for TVP
        for (var i = 0; i < currentSelectedID; i++) {
            var controlName = '#' + masterDiv.children[i].id;
            var isVisible = $(controlName).is(":visible");
            if (isVisible == true) {
                childControl = $(controlName)[0].childNodes[1].id;
                var selectedValue = document.getElementById(childControl).value
                var controlDetail = getTargetControlDetails(vJsonControlData, i + 1)
                var selectedKey = controlDetail.ControlName;
                var obj = selectedKey + '&:#' + selectedValue;
                if (strCurrentDataAsParam !== '') {
                    strCurrentDataAsParam = strCurrentDataAsParam + "&,#" + obj;
                }
                else {
                    strCurrentDataAsParam = obj;
                }
            }
        }

        if (IsLastDropdown == true) {
            LoadEquipmentControl('Equipment', strCurrentDataAsParam);
        }
        else {
            var paramChildControl = targetControlName.ControlName;
            var mandatory = targetControlName.Mandatory;
            var mandatorytext = targetControlName.MandatoryText;
            var AdditionalInfo = targetControlName.AdditionalInfo;
            childTargetControlID = targetControlName.ControlName;
            LoadChildControl(paramChildControl, strCurrentDataAsParam, mandatory, mandatorytext, AdditionalInfo);
        }
    }
    //Display the Additional Info
    var currentControlName = objSelectedIdConfig[0].ControlName;
    var labelAdditionalInfoID = "#lblAI_" + currentControlName;
    if ($(labelAdditionalInfoID)[0] != undefined)
        $(labelAdditionalInfoID)[0].style.display = "none";

    if (objSelectedIdConfig[0].AdditionalInfo != undefined && objSelectedIdConfig[0].AdditionalInfo.toLowerCase() == 'yes') {
        var _additionalInfo = '';
        $(labelAdditionalInfoID)[0].style.display = "block";
        if (element.selectedIndex != 0) {
            if (element.options[element.selectedIndex].attributes["data-additionalInfo"] != undefined)
                _additionalInfo = element.options[element.selectedIndex].attributes["data-additionalInfo"].value;
            $(labelAdditionalInfoID)[0].innerHTML = _additionalInfo;
        }
        else {
            $(labelAdditionalInfoID)[0].style.display = "none";
            $(labelAdditionalInfoID)[0].innerHTML = _additionalInfo;
        }
    }
    $("#Controldiv" + currentSelectedID).show();
}

//For Equipment Dropdown Change 
function OnEquipmentDropdownChange() {

    ResetSessionTimeout();

    var strEquipmentID = $('#ddlEquipment')[0].selectedOptions[0].text;
    var EqipmentselectedKey = $('#ddlEquipment')[0].value;
    document.getElementById("hdnSelectedEquipment").value = strEquipmentID;
    var strSampleContextJSON = '';
    TimeoutCountdownTimer(0);
    ContextRefreshTimer(false);
    SetDeviceStatus('DISCONNECTED');
    //For UI notification 
    SetDeviceStatusStyle();
    buttonDisabled(true, true, true, true);
    SetRawDataPacket('');
    SetRawDataPanelStyle('gray'); // Gray Color.

    if (EqipmentselectedKey != 0) {
        var masterDiv = document.getElementById("divDynamicControl");
        GetSampleContextJSON();
        LoadSampleContextByEquipment();
        //ToDo Service Status
    }
    else {
        document.getElementById("hdnSelectedEquipment").value = '';
        document.getElementById("hdnSampleContextJSON").value = '';
        return false;
    }
    return false;
}

//Call this function in Equipment dropdown onchange event. 
function LoadSampleContextByEquipment() {

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var strEquipmentID = document.getElementById("hdnSelectedEquipment").value;
    var strSampleContextJSON = document.getElementById("hdnSampleContextJSON").value;

    serverId = '';
    deviceRawDataOID = 0;

    if (strEquipmentID != '' && strSampleContextJSON != '') {

        var urlString = "SampleContext.aspx/GetSampleContextDetail";
        $.ajax({
            type: "POST",
            url: urlString,
            data: "{_strLoggedInUser: '" + strLoggedInUser + "',_strEquipmentID: '" + strEquipmentID + "',_strSampleContextJSON: '" + strSampleContextJSON + "' }",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: false,
            success: function (response) {
                var vResult = response.d;
                if (vResult == 0) {
                    buttonDisabled(false, true, true, true);
                }
                else {
                    deviceRawDataOID = vResult[0].DEVICERAWDATAOID;
                    var _sampleStatus = vResult[0].SAMPLESTATUS;
                    var _rawDataPacket = vResult[0].RAWDATAPACKET;
                    SetDeviceStatus(vResult[0].DEVICESTATUS);
                    var _timerIntervalInSec = vResult[0].TIMERINTERVALINSEC;
                    var _fileName = vResult[0].FILENAME;
                    serverId = vResult[0].SERVERID;

                    LoadServiceStatus();

                    if (serviceStatus == 'OFFLINE') {
                        SetDeviceStatus('DISCONNECTED');
                        buttonDisabled(true, false, false, false);
                    }

                    LoadSampleContextDetail(_sampleStatus, _rawDataPacket, _fileName, _timerIntervalInSec);
                }
            },
            error: OnErrorCall
        });
    }
}

//Call this function on ContextRefreshTimer - every 2 secs
function RefreshContext() {

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;

    if (deviceRawDataOID <= 0) {
        return false;
    }

    var urlString = "SampleContext.aspx/RefreshSampleContextDetails";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_strLoggedInUser: '" + strLoggedInUser + "', _oid: " + deviceRawDataOID + "}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            var vResult = response.d;
            if (vResult == 0) {
                buttonDisabled(false, true, true, true);
            }
            else {
                var _sampleStatus = vResult[0].SAMPLESTATUS;
                var _rawDataPacket = vResult[0].RAWDATAPACKET;
                SetDeviceStatus(vResult[0].DEVICESTATUS);
                var _timerIntervalInSec = vResult[0].TIMERINTERVALINSEC;
                var _fileName = vResult[0].FILENAME;

                LoadSampleContextDetail(_sampleStatus, _rawDataPacket, _fileName, _timerIntervalInSec);
            }
        },
        error: OnErrorCall
    });
}

/*
function SetSampleContext() {
    SetRawDataPanelStyle('blue');   // Blue Color.
    SetDeviceStatusStyle();
    ContextRefreshTimer(true);
}*/


function LoadSampleContextDetail(_sampleStatus, _rawDataPacket, _fileName, _timerIntervalInSec) {

    if (_sampleStatus == 'CONTEXT_SET') {
        setContextFlag = true;
        SetRawDataPanelStyle('blue');   // Blue Color.
        SetRawDataPacket(_rawDataPacket);
        SetDeviceStatusStyle();

        if (deviceStatus == 'CONNECTED')
            TimeoutCountdownTimer(_timerIntervalInSec);   //use this function for showing timeout count down for the user.
        else
            TimeoutCountdownTimer(0);

        if (deviceStatus == 'CONNECTED' || deviceStatus == 'WAITING_TO_CONNECT' || deviceStatus == 'WAITING_TO_DISCONNECT') {
            buttonDisabled(true, false, true, false);
            ContextRefreshTimer(true);  //Start the timeout timer if CONNECTED,WAITING_TO_CONNECT,WAITING_TO_DISCONNECT
        }
        else {
            buttonDisabled(true, false, false, false);
            ContextRefreshTimer(false);    //Stop the timeout timer if DISCONNECTED,CONNECTION_ERROR
        }
    }
    else if (_sampleStatus == 'COMPLETED') {
        setContextFlag = false;
        TimeoutCountdownTimer(0);
        ContextRefreshTimer(false);
        buttonDisabled(true, true, true, true);
        SetDeviceStatusStyle();
        SetRawDataPanelStyle('gray'); // Gray Color.
        if (createFileFlag) {
            return false;
        }
        var _displayMessage = 'File ' + _fileName + ' already created for this sample \n \n';
        _displayMessage = _displayMessage + _rawDataPacket;
        SetRawDataPacket(_displayMessage);
    }
}


function getTargetControlDetails(vJsonControlData, targetControl) {
    var objTargetControl = vJsonControlData.filter(function (el) {
        return el.ControlID == targetControl
    });
    return objTargetControl[0];
}


function LoadServiceStatus() {

    var strEquipmentID = document.getElementById("hdnSelectedEquipment").value;
    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var urlString = "SampleContext.aspx/GetServiceStatus";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_equipmentID: '" + strEquipmentID + "',_strLoggedInUser: '" + strLoggedInUser + "' }",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            var _result = response.d;
            SetServiceStatusStyle(_result);
        },
        error: OnErrorCall
    });
}

function SetServiceStatusStyle(result) {
    if (result == '' || result == null)
        result = 'OFFLINE';

    serviceStatus = result;
    $('#lblService')[0].innerText = serviceStatus;
    if (serviceStatus == 'ONLINE') {
        $('#lblService').last().removeClass("serviceStatus_Red");
        $('#lblService').last().removeClass("serviceStatus_Yellow");
        $('#lblService').last().addClass("serviceStatus_Green");
    }
    else if (serviceStatus == 'PARTIALLY OFFLINE') {
        $('#lblService').last().removeClass("serviceStatus_Red");
        $('#lblService').last().removeClass("serviceStatus_Green");
        $('#lblService').last().addClass("serviceStatus_Yellow");
    }
    else {
        $('#lblService').last().removeClass("serviceStatus_Green");
        $('#lblService').last().removeClass("serviceStatus_Yellow");
        $('#lblService').last().addClass("serviceStatus_Red");
    }
}

function SetDeviceStatusStyle() {

    $('#lblStatusCircle').last().removeClass("fa fa-circle fa-circle_green");
    $('#lblStatusCircle').last().removeClass("fa fa-circle fa-circle_red");
    $('#lblStatusCircle').last().removeClass("fa fa-circle fa-circle_gray");
    $('#lblStatusCircle').last().removeClass("fa fa-circle fa-circle_yellow");
    if (deviceStatus == 'CONNECTED') {
        $('#lblStatusCircle').last().addClass("fa fa-circle fa-circle_green");
        $('#lblStatus')[0].innerText = 'Connected';
    }
    else if (deviceStatus == 'DISCONNECTED') {
        $('#lblStatusCircle').last().addClass("fa fa-circle fa-circle_gray");
        $('#lblStatus')[0].innerText = 'Disconnected';
    }
    else if (deviceStatus == 'WAITING_TO_CONNECT') {
        $('#lblStatusCircle').last().addClass("fa fa-circle fa-circle_yellow");
        $('#lblStatus')[0].innerText = 'Waiting to Connect';
    }
    else if (deviceStatus == 'WAITING_TO_DISCONNECT') {
        $('#lblStatusCircle').last().addClass("fa fa-circle fa-circle_yellow");
        $('#lblStatus')[0].innerText = 'Waiting to Disconnect';
    }
    else if (deviceStatus == 'CONNECTION_ERROR') {
        $('#lblStatusCircle').last().addClass("fa fa-circle fa-circle_red");
        $('#lblStatus')[0].innerText = 'Connection Error';
    }
    else {
        $('#lblStatus')[0].innerText = 'Disconnected';
        $('#lblStatusCircle').last().addClass("fa fa-circle fa-circle_gray"); //WAITING_TO_CONNECT & WAITING_TO_DISCONNECT 
    }
}

function OnErrorCall(response) {
    //alert(response);
}


function LoadDefaultControl(vDefaultJsonControlData) {

    ResetSessionTimeout();

    var mandatory = vDefaultJsonControlData.Mandatory;

    var AdditionalInfo = vDefaultJsonControlData.AdditionalInfo;
    var targetControlName = vDefaultJsonControlData.ControlName;


    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var urlString = "SampleContext.aspx/GetChildControlData";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_strLoggedInUser: '" + strLoggedInUser + "', targetControl: '" + targetControlName + "',visibleControlsData: '" + "" + "',mandatory: '" + mandatory + "',mandatorytext: '" + '' + "' }",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {

            var iResult = response.d;
            for (var i = 0, len = iResult.length; i < len; i++) {
                var item = iResult[JSON.parse(i)];
                if (AdditionalInfo != undefined && AdditionalInfo.toLowerCase() == 'yes') {
                    $('#' + childTargetControlID).append($("<option/>").val(item.FieldKey).text(item.FieldValue).attr('data-additionalInfo', item.AdditionalInfo));
                }
                else {
                    $('#' + childTargetControlID).append($("<option/>").val(item.FieldKey).text(item.FieldValue));
                }
            }
        },
        error: OnErrorCall
    });
}


function LoadChildControl(varChildControl, objSelectedVisibleData, mandatory, mandatorytext, AdditionalInfo) {

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var urlString = "SampleContext.aspx/GetChildControlData";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_strLoggedInUser: '" + strLoggedInUser + "',targetControl: '" + varChildControl + "',visibleControlsData: '" + objSelectedVisibleData + "',mandatory: '" + mandatory + "',mandatorytext: '" + mandatorytext + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            var iResult = response.d;
            var nomandatory = false;
            for (var i = 0, len = iResult.length; i < len; i++) {
                var item = iResult[JSON.parse(i)];
                if (AdditionalInfo != undefined && AdditionalInfo.toLowerCase() == 'yes') {
                    $('#' + childTargetControlID).append($("<option/>").val(item.FieldKey).text(item.FieldValue).attr('data-additionalInfo', item.AdditionalInfo));
                }
                else {
                    $('#' + childTargetControlID).append($("<option/>").val(item.FieldKey).text(item.FieldValue));
                }
                if (item.FieldKey.toLowerCase() == 'na') {
                    nomandatory = true;
                }
            }

            if (nomandatory == true) {
                $('#' + childTargetControlID).val('NA').change();
            }


        },
        error: OnErrorCall
    });
}
function LoadEquipmentControl(varChildControl, objSelectedVisibleData) {

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var urlString = "SampleContext.aspx/GetChildControlData";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_strLoggedInUser: '" + strLoggedInUser + "',targetControl: '" + varChildControl + "',visibleControlsData: '" + objSelectedVisibleData + "', mandatory: '" + '' + "',mandatorytext: '" + '' + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            var iResult = response.d;
            for (var i = 0, len = iResult.length; i < len; i++) {
                var item = iResult[JSON.parse(i)];
                $('#ddlEquipment').append($("<option/>").val(item.FieldKey).text(item.FieldValue));
            }
        },
        error: OnErrorCall
    });
}
function onReconnect() {

    ResetSessionTimeout();

    //Validation check
    if (!setContextFlag || (deviceStatus != 'DISCONNECTED' && deviceStatus != 'CONNECTION_ERROR')) {
        return false;
    }

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    if (deviceRawDataOID <= 0) {
        return false;
    }

    TimeoutCountdownTimer(0);
    LoadServiceStatus();

    if (serviceStatus == 'OFFLINE') {
        generate('error', 'Service is Offline. Please contact System Administrator.', 'alert', 'CloseErrorAlertMessage');
        SetDeviceStatus('DISCONNECTED');
        SetDeviceStatusStyle();
        ContextRefreshTimer(false);
        buttonDisabled(true, false, false, false);
        return false;
    }

    var urlString = "SampleContext.aspx/ReconnectDevice";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_strLoggedInUser: '" + strLoggedInUser + "',deviceRawDataOID: " + deviceRawDataOID + "}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            var _result = response.d;
            if (_result == 0) {
                SetDeviceStatus('WAITING_TO_CONNECT');
                SetDeviceStatusStyle();
                buttonDisabled(true, false, true, false); //(SetContext, CreateFile, Reconnect, Discard)
                ContextRefreshTimer(true);
            }
            else {
                generate('error', 'Reconnect failed due to unknown error. Please try again later.', 'alert', 'CloseErrorAlertMessage');
            }
        },
        error: OnErrorCall
    });
    return false;
}

function showFileSavedSuccessAlert() {
    var n = noty({
        text: 'File created successfully.',
        type: 'success',
        dismissQueue: true,
        layout: 'top',
        closeWith: ['button'],
        theme: 'relax',
        progressBar: false,
        modal: true,
        maxVisible: 1,
        template: '<div class="noty_message"><span class="noty_text"></span></div>',
        animation: {
            open: 'animated bounceInLeft',
            close: 'animated bounceOutRight',
            easing: 'swing',
            speed: 500 // opening & closing animation speed
        },
        buttons: [
            {
                addClass: 'btn btn-success btn-success-width', text: 'Ok', onClick: function ($noty) {
                    createFileFlag = false;
                    window.location.reload();  //Clear Form.
                    $noty.close();
                    return false;
                }
            }
        ]
    });
    return false;
}

//Code to fix the touch screen 3 secs delay in caling click event. Need to revisit to deal wigth actual touch for replacing click event
//$(document).on('touchstart click', '#btnSetContext', function (event) {
//    onSetContext();
//});

function onSetContext() {

    ResetSessionTimeout();

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var strEquipmentID = document.getElementById("hdnSelectedEquipment").value;
    var strSampleContextJSON = document.getElementById("hdnSampleContextJSON").value;

    if (strEquipmentID == '' || strSampleContextJSON == '') {
        return false;
    }

    TimeoutCountdownTimer(0);

    var urlString = "SampleContext.aspx/SetSampleContextDetail";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_strLoggedInUser: '" + strLoggedInUser + "',_strEquipmentID: '" + strEquipmentID + "',_strSampleContextJSON: '" + strSampleContextJSON + "' }",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            var _result = response.d;

            if (_result.includes("<|>")) {
                var response = _result.split('<|>');
                if (response.length > 0) {
                    serverId = response[0];
                    deviceRawDataOID = response[1];
                }
            }
            else if (_result != '0') {
                generate('error', _result, 'alert', 'CloseErrorAlertMessage');
                return false;
            }
            //if (_result.startsWith('ServerID:')) {
            //    serverId = _result.substr(9);
            //}
            //else if (_result != '0') {
            //    generate('error', _result, 'alert', 'CloseErrorAlertMessage');
            //    return false;
            //}

            SetRawDataPanelStyle('blue');   // Blue Color.			 
            setContextFlag = true;
            LoadServiceStatus();

            if (serviceStatus == 'ONLINE') {
                ContextRefreshTimer(true);
                SetDeviceStatus('WAITING_TO_CONNECT');
                SetDeviceStatusStyle();
                buttonDisabled(true, false, true, false);  //(SetContext, CreateFile, Reconnect, Discard)
            }
            else if (serviceStatus == 'OFFLINE') {
                generate('error', 'Service is Offline. Please contact System Administrator.', 'alert', 'CloseErrorAlertMessage');
                SetDeviceStatus('DISCONNECTED');
                SetDeviceStatusStyle();
                ContextRefreshTimer(false);
                buttonDisabled(true, false, false, false);
                return false;
            }
        },
        error: OnErrorCall
    });
    return false;
}


function onCreateFile() {

    ResetSessionTimeout();

    if (!setContextFlag)
        return false;

    createFileFlag = true;
    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var strEquipmentID = document.getElementById("hdnSelectedEquipment").value;
    var strSampleContextJSON = document.getElementById("hdnSampleContextJSON").value;

    var strFileHeaderDisplayText = 'User ID: ' + strLoggedInUser + '\n';
    var masterDiv = document.getElementById("divDynamicControl");

    for (var i = 0; i < masterDiv.children.length; i++) {
        var controlName = '#' + masterDiv.children[i].id;
        var isVisible = $(controlName).is(":visible");
        if (isVisible == true) {
            childControl = $(controlName)[0].childNodes[1].id;
            var selectedControl = document.getElementById(childControl);
            var selectedText = selectedControl.options[selectedControl.selectedIndex].text;
            var selectedKey = selectedControl.options[selectedControl.selectedIndex].value;
            var controlDetail = getTargetControlDetails(vJsonControlData, i + 1)
            var _FileHeaderDisplayText = controlDetail.FileHeaderDisplayText;
            if (_FileHeaderDisplayText == null || _FileHeaderDisplayText == '')
                _FileHeaderDisplayText = controlDetail.DisplayLabelValue;

            var AIFileHeaderDisplayText = '';
            var AdditionalInfo = '';
            if (controlDetail.AdditionalInfo != undefined && controlDetail.AdditionalInfo.toLowerCase() == 'yes') {
                AIFileHeaderDisplayText = controlDetail.AIFileHeaderDisplayText;
                var labelAdditionalInfoID = "#lblAI_" + controlDetail.ControlName;
                if ($(labelAdditionalInfoID)[0] != undefined) {
                    AdditionalInfo = $(labelAdditionalInfoID)[0].innerHTML;
                }
            }
            if (AdditionalInfo != '') {
                strFileHeaderDisplayText = strFileHeaderDisplayText + _FileHeaderDisplayText + ' : ' + selectedKey + '<|>' + selectedText + '<|>'
                    + AIFileHeaderDisplayText + ' : ' + AdditionalInfo + '\n';
            }
            else {
                strFileHeaderDisplayText = strFileHeaderDisplayText + _FileHeaderDisplayText + ' : ' + selectedKey + '<|>' + selectedText + '\n';
            }
        }
    }
    var strEquipmentSelectedKey = $('#ddlEquipment')[0].value;
    var strEquipmentSelectText = $('#ddlEquipment')[0].options[$('#ddlEquipment')[0].selectedIndex].text;
    strFileHeaderDisplayText = strFileHeaderDisplayText + 'Equipment' + ' : ' + strEquipmentSelectedKey + '<|>' + strEquipmentSelectText + '\n';

    if (strEquipmentID != '' && strSampleContextJSON != '') {
        GetFileMetaData(strLoggedInUser, strEquipmentID, strFileHeaderDisplayText);
    }
    return false;
}

function timerCountDown() {
    var secs = timeInSecs;
    if (secs > 0) {
        timeInSecs--;
    } else {
        clearInterval(ticker);
    }
    var mins = Math.floor(secs / 60);
    secs %= 60;
    var _timerValue = (mins < 10 ? "0" : "") + mins + ":" + (secs < 10 ? "0" : "") + secs;
    document.getElementById("countdown").innerHTML = _timerValue;
}

function AppVersion() {
    var strAppVersion = document.getElementById("hdnVersion").value;
    var arrayAppVersion = strAppVersion.split(",");
    var strAppVersionNo = arrayAppVersion[0];
    var strAppVersionDate = arrayAppVersion[1];
    var strDisplaytext = "App Version : " + strAppVersionNo + " <br \> " + strAppVersionDate;
    var n = noty({
        text: strDisplaytext,
        type: 'confirm',
        layout: 'center',
        modal: true,
        maxVisible: 1,
        closeWith: ['button'],
        theme: 'relax',
        template: '<div class="noty_message" style="font-size:13px; width:100px; cursor:default;"><div class="VersionMsgcss"></div><span class="noty_text"></span></div>',
        animation: {
            open: 'animated bounceInLeft',
            close: 'animated bounceOutRight',
            easing: 'swing',
            speed: 500 // opening & closing animation speed
        },
        buttons: [
            {
                addClass: 'btn btn-primary btn-primary-width', text: 'Ok', onClick: function ($noty) {
                    $noty.close();
                    return false;
                }
            }
        ]
    })
    return false;
}

function Clearform() {
    window.location.reload();
    return false;
}


function TimeoutCountdownTimer(interval) {
    timeInSecs = parseInt(interval);
    clearInterval(ticker);
    clearTimeout(ticker);
    ticker = setInterval("timerCountDown()", 1000);
}


function ContextRefreshTimer(_isStartTimer) {
    clearInterval(contextRefreshTimerId);
    clearTimeout(contextRefreshTimerId);
    if (_isStartTimer) {
        contextRefreshTimerId = setInterval(function () {
            ContextRefreshTimer(false);
            RefreshContext();
        }, 2000); //2 secs
    }
}

function SetDeviceStatus(status) {
    deviceStatus = status;
}

function SetRawDataPacket(value) {
    document.getElementById("txtRawData").value = value;
}

function SetRawDataPanelStyle(color) {
    if (color == 'gray') {
        document.getElementById("txtRawData").style.backgroundColor = '#DFDFDF';   // Gray Color.
    }
    else if (color == 'blue') {
        document.getElementById("txtRawData").style.backgroundColor = '#97CBFF';    //Blue Color
    }

}

function GetSampleContextJSON() {
    var strSampleContextJSON = '';
    var masterDiv = document.getElementById("divDynamicControl");
    var EqipmentselectedKey = $('#ddlEquipment')[0].value;
    for (var i = 0; i < masterDiv.children.length; i++) {
        var controlName = '#' + masterDiv.children[i].id;
        var isVisible = $(controlName).is(":visible");
        if (isVisible == true) {
            childControl = $(controlName)[0].childNodes[1].id;
            var selectedKey = document.getElementById(childControl).value
            var controlDetail = getTargetControlDetails(vJsonControlData, i + 1)
            var DisplayLabelValue = controlDetail.DisplayLabelValue;
            strSampleContextJSON = strSampleContextJSON + DisplayLabelValue + ':' + selectedKey + ',';
        }
    }

    strSampleContextJSON = strSampleContextJSON + 'Equipment' + ':' + EqipmentselectedKey;
    document.getElementById("hdnSampleContextJSON").value = strSampleContextJSON;
}

function GetFileMetaData(strLoggedInUser, strEquipmentID, strFileHeaderDisplayText) {
    var urlString = "SampleContext.aspx/GetFileMetaData";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{_strLoggedInUser: '" + strLoggedInUser + "',_strEquipmentID: '" + strEquipmentID + "',deviceRawDataOID: " + deviceRawDataOID + ",strFileHeaderDisplayText: '" + strFileHeaderDisplayText + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            var vResult = response.d;
            if (vResult == 'File created successfully.') {
                //alert(vResult);
                //window.location.reload();  //Clear Form.
                if (deviceStatus == 'CONNECTED') {
                    SetDeviceStatus('DISCONNECTED');
                }
                showFileSavedSuccessAlert();
                //createFileFlag = false;
                return false;
            }
            else {
                generate('error', vResult, 'alert', 'CloseErrorAlertMessage');
                createFileFlag = false;
                return false;
            }
        },
        error: OnErrorCall
    });
}

function Logout() {

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var urlString = "SampleContext.aspx/Logout";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{ LoggedInUser: '" + strLoggedInUser + "' }",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            window.location.href = "Login";
        },
        error: OnErrorCall
    });
    return false;
}

function ResetSessionTimeout() {
    SessionTimeout = document.getElementById("hdnSessionTimeout").value;
    SessionAlertWindow = document.getElementById("hdnSessionAlertWindow").value;
    var SessionTimeoutInMilliSeconds = (SessionTimeout * 60000);
    var SessionAlertWindowInMilliSeconds = ((SessionTimeout - SessionAlertWindow) * 60000); //(SessionAlertWindow * 60000);   

    SessionCountdownDiff = ((SessionTimeoutInMilliSeconds - SessionAlertWindowInMilliSeconds) / 1000); //Convert milli seconds value to seconds.

    clearInterval(SessionAlertTimeoutTimerId);
    clearTimeout(SessionAlertTimeoutTimerId);
    clearInterval(SessionTimeoutTimerId);
    clearTimeout(SessionTimeoutTimerId);
    clearInterval(SessionCountDownTimerId);
    clearTimeout(SessionCountDownTimerId);

    if (SessionAlertWindowInMilliSeconds > 0) {
        SessionAlertTimeoutTimerId = setInterval(function () {
            SessionTimeoutCountdownTimer(SessionCountdownDiff)
            ConfirmSessionTimeOut();
            return false;
        }, SessionAlertWindowInMilliSeconds);
    }

    SessionTimeoutTimerId = setInterval(function () {
        //Redirect to Session page,
        SessionExpired();
    }, SessionTimeoutInMilliSeconds);

    return false;
}
function onSessionExtend() {
    ResetSessionTimeout();
    return false;
}

function ConfirmSessionTimeOut() {
    $('#SessionExpiryPopup').modal({
        backdrop: 'static',
        keyboard: false
    });
    $('#SessionExpiryPopup').modal('show');

    //Remove the timer after showing the confirmation message.
    clearInterval(SessionAlertTimeoutTimerId);
    clearTimeout(SessionAlertTimeoutTimerId);
    return false;
}

function Sessionlogout() {
    Logout();
    $('#SessionExpiryPopup').modal('hide');
    return false;
}

function ContinueSession() {
    onSessionExtend();
    $('#SessionExpiryPopup').modal('hide');
    return false;
}

function SessionTimeoutCountdownTimer(interval) {
    SessiontimeInSecs = parseInt(interval);
    clearInterval(SessionCountDownTimerId);
    clearTimeout(SessionCountDownTimerId);
    SessionCountDownTimerId = setInterval("SessiontimerCountDown()", 1000);
}

function SessiontimerCountDown() {
    var secs = SessiontimeInSecs;
    if (secs > 0) {
        SessiontimeInSecs--;
    } else {
        clearInterval(SessionCountDownTimerId);
        SessionExpired();
    }
    var mins = Math.floor(secs / 60);
    secs %= 60;
    var _timerValue = (mins < 10 ? "0" : "") + mins + ":" + (secs < 10 ? "0" : "") + secs;
    document.getElementById("sessioncountdown").innerHTML = "<br /> You will be logged out in <b><h1 style='margin-bottom:0px;color:red;'>  " + _timerValue + "</h1> </b> seconds";
}

function SessionExpired() {

    var strLoggedInUser = document.getElementById("hdnLoggedInUser").value;
    var sessionTimeout = document.getElementById("hdnSessionTimeout").value;
    var urlString = "SampleContext.aspx/SessionExpired";
    $.ajax({
        type: "POST",
        url: urlString,
        data: "{ LoggedInUser: '" + strLoggedInUser + "' ,sessionTimeout: '" + sessionTimeout + "' }",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        success: function (response) {
            window.location.href = "Session";
        },
        error: OnErrorCall
    });
    return false;
}

/*
function ConfirmOnDiscard() {
    var n = parent.noty({
        text: 'The sample context and any data collected so far will be discarded. Do you still want to proceed?',
        type: 'confirm',
        layout: 'center',
        modal: true,
        maxVisible: 1,
        closeWith: ['button'],
        theme: 'relax',
        template: '<div class="noty_message" style="font-size:13px; cursor:default;"><div class="ConfMsgcss"></div><span class="noty_text"></span></div>',
        animation: {
            open: 'animated bounceInLeft',
            easing: 'swing',
            speed: 500 // opening & closing animation speed
        },
        buttons: [
            {
                addClass: 'btn btn-success btn-success-width', text: 'Yes', onClick: function ($noty) {
                    var IsShowDiscardReasonPopup = document.getElementById("hdnShowDiscardReasonPopup").value;
                    if (IsShowDiscardReasonPopup) {
                        OpenDiscardPopup();
                    }
                    else {
                        OnDiscard();
                    }                   
                    $noty.close();
                    return false;
                }
            },
            {
                addClass: 'btn btn-danger btn-danger-width', text: 'No', onClick: function ($noty) {
                    ResetSessionTimeout();
                    $noty.close();
                    return false;
                }
            }
        ]
    })
    return false;
}
*/

function OpenDiscardPopup() {
    ResetSessionTimeout();

    var IsShowDiscardReasonPopup = document.getElementById("hdnShowDiscardReasonPopup").value;
    if (IsShowDiscardReasonPopup.toLowerCase() == 'true') {
        document.getElementById('iFramePopup').style.height = '350px';
    }
    else {
        document.getElementById('iFramePopup').style.height = '110px';
    }

    var PageURL = 'Discard.aspx?deviceRawDataOID=' + deviceRawDataOID + '&Date=new Date().toUTCString();'
    OpenPopup(PageURL, 'Medium');
    return false;
}

function OpenPopup(PageURL, PopupSize) {
    document.getElementById('iFramePopup').src = PageURL;
    $('#modalPopup').modal({
        backdrop: 'static',
        keyboard: false
    });

    if (PopupSize == 'Large') {
        $("#modalPopup").removeClass("modal fade bd-example-modal-md");
        $("#modalPopupInner").removeClass("modal-dialog modal-md modal-dialog-centered");
        $("#modalPopup").addClass("modal fade bd-example-modal-lg");
        $("#modalPopupInner").addClass("modal-dialog modal-lg modal-dialog-centered");
    }
    else {
        $("#modalPopup").removeClass("modal fade bd-example-modal-lg");
        $("#modalPopupInner").removeClass("modal-dialog modal-lg modal-dialog-centered");
        $("#modalPopup").addClass("modal fade bd-example-modal-md");
        $("#modalPopupInner").addClass("modal-dialog modal-md modal-dialog-centered");
    }
    $('#modalPopup').modal('handleUpdate')
    $('#modalPopup').modal('show');
    return false;
}


function ClosePopup() {
    $('#modalPopup').modal('hide');
    return false;
}

function ShowErrorAlertMessage(errormessage) {
    generate('error', errormessage, 'alert', 'CloseErrorAlertMessage');
    return false;
} function CloseErrorAlertMessage() {
    ResetSessionTimeout();
}

function DiscardPopupReturn(result) {
    if (result == 0) {
        //To close the discard popup
        ClosePopup();

        //On discard click, after returning from DiscardPopup, we need to keep all the selections intact,
        //but the Raw Data Panel should get refreshed
        OnEquipmentDropdownChange();
    }
}

$(document).scroll(function (e) {
    var scrollAmount = $(window).scrollTop();
    var documentHeight = $(document).height();
    var windowHeight = $(window).height();
    var scrollPercent = (scrollAmount / (documentHeight - windowHeight)) * 100;
    var roundScroll = Math.round(scrollPercent);
    if (isNaN(roundScroll)) {
        roundScroll = 100;
        scrollPercent = 100;
    }
    // For scrollbar 1
    $(".scrollBar1").css("width", scrollPercent + "%");
    //$(".scrollBar1 span").text(roundScroll);


});

function ViewByEquipment() {
    ResetSessionTimeout();

    document.getElementById('iFramePopup').style.height = '350px';
    var PageURL = 'ViewByEquipment.aspx?&Date=new Date().toUTCString();'
    OpenPopup(PageURL, 'Large');
    return false;
}

function getJSONData(data) {

    //  vJsonControlData = data.Config.Control;
    //  childTargetControlID = vJsonControlData[0]["ControlName"];
    //  LoadDefaultControl(vJsonControlData[0]);

    vJsonControlData = JSON.parse(data);
    childTargetControlID = vJsonControlData[0]["ControlName"];
    LoadDefaultControl(vJsonControlData[0]);

    return false;
}
