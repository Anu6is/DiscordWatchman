﻿Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports Discord
Imports Discord.Commands
Imports Discord.WebSocket
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection

Public Class Watchman
    Private Const CONFIG_FILE As String = "config.json"

    Public Shared Async Function StartAsync() As Task
        If Not File.Exists(CONFIG_FILE) Then Throw New FileNotFoundException("No config file found")

        Await New Watchman().RunAsync()
    End Function

    Private Async Function RunAsync() As Task
        Dim provider = ConfigureServices()

        Using scope = provider.CreateScope()
            Await scope.ServiceProvider.GetRequiredService(Of StartupService).InitializeAsync(provider)
        End Using

        Await Task.Delay(Timeout.Infinite)
    End Function

    Private Function ConfigureServices() As IServiceProvider
        Dim collection As New ServiceCollection()

        With collection
            .AddSingleton(New CommandService(New CommandServiceConfig() With {.LogLevel = LogSeverity.Verbose}))
            .AddSingleton(New DiscordSocketClient(New DiscordSocketConfig() With {.LogLevel = LogSeverity.Verbose, .ExclusiveBulkDelete = True}))
            .AddSingleton(New ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory).AddJsonFile(CONFIG_FILE, False, True).Build)
        End With

        Return Assembly.GetEntryAssembly().LoadCustomServices(collection).BuildServiceProvider
    End Function
End Class