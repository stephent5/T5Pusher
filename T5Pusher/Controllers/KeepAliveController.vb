Imports Microsoft.AspNet.SignalR
Imports Microsoft.AspNet.SignalR.Hubs

'Namespace T5Pusher
Public Class KeepAliveController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /KeepAlive

    Function Index() As ActionResult
        Return View()
    End Function

    'This function will send a simple keepAlive message to all clients connected to the T5Pusher Hub
    'This will enable us to know on each client if we are receving all the messages we should 
    Function Send() As ActionResult
        Response.Cache.SetCacheability(HttpCacheability.NoCache)
        Dim pusherHub = GlobalHost.ConnectionManager.GetHubContext(Of T5Pusher)()
        pusherHub.Clients.All.keepalive()
        Return Json(1) 'need to return something a bit better than this!!
    End Function

    Function SendJSONP() As JsonpResult
        Response.Cache.SetCacheability(HttpCacheability.NoCache)
        Dim pusherHub = GlobalHost.ConnectionManager.GetHubContext(Of T5Pusher)()
        pusherHub.Clients.All.keepalive()
        Return Me.Jsonp(1) 'need to return something a bit better than this!!
    End Function

End Class
'End Namespace