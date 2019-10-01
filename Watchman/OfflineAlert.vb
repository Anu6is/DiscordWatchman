Imports System.Threading
Imports Discord
Imports Discord.WebSocket
Imports Humanizer
Imports Humanizer.Localisation.TimeUnit

Public Class OfflineAlert
    Public Shared PendingNotifications As New Dictionary(Of Integer, List(Of OfflineAlert))

    Private ReadOnly _trigger As Task
    Private ReadOnly _target As Target
    Private ReadOnly _contract As Contract
    Private ReadOnly _client As DiscordSocketClient
    Private ReadOnly _statusChanged As DateTimeOffset
    Private ReadOnly _tokenSource As New CancellationTokenSource

    Public Sub New(client As DiscordSocketClient, target As Target, contract As Contract, offline As ULong)
        _client = client
        _target = target
        _contract = contract
        _statusChanged = New DateTimeOffset(offline, DateTimeOffset.UtcNow.Offset)
        _trigger = Task.Delay(_contract.Delay, _tokenSource.Token).ContinueWith(Function() Notify())
    End Sub

    Public Async Function SendAsync() As Task
        Dim guild = _client.GetGuild(_target.GuildId) : If guild Is Nothing Then Return
        Dim bot = guild.GetUser(_target.BotId) : If bot Is Nothing OrElse bot.Status = UserStatus.Online Then Return
        Dim user = guild.GetUser(_contract.UserId) : If user Is Nothing Then Return

        Dim builder As New EmbedBuilder With {
            .Title = "Watchman Report",
            .Color = Color.Red,
            .Description = $"{bot.Mention} ({bot}) has gone **OFFLINE** in {guild}",
            .Footer = New EmbedFooterBuilder With {.Text = $"Uptime: {(DateTimeOffset.UtcNow - _statusChanged).Humanize(3, maxUnit:=Day, minUnit:=Millisecond)}"}
        }

        Await user.SendMessageAsync(embed:=builder.Build)

        RemoveNotification()
    End Function

    Public Sub Cancel()
        _tokenSource.Cancel()
        RemoveNotification()
    End Sub

    Private Async Function Notify() As Task
        If _tokenSource.IsCancellationRequested Then Return Else Await SendAsync()
    End Function

    Private Sub RemoveNotification()
        Dim notifications = PendingNotifications(_target.TargetId)

        If notifications.Remove(Me) AndAlso notifications.Count = 0 Then PendingNotifications.Remove(_target.TargetId)

        _tokenSource.Dispose()
    End Sub
End Class
