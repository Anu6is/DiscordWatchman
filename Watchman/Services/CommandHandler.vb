Imports Discord.Commands
Imports Discord.WebSocket

<Service(ServiceScope.Singleton)>
Public Class CommandHandler
    Private ReadOnly _client As DiscordSocketClient
    Private ReadOnly _commands As CommandService
    Private ReadOnly _service As PrefixService
    Private ReadOnly _provider As IServiceProvider

    Public Sub New(client As DiscordSocketClient, commands As CommandService, service As PrefixService, provider As IServiceProvider)
        _client = client
        _commands = commands
        _service = service
        _provider = provider
    End Sub

    Public Sub Initialize()
        AddHandler _client.MessageReceived, AddressOf HandleCommandAsync
    End Sub

    Private Async Function HandleCommandAsync(ByVal message As SocketMessage) As Task

        Dim userMessage = TryCast(message, SocketUserMessage)

        If userMessage Is Nothing OrElse userMessage.Author.IsBot Then Return

        Dim pos = 0
        Dim prefix = _service.GetPrefix(userMessage.Guild)
        Dim context As New SocketCommandContext(_client, userMessage)

        If userMessage.IsUserMention(_client.CurrentUser) Then Await context.Channel.SendMessageAsync($"My current prefix is **{prefix}**") : Return

        If Not userMessage.HasMentionPrefix(_client.CurrentUser, pos) AndAlso
            Not userMessage.HasStringPrefix(prefix, pos, StringComparison.OrdinalIgnoreCase) Then Return

        Dim command As String = userMessage.Content.Substring(pos).Trim

        Await _commands.ExecuteAsync(context, command, _provider)
    End Function
End Class