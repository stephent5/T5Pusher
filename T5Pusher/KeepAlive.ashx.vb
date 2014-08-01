Imports System.Web
Imports System.Web.Services
Imports Microsoft.AspNet.SignalR
Imports Microsoft.AspNet.SignalR.Hubs

Public Class KeepAlive
    Implements System.Web.IHttpHandler
    Implements System.Web.SessionState.IRequiresSessionState

    Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        Dim result As Integer = 0
        Try

            '"If you need to use the context multiple-times in a long-lived object, 
            'get the reference once and save it rather than getting it again each time. 
            'Getting the context once ensures that SignalR sends messages to clients in the same sequence in which your 
            'Hub methods make client method invocations"
            ''http://www.asp.net/signalr/overview/hubs-api/hubs-api-guide-javascript-client
            Dim pusherHub
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------1")
            Try
                pusherHub = HttpContext.Current.Cache("pusherHub")
                Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------2")
            Catch ex As Exception
                'Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------3")
            End Try

            If pusherHub Is Nothing Then
                Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------4")
                pusherHub = GlobalHost.ConnectionManager.GetHubContext(Of T5Pusher)()
                Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------5")
                HttpContext.Current.Cache("pusherHub") = pusherHub
                Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------6")
            End If

            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------7")
            pusherHub.Clients.All.keepalive()
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------8")
            result = 1
        Catch ex As Exception
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In KeepAlive ------9")
            Logger.LogError(Me, ex)
            result = -1
        End Try
        context.Response.Write(result)
    End Sub

    ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property

End Class