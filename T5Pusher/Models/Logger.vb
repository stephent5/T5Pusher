Imports System.Data.SqlClient
Imports BitFactory.Logging
Imports Microsoft.VisualBasic

Public Class Logger

    Private Shared _Logger As BitFactory.Logging.CompositeLogger
    Private Shared _LogDir As String

    Public Shared Sub Init(ByVal LogDir As String)
        _LogDir = LogDir
        If _Logger Is Nothing Then
            _Logger = New BitFactory.Logging.CompositeLogger()
            ' Code that runs on application startup
            '_Logger.AddLogger("Socket", _
            '    New BitFactory.Logging.InsistentLogger( _
            '    New BitFactory.Logging.SerialSocketLogger("193.120.208.79", 999), 100, 60))
            _Logger.Application = "Trust5"
            _Logger.SeverityThreshold = BitFactory.Logging.LogSeverity.Debug
            Try

                Dim filelog As New RollingFileLogger(New RollingFileLogger.RollOverDateStrategy(LogDir & "{0}.txt"))

                _Logger.AddLogger("filelog", filelog)
                _Logger.AddLogger("trace", New BitFactory.Logging.TraceLogger())


            Catch ex As Exception

            End Try

            'Application.Add("log", _Log)
            Log(LogSeverity.Info, "Logger", "Logger created")
        Else
            Log(LogSeverity.Info, "Logger", "Logger already created")
        End If
    End Sub

    Public Shared Sub Log(ByVal Severity As BitFactory.Logging.LogSeverity, ByVal Category As Object, ByVal Message As Object)
        '_Logger.AddLogger("File", New BitFactory.Logging.FileLogger(_LogDir & Date.Now.ToString("yyyy-MM-dd") & ".txt"))
        _Logger.Log(Severity, Category, Message)
        '_Logger.RemoveLogger("File")
    End Sub

    Public Shared Sub LogError(ByVal Category As Object, ByVal ex As Exception)
        'format ex
        Log(LogSeverity.Error, Category, ex.ToString & Environment.NewLine & ex.StackTrace)
    End Sub

End Class




