Public Class ConnectionController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /Connection

    Function Index() As ActionResult
        Return View()
    End Function

    Function CheckGroups(ByVal connectionid As String) As JsonpResult
        'Return Me.Jsonp(DynamoDB.HowManyGroupsIsThisConnectionIn(connectionid))
        Return Me.Jsonp(SQLAzure.GetNumGroupsForConnectionID(connectionid))
    End Function

End Class
