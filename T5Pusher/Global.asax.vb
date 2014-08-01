' Note: For instructions on enabling IIS6 or IIS7 classic mode, 
' visit http://go.microsoft.com/?LinkId=9394802
Imports System.Web.Http
Imports System.Web.Optimization
Imports Microsoft.AspNet.SignalR
Imports Microsoft.AspNet.SignalR.Hubs
Imports Repositories.Framework


Public Class MvcApplication
    Inherits System.Web.HttpApplication

    Sub Application_Start()

        SetUpLogger()
        SetUpDBRepositories()

        AreaRegistration.RegisterAllAreas()

        SetUpSignalR() ' The order of this is important - from https://github.com/SignalR/Samples/blob/master/BasicChat.Mvc/Global.asax.cs

        WebApiConfig.Register(GlobalConfiguration.Configuration)
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters)
        RouteConfig.RegisterRoutes(RouteTable.Routes)
        BundleConfig.RegisterBundles(BundleTable.Bundles)
        AuthConfig.RegisterAuth()
    End Sub

    Sub SetUpLogger()
        Dim loglocation As String = "/Logs/"
        'Try
        '    loglocation = ConfigurationManager.AppSettings("loglocation")
        'Catch ex As Exception
        'End Try
        Logger.Init(loglocation)
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------we have set up Logger!!!!")
    End Sub


    Sub SetUpDBRepositories()
        Try
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------about to call SetUpDBRepositories !!!!")
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------about to call SetUpDBRepositories sql is  !!!!" & ConfigurationManager.ConnectionStrings("AzureSQL").ConnectionString)
            Repositories.Framework.RepositoryManager.AddRepository("Sql", New Repositories.SQLServer.SQLServerRespository(ConfigurationManager.ConnectionStrings("AzureSQL").ConnectionString))
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------after calling SetUpDBRepositories !!!!")
        Catch ex As Exception
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------ERROR calling SetUpDBRepositories !!!! - error is " & ex.ToString)
        End Try
    End Sub

    Sub SetUpSignalR()
        ' Make long polling connections wait a maximum of 110 seconds for a
        'response. When that time expires, trigger a timeout command and
        'make the client reconnect.
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------SetUpSignalR1!!!!")
        GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(110)

        ' Wait a maximum of 15 seconds after a transport connection is lost
        ' before raising the Disconnected event to terminate the SignalR connection.
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------SetUpSignalR2!!!!")
        GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(15)

        ' For transports other than long polling, send a keepalive packet every
        ' 5 seconds. 
        ' This value must be no more than 1/3 of the DisconnectTimeout value.
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------SetUpSignalR3!!!!")
        GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(5)

        'the game below needs to be removed for friends puhser - needs to be IN for normal pusher
        'SetUpSignalRForAzureServiceBus()
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------SetUpSignalR4!!!!")
        Dim config As New HubConfiguration()
        config.EnableCrossDomain = True
        RouteTable.Routes.MapHubs(config)
        Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------SetUpSignalR5!!!!")
    End Sub


    Public Sub SetUpSignalRForAzureServiceBus()
        Try
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------about to connect to service bus !!!!")
            'GlobalHost.DependencyResolver.UseServiceBus("Endpoint=sb://t5pushertestv1.servicebus.windows.net/;SharedSecretIssuer=owner;SharedSecretValue=yUkAO14gIMkpuPzNn725XtQ+qyQvg8WCZud8cKf0l8U=", "T5Pusher")

            GlobalHost.DependencyResolver.UseServiceBus(ConfigurationManager.AppSettings("Microsoft.ServiceBus.ConnectionString"), "T5Pusher")


            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------after connecting to service bus !!!!")
        Catch ex As Exception
            Logger.Log(BitFactory.Logging.LogSeverity.Info, Me, "In Global ------ERROR connecting to service bus !!!! error is " + ex.ToString)
        End Try
    End Sub

End Class
