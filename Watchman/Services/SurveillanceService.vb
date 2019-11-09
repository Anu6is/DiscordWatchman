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

    Public Async Function GetByIdAsync(Of T)(obj As T) As Task(Of T)
        Return Await _data.GetByIdAsync(obj)
    End Function

    Public Async Function InsertAsync(Of T)(obj As T) As Task(Of T)
        Return Await _data.InsertAsync(obj)
    End Function

    Public Async Function DeleteAsync(Of T)(obj As T) As Task(Of Boolean)
        Return Await _data.DeleteAsync(obj)
    End Function

    Public Async Function UpdateAsync(Of T)(obj As T) As Task(Of Boolean)
        Return Await _data.UpdateAsync(obj)
    End Function

    Public Async Function BulkUpdateAsync(Of T)(obj As T, columns As String(), condition As FormattableString) As Task(Of Boolean)
        Return Await _data.BulkUpdateAsync(obj, columns, condition)
    End Function

    Public Async Function GetGuildTargetsAsync(guildId As ULong) As Task(Of List(Of Target))
        Return Await _data.GetListAsync(Of Target)($"{NameOf(Target.GuildId):C}=@GuildIdParam", New With {Key .GuildIdParam = guildId})
    End Function

    Public Async Function GetUserTargetsForGuildAsync(guildId As ULong, userId As ULong) As Task(Of List(Of Target))
        Return Await _data.GetJointListAsync(Of Target, Contract)($"{NameOf(Target.GuildId):C}=@GuildIdParam",
                                                                  New With {Key .GuildIdParam = guildId, Key .UserIdParam = userId},
                                                                  $"{NameOf(Contract.UserId):C}=@UserIdParam")
    End Function

    Public Async Function GetUserContractsForGuildAsync(guildId As ULong, userId As ULong) As Task(Of List(Of Contract))
        Return Await _data.GetJointListAsync(Of Contract, Target)($"{NameOf(Contract.UserId):C}=@UserIdParam",
                                                                  New With {Key .UserIdParam = userId, Key .GuildIdParam = guildId},
                                                                  $"{NameOf(Target.GuildId):C}=@GuildIdParam")
    End Function

    Public Async Function GetSelectedContractsForGuildAsync(guildId As ULong, userId As ULong, targetIds As IEnumerable(Of ULong)) As Task(Of List(Of Contract))
        Dim ids = String.Join(",", targetIds)
        Return Await _data.GetJointListAsync(Of Contract, Target)($"{NameOf(Contract.UserId):C}=@UserIdParam",
                                                                  New With {Key .UserIdParam = userId, Key .GuildIdParam = guildId},
                                                                  $"{NameOf(Target.GuildId):C}=@GuildIdParam AND {NameOf(Target.BotId):C} IN ({ids})")
    End Function
End Class