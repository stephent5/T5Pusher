Imports Microsoft.VisualBasic
Imports System.Security.Cryptography
Imports System.Text

'Namespace T5Pusher
Public Class ValidateClientController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /ValidateClient

    Function Index() As ActionResult
        Return View()
    End Function

    'Function Validate(ByVal username As String, ByVal password As String) As JsonResult

    '    'Do real validation later!!!!!!
    '    If (username = "app1") Or (username = "app2") Then
    '        FormsAuthentication.SetAuthCookie(username, True)
    '        Return Json(1)
    '    Else
    '        Return Json(-1)
    '    End If
    'End Function

    Function Validate(ByVal username As String, ByVal password As String) As JsonpResult

        'Do real validation later!!!!!! (in fast db!!!!)
        If Not String.IsNullOrEmpty(username) Then
            'we cant use this way any more as we wont be using FormsAuthentication (due to cross domain issues!!)
            'FormsAuthentication.SetAuthCookie(username, True)
            Return Me.Jsonp(1)
        Else
            Return Me.Jsonp(-1)
        End If
    End Function

    Function ValidateAUTH(ByVal connectionid As String) As JsonpResult
        Dim appkey As String = "app1test"
        Dim appSecret As String = "secret123"

        Dim returnJSON As String = "{}"

        If Not String.IsNullOrEmpty(connectionid) Then
            Dim hash = getMd5Hash(appSecret + ":" + connectionid)
            returnJSON = "{""auth"":""" + appkey + ":" + hash + """}"
        End If

        Return Me.Jsonp(returnJSON)
    End Function

    ' Hash an input string and return the hash as
    ' a 32 character hexadecimal string.
    Public Shared Function getMd5Hash(ByVal input As String) As String
        ' Create a new instance of the MD5 object.
        Dim md5Hasher As MD5 = MD5.Create()

        ' Convert the input string to a byte array and compute the hash.
        Dim data As Byte() = md5Hasher.ComputeHash(System.Text.Encoding.Default.GetBytes(input))

        ' Create a new Stringbuilder to collect the bytes
        ' and create a string.
        Dim sBuilder As New StringBuilder()

        ' Loop through each byte of the hashed data 
        ' and format each one as a hexadecimal string.
        Dim i As Integer
        For i = 0 To data.Length - 1
            sBuilder.Append(data(i).ToString("x2"))
        Next i

        ' Return the hexadecimal string.
        Return sBuilder.ToString()

    End Function

    ' Verify a hash against a string.
    Public Shared Function verifyMd5Hash(ByVal input As String, ByVal hash As String) As Boolean
        ' Hash the input.
        Dim hashOfInput As String = getMd5Hash(input)

        ' Create a StringComparer an comare the hashes.
        Dim comparer As StringComparer = StringComparer.OrdinalIgnoreCase

        If 0 = comparer.Compare(hashOfInput, hash) Then
            Return True
        Else
            Return False
        End If

    End Function



End Class
'End Namespace