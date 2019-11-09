Imports System.Reflection
Imports Discord
Imports Discord.Commands
Imports Discord.WebSocket
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection

<Service(ServiceScope.Singleton)>
Friend Class StartupService
    Private ReadOnly _client As DiscordSocketClient
    Private ReadOnly _commands As CommandService
    Private ReadOnly _config As IConfigurationRoot

    Public Sub New(client As DiscordSocketClient, commands As CommandService, config As IConfigurationRoot, logger As LogService)
        _config = config
        _client = client
        _commands = commands
    End Sub

    Public Async Function InitializeAsync(services As IServiceProvider) As Task
        With New DatabaseDesigner(_config)
            .CreateDb()
            .BuildTables()
        End With

        Await _client.LoginAsync(TokenType.Bot, _config("token"))
        Await _client.StartAsync()

        Dim baseAssembly = Assembly.GetEntryAssembly()

        Await _commands.RegisterTypeReadersAsync(baseAssembly)
        Await _commands.AddModulesAsync(baseAssembly, services)

        Using scope = services.CreateScope()
            scope.ServiceProvider.GetRequiredService(Of PrefixService)
            scope.ServiceProvider.GetRequiredService(Of CommandHandler).Initialize()
            scope.ServiceProvider.GetRequiredService(Of ActivityService).Initialize()
            scope.ServiceProvider.GetRequiredService(Of WatchDogService).Initialize()
        End Using
    End Function
End Class