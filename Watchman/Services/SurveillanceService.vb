Imports Discord
Imports Discord.UserStatus

<Service(ServiceScope.Singleton)>
Public Class SurveillanceService
    Private ReadOnly _data As DataService

    Public Sub New(data As DataService)
        _data = data
    End Sub

    Public Async Function AddContractAsync(user As IUser, bot As IGuildUser, target As Target) As Task
        If target Is Nothing Then
            target = New Target With {.BotId = bot.Id,
                                      .GuildId = bot.GuildId,
                                      .Status = bot.Status,
                                      .LastOffline = If(bot.Status = Offline, DateTimeOffset.UtcNow.Ticks, DateTimeOffset.MaxValue.UtcTicks),
                                      .LastOnline = If(bot.Status = Online, DateTimeOffset.UtcNow.Ticks, DateTimeOffset.MaxValue.UtcTicks)}

            target = Await _data.InsertAsync(target)
        End If

        Await _data.InsertAsync(New Contract With {.UserId = user.Id, .TargetId = target.TargetId, .Delay = TimeSpan.FromSeconds(5).TotalMilliseconds})
    End Function

    Public Function RemoveDuplicates(currentBots As List(Of Target), ByRef newBots As IGuildUser()) As List(Of IGuildUser)
        Dim duplicates As New List(Of IGuildUser)
        Dim bots = newBots.ToList

        For index = newBots.Count - 1 To 0 Step -1
            Dim i = index
            If currentBots.Any(Function(target) target.BotId = bots(i).Id) Then
                duplicates.Add(bots(i))
                bots.RemoveAt(i)
            End If
        Next

        newBots = bots.ToArray

        Return duplicates
    End Function
End Class