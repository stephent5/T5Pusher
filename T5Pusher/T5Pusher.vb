Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports Microsoft.AspNet.SignalR
Imports Microsoft.AspNet.SignalR.Hubs
Imports System.Threading.Tasks

'Cant do authorize now due to cross domain issues - we will do our own validation now!!!! - redis or other fast mem db
'<Authorize()> _

<HubName("t5pusher")> _
Public Class T5Pusher
    Inherits Hub

    Public Overrides Function OnConnected() As Task
        ' Add your own code here.
        ' For example: in a chat application, record the association between
        ' the current connection ID and user name, and mark the user as online.
        ' After the code in this method completes, the client is informed that
        ' the connection is established; for example, in a JavaScript client,
        ' the start().done callback is executed.

        Try
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In OnConnected ------1!!!!")
            Dim queryString As System.Collections.Specialized.NameValueCollection = Context.Request.QueryString
            Dim UserRequestingToCreateConnection As String = queryString("ur") 'ur = user requesting
            Dim transportMethod As String = queryString("transport")

            'Log in DB that this user is trying to establish a new connection
            'also make sure that this user has less then the allowed number of currenct connections

            'problem is if not all connections disconnect - we could send a message from each conn every 30 seconds - a reverse keepAlive???? 
            '- probably makes most sense

            'cant do this cos we might be switchin to Azure!!!!!!!!!!!

            'connid/user/ts - will need to discard all
            Dim logged As Integer = SQLAzure.LogConnectionCreated(UserRequestingToCreateConnection, Context.ConnectionId, transportMethod)

            'in future we can put checks on the amount of connections each user has in db 
            'if they have exceeed thisamount we can decide NOT to return  MyBase.OnConnected()
        Catch ex As Exception
            Logger.LogError("SQLAzure.LogConnectionCreated", ex)
        End Try

        Return MyBase.OnConnected()
    End Function

    Public Overrides Function OnDisconnected() As Task
        ' Add your own code here.
        ' For example: in a chat application, record the association between
        ' the current connection ID and user name, and mark the user as online.
        ' After the code in this method completes, the client is informed that
        ' the connection is established; for example, in a JavaScript client,
        ' the start().done callback is executed.
        Try
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In OnDisconnected ------1!!!!")
            Dim queryString As System.Collections.Specialized.NameValueCollection = Context.Request.QueryString
            Dim UserRequestingToCreateConnection As String = queryString("ur") 'ur = user requesting


            'Log in DB that this connection is now disconnected - remove it from all tables where it may be linked to groups
            'we will also need a clena up job that removes connections created a while ago - i.e after a certain time limit - because what happens if our serveers go down 
            'we will have a shitload of connections still in db cos we will have never fired off the ondisconnect event
            Dim logged As Integer = SQLAzure.LogConnectionDisconnected(UserRequestingToCreateConnection, Context.ConnectionId)
        Catch ex As Exception
            Logger.LogError("SQLAzure.LogConnectionCreated", ex)
        End Try
        Return MyBase.OnDisconnected()
    End Function


    'validate third party - this can return data directly to the function that called it - i.e we dont have to call from one part in js and then listen for "vu"
    'in another part check out - http://www.asp.net/signalr/overview/hubs-api/hubs-api-guide-server
    Public Function vtp(ByVal auth As String) As Integer
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In vtp ------1!!!!")
        Try
            Dim Authdetails() As String = auth.Split(":")
            Dim userkey As String = Authdetails(0)
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "vtp details are: userkey-  " + userkey + ",Context.ConnectionId is " + Context.ConnectionId)

            Dim queryString As System.Collections.Specialized.NameValueCollection = Context.Request.QueryString
            Dim UserRequestingToCreateConnection As String = queryString("ur") 'ur = user requesting
            Dim transportMethod As String = queryString("transport")

            If  UserRequestingToCreateConnection = userkey Then
                Dim ValidThirdPartyconnection = Authentication.ValidateThirdPartyConnection(auth, Context.ConnectionId)

                If ValidThirdPartyconnection Then
                    Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "vtp - Context.ConnectionId is valid " + Context.ConnectionId)
                    'we need to store in Dynamo - connectionid/validated/groupname/date

                    'DynamoDB.LogValidConnection(userkey, Context.ConnectionId)
                    SQLAzure.LogConnectionAuthorized(userkey, Context.ConnectionId, transportMethod)

                    '            Return Await Clients.Caller.vu(1) 'validation update
                    'Return Clients.Caller.vu(1) 'validation update
                    Return 1
                Else
                    Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "vtp - Context.ConnectionId NOT is valid " + Context.ConnectionId)
                    'Clients.Caller.vu(0) 'validation update failed
                    Return 0
                End If
            Else
                Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "vtp - Not UserRequestingToCreateConnection = userkey " + Context.ConnectionId)
                'Clients.Caller.vu(0) 'validation update failed
                Return -2
            End If
        Catch ex As Exception
            'Throw New ApplicationException("Server Side t5pusher error - SendThirdPartyGroupMessage  - details ...." & ex.ToString())
            T5Error.LogError("VB - T5Pusher.ValidateThirdParty", ex.ToString)
            Logger.LogError("T5Pusher.ValidateThirdParty", ex)
            Try
                'Clients.Caller.vu(-1) 'validation update failed due to error
                Return -1
            Catch ex2 As Exception
                T5Error.LogError("VB - T5Pusher.ValidateThirdParty  - error attempting to call Clients.Caller.vu(-1) from within Catch", ex.ToString)
                Logger.LogError("T5Pusher.ValidateThirdParty - error attempting to call Clients.Caller.vu(-1) from within Catch - ", ex)
            End Try
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "-------------------------------")
    End Function

    'csg = create secure group
    Public Function csg(ByVal auth As String, ByVal GroupName As String, ByVal Broadcast As Integer) As Integer
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In csg ------1!!!!")
        Try
            Dim Authdetails() As String = auth.Split(":")
            Dim userkey As String = Authdetails(0)
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "csg details are: userkey-  " + userkey + ",Context.ConnectionId is " + Context.ConnectionId + ",GroupName is " + GroupName)

            Dim ValidbroadcastGroupCreationHash As Integer = Authentication.ValidateThirdPartySecureGroupCreation(auth, Context.ConnectionId, GroupName, Broadcast, 0)

            If ValidbroadcastGroupCreationHash > 0 Then
                'DynamoDB.LogSecureGroupCreation(userkey, Context.ConnectionId, GroupName, Broadcast)
                'Return 1

                If SQLAzure.CreateSecureGroup(userkey, Context.ConnectionId, GroupName, Broadcast) > 0 Then
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return 0
            End If

        Catch ex As Exception
            T5Error.LogError("VB - T5Pusher.ValidateThirdParty", ex.ToString)
            Logger.LogError("T5Pusher.ValidateThirdParty", ex)
            Try
                Return -1
            Catch ex2 As Exception
                T5Error.LogError("VB - T5Pusher.ValidateThirdParty  - error attempting to call Clients.Caller.vu(-1) from within Catch", ex.ToString)
                Logger.LogError("T5Pusher.ValidateThirdParty - error attempting to call Clients.Caller.vu(-1) from within Catch - ", ex)
            End Try
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "-------------------------------")
    End Function

    'dsg = delete secure group
    Public Function dsg(ByVal auth As String, ByVal GroupName As String) As Integer
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "-------------------------------")
        Try
            Dim Authdetails() As String = auth.Split(":")
            Dim userkey As String = Authdetails(0)
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "dsg details are: userkey-  " + userkey + ",Context.ConnectionId is " + Context.ConnectionId + ",GroupName is " + GroupName)

            Dim ValidbroadcastGroupCreationHash As Integer = Authentication.ValidateThirdPartySecureGroupCreation(auth, Context.ConnectionId, GroupName, 0, 1)

            If ValidbroadcastGroupCreationHash > 0 Then
                'If DynamoDB.DeleteGroup(userkey, Context.ConnectionId, GroupName) Then
                '    Return 1
                'Else
                '    Return -1
                'End If
                If SQLAzure.DeleteSecureGroup(userkey, Context.ConnectionId, GroupName) > 0 Then
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return 0
            End If
        Catch ex As Exception
            T5Error.LogError("VB - T5Pusher.delete secure group", ex.ToString)
            Logger.LogError("T5Pusher.ValidateThirdParty", ex)
            Try
                Return -1
            Catch ex2 As Exception
                T5Error.LogError("VB - T5Pusher.delete secure group  ", ex.ToString)
                Logger.LogError("T5Pusher.ValidateThirdParty - ", ex)
            End Try
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "-------------------------------")
    End Function

    'the function below is the way we send messages when using SQL server- we have rewritten this slightly so we only need to connect to the DB once!!
    Public Function stpm(ByVal messageList As System.Collections.Generic.List(Of String), ByVal GroupName As String, ByVal processName As String) As Task
        Try
            'there IS a groupName so this message only goes to those in the group!!!
            'Return Clients.Group(GroupName).receivethirdpartygroupmessage(message)

            Dim Messagedetails() As String = GroupName.Split(":")
            Dim userkey As String = Messagedetails(1)
            Dim RemoteIP As String = ""

            'Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "RemoteIP is " + RemoteIP + ",origin is " + Context.Headers("Origin"))

            Dim ValidSend As Integer = SQLAzure.ValidateMessageSend(userkey, Context.ConnectionId, GroupName)
            'Dim theseUserDetails As UserDetails = DynamoDB.GetUserDetails(userkey)

            If ValidSend = -104 Then
                'this userkey is not valid 
                Return Clients.Caller.invalidsendattempt("Invalid Credentials!!!")
            ElseIf ValidSend = -101 Then
                'This user is configured to only allow authorized connections to send. this connection is NOT authorized
                Return Clients.Caller.invalidsendattempt("Only authorised clients may send")
            ElseIf ValidSend = -103 Then
                'this send attempt IS an attempt to send to All users - i.e the group is the default user group!!!
                'this action is forbidden because the user has been configured to not allow a message to be sent to all users (or broadcast)
                'unless it is through a secure group!!!!!!
                Return Clients.Caller.invalidsendattempt("You can only send to all in a secure broadcast group!!")
            ElseIf ValidSend = 2 Then
                'this group IS configured as a broadcast group ...so send message to all!!!!!
                Return Clients.Group("3P:" & userkey).rtpm(processName, messageList)
            ElseIf ValidSend = 1 Then
                ' do normal send logic
                Return Clients.Group(GroupName).rtpm(processName, messageList) 'ReceiveThirdPartyMessage
            End If
        Catch ex As Exception
            'Throw New ApplicationException("Server Side t5pusher error - SendThirdPartyGroupMessage  - details ...." & ex.ToString())
            T5Error.LogError("VB - T5Pusher.SendThirdPartyGroupMessage", ex.ToString)
            Logger.LogError("T5Pusher.SendThirdPartyGroupMessage", ex)
            Try
                Return Clients.Caller.invalidsendattempt("Send error!!!")
            Catch ex2 As Exception
            End Try
        End Try
    End Function

    ''the function below is the way we send messages when using Dynamo DB - we have rewritten this slightly for SQL server so we only need to connect to the DB once!!
    'Public Function stpm(ByVal messageList As System.Collections.Generic.List(Of String), ByVal GroupName As String, ByVal processName As String) As Task
    '    Try
    '        'there IS a groupName so this message only goes to those in the group!!!
    '        'Return Clients.Group(GroupName).receivethirdpartygroupmessage(message)

    '        Dim Messagedetails() As String = GroupName.Split(":")
    '        Dim userkey As String = Messagedetails(1)
    '        Dim RemoteIP As String = ""

    '        'Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "RemoteIP is " + RemoteIP + ",origin is " + Context.Headers("Origin"))

    '        Dim ValidConnection As Integer
    '        'Dim theseUserDetails As UserDetails = DynamoDB.GetUserDetails(userkey)
    '        Dim theseUserDetails As UserDetails = SQLAzure.GetUserDetails(userkey)

    '        If Not String.IsNullOrEmpty(theseUserDetails.UserSecret) Then

    '            If theseUserDetails.SecureConnectionEnabled = 1 Then
    '                'The user does want to check that each connection has been authorised before it can send a message
    '                ValidConnection = DynamoDB.MakeSureConnectionIsValid(userkey, Context.ConnectionId)
    '            Else
    '                'This user has NOT flagged that they want to use secure connections- this means -  allow messages to be sent by any connection  ( even one that was not authorised by the user)
    '                ValidConnection = 1
    '            End If

    '            If ValidConnection > 0 Then

    '                If theseUserDetails.OnlyAllowSendToAllInABroadcastGroup = 1 Then
    '                    'this user has been configured so that you can only SendToAll in a secure broadcast group!!!
    '                    'so...check if the groupName specified here is the default "SendToALL" group

    '                    If GroupName = "3P:" & userkey Then
    '                        'this send attempt IS an attempt to send to All users - i.e the group is the default user group!!!
    '                        'this action is forbidden because the user has been configured to not allow a message to be sent to all users (or broadcast)
    '                        'unless it is through a secure group!!!!!!
    '                        Return Clients.Caller.invalidsendattempt("You can only send to all in a secure broadcast group!!")
    '                    Else
    '                        'this group is NOT the default group ...so check if the group is currently set up to be a broadcast group
    '                        If DynamoDB.CheckIfGroupIsABroadcastGroup(userkey, GroupName) Then
    '                            'this group IS configured as a broadcast group ...so send message to all!!!!!
    '                            Return Clients.Group("3P:" & userkey).rtpm(processName, messageList)
    '                        Else
    '                            'this group is NOT configured as a broadcast group ...so just send to THIS group - i.e do as normal!!!!
    '                            Return Clients.Group(GroupName).rtpm(processName, messageList) 'ReceiveThirdPartyMessage
    '                        End If
    '                    End If
    '                Else
    '                    'this user is not configured to only send to all in a broadcast group ...so - do normal send logic
    '                    Return Clients.Group(GroupName).rtpm(processName, messageList) 'ReceiveThirdPartyMessage
    '                End If
    '            Else
    '                'Send a message to the caller ONLY = telling them that the u/p from their attempt is invalid
    '                Return Clients.Caller.invalidsendattempt("Invalid Credentials!!!")
    '            End If
    '        Else
    '            'this userkey is not valid 
    '            Return Clients.Caller.invalidsendattempt("Only authorised clients may send")
    '        End If

    '    Catch ex As Exception
    '        'Throw New ApplicationException("Server Side t5pusher error - SendThirdPartyGroupMessage  - details ...." & ex.ToString())
    '        T5Error.LogError("VB - T5Pusher.SendThirdPartyGroupMessage", ex.ToString)
    '        Logger.LogError("T5Pusher.SendThirdPartyGroupMessage", ex)
    '        Try
    '            Return Clients.Caller.invalidsendattempt("Send error!!!")
    '        Catch ex2 As Exception
    '        End Try
    '    End Try
    'End Function

    'Try
    ''we should be coming from a load balancer in a live environment - so check there first!!!
    'Dim RequestHeaders As NameValueCollection = Context.Request.Headers
    '            RemoteIP = RequestHeaders.Item("X-Forwarded-For")

    '            If String.IsNullOrEmpty(RemoteIP) Then 'if we dont have this value then check the regular ip from the signalR owin values!!!
    'Dim dictionary As IDictionary(Of String, Object)
    '                dictionary = Context.Request.Items("owin.environment")
    '                RemoteIP = dictionary.Item("server.RemoteIpAddress")
    '            End If
    '        Catch ex As Exception
    '            Logger.Log(BitFactory.Logging.LogSeverity.Error, Me, "Error getting RemoteIP!!! -" + ex.ToString())
    '        End Try
    'Dim forwardedRemoteIP = RequestHeadersDictionary.Item("X-Forwarded-For")

    '        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "------------------------------------")
    '        For Each header In RequestHeadersDictionary.Keys
    'Dim headerName As String = header
    'Dim headerValue As String

    '            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "headerName is " + headerName)

    '            Try
    '                headerValue = RequestHeadersDictionary.Item(headerName).ToString()
    '                Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "headerValue is " + headerValue)
    '            Catch ex As Exception
    '            End Try
    '        Next
    '        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "------------------------------------")

    ' Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "***************************************************")
    '            For Each element In dictionary.Keys
    'Dim ElementName As String = element
    'Dim thisItemValue As String
    '                Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "ElementName is " + ElementName)
    '                Try
    '                    thisItemValue = dictionary.Item(ElementName).ToString()
    '                    Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "thisItemValue is " + thisItemValue)

    '                Catch ex As Exception
    '                    Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, ex)
    '                End Try
    '            Next

    'will need username/pass or some form of authentication here!!!!!
    'Public Sub SendThirdPartyMessage(ByVal message As String, ByVal Username As String, ByVal Password As String, ByVal GroupName As String)
    '    Try
    '        'the flow below will change once we set up username/pass etc
    '        'as EVERY message we send will be to a particular group!!!!!!
    '        '(i.e each 3rd party will send messages only to their group!!!!!!)
    '        If String.IsNullOrEmpty(GroupName) Then
    '            'there is NO gropu name - so this message goes to all users!!!!!

    '            'No longer call this line ( as it will send messages to ALL connected clients while we only want to send messages to those connected to this 3rd party!!!)
    '            'Clients.All.receivethirdpartymessage(message)

    '            'we cant use this way any more as we wont be using FormsAuthentication (due to cross domain issues!!)
    '            'Clients.Group("3P:" & Me.Context.User.Identity.Name).receivethirdpartymessage(message)

    '            Clients.Group("3P:" & Username).receivethirdpartymessage(message)
    '        Else
    '            'there IS a groupName so this message only goes to those in the group!!!
    '            Clients.Group(GroupName).receivethirdpartymessage(message)
    '        End If
    '    Catch ex As Exception
    '        Throw New ApplicationException("Server Side t5pusher error - SendThirdPartyMessage  - details ...." & ex.ToString())
    '    End Try
    'End Sub

    Public Function sendkeepalive() As Task
        'Send a keepAliveMessageToAllClients on all clients.
        Try
            Return Clients.All.keepalive()
        Catch ex As Exception
            'Throw New ApplicationException("Server Side liveevent error - sendkeepalive  - details ...." & ex.ToString())
            T5Error.LogError("VB - T5Pusher.sendkeepalive", ex.ToString)
            Logger.LogError("T5Pusher.sendkeepalive", ex)
        End Try
    End Function

    'new way - added Stephen 26-sep - based on docs http://weblogs.asp.net/davidfowler/archive/2012/05/04/api-improvements-made-in-signalr-0-5.aspx
    'Public Function ThirdPartyJoinGroup(ByVal groupName As String, ByVal auth As String) As Task
    '    'users join a group to receive messages about their friends bets/scores
    '    Try
    '        'not a secure group - allow user to join 
    '        DynamoDB.LogConnectionGroup(groupName, Context.ConnectionId)
    '        Return Groups.Add(Context.ConnectionId, groupName)

    '        '"When you return a Task object from the method, SignalR waits for the Task to complete, and then it sends the unwrapped result back to the client, so there is no difference in how you code the method call in the client."
    '        '- from http://www.asp.net/signalr/overview/hubs-api/hubs-api-guide-server
    '    Catch ex As Exception
    '        T5Error.LogError("VB - T5Pusher.ThirdPartyJoinGroup", ex.ToString)
    '        Logger.LogError("T5Pusher.ThirdPartyJoinGroup", ex)
    '        'Throw New ApplicationException("Server Side liveevent error - joingroup  - details ...." & ex.ToString())
    '    End Try
    'End Function


    'new way - added Stephen 26-sep - based on docs http://weblogs.asp.net/davidfowler/archive/2012/05/04/api-improvements-made-in-signalr-0-5.aspx
    Public Function ThirdPartyJoinGroupV2(ByVal groupName As String, ByVal secure As Integer, ByVal auth As String) As Integer
        'users join a group to receive messages about their friends bets/scores
        Try
            Dim Authdetails() As String = auth.Split(":")
            Dim userkey As String = groupName.Split(":")(1)

            If secure Then 'the client is attempting to join a secure group
                If Not String.IsNullOrEmpty(auth) Then

                    'make sure this connection request comes with a valid auth string (you need this to connect to a secure group)
                    Dim ValidSecureGroupHash As Integer = Authentication.ValidateThirdPartySecureGroupCreation(auth, Context.ConnectionId, groupName, 0, 0)

                    If ValidSecureGroupHash Then 'the hashed value IS valid!!!! - so allow the user join the group

                        Dim joined As Integer = SQLAzure.JoinGroup(userkey, Context.ConnectionId, groupName, secure)
                        If joined > 0 Then
                            Groups.Add(Context.ConnectionId, groupName)
                            Return 1 'secure group joined ok
                        Else
                            Return joined 'possible return value here could be -3 'this is NOT a secure group yet the client has attempted to join a secure group
                        End If
                    Else
                        Return -4 'attempted to join a secure group with an incorrect hash value
                    End If
                Else
                    'the user has attempted to join a secure group but has NOT passed up a auth value
                    Return -2
                End If
            Else
                Dim joined As Integer = SQLAzure.JoinGroup(userkey, Context.ConnectionId, groupName, secure)
                If joined > 0 Then
                    Groups.Add(Context.ConnectionId, groupName)
                    Return 1 'group joined ok
                Else
                    Return joined 'possible return value here could be -5 'the user is attemping to join a secure group the normal way - i.e WITHOUT the SECURE group details 
                End If
            End If

            '"When you return a Task object from the method, SignalR waits for the Task to complete, and then it sends the unwrapped result back to the client, so there is no difference in how you code the method call in the client."
            '- from http://www.asp.net/signalr/overview/hubs-api/hubs-api-guide-server
        Catch ex As Exception
            T5Error.LogError("VB - T5Pusher.ThirdPartyJoinGroup", ex.ToString)
            Logger.LogError("T5Pusher.ThirdPartyJoinGroup", ex)
            Return -1
            'Throw New ApplicationException("Server Side liveevent error - joingroup  - details ...." & ex.ToString())
        End Try
    End Function


    'this is the old DynamoDB way of joining groups
    'Public Function ThirdPartyJoinGroupV2(ByVal groupName As String, ByVal secure As Integer, ByVal auth As String) As Integer
    '    'users join a group to receive messages about their friends bets/scores
    '    Try
    '        Dim Authdetails() As String = auth.Split(":")
    '        Dim userkey As String = Authdetails(0)

    '        Dim secureGroup As Boolean = DynamoDB.CheckIfGroupIsASecureGroup(groupName)

    '        If secure Then 'the client is attempting to join a secure group
    '            If Not String.IsNullOrEmpty(auth) Then

    '                If secureGroup Then
    '                    'make sure this connection request comes with a valid auth string (you need this to connect to a secure group)
    '                    Dim ValidSecureGroupHash As Integer = Authentication.ValidateThirdPartySecureGroupCreation(auth, Context.ConnectionId, groupName, 0, 0)

    '                    If ValidSecureGroupHash Then 'the hashed value IS valid!!!! - so allow the user join the group
    '                        DynamoDB.LogConnectionGroup(groupName, Context.ConnectionId)
    '                        Groups.Add(Context.ConnectionId, groupName)
    '                        Return 1 'secure group joined ok
    '                    Else
    '                        Return -4 'attempted to join a secure group with an incorrect hash value
    '                    End If
    '                Else
    '                    'this is NOT a secure group yet the client has attempted to join a secure group
    '                    Return -3
    '                End If
    '            Else
    '                'the user has attempted to join a secure group but has NOT passed up a 
    '                Return -2
    '            End If
    '        Else
    '            'not a secure group - allow user to join 
    '            If secureGroup Then
    '                'the user is attemping to join a secure group the normal way - i.e WITHOUT the SECURE group details 
    '                Return -5
    '            Else

    '                'Check if this group is the official group - log the fact that this connid is joining the official gropu for a user
    '                'this will give us a list of all connid's that have succesfully joined a 3rd parties group 
    '                'this means we can then say for a particualr 3rd party group they have x amount of people currenlty connected
    '                'we can then count this amount every time a user tries to join as each 3rd party group on our free offering will only be
    '                'allowed a certain amount of connections at any one time

    '                'on the disconnect we can remove each connection from 


    '                DynamoDB.LogConnectionGroup(groupName, Context.ConnectionId)
    '                Groups.Add(Context.ConnectionId, groupName)
    '                Return 1
    '            End If
    '        End If

    '        '"When you return a Task object from the method, SignalR waits for the Task to complete, and then it sends the unwrapped result back to the client, so there is no difference in how you code the method call in the client."
    '        '- from http://www.asp.net/signalr/overview/hubs-api/hubs-api-guide-server
    '    Catch ex As Exception
    '        T5Error.LogError("VB - T5Pusher.ThirdPartyJoinGroup", ex.ToString)
    '        Logger.LogError("T5Pusher.ThirdPartyJoinGroup", ex)
    '        Return -1
    '        'Throw New ApplicationException("Server Side liveevent error - joingroup  - details ...." & ex.ToString())
    '    End Try
    'End Function


    'Public Function SendThirdPartyGroupMessage(ByVal message As String, ByVal GroupName As String, ByVal Password As String) As Task
    '    Try
    '        'there IS a groupName so this message only goes to those in the group!!!
    '        'Return Clients.Group(GroupName).receivethirdpartygroupmessage(message)

    '        Dim friendID As String = "friendID"
    '        'Return Clients.Group(GroupName).ProcessFriendUpdate(message, friendID, 1) '"3PG"
    '        Return Clients.Group(GroupName).ReceiveThirdPartyGroupMessage(message)
    '    Catch ex As Exception
    '        Throw New ApplicationException("Server Side t5pusher error - SendThirdPartyGroupMessage  - details ...." & ex.ToString())
    '    End Try
    'End Function


    'Public Function SendThirdPartyMessage(ByVal message As String, ByVal GroupName As String, ByVal Password As String) As Task
    '    Try
    '        'there IS a groupName so this message only goes to those in the group!!!
    '        'Return Clients.Group(GroupName).receivethirdpartygroupmessage(message)

    '        Dim username As String
    '        Dim Messagedetails() As String = GroupName.Split(":")

    '        username = Messagedetails(1)

    '        'Validate Username/password here then call function if validated!!
    '        Return Clients.Group(GroupName).ReceiveThirdPartyGroupMessage(message)
    '    Catch ex As Exception
    '        Throw New ApplicationException("Server Side t5pusher error - SendThirdPartyGroupMessage  - details ...." & ex.ToString())
    '    End Try
    'End Function




    'Public Function joingroup(ByVal groupName As String) As Task
    '    'users join a group to receive messages about their friends bets/scores
    '    Try
    '        Return Groups.Add(Context.ConnectionId, groupName)
    '    Catch ex As Exception
    '        T5Error.LogError("VB", ex.ToString)
    '        Throw New ApplicationException("Server Side liveevent error - joingroup  - details ...." & ex.ToString())
    '    End Try
    'End Function

    'Public Function processfriendupdates(ByVal betdetails As String, ByVal groupName As String, ByVal FixtureID As Integer) As Task
    '    'Send the users message to only the people in the correct group ( i.e. the people who are specifically listening out for his events - his freinds!!!!)
    '    Try
    '        Dim friendID As String = "friendID"
    '        Return Clients.Group(groupName).ProcessFriendUpdate(betdetails, friendID, FixtureID) '"3PG"
    '    Catch ex As Exception
    '        T5Error.LogError("VB", ex.ToString)
    '        Throw New ApplicationException("Server Side liveevent error - processfriendupdates  - details ...." & ex.ToString())
    '    End Try
    'End Function


End Class




