Imports Repositories.Framework
Imports System.Data.SqlClient

Public Class SQLAzure


    Public Shared Function GetSQLAzureRepository() As Repository
        Dim _dbRepository As Repository
        Try
            'Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In GetSQLAzureRepository ------1")
            _dbRepository = CType(HttpContext.Current.Session("SQLAzureDBRepository"), Repository)
            '_dbRepository = CType(HttpRuntime.Cache("SQLAzureDBRepository"), Repository)

            If _dbRepository Is Nothing Then
                _dbRepository = Repositories.Framework.RepositoryManager.GetRepository("Sql")
                HttpContext.Current.Session.Add("SQLAzureDBRepository", _dbRepository)
                'HttpRuntime.Cache("SQLAzureDBRepository") = _dbRepository
            End If
        Catch ex As Exception
            'Logger.LogError("SQLAzure.GetSQLAzureRepository", ex)
            Try
                _dbRepository = Repositories.Framework.RepositoryManager.GetRepository("Sql")
                Try
                    HttpContext.Current.Session.Add("SQLAzureDBRepository", _dbRepository)
                    'HttpRuntime.Cache("SQLAzureDBRepository") = _dbRepository
                Catch ex2 As Exception
                    Logger.LogError("SQLAzure.GetSQLAzureRepository_1", ex)
                End Try
            Catch ex2 As Exception
                Logger.LogError("SQLAzure.GetSQLAzureRepository_2", ex2)
            End Try
        End Try
        Return _dbRepository
    End Function

    Public Shared Function GetNumGroupsForConnectionID(ByVal ConnectionID As String) As Integer
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim numGroups As Integer
        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("ConnectionId", ConnectionID)

            Dim result = GetSQLAzureRepository().ExecuteQueryByStoredProcedure("USP_GetNumGroupsForConnectionID", inputParameters)

            Dim rows = result.ResultSet.Tables(0).[Select]()
            For Each row As System.Data.DataRow In rows
                Integer.TryParse(row.Item("numGroups"), numGroups)
            Next
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before

            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_GetNumGroupsForConnectionID", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.USP_GetNumGroupsForConnectionID", ex)
        End Try
        Return numGroups
    End Function


    Public Shared Function LogConnectionCreated(ByVal UserKey As String, ByVal ConnectionID As String, ByVal transportMethod As String) As Integer

        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "In LogConnectionCreated ------1")
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim connectionLogged As Integer
        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            If String.IsNullOrEmpty(UserKey) Then
                inputParameters.Add("UserKey", DBNull.Value)
            Else
                inputParameters.Add("UserKey", UserKey)
            End If

            If String.IsNullOrEmpty(ConnectionID) Then
                inputParameters.Add("ConnectionId", DBNull.Value)
            Else
                inputParameters.Add("ConnectionId", ConnectionID)
            End If

            If String.IsNullOrEmpty(transportMethod) Then
                inputParameters.Add("transport", DBNull.Value)
            Else
                inputParameters.Add("transport", transportMethod)
            End If

            Dim result = GetSQLAzureRepository().ExecuteQueryByStoredProcedure("USP_T5Pusher_LogConnectionCreated", inputParameters)

            Dim rows = result.ResultSet.Tables(0).[Select]()
            For Each row As System.Data.DataRow In rows
                Integer.TryParse(row.Item("RC"), connectionLogged)
            Next
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before

            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_T5Pusher_LogConnectionCreated", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.LogConnectionCreated", ex)
        End Try
        Return connectionLogged
    End Function

    Public Shared Function CreateSecureGroup(ByVal UserKey As String, ByVal ConnectionID As String, ByVal GroupName As String, ByVal Broadcast As Integer) As Integer
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim groupCreated As Integer
        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("UserKey", UserKey)
            inputParameters.Add("ConnectionId", ConnectionID)
            inputParameters.Add("Broadcast", Broadcast)
            inputParameters.Add("GroupName", "3PG:" & UserKey + ":" + GroupName)

            Dim result = GetSQLAzureRepository().ExecuteQueryByStoredProcedure("USP_CreateSecureGroup", inputParameters)

            Dim rows = result.ResultSet.Tables(0).[Select]()
            For Each row As System.Data.DataRow In rows
                Integer.TryParse(row.Item("RC"), groupCreated)
            Next
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before

            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_CreateSecureGroup", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.LogSecureGroupCreation", ex)
        End Try
        Return groupCreated
    End Function

    Public Shared Function DeleteSecureGroup(ByVal UserKey As String, ByVal ConnectionID As String, ByVal GroupName As String) As Integer
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim groupDeleted As Integer
        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("UserKey", UserKey)
            inputParameters.Add("ConnectionId", ConnectionID)
            inputParameters.Add("GroupName", "3PG:" & UserKey + ":" + GroupName)

            Dim result = GetSQLAzureRepository().ExecuteQueryByStoredProcedure("USP_DeleteSecureGroup", inputParameters)

            Dim rows = result.ResultSet.Tables(0).[Select]()
            For Each row As System.Data.DataRow In rows
                Integer.TryParse(row.Item("RC"), groupDeleted)
            Next
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before

            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_DeleteSecureGroup", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.DeleteSecureGroup", ex)
        End Try
        Return groupDeleted
    End Function

    Public Shared Function ValidateMessageSend(ByVal UserKey As String, ByVal ConnectionID As String, ByVal GroupName As String) As Integer
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim valid As Integer
        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("UserKey", UserKey)
            inputParameters.Add("ConnectionId", ConnectionID)
            inputParameters.Add("GroupName", GroupName) '"3PG:" & UserKey + ":"

            Dim result = GetSQLAzureRepository().ExecuteQueryByStoredProcedure("USP_ValidateMessageSend", inputParameters)

            Dim rows = result.ResultSet.Tables(0).[Select]()
            For Each row As System.Data.DataRow In rows
                Integer.TryParse(row.Item("valid"), valid)
            Next
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before

            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_ValidateMessageSend", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.DeleteSecureGroup", ex)
        End Try
        Return valid
    End Function


    Public Shared Function JoinGroup(ByVal UserKey As String, ByVal ConnectionID As String, ByVal GroupName As String, ByVal secure As Integer) As Integer
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim joined As Integer
        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("UserKey", UserKey)
            inputParameters.Add("ConnectionId", ConnectionID)
            inputParameters.Add("GroupName", GroupName)
            inputParameters.Add("secure", secure)

            Dim result = GetSQLAzureRepository().ExecuteQueryByStoredProcedure("USP_JoinGroup", inputParameters)

            Dim rows = result.ResultSet.Tables(0).[Select]()
            For Each row As System.Data.DataRow In rows
                Integer.TryParse(row.Item("Joined"), joined)
            Next
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before

            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_JoinGroup", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.JoinGroup", ex)
        End Try
        Return joined
    End Function

    Public Shared Function GetUserDetails(ByVal UserKey As String) As UserDetails
        Dim thisUser As New UserDetails
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("UserKey", UserKey)
            Dim result = GetSQLAzureRepository().ExecuteQueryByStoredProcedure("USP_T5Pusher_GetUserDetails", inputParameters)

            Dim rows = result.ResultSet.Tables(0).[Select]()
            For Each row As System.Data.DataRow In rows
                Integer.TryParse(row.Item("ID"), thisUser.UserID)
                Integer.TryParse(row.Item("OnlySendToAllOnSecureChannel"), thisUser.OnlyAllowSendToAllInABroadcastGroup)
                Integer.TryParse(row.Item("SecureConnectionEnabled"), thisUser.SecureConnectionEnabled)
                thisUser.UserName = row.Item("UserName")
                thisUser.UserSecret = row.Item("Secret")
            Next
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before

            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_T5Pusher_GetUserDetails", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.GetUserDetails", ex)
        End Try
        Return thisUser
    End Function




    Public Shared Function LogConnectionDisconnected(ByVal UserKey As String, ByVal ConnectionID As String) As Integer
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim connectionLogged As Integer
        Try
            Dim inputParameters As New Dictionary(Of String, Object)
            'inputParameters.Add("UserKey", UserKey)
            'inputParameters.Add("ConnectionId", ConnectionID)

            If String.IsNullOrEmpty(UserKey) Then
                inputParameters.Add("UserKey", DBNull.Value)
            Else
                inputParameters.Add("UserKey", UserKey)
            End If

            If String.IsNullOrEmpty(ConnectionID) Then
                inputParameters.Add("ConnectionId", DBNull.Value)
            Else
                inputParameters.Add("ConnectionId", ConnectionID)
            End If

            



            GetSQLAzureRepository().ExecuteNonQueryByStoredProcedure("USP_T5Pusher_LogConnectionDisconnect", inputParameters)
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before
            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_T5Pusher_LogConnectionDisconnect", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.LogConnectionDisconnected", ex)
        End Try
        Return connectionLogged
    End Function

    'we log the transport again here as the transport logged when the user connects may not be the final/correct transport used
    'loggin the transport here again will give us a more accurate detail of the actual transport used
    Public Shared Function LogConnectionAuthorized(ByVal UserKey As String, ByVal ConnectionID As String, ByVal transportMethod As String) As Integer
        Dim afterQuery As DateTime
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now

        Dim connectionLogged As Integer
        Try
            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("UserKey", UserKey)
            inputParameters.Add("ConnectionId", ConnectionID)
            inputParameters.Add("transport", transportMethod)
            GetSQLAzureRepository().ExecuteNonQueryByStoredProcedure("USP_LogConnectionAuthorization", inputParameters)
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - before
            Dim AsyncCaller As New LogTimeTakenAsyncMethodCaller(AddressOf LogTimeTaken)
            Dim LogResult As IAsyncResult = AsyncCaller.BeginInvoke("USP_LogConnectionAuthorization", QuerySpan.TotalMilliseconds, Nothing, Nothing)
        Catch ex As Exception
            Logger.LogError("SQLAzure.LogConnectionAuthorized", ex)
        End Try
        Return connectionLogged
    End Function


    ' The delegate must have the same signature as the method  it will call asynchronously i.e - LogTimeTaken
    Public Delegate Sub LogTimeTakenAsyncMethodCaller(ByVal processName As String, ByVal TimeTaken As Double)


    Public Shared Sub LogTimeTaken(ByVal processName As String, ByVal TimeTaken As Double)
        Try

            Dim inputParameters As New Dictionary(Of String, Object)
            inputParameters.Add("ProcessName", processName)
            inputParameters.Add("timetaken", TimeTaken)
            inputParameters.Add("location", ConfigurationManager.AppSettings("location"))

            GetSQLAzureRepository().ExecuteNonQueryByStoredProcedure("USP_LogTimeTaken", inputParameters)
        Catch ex As Exception
            Logger.LogError("SQLAzure.LogTimeTaken", ex)
        End Try
    End Sub


End Class
