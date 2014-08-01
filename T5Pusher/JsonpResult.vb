Imports System.Web.Script.Serialization
'Imports System.Web.Mvc.Resources
Imports System.Web
Imports System.Text
Imports System

Public Class JsonpResult
    Inherits ActionResult

    Public Sub New()
    End Sub

    Public Property ContentEncoding() As Encoding

    Public Property ContentType() As String

    Public Property Data() As Object

    Public Property JsonCallback() As String

    Public Overrides Sub ExecuteResult(ByVal context As ControllerContext)
        If context Is Nothing Then
            Throw New ArgumentNullException("context")
        End If

        Me.JsonCallback = context.HttpContext.Request("jsoncallback")

        If String.IsNullOrEmpty(Me.JsonCallback) Then
            Me.JsonCallback = context.HttpContext.Request("callback")
        End If

        If String.IsNullOrEmpty(Me.JsonCallback) Then
            Throw New ArgumentNullException("JsonCallback required for JSONP response.")
        End If

        Dim response As HttpResponseBase = context.HttpContext.Response

        If Not String.IsNullOrEmpty(ContentType) Then
            response.ContentType = ContentType
        Else
            response.ContentType = "application/json"
        End If
        If ContentEncoding IsNot Nothing Then
            response.ContentEncoding = ContentEncoding
        End If
        If Data IsNot Nothing Then
            Dim serializer As New JavaScriptSerializer()
            response.Write(String.Format("{0}({1});", Me.JsonCallback, serializer.Serialize(Data)))
        End If
    End Sub
End Class

'extension methods for the controller to allow jsonp.
Public Module ContollerExtensions
    <Runtime.CompilerServices.Extension()> _
    Public Function Jsonp(ByVal controller As Controller, ByVal data As Object) As JsonpResult
        Dim result As New JsonpResult()
        result.Data = data
        result.ExecuteResult(controller.ControllerContext)
        Return result
    End Function

End Module

