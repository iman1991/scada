﻿// Filter events by view
var eventsByView = true;

// Set current view date and process the consequent changes
function changeViewDate(date, notify) {
    setViewDate(date);
    //updateHourDataColHdrText();
    //restartUpdatingHourData();

    if (notify) {
        sendViewDateNotification(date);
    }
}

// Enable or disable events by view filter
function setEventsByVeiw(val) {
    eventsByView = val;
    saveEventFilter();

    if (val) {
        $("#spanAllEventsBtn").removeClass("selected");
        $("#spanEventsByViewBtn").addClass("selected");
    } else {
        $("#spanAllEventsBtn").addClass("selected");
        $("#spanEventsByViewBtn").removeClass("selected");
    }
}

// Load the event filter from the cookies
function loadEventFilter() {
    var val = scada.utils.getCookie("Table.EventsByView");
    setEventsByVeiw(val != "false");
}

// Save the event filter in the cookies
function saveEventFilter() {
    scada.utils.setCookie("Table.EventsByView", eventsByView);
}

$(document).ready(function () {
    scada.clientAPI.rootPath = "../../";
    styleIOS();
    updateLayout();
    initViewDate();
    loadEventFilter();
    notifier = new scada.Notifier("#divNotif");
    notifier.startClearingNotifications();

    if (DEBUG_MODE) {
        initDebugTools();
    }

    $(window).on("resize " + scada.EventTypes.UPDATE_LAYOUT, function () {
        updateLayout();
    });

    // process the view date changing
    $(window).on(scada.EventTypes.VIEW_DATE_CHANGED, function (event, sender, extraParams) {
        changeViewDate(extraParams, false);
    });

    // select view date on click the calendar icon
    $("#spanDate i").click(function (event) {
        selectViewDate(changeViewDate);
    });

    // parse manually entered view date
    $("#txtDate").change(function () {
        parseViewDate($(this).val(), changeViewDate);
    });

    // switch event filter
    $("#spanAllEventsBtn").click(function () {
        if (!$(this).hasClass("disabled")) {
            setEventsByVeiw(false);
        }
    });

    $("#spanEventsByViewBtn").click(function () {
        setEventsByVeiw(true);
    });

    // export events on the button click
    $("#spanExportBtn").click(function () {
        alert("Export is not implemented yet.");
    });
});