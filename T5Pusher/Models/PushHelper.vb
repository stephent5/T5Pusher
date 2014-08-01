Public Class PushHelper

    Public Async Function SendMessage(ByVal messageList As System.Collections.Generic.List(Of String), ByVal GroupName As String, ByVal processName As String, ByVal ConnectionID As Integer) As Threading.Tasks.Task(Of Integer)

        Dim Messagedetails() As String = GroupName.Split(":")
        Dim userkey As String = Messagedetails(1)
        Dim RemoteIP As String = ""

        'Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "RemoteIP is " + RemoteIP + ",origin is " + Context.Headers("Origin"))

        Dim ValidConnection As Integer
        Dim SecureConnectionEnabledForThisUser As Integer = DynamoDB.CheckIfUserIsUsingSecureConnection(userkey)
        If SecureConnectionEnabledForThisUser = 1 Then
            'The user does want to check that each connection has been authorised before it can send a message
            ValidConnection = DynamoDB.MakeSureConnectionIsValid(userkey, ConnectionID)
        Else
            'This user has NOT flagged that they want to use secure connections- this means - allow allow messages to be sent 
            'dont check if the connectionid has previously been validated 
            ValidConnection = 1
        End If

        If ValidConnection > 0 Then
            Return 1
        Else
            Return 0
        End If
        Return 1
    End Function

End Class
