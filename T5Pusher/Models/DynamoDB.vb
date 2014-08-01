Imports Amazon
Imports Amazon.DynamoDB
Imports Amazon.DynamoDB.Model
Imports Amazon.DynamoDB.DataModel
Imports Amazon.SecurityToken
Imports Amazon.Runtime

Imports System
Imports System.Collections.Generic
Imports Amazon.DynamoDB.DocumentModel
Imports Amazon.Util

Public Class DynamoDB

    Public Shared Function LogConnectionGroup(ByVal groupName As String, ByVal connectionid As String) As Integer

        Dim numItemsMatchingUserDEtails As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Dim Inserted As Integer = 0
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"
            Dim client As New AmazonDynamoDBClient(config)

            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- LogConnectionGroup connectionid is " + connectionid + ",  Attempting Retry of Insert - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    Dim groupconnectionsTable As Table = Table.LoadTable(client, "tblConnectionGroups")
                    Dim Gc As New Document()
                    Gc("ConnectionID") = connectionid
                    Gc("Group") = groupName
                    groupconnectionsTable.PutItem(Gc)
                    retry = False 'we inserted data - dont do retry
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.LogConnectionGroup", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
            Inserted = 1
        Catch ex As Exception
            Inserted = -1
            Logger.LogError("DynamoDB.LogConnectionGroup", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- LogConnectionGroup connectionid is " + connectionid + ",  connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , InsertSpan is " + QuerySpan.TotalMilliseconds.ToString)
        Return Inserted
    End Function

    Public Shared Function DeleteGroup(ByVal userkey As String, ByVal connectionid As String, ByVal GroupName As String) As Boolean
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Dim Deleted As Boolean = False
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"
            Dim client As New AmazonDynamoDBClient(config)

            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- LogSecureGroupDeletion connectionid is " + connectionid + ",  Attempting Retry of Delete - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    Dim DeleteRequest = New DeleteItemRequest() With { _
                     .TableName = "tblSecureGroups", _
                     .Key = New Key() With { _
                         .HashKeyElement = New AttributeValue() With { _
                            .S = "3PG:" & userkey + ":" + GroupName _
                        }
                        }
                    }
                    client.DeleteItem(DeleteRequest)
                    retry = False 'we inserted data - dont do retry
                    Deleted = True

                    'also create log table - just for records
                    Dim broadcastGroupsLogTable As Table = Table.LoadTable(client, "tblSecureGroupLog")
                    Dim newlogRow As New Document()
                    newlogRow("GroupName") = "3PG:" & userkey + ":" + GroupName
                    newlogRow("ConnectionID") = connectionid

                    'One or more parameter values were invalid: Missing the key ConnectionID in the item

                    newlogRow("TimeCreated") = DateTime.UtcNow
                    newlogRow("Action") = -1 '1 = create , -1 = delete
                    broadcastGroupsLogTable.PutItem(newlogRow)

                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.LogValidConnection", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
        Catch ex As Exception
            Logger.LogError("DynamoDB.LogValidConnection", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- LogValidConnection connectionid is " + connectionid + ", connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , InsertSpan is " + QuerySpan.TotalMilliseconds.ToString)
        Return Deleted
    End Function

    Public Shared Function LogSecureGroupCreation(ByVal userkey As String, ByVal connectionid As String, ByVal GroupName As String, ByVal Broadcast As Integer) As Integer
        Dim numItemsMatchingUserDetails As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Dim Inserted As Integer = 0
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"
            Dim client As New AmazonDynamoDBClient(config)

            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- tblBroadcastGroups connectionid is " + connectionid + ",  Attempting Retry of Insert - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    Dim broadcastGroupsTable As Table = Table.LoadTable(client, "tblSecureGroups")
                    Dim newRow As New Document()
                    newRow("GroupName") = "3PG:" & userkey + ":" + GroupName
                    newRow("Broadcast") = Broadcast
                    'newRow("TimeCreated") = DateTime.UtcNow - we can get time from tblBroadcastGroupLog
                    broadcastGroupsTable.PutItem(newRow)

                    retry = False 'we inserted data - dont do retry

                    'also create log table - just for records
                    Dim broadcastGroupsLogTable As Table = Table.LoadTable(client, "tblSecureGroupLog")
                    Dim newlogRow As New Document()
                    newlogRow("GroupName") = "3PG:" & userkey + ":" + GroupName
                    newRow("Broadcast") = Broadcast
                    newlogRow("ConnectionID") = connectionid

                    'One or more parameter values were invalid: Missing the key ConnectionID in the item

                    newlogRow("TimeCreated") = DateTime.UtcNow
                    newlogRow("Action") = 1 '1 = create , -1 = delete
                    broadcastGroupsLogTable.PutItem(newlogRow)

                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.LogValidConnection", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
            Inserted = 1
        Catch ex As Exception
            Inserted = -1
            Logger.LogError("DynamoDB.LogValidConnection", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- LogValidConnection connectionid is " + connectionid + ", connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , InsertSpan is " + QuerySpan.TotalMilliseconds.ToString)
        Return Inserted
    End Function

    Public Shared Function CheckIfGroupIsASecureGroup(ByVal groupName As String) As String
        Dim GroupIsABroadcastGroup As Boolean = False
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- CheckIfGroupIsABroadcastGroup Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    'Dim request As New GetItemRequest
                    'request.TableName = "tblBroadcastGroups"
                    'request.Key = New Key With {.HashKeyElement = New AttributeValue With {.S = groupName + "#" + userkey}}

                    Dim request As New GetItemRequest
                    request.TableName = "tblSecureGroups"
                    request.Key = New Key With {.HashKeyElement = New AttributeValue With {.S = groupName}}

                    Dim response = client.GetItem(request)
                    If response.GetItemResult.Item IsNot Nothing AndAlso response.GetItemResult.Item.Count > 0 Then
                        GroupIsABroadcastGroup = True
                    End If

                    'Dim primaryKey As String = groupName
                    'Dim request As QueryRequest = New QueryRequest().WithTableName("tblBroadcastGroups").WithHashKeyValue(New AttributeValue().WithS(primaryKey))
                    'Dim response = client.Query(request)

                    'If response.QueryResult.Items.Count > 0 Then
                    '    GroupIsABroadcastGroup = True
                    'End If
                    retry = False 'we inserted data - dont do retry
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.CheckIfGroupIsABroadcastGroup", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
        Catch ex As Exception
            Logger.LogError("DynamoDB.CheckIfGroupIsABroadcastGroup", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- CheckIfGroupIsABroadcastGroup connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " ,  QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return GroupIsABroadcastGroup
    End Function


    Public Shared Function CheckIfGroupIsABroadcastGroup(ByVal userkey As String, ByVal groupName As String) As String
        Dim GroupIsABroadcastGroup As Boolean = False
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- CheckIfGroupIsABroadcastGroup Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    'Dim request As New GetItemRequest
                    'request.TableName = "tblBroadcastGroups"
                    'request.Key = New Key With {.HashKeyElement = New AttributeValue With {.S = groupName + "#" + userkey}}

                    Dim request As New GetItemRequest
                    request.TableName = "tblSecureGroups"
                    request.Key = New Key With {.HashKeyElement = New AttributeValue With {.S = groupName}}

                    Dim response = client.GetItem(request)
                    Dim result As Amazon.DynamoDB.Model.GetItemResult = response.GetItemResult

                    If result IsNot Nothing AndAlso result.Item IsNot Nothing Then
                        Dim attributeMap As Dictionary(Of String, AttributeValue) = result.Item
                        Dim isBroadcast As Integer = attributeMap.Item("Broadcast").N

                        If isBroadcast > 0 Then
                            GroupIsABroadcastGroup = True
                        End If
                    End If

                    'Dim primaryKey As String = groupName
                    'Dim request As QueryRequest = New QueryRequest().WithTableName("tblBroadcastGroups").WithHashKeyValue(New AttributeValue().WithS(primaryKey))
                    'Dim response = client.Query(request)

                    'If response.QueryResult.Items.Count > 0 Then
                    '    GroupIsABroadcastGroup = True
                    'End If
                    retry = False 'we inserted data - dont do retry
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.CheckIfGroupIsABroadcastGroup", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
        Catch ex As Exception
            Logger.LogError("DynamoDB.CheckIfGroupIsABroadcastGroup", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- CheckIfGroupIsABroadcastGroup connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " ,  QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return GroupIsABroadcastGroup
    End Function


    Public Shared Function LogValidConnection(ByVal userkey As String, ByVal connectionid As String) As Integer

        Dim numItemsMatchingUserDEtails As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Dim Inserted As Integer = 0
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"
            Dim client As New AmazonDynamoDBClient(config)

            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- LogValidConnection connectionid is " + connectionid + ",  Attempting Retry of Insert - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    Dim validconnectionsTable As Table = Table.LoadTable(client, "tblValidConnectionLog")
                    Dim book1 As New Document()
                    book1("UserConnection") = userkey + "#" + connectionid
                    book1("TSCreated") = DateTime.UtcNow
                    validconnectionsTable.PutItem(book1)
                    retry = False 'we inserted data - dont do retry
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.LogValidConnection", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
            Inserted = 1
        Catch ex As Exception
            Inserted = -1
            Logger.LogError("DynamoDB.LogValidConnection", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- LogValidConnection connectionid is " + connectionid + ", connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , InsertSpan is " + QuerySpan.TotalMilliseconds.ToString)
        Return Inserted
    End Function

    Public Shared Function GetUserSecret(ByVal userkey As String) As String
        Dim appSecret As String = ""
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- GetUserSecret Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    Dim request As New GetItemRequest
                    request.TableName = "tblUserDetails"
                    request.Key = New Key With {.HashKeyElement = New AttributeValue With {.S = userkey}}

                    Dim response = client.GetItem(request)
                    Dim result As Amazon.DynamoDB.Model.GetItemResult
                    result = response.GetItemResult

                    Dim attributeMap As Dictionary(Of String, AttributeValue) = result.Item
                    appSecret = attributeMap.Item("Secret").S
                    retry = False 'we inserted data - dont do retry
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.GetUserSecret", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
        Catch ex As Exception
            Logger.LogError("DynamoDB.GetUserSecret", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- GetUserSecret connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , appSecret QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return appSecret
    End Function

    'this will return the users configuration details
    Public Shared Function GetUserDetails(ByVal userkey As String) As UserDetails
        Dim theseUserDetails As New UserDetails
        Dim SecureConnectionEnabled As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- GetUserDetails Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    Dim request As New GetItemRequest
                    request.TableName = "tblUserDetails"
                    request.Key = New Key With {.HashKeyElement = New AttributeValue With {.S = userkey}}

                    Dim response = client.GetItem(request)

                    Dim result As Amazon.DynamoDB.Model.GetItemResult
                    result = response.GetItemResult

                    Dim attributeMap As Dictionary(Of String, AttributeValue) = result.Item
                    theseUserDetails.SecureConnectionEnabled = attributeMap.Item("SecureConnectionEnabled").N
                    theseUserDetails.OnlyAllowSendToAllInABroadcastGroup = attributeMap.Item("OnlySendToAllOnSecureChannel").N
                    theseUserDetails.UserKey = userkey
                    theseUserDetails.UserName = attributeMap.Item("User").S
                    theseUserDetails.UserSecret = attributeMap.Item("Secret").S
                    retry = False 'we inserted data - dont do retry
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.GetUserDetails", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
        Catch ex As Exception
            SecureConnectionEnabled = -1
            Logger.LogError("DynamoDB.GetUserDetails", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "-GetUserDetails connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , SecureConnectionEnabled QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return theseUserDetails
    End Function


    Public Shared Function CheckIfUserIsUsingSecureConnection(ByVal userkey As String) As String
        Dim SecureConnectionEnabled As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- CheckIfUserIsUsingSecureConnection Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    Dim request As New GetItemRequest
                    request.TableName = "tblUserDetails"
                    request.Key = New Key With {.HashKeyElement = New AttributeValue With {.S = userkey}}

                    Dim response = client.GetItem(request)

                    Dim result As Amazon.DynamoDB.Model.GetItemResult
                    result = response.GetItemResult

                    Dim attributeMap As Dictionary(Of String, AttributeValue) = result.Item
                    SecureConnectionEnabled = attributeMap.Item("SecureConnectionEnabled").N
                    retry = False 'we inserted data - dont do retry
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.CheckIfUserIsUsingSecureConnection", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection
        Catch ex As Exception
            SecureConnectionEnabled = -1
            Logger.LogError("DynamoDB.GetUserSecret", ex)
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "-CheckIfUserIsUsingSecureConnection connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , SecureConnectionEnabled QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return SecureConnectionEnabled
    End Function

    Public Shared Function HowManyGroupsIsThisConnectionIn(ByVal connectionid As String) As Integer
        Dim numGroups As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- HowManyGroupsIsThisConnectionIn connectionid is " + connectionid + " Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    'we want to check if this connection has been authorised to send(and receive) from this group in the last 24 hours
                    '- no point in checking longer ago than that( i.e a connectionis never going to last a full 24 hours is it???) - we could then add this connection again here - 
                    'i.e - tblValidConnections could list the all the times that the connection has connected!!! - in that case we could reduce the time from 24 hours.
                    Dim primaryKey As String = connectionid

                    Dim request As QueryRequest = New QueryRequest().WithTableName("tblConnectionGroups").WithHashKeyValue(New AttributeValue().WithS(primaryKey))
                    Dim response = client.Query(request)

                    'Console.WriteLine("No. of reads used (by query in FindRepliesForAThreadSpecifyLimit) {0}\n", response.QueryResult.ConsumedCapacityUnits)
                    numGroups = response.QueryResult.Items.Count
                    retry = False
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.MakeSureConnectionIsValid", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection

        Catch ex As Exception
            Logger.LogError("DynamoDB.MakeSureConnectionIsValid", ex)
            numGroups = -1
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- HowManyGroupsIsThisConnectionIn connectionid is " + connectionid + ", connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return numGroups
    End Function



    Public Shared Function MakeSureConnectionIsValid(ByVal userkey As String, ByVal connectionid As String) As Integer
        Dim numItemsMatchingUserDEtails As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- MakeSureConnectionIsValid connectionid is " + connectionid + " Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    'we want to check if this connection has been authorised to send(and receive) from this group in the last 24 hours
                    '- no point in checking longer ago than that( i.e a connectionis never going to last a full 24 hours is it???) - we could then add this connection again here - 
                    'i.e - tblValidConnections could list the all the times that the connection has connected!!! - in that case we could reduce the time from 24 hours.
                    Dim TwentyFourHoursAgoDate As DateTime = DateTime.UtcNow - TimeSpan.FromDays(1)
                    Dim TwentyFourHoursAgoString As String = TwentyFourHoursAgoDate.ToString(AWSSDKUtils.ISO8601DateFormat)

                    Dim rangeKeyCondition As Condition = New Condition().WithComparisonOperator("GT").WithAttributeValueList(New AttributeValue().WithS(TwentyFourHoursAgoString))
                    Dim primaryKey As String = userkey + "#" + connectionid

                    Dim request As QueryRequest = New QueryRequest().WithTableName("tblValidConnectionLog").WithHashKeyValue(New AttributeValue().WithS(primaryKey)).WithRangeKeyCondition(rangeKeyCondition)
                    Dim response = client.Query(request)

                    'Console.WriteLine("No. of reads used (by query in FindRepliesForAThreadSpecifyLimit) {0}\n", response.QueryResult.ConsumedCapacityUnits)
                    numItemsMatchingUserDEtails = response.QueryResult.Items.Count
                    retry = False
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.MakeSureConnectionIsValid", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection

        Catch ex As Exception
            Logger.LogError("DynamoDB.MakeSureConnectionIsValid", ex)
            numItemsMatchingUserDEtails = -1
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- MakeSureConnectionIsValid connectionid is " + connectionid + ", connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return numItemsMatchingUserDEtails
    End Function

    Public Shared Function CheckIfConnectionIsConfiguredToSendToAllWithoutUsingSecureChannel(ByVal userkey As String, ByVal connectionid As String) As Integer
        Dim numItemsMatchingUserDEtails As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"

            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim retry As Boolean = True
            Dim NumAttmempts As Integer = 0
            Do While retry AndAlso NumAttmempts < 3
                Try
                    If NumAttmempts > 0 Then
                        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- MakeSureConnectionIsValid connectionid is " + connectionid + " Attempting Retry of Query - must have recevied a ProvisionedThroughputExceededException!!")
                    End If

                    'we want to check if this connection has been authorised to send(and receive) from this group in the last 24 hours
                    '- no point in checking longer ago than that( i.e a connectionis never going to last a full 24 hours is it???) - we could then add this connection again here - 
                    'i.e - tblValidConnections could list the all the times that the connection has connected!!! - in that case we could reduce the time from 24 hours.
                    Dim TwentyFourHoursAgoDate As DateTime = DateTime.UtcNow - TimeSpan.FromDays(1)
                    Dim TwentyFourHoursAgoString As String = TwentyFourHoursAgoDate.ToString(AWSSDKUtils.ISO8601DateFormat)

                    Dim rangeKeyCondition As Condition = New Condition().WithComparisonOperator("GT").WithAttributeValueList(New AttributeValue().WithS(TwentyFourHoursAgoString))
                    Dim primaryKey As String = userkey + "#" + connectionid

                    Dim request As QueryRequest = New QueryRequest().WithTableName("tblValidConnectionLog").WithHashKeyValue(New AttributeValue().WithS(primaryKey)).WithRangeKeyCondition(rangeKeyCondition)
                    Dim response = client.Query(request)

                    'Console.WriteLine("No. of reads used (by query in FindRepliesForAThreadSpecifyLimit) {0}\n", response.QueryResult.ConsumedCapacityUnits)
                    numItemsMatchingUserDEtails = response.QueryResult.Items.Count
                    retry = False
                Catch ex As AmazonDynamoDBException
                    If ex.ToString.Contains("ProvisionedThroughputExceededException") Then
                        retry = True 'only retry if error is related to ProvisionedThroughputExceededException
                    Else
                        retry = False
                    End If
                    Logger.LogError("DynamoDB.MakeSureConnectionIsValid", ex)
                End Try
                NumAttmempts = NumAttmempts + 1
            Loop

            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection

        Catch ex As Exception
            Logger.LogError("DynamoDB.MakeSureConnectionIsValid", ex)
            numItemsMatchingUserDEtails = -1
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "- MakeSureConnectionIsValid connectionid is " + connectionid + ", connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return numItemsMatchingUserDEtails
    End Function



    Public Shared Function CheckUserIP(ByVal username As String, ByVal IPAddress As String) As Integer
        'Dim client As AmazonDynamoDB

        'Dim stsClient As AmazonSecurityTokenServiceClient = New AmazonSecurityTokenServiceClient()
        'Dim sessionCredentials As RefreshingSessionAWSCredentials = New RefreshingSessionAWSCredentials(stsClient)
        'client = New AmazonDynamoDBClient(sessionCredentials)

        'Dim context As DynamoDBContext = New DynamoDBContext(client)

        'Dim reqQuery As QueryRequest = New QueryRequest()
        'reqQuery.TableName = "tblUserDetails"
        'Dim thisAttributeValue As New AttributeValue
        'thisAttributeValue.S = "title"
        'reqQuery.HashKeyValue = thisAttributeValue

        'Dim resQuery As QueryResponse = client.Query(reqQuery)

        'Return resQuery.QueryResult.Items.Count


        'Dim context DynamoDBContext  = new DynamoDBContext(client)
        'Dim alldvds As IEnumerable = context.Query(username, Amazon.DynamoDB.DocumentModel.QueryOperator.Equal, OriginURL)
        'return alldvds

        Dim numItemsMatchingUserDEtails As Integer = 0
        Dim afterconnection As DateTime
        Dim afterQuery As DateTime
        Dim connectionSpan As TimeSpan
        Dim QuerySpan As TimeSpan
        Dim before As DateTime = DateTime.Now
        Try
            Dim config As AmazonDynamoDBConfig = New AmazonDynamoDBConfig()
            config.ServiceURL = "http://dynamodb.eu-west-1.amazonaws.com"
            Dim client As New AmazonDynamoDBClient(config)
            afterconnection = DateTime.Now
            connectionSpan = afterconnection - before

            Dim rangeKeyCondition As Condition = New Condition().WithComparisonOperator("EQ").WithAttributeValueList(New AttributeValue().WithS(IPAddress))

            Dim request As QueryRequest = New QueryRequest().WithTableName("tblUser").WithHashKeyValue(New AttributeValue().WithS(username)).WithRangeKeyCondition(rangeKeyCondition)
            Dim response = client.Query(request)

            'Console.WriteLine("No. of reads used (by query in FindRepliesForAThreadSpecifyLimit) {0}\n", response.QueryResult.ConsumedCapacityUnits)
            numItemsMatchingUserDEtails = response.QueryResult.Items.Count
            afterQuery = DateTime.Now
            QuerySpan = afterQuery - afterconnection

        Catch ex As Exception
            numItemsMatchingUserDEtails = -1
        End Try
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Nothing, "-CheckUserIP connectionSpan is " + connectionSpan.TotalMilliseconds.ToString + " , QuerySpan is " + QuerySpan.TotalMilliseconds.ToString)

        Return numItemsMatchingUserDEtails


        'Dim request As var = New GetItemRequest() {TableName=tableName, Key=newKey{HashKeyElement=newAttributeValue{N=id.ToString(UnknownUnknownUnknown}
        'Dim response As var = client.GetItem(request)
        'Console.WriteLine("No. of reads used (by get book item) {0}" & vbLf, response.GetItemResult.ConsumedCapacityUnits)
        'PrintItem(response.GetItemResult.Item)
        'Console.WriteLine("To continue, press Enter")
        'Console.ReadLine()
    End Function

End Class
