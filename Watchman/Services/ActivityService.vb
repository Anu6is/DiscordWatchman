Imports Discord
Imports Discord.WebSocket
Imports Humanizer
Imports Humanizer.Localisation.TimeUnit

<Service(ServiceScope.Singleton)>
Public Class ActivityService
    Private ReadOnly Property _client As DiscordSocketClient
    Private ReadOnly Property _data As DataService

    Public Sub New(client As DiscordSocketClient, data As DataService)
        _client = client
        _data = data
    End Sub

    Public Sub Initialize()
        AddHandler _client.UserLeft, AddressOf UserLeft
        AddHandler _client.GuildMemberUpdated, AddressOf MemberUpdated
    End Sub

    Private Function MemberUpdated(before As SocketGuildUser, after As SocketGuildUser) As Task
        If Not after.IsBot Then Return Task.CompletedTask

        If before.Status = UserStatus.Online AndAlso after.Status = UserStatus.Offline Then Return NotifyOfflineAsync(after)
        If before.Status = UserStatus.Offline AndAlso after.Status <> UserStatus.Offline Then Return NotifyOnlineAsync(after)

        Return Task.CompletedTask
    End Function

    Private Function UserLeft(user As SocketGuildUser) As Task
        If user.IsBot Then Return RemoveTarget(user) Else Return RemoveContracts(user)
    End Function

    Private Async Function NotifyOfflineAsync(bot As SocketGuildUser) As Task
        Dim offline = DateTimeOffset.UtcNow.Ticks
        Dim sTarget = Await _data.GetJointEntityAsync(Of Target, Contract)($"{NameOf(Target.GuildId):C}=@GuildIdParam AND {NameOf(Target.BotId):C}=@BotIdParam",
                                                                          New With {Key .GuildIdParam = bot.Guild.Id, Key .BotIdParam = bot.Id})

        If sTarget Is Nothing OrElse sTarget.Contracts.Count = 0 Then Return

        sTarget.LastOffline = offline : Await _data.UpdateAsync(sTarget)

        Dim contracts = sTarget.Contracts

        For Each contract In contracts
            If Not contract.DirectMessage Then Continue For

            If OfflineAlert.PendingNotifications.ContainsKey(sTarget.TargetId) Then
                OfflineAlert.PendingNotifications(sTarget.TargetId).Add(New OfflineAlert(_client, sTarget, contract, offline))
            Else
                OfflineAlert.PendingNotifications.Add(sTarget.TargetId, New List(Of OfflineAlert) From {New OfflineAlert(_client, sTarget, contract, offline)})
            End If
        Next
    End Function

    Private Async Function NotifyOnlineAsync(bot As SocketGuildUser) As Task
        Dim online = DateTimeOffset.UtcNow.Ticks
        Dim notifications As New List(Of OfflineAlert)
        Dim sTarget = Await _data.GetJointEntityAsync(Of Target, Contract)($"{NameOf(Target.GuildId):C}=@GuildIdParam AND {NameOf(Target.BotId):C}=@BotIdParam",
                                                                          New With {Key .GuildIdParam = bot.Guild.Id, Key .BotIdParam = bot.Id})

        If sTarget Is Nothing OrElse sTarget.Contracts.Count = 0 Then Return

        Dim onlineDate = sTarget.LastOnlineDate
        Dim offlineDate = sTarget.LastOfflineDate

        sTarget.LastOnline = online : Await _data.UpdateAsync(sTarget)

        If OfflineAlert.PendingNotifications.TryGetValue(sTarget.TargetId, notifications) Then
            For Each notification In notifications
                notification.Cancel()
            Next
        ElseIf onlineDate < offlineDate Then
            Dim builder As New EmbedBuilder With {
                    .Title = "Watchman Report",
                    .Color = Color.Green,
                    .Description = $"{bot.Mention} ({bot}) is now **ONLINE** in {bot.Guild}",
                    .Footer = New EmbedFooterBuilder With {.Text = $"Downtime: {(DateTimeOffset.UtcNow - offlineDate).Humanize(2, maxUnit:=Hour, minUnit:=Millisecond)}"}
                }

            For Each contract In sTarget.Contracts
                If Not contract.DirectMessage Then Continue For
                Await bot.Guild.GetUser(contract.UserId)?.SendMessageAsync(embed:=builder.Build)
            Next
        End If
    End Function

    Private Async Function RemoveTarget(user As SocketGuildUser) As Task
        Dim targets = Await _data.GetListAsync(Of Target)($"{NameOf(Target.GuildId):C}=@GuildIdParam AND {NameOf(Target.BotId):C}=@UserIdParam",
                                                          New With {Key .GuildIdParam = user.Guild.Id, Key .UserIdParam = user.Id})

        If targets.Count = 1 Then Await _data.DeleteAsync(targets.First)
    End Function

    Private Async Function RemoveContracts(user As SocketGuildUser) As Task
        If Not user.GuildPermissions.ManageGuild Then Return

        Dim userContracts = Await _data.GetJointListAsync(Of Contract, Target)($"{NameOf(Contract.UserId):C}=@UserIdParam",
                                                                     New With {Key .UserIdParam = user.Id, Key .GuildIdParam = user.Guild.Id},
                                                                     $"{NameOf(Target.GuildId):C}=@GuildIdParam")
        If userContracts.Count = 0 Then Return

        If userContracts.Count = 1 Then Await _data.DeleteAsync(userContracts.First) : Return

        Await _data.BulkDeleteAsync(Of Contract)($"{NameOf(Contract.ContractId):C} IN ({String.Join(",", userContracts.Select(Function(c) c.ContractId))})")
    End Function
End Class