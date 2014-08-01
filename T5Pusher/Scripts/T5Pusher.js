//start init file/////////////////////////////////
//this file includes global variables and functions which are used throughout the scripts
//var T5Pusher_URL = "http://t5pusherl-elasticl-ofhjll7tm6gy-1917470084.eu-west-1.elb.amazonaws.com"; //t5pusher web socket load balanced medium tcp ( only one instance due to waiting for signalR scale out updates)
//var T5Pusher_URL = "https://t5pusher.t5livegames.com/"; //t5pusher web socket load balanced medium tcp ( only one instance due to waiting for signalR scale out updates)
//var T5Pusher_URL = "http://ec2-54-228-78-214.eu-west-1.compute.amazonaws.com"; //single instance medium test
//var T5Pusher_URL = "http://t5pusherl-elasticl-1mbwaj1whlro5-816142068.eu-west-1.elb.amazonaws.com"; //t5pusher aws load balanced - medium - no longer used as there are scale out issues untill 1.1
//var T5Pusher_URL = "http://localhost:62261"; ////local
//var T5Pusher_URL = "http://ec2-54-228-111-127.eu-west-1.compute.amazonaws.com"; //new AWS temporary test pusher
//var T5Pusher_URL = "http://t5pusherazure2.azurewebsites.net"; ////Azure website
//var T5Pusher_URL = "http://7bf57375c5d545708631c520e8166cd4.cloudapp.net"; ////Azure CLOUD SERVICE
//var T5Pusher_URL = "http://127.0.0.1";
//var T5Pusher_URL = "http://84d937b7df98433c99c60a4c135ecca6.cloudapp.net";
//var T5Pusher_URL = "http://localhost:62261"; //local redis pusher
var T5Pusher_URL = ""; //temp redis pusher
////end init file/////////////////////////////////

//start error file/////////////////////////////////
window.onerror = function (msg, url, lineNo) {
    logT5PusherError("onerror", msg, url, lineNo, '', '');
}

//Error object used for sending error details to DB
function T5Error(errordesc) {
    this.errordesc = errordesc;
    this.origin = "JS";
}

//this is our T5 custom error handler - we should log errors in DB (via AJAX call)
function logT5PusherError(origin, msg, url, lineNo, ThirdPartyUserkey, currentConnectionID) {
    try {

        var errordetails = "";
        try {
            if (msg.message) {
                errordetails = msg.message;
            }
            else {
                errordetails = msg;
            }

        } catch (ex) { errordetails = msg; }

        //need to JSONP this API!!!!!!
        $.ajax({
            url: T5Pusher_URL + "/Error/logError",
            data: { clientdetails: currentConnectionID, localtime: T5Pusher.GetCurrentTimeStamp(), clientdetails: currentConnectionID, userdetails: ThirdPartyUserkey, origin: origin, details: errordetails },
            dataType: "jsonp",
            jsonpCallback: 'JSONPCallback',
            //data: JSON.stringify(thisError),
            contentType: "application/json: charset=utf-8",
            success: function (response) {
            }
        });
    }
    catch (ex) { }
}
///end error file////////////////////////////////////////

//start T5Pusher////////////////////////////////////////
var PushQueueItem = function (processName_in, messageList_in, groupName_in) {

    var processName = processName_in, messageList = messageList_in, groupName = groupName_in;

    function GetProcessName() {
        return processName;
    }

    function GetMessageList() {
        return messageList;
    }

    function GetGroupName() {
        return groupName;
    }

    return {
        GetGroupName: GetGroupName,
        GetMessageList: GetMessageList,
        GetProcessName: GetProcessName
    };
};


