Imports Discord
Imports Discord.WebSocket
Imports Microsoft.Extensions.Configuration

<Service(ServiceScope.Singleton)>
Public Class PrefixService
    Private ReadOnly _prefix As String
    Private ReadOnly _prefixes As New Dictionary(Of ULong, String)

    Private ReadOnly Property Data As DataService
    Private ReadOnly Property Client As DiscordSocketClient

    Public Sub New(client As DiscordSocketClient, data As DataService, config As IConfigurationRoot)
        Me.Data = data
        Me.Client = client
        _prefix = config("prefix")

        AddHandler Me.Client.Ready, AddressOf Ready
    End Sub

    Private Async Function Ready() As Task
        RemoveHandler Client.Ready, AddressOf Ready
        For Each guild As SocketGuild In Client.Guilds
            Dim settings = Await Data.GetByIdAsync(New GuildSettings() With {.GuildId = guild.Id})

            If settings IsNot Nothing Then _prefixes.TryAdd(settings.GuildId, settings.Prefix)
        Next
    End Function

    Public Function GetPrefix(guild As IGuild) As String
        If guild Is Nothing Then Return _prefix

        Dim customPrefix As String = String.Empty
        If _prefixes.TryGetValue(guild.Id, customPrefix) Then Return customPrefix

        Return _prefix
    End Function

    Public Async Function SetPrefixAsync(guild As IGuild, prefix As String) As Task
        Dim guildSettings = New GuildSettings() With {.GuildId = guild.Id, .Prefix = prefix}

        If _prefixes.ContainsKey(guild.Id) Then
            Await Data.UpdateAsync(guildSettings)
        Else
            Await Data.InsertAsync(guildSettings)
        End If

        _prefixes(guild.Id) = prefix
    End Function
End Class