Imports Discord.WebSocket

<Service(ServiceScope.Singleton)>
Public Class WatchDogService
    Private ReadOnly Property _client As DiscordSocketClient

    Public Sub New(client As DiscordSocketClient)
        _client = client
    End Sub

    Public Sub Initialize()

    End Sub
End Class