//using the revealing module pattern
var T5Pusher = function () {
    //private variables - which the 3rd party cannot see
    var SignalRConnection;

    var listOfGroupsJoined = new Array(); //this will hold the list of the groups we have attempted to join and we will use this in cases where we've lost connection and wish to reinitialise our connection
    var listOfGroupsJoined_times = new Array(); //this will hold the list of the groups we have attempted to join and we will use this in cases where we've lost connection and wish to reinitialise our connection
    var listOfGroupsJoined_secure = new Array();
    var listOfGroupsJoined_return = new Array();

    var numReconnectsSinceWeLastReceivedASignalRMessage = 0;
    var lastInteractionTime;
    var connectionRetryTimeout;
    var connectionRetryInterval = 10000; //if , after attempting to connect for this interval we still haven't re-established connection to T5Pusher = then take action!
    var ConnectionCheckInProgress = 0;
    var StartingInProgress = 0;
    var SignalRWaitInterval = 10000; //10 second wait whenever we try to connect to SignalR!!!!
    var srstarted = 0;
    var timeBetweenSignalRConnections = 1400000; //14000000; //should be 14 in live
    var SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce = 0;
    var validated = 0;
    var ThirdPartyUserkey;
    var ThirdPartyPassword;
    var userHasCalledStartAgain = 0;

    var internalconnection = function () {
        var connectionstatus;

        return {
            connectionstatus: connectionstatus
        };
    }
    var pushQueue = new Array();

    var currentConnectionID;
    var LastConnectionState = -1; //this is to keep track of the signalr connection states - they will often call the state changed function but the state will be the same as the last time???? - is this because i kill the previous connection and start a new one????

    ////////////BINDING VARIABLES////////////////////////////////////////////////////
    var listOf3rdPartyEventNames = new Array();
    var listOf3rdPartyEvents = new Array();
    var thirdPartyConnectionstartMethod = null;
    var thirdPartyUnAuthorisedMethod = null;
    var thirdPartyConnectionfailedMethod = null;
    var thirdPartyConnectionSlowMethod = null;
    var thirdPartyConnectionstateChangedMethod = null;
    var thirdPartyConnectionlostMethod = null;
    ////////////END BINDING VARIABLES////////////////////////////////////////////////////

    var secureConnection = false;
    var clientvalidationurl = "AuthHandler.ashx"; //"/t5pusher/validate"; //client needs to be able to change this
    var jsonpclientvalidation = false;

    //the below is crude examination of the useragent - we should call Eannas model
    //api to find out if the browser is indeed a samsung
    var isSafari = 0;
    var isChrome = 0;
    var isAndroid = 0;
    var isSamsung = 0;
    function ExamineUserAgent() {
        try {

            var ua = navigator.userAgent.toLowerCase();

            if (ua.indexOf('android') > -1) {
                isAndroid = 1;

                if (ua.indexOf('gt-') > -1) {

                    isSamsung = 1;
                }
            }

            if (ua.indexOf('safari') != -1) {
                if ((ua.indexOf('chrome') > -1) || (ua.indexOf('crios') > -1)) { //crios is the name for Chrome on iOS. 
                    isSafari = 0;
                    isChrome = 1;
                } else {
                    isSafari = 1;
                    isChrome = 0;
                }
            }
        } catch (e) { logT5PusherError("DetermineIfSafari", ex, null, 0, ThirdPartyUserkey, currentConnectionID); }
    }


    //private functions - which the 3rd party cannot see
    function LogThisInteractionTime() { //This function logs each time we interact with the T5Pusher server
        //we have received a message from SignalR - so reset numREconnects value
        numReconnectsSinceWeLastReceivedASignalRMessage = 0;
        var DateHelper = new Date();
        lastInteractionTime = DateHelper.getTime();
    } //end LogThisInteractionTime

    function GetCorrectGroupName(groupName, T5internalgroup) {
        var correctGroupName;

        if (groupName) {
            correctGroupName = "3PG:" + ThirdPartyUserkey + ":" + groupName;
        }
        else {
            //there is no groupName - so return groupName in the format "3P:Userkey"
            correctGroupName = "3P:" + ThirdPartyUserkey;
        }
        return correctGroupName;
    } //end GetCorrectGroupName

    function GetOriginalGroupName(groupName) {
        var OriginalGroupName;

        try {
            var preFix = "3PG:" + ThirdPartyUserkey;
            if (groupName.indexOf(preFix) == 0) {
                OriginalGroupName = groupName.substring(preFix.length + 1, groupName.length);
            }
            else {
                OriginalGroupName = groupName;
            }
        } catch (ex) { OriginalGroupName = groupName }

        return OriginalGroupName;
    } //end GetOriginalGroupName

    function GetCurrentConnectionMethod() {
        try {
            return $.connection.hub.transport.name;
        } catch (ex) { logT5PusherError("GetCurrentConnectionMethod", ex, null, 0, ThirdPartyUserkey, currentConnectionID); }
    }

    //this function makes sure that the connection is still Connected at all times!!!!!!!
    function ManageConnection() {
        try {
            var restartTriggered = 0;

            if ((lastInteractionTime == null) || (SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0)) {
                //this is the first time this function has been called and we have still not connected to or interacted with the T5Pusher server!!!!
                //so.....attempt to connect to the current LiveEvent server!!!!

                //we now check SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0 here - 
                //the reason for this is that untill we have validated the user we dont officially class SignalR as having started
                //and so we dont call the users onconnectionstarted function - this means we CAN receive messages from signalR 
                //- so lastInteractionTime will NOT be null BUT we wont have called onconnectionstarted

                //this does not necessarily means connection failed - keepAlive could be down
                //BUT we HAVE to gaurantee that keepAlive will always work or else 
                //if (thirdPartyConnectionfailedMethod) {
                //    thirdPartyConnectionfailedMethod.call();
                //}

                //LogPushEvent("NotReceivedAnyKeepAliveYet!!!!!");
                //logT5PusherError("ManageConnection", "ManageConnection - NotReceivedAnyKeepAliveYet  about to call RestartSignalRConnection!", null, 0, ThirdPartyUserkey, currentConnectionID);
                //RestartSignalRConnection("ManageConnection_NotReceivedAnyKeepAliveYet");
                //restartTriggered = 1;
            }
            else {
                var DateHelper = new Date();
                var currentTime = DateHelper.getTime();
                var TimeSinceLastInteraction = parseInt((currentTime - lastInteractionTime) / 1000);

                if (
                    (TimeSinceLastInteraction > timeBetweenSignalRConnections)
                   ) {
                    //we have exceeded the acceptable length of time without a message from the T5Pusher server!!!!
                    //so...attempt reconnection to  signalR!!!!                 //- which of course we cannot do for Pusher!!!

                    LogPushEvent("ManageConnection_TimeTooGreat!!!!!");
                    logT5PusherError("ManageConnection", "ManageConnection - about to call RestartSignalRConnection! - TimeSinceLastInteraction is " + TimeSinceLastInteraction + " and timeBetweenConnections is " + timeBetweenSignalRConnections, null, 0, ThirdPartyUserkey, currentConnectionID);
                    RestartSignalRConnection("ManageConnection_TimeTooGreat");
                    restartTriggered = 1;
                }
                else {

                    if ((numGroupsDB != -101) && (numGroupsDB != -102)) {
                        //numGroupsInDB has been returned from DB correctly
                        if
                           ((typeof (listOfGroupsJoined) != 'undefined' && listOfGroupsJoined != null) && (listOfGroupsJoined.length > 0)) {
                            //we HAVE joined groups
                            if (numGroupsDB < listOfGroupsJoined.length) {
                                //the DB is telling us that this connectionid is NOT in the same amount of groups that the client thinks!!!!

                                //so - reinitialize our group details!!!
                                logT5PusherError("ManageConnection", "T5Pusher ManageConnection - we have joined groups BUT we DONT have any groups set up with signalr (well accoring to DB we dont anyway!!!) - so REjoining groups now!!!", null, 0, ThirdPartyUserkey, currentConnectionID);
                                LogPushEvent("ManageConnection_GroupsNotAllThere!!!!!");
                                var rejoined = RejoinGroups(); //now attempt to rejoin groups

                                if (rejoined <= 0) {
                                    //we were unsuccessfull in our attempt to rejoin groups - so restart total connection!!!!
                                    logT5PusherError("ManageConnection", "CheckConnection - unable to rejoin groups - so restarting entire connection", null, 0, ThirdPartyUserkey, currentConnectionID);
                                    RestartSignalRConnection("ManageConnection_NoGroups");
                                    restartTriggered = 1;
                                }
                            }
                        }
                    }
                }
                //stephen 27-Feb-13
                //temporarily commented out the grou checking part of this as there have been changes in SignalR 1.0 and we can no longer access $.connection.hub.groups
                //awaiting documentation to see how this works!!!!!!!! - something to do with $.connection.hub.groupsToken
                /*
                else {
                    //start group else
                    //we are receiving all the keepALive messages - now check if we are are currently listening out for our groups!!!

                    if (
                           ((typeof (listOfGroupsJoined) != 'undefined' && listOfGroupsJoined != null) && (listOfGroupsJoined.length > 0)) //we HAVE joined groups
                           &&
                            (($.connection.hub.groups == null) || ($.connection.hub.groups.length == 0) || ($.connection.hub.groups.length < listOfGroupsJoined.length)) //BUT we DONT have any groups set up with signalr!!!!!!
                       ) {
                        //we have joined groups BUT we DONT have any groups set up with signalr!!!!!!
                        //so - reinitialize our group details!!!
                        logT5PusherError("SR_Con", "T5Pusher ManageConnection - we have joined groups BUT we DONT have any groups set up with signalr - so REjoining groups now!!!", "", "", GetCurrentTimeStamp());
                        LogPushEvent("ManageConnection_GroupsNotAllThere!!!!!");
                        var rejoined = ReJoinGroups(); //now attempt to rejoin groups

                        if (rejoined <= 0) {
                            //we were unsuccessfull in our attempt to rejoin groups - so restart total connection!!!!
                            logT5PusherError("T5Pusher", "CheckConnection - unable to rejoin groups - so restarting entire connection", "", "", GetCurrentTimeStamp());
                            RestartSignalRConnection("ManageConnection_NoGroups");
                            restartTriggered = 1;
                        }
                    }
                }//end group else
                */
            }
        }
        catch (ex) {
            logT5PusherError("CheckConnection", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
        }

        if (restartTriggered == 0) {
            //we have NOT triggered a restart - so continue checking the connection!!!!!!
            LogPushEvent("setting manageConnection in " + ((timeBetweenSignalRConnections / 2) * 1000) + " seconds");

            //before we go to sleep - go to DB to check if we still have the correct amount of groups
            //this is an ajax call - so the thinking is - we will have the answer next time we call this function - we can then compare the numbers to make sure we have the correct number of groups

            if (numGroupsDB == -101) {
                //we do NOT know the number of connections in the DB for this connection
                //this is due to the connection just being started/reset or due to the fact we have just joined another group
                GetNumberOfGroupsThisConnectionIsIn($.connection.hub.id);
            }
            window.setTimeout(function () {
                ManageConnection();
            }, ((timeBetweenSignalRConnections / 2) * 1000));
        } //end restartTriggered
    } //end ManageConnection

    function GetCurrentTimeStamp() {
        var EventTimeStamp = new Date();
        return EventTimeStamp.getTime();
    }//end GetCurrentTimeStamp

    //this function restarts the signalR Connection!!!!
    function RestartSignalRConnection(Origin) {

        try {

            LogPushEvent("in RestartSignalRConnection - restarting connection Origin is " + Origin + "!!!!!");
            //logT5PusherError("RestartSignalRConnection", "In RestartSignalRConnection Origin is " + Origin,null,0, ThirdPartyUserkey, currentConnectionID));
            StartingInProgress = 0; //reset this value

            //set this so we can start the CheckConnection flow again after the signalR restart!
            //we dont want the connection flow running when we are restarting the connetion - we know the connection is down!!!!
            //thats why we are here!!!! - so there's no need for the connection flow to continue!!!!!
            ConnectionCheckInProgress = 0;
            numReconnectsSinceWeLastReceivedASignalRMessage = numReconnectsSinceWeLastReceivedASignalRMessage + 1;

            if (numReconnectsSinceWeLastReceivedASignalRMessage == 2) {
                //tell client we are having connection issues
                if (thirdPartyConnectionSlowMethod) {
                    thirdPartyConnectionSlowMethod.call();
                }
            }
            if (numReconnectsSinceWeLastReceivedASignalRMessage == 4) {
                //tell client we are still having more connection issues - this is now more serious - looks like connection is lost!!

                //we have tried on 3 previous occasions to re-establish connection to T5Pusher and we have notbeen able to receive
                //any message - so....tell 3rd party so they can refresh page or notify user
                if (thirdPartyConnectionlostMethod) {
                    thirdPartyConnectionlostMethod.call();
                }
            }

            //start signalR as normal!!!!
            $.connection.hub.stop();
            SignalRConnection = null;
            srstarted = 0;
            internalconnection.connectionstatus = "reconnecting";
            numGroupsDB = -101; //reset this so we will check in DB that gropus have been set up correctly oncve we re-establish the connection
            Connect();

        }
        catch (ex) {

            internalconnection.connectionstatus = "reconnection attempt failed - " + error;
            if (thirdPartyConnectionfailedMethod) {
                thirdPartyConnectionfailedMethod.call();
            }
            logT5PusherError("RestartSignalRConnection", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
        }

    } //end RestartSignalRConnection

    //this is not currently used - waiting fot signalr fix for this issue
    function stopBrowserLoading() {
        alert("isbl");
        var $fakeFrame = $('<iframe style="height:0;width:0;display:none" src=""></iframe>');
        $('body').append($fakeFrame);
        $fakeFrame.remove();
    }

    //this function is going to connect the 3rd Party to SignalR!!!!
    function Connect() {
        try {

            //if this has been called before then maybe we should call reconnet
            //i.e - the 3rd party may call thid over and over ( they shouldn't BUT - they might!!!)
            //so if this happens we should at least kill any previous variables,connections etc!!!!!!
            var temp = ThirdPartyUserkey;

            $.connection.hub.logging = true;

            $.connection.hub.qs = { "ur": ThirdPartyUserkey };

            //this line means we look to an external domain for SignalR!!!!!
            $.connection.hub.url = T5Pusher_URL + '/signalr' //this points to the T5Pusher Hub

            //connection = $.connection.T5Pusher;
            SignalRConnection = $.connection.t5pusher;

            //receiveThirdPartyMessage
            SignalRConnection.client.rtpm = function (processName, messageList) {
                LogThisInteractionTime();
                var locationofMethod = listOf3rdPartyEventNames.indexOf(processName);
                if (locationofMethod > -1) {
                    listOf3rdPartyEvents[locationofMethod].call(undefined, messageList); //undefined = valueForThis
                }
            };

            SignalRConnection.client.invalidsendattempt = function (message) {
                alert(message);
            };

            //no longer use this - result now gets returned to the calling function
            ////validation update
            //SignalRConnection.client.vu = function (result) {
            //    if (result == 1) {
            //        //this connection WAS validated!!!!
            //        //so ...now join to the correct groups!!!!!
            //        CompleteConnection();
            //    }
            //    else {
            //        //validation failed!!! - set a property that tells 3rd party this current connection status
            //        internalconnection.connectionstatus = "unauthorized - failed authorization";
            //        if (thirdPartyUnAuthorisedMethod) {
            //            thirdPartyUnAuthorisedMethod.call();
            //        }
            //    }
            //};

            //initialiseProxyFunctions();

            SignalRConnection.client.keepalive = function () {
                LogThisInteractionTime();
            };

            $.connection.hub.connectionSlow(function () {
                //we go in here if signalR notices itself that it is having some connection issues due to slow/buggy networks
                //if this event is triggered 'signalR will try to recover. When this succeeds, nothing happens. 
                //If this fails, the stateChanged event will fire with "reconnecting" as it's new state.'

                if (thirdPartyConnectionSlowMethod) {
                    thirdPartyConnectionSlowMethod.call();
                }
            });

            $.connection.hub.stateChanged(function (change) {

                if (LastConnectionState != change.newState) //dont preceed any further with this function if the state has NOT ACTUALLY changed
                {
                    LastConnectionState = change.newState;

                    /* 
                    //no longer listen out for this state
                    //testing has shown that after calling RestartSignalRConnection we will connect and then at the end it will go into 
                    //this state - i.e reconnecting - but then it will stay ther - ie - it will go reconneting but then never connecting AND it doesn't seem
                    //to be reconnecting - it seems to just be connected fine - so we end up calling whatever logic we have for reconnecting when we are not 
                    //therefore we've removed this part of flow 
                    if (change.newState === $.signalR.connectionState.reconnecting)
                    {
                        //tell 3rd party we are reconnecting so they can display a message to the user???
                        //or do we just try and manage the connection ourselves???

                        thisConnectionIsAReconnect = 1;
                        //connectionRetryTimeout = setTimeout(function () {
                        //    //if we reach here then we have spent 10 seconds trying to re- establish the connection and have not been able to - so prompt a reload of page here!!!!! //if we reach here then we have spent 10 seconds trying to re- establish the connection and have not been able to - so prompt a reload of page here!!!!!
                        //    RestartSignalRConnection("ReconnectingTimeout");
                        //}, connectionRetryInterval);

                        internalconnection.connectionstatus = "reconnecting";
                    } else
                    */

                    if (change.newState === $.signalR.connectionState.connected) {

                        //setTimeout(T5Pusher.stopBrowserLoading(), 1500);
                        //var $fakeFrame = $('<iframe style="height:0;width:0;display:none" src=""></iframe>');
                        //$('body').append($fakeFrame);
                        //$fakeFrame.remove();

                        internalconnection.connectionstatus = "connected";
                        if (connectionRetryTimeout) //if we  were previously trying to reconnect - then clear the values set in this process
                        {
                            clearTimeout(connectionRetryTimeout);
                            connectionRetryTimeout = null;
                        }

                        if (currentConnectionID != $.connection.hub.id) {
                            currentConnectionID = $.connection.hub.id;
                            numGroupsDB = -101; //our connectionid has changed - so reset this so we will check we have the correct groups in DB linked to this connectionid that we think we have!!
                        }

                        if (thirdPartyConnectionstateChangedMethod) {
                            thirdPartyConnectionstateChangedMethod.call(undefined, internalconnection.connectionstatus);
                        }

                    }
                    else if (change.newState === $.signalR.connectionState.disconnected) {
                        internalconnection.connectionstatus = "disconnected";

                        if (thirdPartyConnectionstateChangedMethod) {
                            thirdPartyConnectionstateChangedMethod.call(undefined, internalconnection.connectionstatus);
                        }
                    }
                    else if (change.newState === $.signalR.connectionState.connecting) {

                        if (SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 1) {
                            //this is NOT the first time we have connected to T5Pusher
                            internalconnection.connectionstatus = "reconnecting";
                        }
                        else {
                            internalconnection.connectionstatus = "connecting";
                        }

                        if (thirdPartyConnectionstateChangedMethod) {
                            thirdPartyConnectionstateChangedMethod.call(undefined, internalconnection.connectionstatus);
                        }

                    }

                }
            });

            if (StartingInProgress == 0) {
                StartingInProgress = 1;

                var transports = ['webSockets', 'longPolling'];//['webSockets', 'longPolling'];
                try {
                    if ((isChrome == 0) && (isAndroid == 1) && (isSamsung == 1)) {
                        //this device is NOT chrome
                        //DOES have android in the useragent
                        //and DOES have "gt-" in the useragent - 
                        //so the odds are that this is a non chrome browser samsung
                        //so due to issues which will hopefully be resolved in future versions of signalr
                        //we need to only specify longpolling!!
                        transports = ['longPolling'];
                        logError("SamsungSwitch", "ua is " + navigator.userAgent.toLowerCase());
                    }
                } catch (ex) {
                    logError("UAIdentifyError", ex);
                }

                $.connection.hub.start({ transport: transports }).done(function () {


                    if (srstarted == 0) {
                        srstarted = 1;

                        //this pushes out all the messages we were unable to send due to signalr not having started
                        processPushQueue();
                        /*
                        if (ConnectionCheckInProgress == 0) {
                            //start connection check
                            var timeToWait = timeBetweenSignalRConnections * 1000;
                            var logSentence = "setting manageConnection in " + timeToWait + " seconds";
                            LogPushEvent(logSentence); //for some reason this line throws an error on internet Explorer?????????
                            window.setTimeout(
                                    function () {
                                        ManageConnection();
                                    }
                                    , ((timeBetweenSignalRConnections ) * 1000) //wait the full length of time for the first check!!!
                            );
                            ConnectionCheckInProgress = 1;
                        }
                        */
                    }

                    //a new connection has been started - WITH a NEW ConnectionID!!!!!!!
                    //this means we need to validate the connection!!!!!
                    ValidateConnection($.connection.hub.id);

                })
                 .fail(function (error) {
                     if (SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0) {
                         internalconnection.connectionstatus = "connection attempt failed - " + error;
                     }
                     else {
                         internalconnection.connectionstatus = "reconnection attempt failed - " + error;
                     }

                     if (thirdPartyConnectionfailedMethod) {
                         thirdPartyConnectionfailedMethod.call();
                     }
                     //instead of calling a fail here should we instead just try the connect function again??? - connect();

                 });
            }

            //now start the connection check right after we start -  $.connection.hub.start
            //the reason for this is - on rare occasions we are seeing situations where signalR just does not start
            //so if we call this function - after z seconds - we will see we haven;'t started yet and will prompt page reload
            //if we haven;t managed to connect to signalr after the timeToWait - then something is wrong!!!!
            if (ConnectionCheckInProgress == 0) {
                //start connection check
                var timeToWait = timeBetweenSignalRConnections * 1000;
                var logSentence = "setting manageConnection in " + timeToWait + " seconds";
                LogPushEvent(logSentence); //for some reason this line throws an error on internet Explorer?????????
                window.setTimeout(
                        function () {
                            ManageConnection();
                        }
                        , ((timeBetweenSignalRConnections) * 1000) //wait the full length of time for the first check!!!
                );
                ConnectionCheckInProgress = 1;
            }



        }
        catch (ex) {
            logT5PusherError("Connect", ex, null, 0, ThirdPartyUserkey, currentConnectionID);

            if (srstarted == 0) {

                //there was an error - we didn't manage to start connection
                if (SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0) {
                    internalconnection.connectionstatus = "connection attempt failed - " + error;
                }
                else {
                    internalconnection.connectionstatus = "reconnection attempt failed - " + error;
                }

                if (thirdPartyConnectionfailedMethod) {
                    thirdPartyConnectionfailedMethod.call();
                }

            }

            return false;
        }
    } //end Connect

    function processPushQueue() {
        for (var i = 0; i < pushQueue.length; i++) {
            try {
                PushMessage(pushQueue[i].GetProcessName(), pushQueue[i].GetMessageList(), pushQueue[i].GetGroupName());
            } catch (ex) { }
        }
        pushQueue = new Array();
    }

    function LogPushEvent(message) {
        var alertFallback = false;
        if (typeof console === "undefined" || typeof LogPushEvent === "undefined") {
            if (alertFallback) {
                alert(message);
            } else {
                //do nothing
            }
        }
        else {
            console.log(message);
        }
    }

    var numGroupsDB = -101;
    function GetNumberOfGroupsThisConnectionIsIn(connectionid) {
        $.ajax({
            url: T5Pusher_URL + "/Connection/CheckGroups",
            type: "GET",
            data: "connectionid=" + connectionid,
            dataType: "jsonp",
            jsonpCallback: 'JSONPCallback',
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                numGroupsDB = -102; //error
            },
            success: function (response) {
                numGroupsDB = parseInt(response, 10);
            }
        });
    }


    //this function runs after we have established a connection to T5Pusher
    //once we have established the conection we can then join groups!
    function CompleteConnection() {

        try {

            if ((SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0) || (userHasCalledStartAgain == 1)) {
                //this is the first time we have connected to T5Pusher
                userHasCalledStartAgain = 0;

                //always join this group!!!!!!!

                T5Pusher.bind("InternalDefaultGroupJoin_T5I", function (data) {
                    if (data) {
                        if (data.joined == 1) {
                            //we have joined default group - so connection is NOW started

                            if (SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0) {
                                if (thirdPartyConnectionstartMethod) {
                                    thirdPartyConnectionstartMethod.call();
                                }
                            }
                            SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce = 1;
                            internalconnection.connectionstatus = "connected";
                        }
                        else {
                            //we failed to join the default group - restart connection 
                            //if we could not join the default group then we have NOt established a connection 
                            if (SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0) {
                                internalconnection.connectionstatus = "connection attempt failed - unable to join the default group ";
                            }
                            else {
                                internalconnection.connectionstatus = "connection attempt failed - unable to join the default group ";
                            }

                            if (thirdPartyConnectionfailedMethod) {
                                thirdPartyConnectionfailedMethod.call();
                            }
                            return;
                        }
                    }
                });

                JoinGroupPrivate("3P:" + ThirdPartyUserkey, "InternalDefaultGroupJoin_T5I", 1);
            }
            else {
                //we have connected previously - so now check if we have previoulsy joined groups on the previous connection
                RejoinGroups();
            }
        }
        catch (ex) {
            logT5PusherError("CompleteConnection", ex, null, 0, ThirdPartyUserkey, currentConnectionID);

            if (SignalRConnectionHasBeenPreviouslyStartedAtLeastOnce == 0) {
                internalconnection.connectionstatus = "connection attempt failed - " + error;
            }
            else {
                internalconnection.connectionstatus = "reconnection attempt failed - " + error;
            }

            if (thirdPartyConnectionfailedMethod) {
                thirdPartyConnectionfailedMethod.call();
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////
    function DeleteSecureGroup(groupName, ReturnFunction) {
        try {

            if (jsonpclientvalidation == false) {
                //the clients validation url is NOT on an external URL 
                //this means - we dont need to do a JSON AJAX call
                $.ajax({
                    url: clientvalidationurl,
                    type: "POST",
                    data: { connectionid: $.connection.hub.id, groupName: groupName, delete: 1 },
                    dataType: "json",
                    error: function (XMLHttpRequest, textStatus, errorThrown) {

                        //add the error message to the status here!!!
                        if (ReturnFunction) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(ReturnFunction);
                            if (locationofMethod > -1) {
                                var returnObject = { groupName: GroupName, deleted: -1, status: "error calling DeleteSecureGroup authorization URL - " + clientvalidationurl };
                                listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                            }
                        }

                    },
                    success: function (response) {
                        try {
                            var responseObject;
                            if ((response) || (response != 'object')) {  //if repsonse is NOT an object - make it an object!!!!
                                responseObject = JSON.parse(response);
                            }
                            else {
                                responseObject = response
                            }
                            DeleteSecureGroupReturn(responseObject, groupName, ReturnFunction);
                        } catch (ex) { DeleteSecureGroupReturn(response, groupName, ReturnFunction); }
                    }
                });
            }
            else {
                //the clients validation url IS on an external URL 
                //this means - we DO need to do a JSONP AJAX call
                $.ajax({
                    url: clientvalidationurl,
                    type: "GET",
                    data: { connectionid: connectionid, groupName: groupName, delete: 1 },
                    dataType: "jsonp",
                    jsonpCallback: 'JSONPCallback'
                    , error: function (XMLHttpRequest, textStatus, errorThrown) {
                        //add the error message to the status here!!!
                        if (ReturnFunction) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(ReturnFunction);
                            if (locationofMethod > -1) {
                                var returnObject = { groupName: GroupName, deleted: -1, status: "error calling DeleteSecureGroup authorization URL - " + clientvalidationurl };
                                listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                            }
                        }
                    },
                    success: function (response) {
                        var responseObject = JSON.parse(response);
                        DeleteSecureGroupReturn(response, groupName, ReturnFunction);
                    }
                });
            }
        }
        catch (ex) {
            logT5PusherError("DeleteSecureGroup", ex, null, 0, ThirdPartyUserkey, currentConnectionID);

            //add the error message to the status here!!!
            if (ReturnFunction) {
                var locationofMethod = listOf3rdPartyEventNames.indexOf(ReturnFunction);
                if (locationofMethod > -1) {
                    var returnObject = { groupName: GroupName, deleted: -1, status: "error calling DeleteSecureGroup JSONP authorization URL - " + clientvalidationurl + ", error is " + ex.toString() };
                    listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                }
            }

            return false;
        }
    }//end CreateSecureGroup

    //we go here after a successfull call of the users secure channel hashing call
    function DeleteSecureGroupReturn(data, groupName, ReturnFunction) {
        //we know want to pass this data to OUR validation API!!!!!!
        try {
            if ((data) && (data.auth)) {
                //Create Secure Group on T5Pusher side
                SignalRConnection.server.dsg(data.auth, groupName).done(function (result) {
                    if (result == 1) {
                        //this secure group WAS deleted!!!
                        //so now what????? - probably need to return or call something here!!

                        if (ReturnFunction) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(ReturnFunction);
                            if (locationofMethod > -1) {
                                var returnObject = { groupName: groupName, deleted: 1, status: "deleted" };
                                listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                            }
                        }

                    }
                    else {
                        //validation failed!!! - set a property that tells 3rd party this current connection status

                        //add the error message to the status here!!!
                        if (ReturnFunction) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(ReturnFunction);
                            if (locationofMethod > -1) {

                                if (result == 0) {
                                    var returnObject = { groupName: GroupName, deleted: -1, status: "DeleteSecureGroup failed authorization" };
                                    listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                                }
                                else {
                                    var returnObject = { groupName: GroupName, deleted: -1, status: "DeleteSecureGroup failed on T5 server" };
                                    listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                                }
                            }
                        }

                    }
                });
            }
            else {
                //validation failed - update validation status variable which the 3rd party can view

                //add the error message to the status here!!!
                if (ReturnFunction) {
                    var locationofMethod = listOf3rdPartyEventNames.indexOf(ReturnFunction);
                    if (locationofMethod > -1) {
                        var returnObject = { groupName: GroupName, deleted: -1, status: "DeleteSecureGroup - client authorization return data not valid - data is " + dataAsJSON };
                        listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                    }
                }

            }
        }
        catch (ex) {
            logT5PusherError("DeleteSecureGroupReturn", ex, null, 0, ThirdPartyUserkey, currentConnectionID);

            if (ReturnFunction) {
                var locationofMethod = listOf3rdPartyEventNames.indexOf(ReturnFunction);
                if (locationofMethod > -1) {
                    var dataAsJSON = JSON.stringify(data);
                    listOf3rdPartyEvents[locationofMethod].call(undefined, "DeleteSecureGroup - error processing client authorization return data - data is " + dataAsJSON + ", error is " + ex.toString()); //undefined = valueForThis
                }
            }
            return false;
        }
    }//end SecureChannelReturn
    /////////////////////////////////////////////////////////////////////////


    function CreateSecureGroup(groupName, JoinGroupReturn, broadcast) {
        try {

            if (!broadcast) {
                broadcast = 0;
            }

            if (jsonpclientvalidation == false) {
                //the clients validation url is NOT on an external URL 
                //this means - we dont need to do a JSON AJAX call
                $.ajax({
                    url: clientvalidationurl,
                    type: "POST",
                    data: { connectionid: $.connection.hub.id, groupName: groupName, broadcast: broadcast }, //, sendToAll: sendToAll
                    dataType: "json",
                    error: function (XMLHttpRequest, textStatus, errorThrown) {

                        //add the error message to the status here!!!
                        if (JoinGroupReturn) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                            if (locationofMethod > -1) {
                                var returnObject = { groupName: GroupName, created: -1, status: "error calling CreateSecureGroup authorization URL - " + clientvalidationurl };
                                listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                            }
                        }

                    },
                    success: function (response) {
                        try {
                            var responseObject;
                            if ((response) || (response != 'object')) {  //if repsonse is NOT an object - make it an object!!!!
                                responseObject = JSON.parse(response);
                            }
                            else {
                                responseObject = response
                            }
                            CreateSecureGroupReturn(responseObject, groupName, broadcast, JoinGroupReturn);
                        } catch (ex) { CreateSecureGroupReturn(response, groupName, broadcast, JoinGroupReturn); }
                    }
                });
            }
            else {
                //the clients validation url IS on an external URL 
                //this means - we DO need to do a JSONP AJAX call
                $.ajax({
                    url: clientvalidationurl,
                    type: "GET",
                    data: { connectionid: connectionid, groupName: groupName, broadcast: broadcast }, //, sendToAll: sendToAll
                    dataType: "jsonp",
                    jsonpCallback: 'JSONPCallback'
                    , error: function (XMLHttpRequest, textStatus, errorThrown) {
                        if (Failure) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(Failure);
                            if (locationofMethod > -1) {
                                listOf3rdPartyEvents[locationofMethod].call(undefined, "unauthorized - error calling CreateSecureGroup JSONP authorization URL - " + errorThrown); //undefined = valueForThis
                            }
                        }
                    },
                    success: function (response) {
                        var responseObject = JSON.parse(response);
                        CreateSecureGroupReturn(responseObject, groupName, broadcast, JoinGroupReturn);
                    }
                });
            }
        }
        catch (ex) {
            logT5PusherError("CreateSecureGroup", ex, null, 0, ThirdPartyUserkey, currentConnectionID);

            if (JoinGroupReturn) {
                var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                if (locationofMethod > -1) {
                    var returnObject = { groupName: GroupName, created: -1, status: "error calling CreateSecureGroup JSONP authorization URL - " + clientvalidationurl + ", error is " + ex.toString() };
                    listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                }
            }

            return false;
        }
    }//end CreateSecureGroup

    //we go here after a successfull call of the users secure channel hashing call
    function CreateSecureGroupReturn(data, groupName, broadcast, JoinGroupReturn) {
        //we know want to pass this data to OUR validation API!!!!!!
        try {
            if ((data) && (data.auth)) {
                //Create Secure Group on T5Pusher side
                SignalRConnection.server.csg(data.auth, groupName, broadcast).done(function (result) {
                    if (result == 1) {
                        //this secure group WAS created!!!
                        //so now what????? - probably need to return or call something here!!

                        if (JoinGroupReturn) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                            if (locationofMethod > -1) {
                                var returnObject = { groupName: groupName, created: 1, status: "created" };
                                listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                            }
                        }
                    }
                    else {
                        if (JoinGroupReturn) {
                            var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                            if (locationofMethod > -1) {
                                var returnObject = { groupName: GroupName, created: -1, status: "failed authorization" };
                                listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                            }
                        }

                    }
                });
            }
            else {
                if (JoinGroupReturn) {
                    var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                    if (locationofMethod > -1) {
                        var dataAsJSON = JSON.stringify(data);
                        var returnObject = { groupName: GroupName, created: -1, status: "failed - client authorization return data not valid - data is " + dataAsJSON };
                        listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                    }
                }
            }
        }
        catch (ex) {
            logT5PusherError("CreateSecureGroupReturn", ex, null, 0, ThirdPartyUserkey, currentConnectionID);

            if (JoinGroupReturn) {
                var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                if (locationofMethod > -1) {
                    var dataAsJSON = JSON.stringify(data);
                    var returnObject = { groupName: GroupName, created: -1, status: "failed - error processing client authorization return data - data is " + dataAsJSON };
                    listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                }
            }

            return false;
        }
    }//end SecureChannelReturn

    function JoinSecureGroupAuthorisation(groupName, JoinGroupReturn) {
        try {

            if (jsonpclientvalidation == false) {
                //the clients validation url is NOT on an external URL 
                //this means - we dont need to do a JSON AJAX call
                $.ajax({
                    url: clientvalidationurl,
                    type: "POST",
                    data: { connectionid: $.connection.hub.id, groupName: groupName, broadcast: 0 }, //, sendToAll: sendToAll
                    dataType: "json",
                    error: function (XMLHttpRequest, textStatus, errorThrown) {
                        //output something here to the debug console to say joiing group failed due to error calling this authorisation url
                    },
                    success: function (response) {
                        try {
                            var responseObject;
                            if ((response) || (response != 'object')) {  //if repsonse is NOT an object - make it an object!!!!
                                responseObject = JSON.parse(response);
                            }
                            else {
                                responseObject = response
                            }
                            joinGroupFinalStep(groupName, responseObject, JoinGroupReturn);
                        } catch (ex) { joinGroupFinalStep(groupName, response, JoinGroupReturn); }
                    }
                });
            }
            else {
                //the clients validation url IS on an external URL 
                //this means - we DO need to do a JSONP AJAX call
                $.ajax({
                    url: clientvalidationurl,
                    type: "GET",
                    data: { connectionid: connectionid, groupName: groupName, broadcast: 0 }, //, sendToAll: sendToAll
                    dataType: "jsonp",
                    jsonpCallback: 'JSONPCallback'
                    , error: function (XMLHttpRequest, textStatus, errorThrown) {
                        //output something here to the debug console to say joiing group failed due to error calling this authorisation url
                    },
                    success: function (response) {
                        var responseObject = JSON.parse(response);
                        joinGroupFinalStep(groupName, responseObject, JoinGroupReturn);
                    }
                });
            }
        }
        catch (ex) {
            logT5PusherError("joinGroupFinalStep", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
            //output something here to the debug console to say joiing group failed due to error calling this authorisation url
            return false;
        }
    }//end JoinSecureGroupAuthorisation


    function ValidateConnection(connectionid) {
        try {
            if (secureConnection == true) { //only do this if the user has explicitly asked for authorisation

                if (jsonpclientvalidation == false) {
                    //the clients validation url is NOT on an external URL 
                    //this means - we dont need to do a JSON AJAX call
                    $.ajax({
                        url: clientvalidationurl,
                        type: "POST",
                        data: "connectionid=" + connectionid,
                        dataType: "json",
                        error: function (XMLHttpRequest, textStatus, errorThrown) {
                            internalconnection.connectionstatus = "unauthorized - error calling client authorization URL - " + clientvalidationurl;
                            if (thirdPartyUnAuthorisedMethod) {
                                thirdPartyUnAuthorisedMethod.call();
                            }
                        },
                        success: function (response) {
                            try {
                                var responseObject;
                                if ((response) || (response != 'object')) {  //if repsonse is NOT an object - make it an object!!!!
                                    responseObject = JSON.parse(response);
                                }
                                else {
                                    responseObject = response
                                }
                                ClientValidationReturn(responseObject);
                            } catch (ex) { ClientValidationReturn(response); }
                        }
                    });
                }
                else {
                    //the clients validation url IS on an external URL 
                    //this means - we DO need to do a JSONP AJAX call
                    $.ajax({
                        url: clientvalidationurl,
                        type: "GET",
                        data: "connectionid=" + connectionid,
                        dataType: "jsonp",
                        jsonpCallback: 'JSONPCallback'
                        , error: function (XMLHttpRequest, textStatus, errorThrown) {
                            internalconnection.connectionstatus = "unauthorized - error calling client authorization URL - " + errorThrown;
                            if (thirdPartyUnAuthorisedMethod) {
                                thirdPartyUnAuthorisedMethod.call();
                            }
                        },
                        success: function (response) {
                            var responseObject = JSON.parse(response);
                            ClientValidationReturn(responseObject);
                        }
                    });
                }
            }
            else {
                //the user does NOT want to do authorisation - so complete connection!
                CompleteConnection();
            }
        }
        catch (ex) {
            logT5PusherError("ValidateConnection", ex, null, 0, ThirdPartyUserkey, currentConnectionID);

            internalconnection.connectionstatus = "unauthorized - error calling client authorization URL - " + ex.toString();
            if (thirdPartyConnectionfailedMethod) {
                thirdPartyConnectionfailedMethod.call();
            }

            return false;
        }
        return validated;
    } //end ValidateUser

    //we go here after a successfull call of the clients validation/hashing API
    function ClientValidationReturn(data) {
        //we know want to pass this data to OUR validation API!!!!!!
        try {
            if ((data) && (data.auth)) {
                //Validate Third Party
                SignalRConnection.server.vtp(data.auth).done(function (result) {
                    if (result == 1) {
                        //this connection WAS validated!!!!
                        //so ...now join to the correct groups!!!!!
                        CompleteConnection();
                    }
                    else {
                        //validation failed!!! - set a property that tells 3rd party this current connection status
                        internalconnection.connectionstatus = "unauthorized - failed authorization";
                        if (thirdPartyUnAuthorisedMethod) {
                            thirdPartyUnAuthorisedMethod.call();
                        }
                    }
                });
            }
            else {
                //validation failed - update validation status variable which the 3rd party can view
                internalconnection.connectionstatus = "unauthorized - client authorization return data not valid";
                if (thirdPartyUnAuthorisedMethod) {
                    thirdPartyUnAuthorisedMethod.call();
                }
            }
        }
        catch (ex) {
            logT5PusherError("ClientValidationReturn", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
            internalconnection.connectionstatus = "unauthorized - error validating clients authorisation return data - data is " + dataAsJSON + ",error is " + ex.toString();
            if (thirdPartyConnectionfailedMethod) {
                thirdPartyConnectionfailedMethod.call();
            }
            return false;
        }
    }

    //This function initialises the Pusher!!!! 
    function Initialise(userkey) {

        try {
            internalconnection.connectionstatus = "connecting";
            ThirdPartyUserkey = userkey;
            if (srstarted == 0) {
                ExamineUserAgent();

                if ((isChrome == 0) && (isAndroid == 1)) {
                    //this is an android device that is NOT using chrome - therefore we may encounter
                    //issues with the page loading forever - so start connection after brief timeout!!!!
                    window.setTimeout(
                        function () {
                            Connect();
                        }
                        , 2000); //SignalR - bug - EVERYTHING - (images etc ) need to be finshed before signalR is finished otherwise it keeps loading 4ever!! - 
                }
                else {
                    Connect();
                }
            }
            else {
                //user has already started t5pusher but they are starting it again!!!
                //so call RestartSignalRConnection!!!
                userHasCalledStartAgain = 1;
                RestartSignalRConnection("Initialise");
            }

        }
        catch (ex) {
            logT5PusherError("Initialise", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
            internalconnection.connectionstatus = "error starting connection - error is " + ex.toString();
            if (thirdPartyConnectionfailedMethod) {
                thirdPartyConnectionfailedMethod.call();
            }
            return false;
        }


    } //end Initialise

    //new way to push content to T5Pusher
    function PushMessage(processName, messageList, groupName) {
        try {

            if (srstarted == 1) {
                //sr HAS started - so send message
                if ((messageList) && (messageList.length > 0)) {
                    SignalRConnection.server.stpm(messageList, GetCorrectGroupName(groupName), processName); //, ThirdPartyPassword
                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                //sr has not started yet - so store this message for when it has
                var thisPushQueueItem = new PushQueueItem(processName, messageList, groupName);
                pushQueue.push(thisPushQueueItem);
                return true; //should we return true here??????
            }
        }
        catch (ex) {
            logT5PusherError("PushMessage", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
            return false;
        }
    }; //end PushMessage


    function joinGroupFinalStep(correctGroupName, Auth, JoinGroupReturn) {

        try {
            var authString = "";
            var secure = 0;
            if (!Auth) {
                //before we join this group we want to validate the user first!!!!
                authString = '';
            }
            else {
                try {
                    if (Auth.auth) {
                        authString = Auth.auth;
                    }
                    else {
                        authString = Auth;
                    }
                } catch (ex) { authString = Auth; }

            }

            if (Auth) {
                secure = 1;
            }

            SignalRConnection.server.thirdPartyJoinGroupV2(correctGroupName, secure, authString).done(function (result) {
                if (result == 1) { //group joined ok

                    //we have NOT joined more than 5 groups in the last 5 seconds - so - continue as normal
                    if (listOfGroupsJoined.indexOf(correctGroupName) < 0) {  //this groupName is not in our list - so add it!!!!
                        listOfGroupsJoined.push(correctGroupName);
                        listOfGroupsJoined_secure.push(secure);

                        if (!JoinGroupReturn) {
                            JoinGroupReturn = "";
                        }
                        else {

                            var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                            if (locationofMethod > -1) {
                                var returnObject = { groupName: GetOriginalGroupName(correctGroupName), joined: 1, status: "joined", secure: secure };
                                listOf3rdPartyEvents[locationofMethod].call(undefined, returnObject); //undefined = valueForThis
                            }

                        }

                        listOfGroupsJoined_return.push(JoinGroupReturn);

                        //record the time we joined this group
                        var now = new Date();
                        listOfGroupsJoined_times.push(now);
                        LogPushEvent("rejoining group - " + correctGroupName);
                    }

                    numGroupsDB = -101; //we have just joined a group - so reset this value so we can go to DB to update it!!!!
                    return 1;
                }
                else {
                    //put a message in the error console that says validation failed on T5Pusher side!!!

                    var locationofMethod = listOf3rdPartyEventNames.indexOf(JoinGroupReturn);
                    if (locationofMethod > -1) {

                        var status;
                        if (result = -4) {
                            status = "failed to join group - attempted to join a secure group with an incorrect hash value";
                        }
                        else if (result = -3) {
                            status = "failed to join group - this is NOT a secure group yet the client has attempted to join a secure group";
                        }
                        else if (result = -2) {
                            status = "failed to join group - the user has attempted to join a secure group but has NOT passed up a hashed authorisation code";
                        }
                        else if (result = -5) {
                            status = "failed to join group - the user has attempted to join a secure group WITHOUT the SECURE group details";
                        }
                        else if (result = -1) {
                            status = "failed to join group - T5Pusher Internal Error";
                        }
                        var returnObject = { groupName: GetOriginalGroupName(correctGroupName), joined: 0, status: status, secure: secure };
                        listOf3rdPartyEvents[locationofMethod].call(undefined, "failed!!"); //undefined = valueForThis
                    }

                }
            });
            return 1; //no error
        }
        catch (ex) {
            logT5PusherError("joinGroupFinalStep", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
            return -1;
        }
    }

    function GetNumGroupsJoinedInLast5Seconds() {
        var numMessagesSentInLast5Seconds = 0;

        try {
            var currentTime = new Date();
            for (var i = 0; i < listOfGroupsJoined_times.length; i++) {
                var groupJoinTime = listOfGroupsJoined_times[i];

                var dif = currentTime.getTime() - groupJoinTime.getTime();
                var Seconds = dif / 1000;

                if (Seconds <= 5) {
                    numMessagesSentInLast5Seconds = numMessagesSentInLast5Seconds + 1;
                }
            }
        }
        catch (ex) {
            logT5PusherError("GetNumGroupsJoinedInLast5Seconds", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
        }

        return numMessagesSentInLast5Seconds;
    }


    function JoinSecureGroupPrivate(groupName, JoinGroupReturn) {
        JoinGroupPrivate(groupName, JoinGroupReturn, null, 1);
    }


    function JoinGroupPrivate(groupName, JoinGroupReturn, T5internalgroup, Secure) {
        try {
            var joined = 0;
            var correctGroupName = "";
            if (!T5internalgroup) {
                //We are NOT joining a group we are using to manage the service
                //i.e we ARE joining a 3rd party group!!!!!
                //so - check if the 3rd party are starting their groupName with 3PG:
                correctGroupName = GetCorrectGroupName(groupName, T5internalgroup);
            }
            else {
                correctGroupName = groupName;
            }

            if (srstarted == 1) { //we should check to make sure we are connected before we attempt to join group
                //before we join the group here - make sure we haven't joined more than 5 groups in the last 5 seconds
                ////////due to issues with SignalR LongPolling (and it's also to do with the length of the groupName) - if we join more than 5 groups in 5 seconds we can have issues    
                if ((GetNumGroupsJoinedInLast5Seconds() < 5) || (GetCurrentConnectionMethod() != "longPolling")) { //|| (GetCurrentConnectionMethod() != "longPolling")
                    //we have NOT joined more than 5 groups in the last 5 seconds - so - join this group - NOW!!!!

                    if (Secure == 1) {
                        //this IS a secure group request ..so get the authorisation hash and then pass that to T5pusher to join the group 
                        JoinSecureGroupAuthorisation(correctGroupName, JoinGroupReturn);
                        joined = 1;//no error - we won't know if the group was actually joined until the response is returned from SignalR
                    }
                        //else if (Auth) {
                        //    //we already have the exising authorisation code for this group
                        //    //this means we must be rejoining this group AFTER a reconnect!!!!
                        //    //joined = joinGroupFinalStep(correctGroupName, Auth,true);//joined will be set to 1 if there was no error - we won't know if the group was actually joined until the response is returned from SignalR
                        //    JoinSecureGroupAuthorisation(correctGroupName);
                        //}
                    else {
                        //this is NOT a secure group request so join user to group as normal
                        joined = joinGroupFinalStep(correctGroupName, null, JoinGroupReturn); //joined will be set to 1 if there was no error - we wont know if the group was actually joined untill the response os returned from SignalR
                    }
                }
                else {
                    //we HAVE joined more than 5 groups in the last 5 seconds - so - join this group - in 5 seconds!!!!
                    window.setTimeout(
                            function () {
                                JoinGroupPrivate(groupName, JoinGroupReturn, T5internalgroup, Secure);
                            }
                            , (5500)
                    );
                    joined = -2;//unable to join due to longpolling issue = will be joined in a few seconds
                }
            }
            else {
                //signalr Connection is NOT started (this can happen if we are trying to connect to a group after a timeout - and while we were waiting we lost connection)
                //- so store this group so when we do establish connection we can join it then!!!)
                if (listOfGroupsJoined.indexOf(correctGroupName) < 0) {
                    //this groupName is not in our list - so add it!!!!
                    listOfGroupsJoined.push(correctGroupName);
                    if (!Secure) {
                        Secure = 0;
                    }
                    listOfGroupsJoined_secure.push(Secure);

                    if (!JoinGroupReturn) {
                        JoinGroupReturn = "";
                    }

                    listOfGroupsJoined_return.push(JoinGroupReturn);
                }
                joined = -3;  //will be joined after T5Pusher reconnect
            }
        }
        catch (ex) {
            logT5PusherError("JoinGroupPrivate", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
            joined = -1; //error
        }
        return joined;
    }  //JoinGroupPrivate

    function RejoinGroups() {
        var joined = 0;
        try {
            if (listOfGroupsJoined.length > 0) {
                //we DID join groups before - so.. we need to rejoin them!!!!!
                var NumGroups = listOfGroupsJoined.length;

                for (var i = 0; i < NumGroups; i++) {
                    var Secure = listOfGroupsJoined_secure[i];
                    var JoinGroupReturn = listOfGroupsJoined_return[i];
                    var GroupName = listOfGroupsJoined[i];
                    joined = JoinGroupPrivate(GroupName, JoinGroupReturn, true, Secure);
                    if (joined < 0) {
                        //if we fail to rejoin even 1 group then the RejoinGroups function has failed and we need to trigger a full signalR reconnect!!!!
                        return joined;
                    }
                }
            }
        }
        catch (ex) {
            logT5PusherError("RejoinGroups", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
            joined = -1; //if we fail to rejoin even 1 group then the RejoinGroups function has failed and we need to trigger a full signalR reconnect!!!!
        }
        return joined;
    }

    function T5Bind(name, method) {
        //first make sure we haven't already bound this event before!!!
        try {
            //only do this if we have BOTH a name AND a method!!!
            if ((name) && (method)) {
                if (listOf3rdPartyEventNames.indexOf(name) < 0) {
                    listOf3rdPartyEventNames.push(name);
                    listOf3rdPartyEvents.push(method);
                }
            }
        }
        catch (ex) {
            logT5PusherError("T5Bind", ex, null, 0, ThirdPartyUserkey, currentConnectionID);
        }
    }

    function T5ConnectionBind(name, method) {
        if (name) {
            if (name.toLowerCase() == "connected") {
                thirdPartyConnectionstartMethod = method;
            }
            else if (name.toLowerCase() == "connectionattemptfailed") {
                thirdPartyConnectionfailedMethod = method;
            }
            else if (name.toLowerCase() == "connectionslow") {
                thirdPartyConnectionSlowMethod = method;
            } else if (name.toLowerCase() == "statechanged") {
                thirdPartyConnectionstateChangedMethod = method;
            }
            else if (name.toLowerCase() == "connectionlost") {
                thirdPartyConnectionlostMethod = method;
            }
            else if ((name.toLowerCase() == "unauthorised") || (name.toLowerCase() == "unauthorized")) {
                thirdPartyUnAuthorisedMethod = method;
            }
        }
    }

    function setSecureConnection() {
        secureConnection = true;
    }

    function setJSONPAuthorisation() {
        jsonpclientvalidation = true;
    }

    function setClientAuthorisationURL(url) {
        clientvalidationurl = url;
    }

    return {
        start: Initialise,
        joinGroup: JoinGroupPrivate,
        push: PushMessage,
        bind: T5Bind,
        connectionbind: T5ConnectionBind,
        useSecureConnection: setSecureConnection,
        useJSONPAuthorisation: setJSONPAuthorisation,
        setClientAuthorisationURL: setClientAuthorisationURL,
        connection: internalconnection,
        GetCurrentConnectionMethod: GetCurrentConnectionMethod,
        GetCurrentTimeStamp: GetCurrentTimeStamp,
        stopBrowserLoading: stopBrowserLoading,
        CreateSecureGroup: CreateSecureGroup,
        JoinSecureGroup: JoinSecureGroupPrivate,
        restartTest: RestartSignalRConnection,
        DeleteSecureGroup: DeleteSecureGroup
    };
}();