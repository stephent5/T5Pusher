Imports Microsoft.VisualBasic
Imports System.Security.Cryptography
Imports System.Text

Public Class Authentication

    Public Shared Function ValidateThirdPartyConnection(ByVal AuthString As String, ByVal ConnectionId As String) As Boolean
        Dim Validated As Boolean = False
        Try
            Dim Authdetails() As String = AuthString.Split(":")
            Dim userKey As String = Authdetails(0)
            Dim ClientHash As String = Authdetails(1)

            'Dim ThirdPartySecret As String = DynamoDB.GetUserSecret(userKey)
            Dim ThirdPartySecret As String = SQLAzure.GetUserDetails(userKey).UserSecret

            Dim StringToValidate As String = ThirdPartySecret & ":" & ConnectionId

            'make sure that their hash matches ours - this means that they MUST have the secret
            If (Not String.IsNullOrEmpty(StringToValidate)) AndAlso verifyMd5Hash(StringToValidate, ClientHash) Then
                Validated = True
            End If

            Try
                If Validated Then
                    Logger.Log(BitFactory.Logging.LogSeverity.Error, Nothing, StringToValidate & " Validated!!!")
                Else
                    Logger.Log(BitFactory.Logging.LogSeverity.Error, Nothing, StringToValidate & " NOT Validated!!!")
                End If
            Catch ex As Exception
            End Try

        Catch ex As Exception
            Logger.LogError("Authentication.ValidateThirdPartyConnection", ex)
            Validated = False
        End Try
        Return Validated
    End Function

    Public Shared Function ValidateThirdPartySecureGroupCreation(ByVal AuthString As String, ByVal ConnectionId As String, ByVal GroupName As String, ByVal Broadcast As Integer, ByVal Delete As Integer) As Integer
        Dim Validated As Integer = 0
        Try
            Dim Authdetails() As String = AuthString.Split(":")
            Dim userKey As String = Authdetails(0)
            Dim ClientHash As String = Authdetails(1)

            'Dim ThirdPartySecret As String = DynamoDB.GetUserDetails(userKey).UserSecret
            Dim ThirdPartySecret As String = SQLAzure.GetUserDetails(userKey).UserSecret

            Dim StringToValidate As String = ""

            If Broadcast = 1 Then 'broadcast group
                StringToValidate = ThirdPartySecret & ":" & ConnectionId & ":" & GroupName & ":broadcast"

                If (Not String.IsNullOrEmpty(StringToValidate)) AndAlso verifyMd5Hash(StringToValidate, ClientHash) Then
                    Validated = 2 'broadcast group
                End If
            Else 'normal non broadcast secure group

                If Delete = 1 Then
                    StringToValidate = ThirdPartySecret & ":" & ConnectionId & ":" & GroupName & ":delete"
                Else
                    StringToValidate = ThirdPartySecret & ":" & ConnectionId & ":" & GroupName
                End If

                'make sure that their hash matches ours - this means that they MUST have the secret
                If (Not String.IsNullOrEmpty(StringToValidate)) AndAlso verifyMd5Hash(StringToValidate, ClientHash) Then
                    Validated = 1 'Normal secure group
                End If
            End If

            Try
                If Validated Then
                    Logger.Log(BitFactory.Logging.LogSeverity.Error, Nothing, StringToValidate & " Validated!!!")
                Else
                    Logger.Log(BitFactory.Logging.LogSeverity.Error, Nothing, StringToValidate & " NOT Validated!!!")
                End If
            Catch ex As Exception
            End Try

        Catch ex As Exception
            Logger.LogError("Authentication.ValidateThirdPartyConnection", ex)
            Validated = False
        End Try
        Return Validated
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
