'Namespace T5Pusher
    Public Class ErrorController
        Inherits System.Web.Mvc.Controller

        '
        ' GET: /Error

        Function Index() As ActionResult
            Return View()
        End Function

    'Function LogError(ByVal thisError As T5Error) As JsonpResult
    '    Response.Cache.SetCacheability(HttpCacheability.NoCache)

    '    thisError.LogError() 'this goes to DB
    '    Return Me.Jsonp(True) 'Always return error
    'End Function

    Function LogError(ByVal clientdetails As String, ByVal localtime As String, ByVal userdetails As String, ByVal origin As String, ByVal details As String) As JsonpResult
        Response.Cache.SetCacheability(HttpCacheability.NoCache)

        T5Error.LogT5PushError(clientdetails, localtime, userdetails, origin, details) 'this goes to DB
        Return Me.Jsonp(True) 'Always return error
    End Function


    End Class
'End Namespace