﻿Imports System.Text
Imports Discord
Imports Discord.Commands
Imports Discord.MentionUtils
Imports Discord.UserStatus
Imports Humanizer
Imports Humanizer.Localisation.TimeUnit
Imports Microsoft.Extensions.Configuration

<RequireContext(ContextType.Guild, ErrorMessage:="This command can only be executed in a server")>
<RequireUserPermission(GuildPermission.ManageGuild, ErrorMessage:="You must have the **Manage Guild** permission to use this command")>
Public Class SurveillanceCommands
    Inherits ModuleBase(Of SocketCommandContext)

    Private ReadOnly _data As DataService
    Private ReadOnly _config As IConfigurationRoot
    Private ReadOnly _surveillance As SurveillanceService

    Public Sub New(data As DataService, surveillance As SurveillanceService, config As IConfigurationRoot)
        _data = data
        _config = config
        _surveillance = surveillance
    End Sub

    <Command("monitor")>
    <[Alias]("watch", "track")>
    <Summary("Adds the mentioned bot to your watchlist")>
    <Remarks("monitor @BotToMonitor @SomeOtherBot")>
    Public Async Function MonitorTarget(<OverrideTypeReader(GetType(TargetUserTypeReader))> ParamArray bots() As IGuildUser) As Task
        bots = bots.Where(Function(bot) bot.Id <> Context.Client.CurrentUser.Id).ToArray

        If bots.Count = 0 Then Await ReplyAsync(":ok_hand:") : Return

        Dim guildTargets = Await _data.GetListAsync(Of Target)($"{NameOf(Target.GuildId):C}=@GuildIdParam", New With {Key .GuildIdParam = Context.Guild.Id})
        Dim userTargets = Await _data.GetJointListAsync(Of Target, Contract)($"{NameOf(Target.GuildId):C}=@GuildIdParam",
                                                                             New With {Key .GuildIdParam = Context.Guild.Id, Key .UserIdParam = Context.User.Id},
                                                                             $"{NameOf(Contract.UserId):C}=@UserIdParam")

        Dim duplicates = _surveillance.RemoveDuplicates(userTargets, bots)

        For Each bot In bots
            Await _surveillance.AddContractAsync(Context.User, bot, guildTargets.FirstOrDefault(Function(target) target.BotId = bot.Id))
        Next

        Dim watchlist As New StringBuilder($"Added {String.Join(", ", bots.Select(Function(b) b.Mention))} to the watchlist!{Environment.NewLine}")

        If duplicates.Count > 0 Then watchlist.Append($"Already monitoring: {String.Join(", ", duplicates.Select(Function(d) d.Mention))}")

        Await ReplyAsync(watchlist.ToString)
    End Function

    <Command("ignore")>
    <[Alias]("remove")>
    <Summary("Removes the mentioned bot to your watchlist")>
    <Remarks("remove @BotToMonitor @SomeOtherBot")>
    Public Async Function RemoveTarget(<OverrideTypeReader(GetType(TargetUserTypeReader))> ParamArray bots() As IGuildUser) As Task
        Dim ids = String.Join(",", bots.Select(Function(bot) bot.Id))
        Dim contracts = Await _data.GetJointListAsync(Of Contract, Target)($"{NameOf(Contract.UserId):C}=@UserIdParam",
                                                                           New With {Key .UserIdParam = Context.User.Id, Key .GuildIdParam = Context.Guild.Id},
                                                                           $"{NameOf(Target.GuildId):C}=@GuildIdParam AND {NameOf(Target.BotId):C} IN ({ids})")

        If contracts.Count = 0 Then
            Await ReplyAsync($"No active surveillance contracts found for {String.Join(", ", bots.Select(Function(bot) bot.Mention))}")
            Return
        End If

        Dim removed As New List(Of IGuildUser)
        Dim unmonitored As New List(Of IGuildUser)

        For Each bot In bots
            Dim contract = contracts.FirstOrDefault(Function(c) c.Target.BotId = bot.Id)

            If contract IsNot Nothing Then
                Await _data.DeleteAsync(contract)
                removed.Add(bot)
            Else
                unmonitored.Add(bot)
            End If
        Next

        If removed.Count = 0 Then Await ReplyAsync($"¯\_(ツ)_/¯ - There's no surveillance activity for {String.Join(", ", bots.Select(Function(bot) bot.Mention))}") : Return

        Dim report As New StringBuilder($"Surveillance cancelled for {String.Join(", ", removed.Select(Function(bot) bot.Mention))}")

        If unmonitored.Count > 0 Then report.Append($"There were no surveillance records for {String.Join(", ", unmonitored.Select(Function(bot) bot.Mention))}")

        Await ReplyAsync(report.ToString)
    End Function

    <Command("uptime")>
    <[Alias]("up")>
    <Summary("Retrieve a top 10 list of bots based on uptime")>
    <Remarks("uptime")>
    Public Async Function TopUptime() As Task
        Dim guildTargets = Await _data.GetListAsync(Of Target)($"{NameOf(Target.GuildId):C}=@GuildIdParam", New With {Key .GuildIdParam = Context.Guild.Id})

        If guildTargets.Count = 0 Then Await ReplyAsync("There are no active surveillance contracts for this guild")

        Dim orderedList = guildTargets.Where(Function(target) Context.Guild.GetUser(target.BotId).Status <> Offline).OrderByDescending(Function(target) target.LastOnline).Take(10)

        Dim builder As New EmbedBuilder With {
            .Description = $"{String.Join(Environment.NewLine,
                                          orderedList.Select(Function(target)
                                                                 Dim bot = MentionUser(target.BotId)
                                                                 Dim uptime = DateTimeOffset.UtcNow.Subtract(target.LastOnlineDate).Humanize(4, maxUnit:=Week, minUnit:=Second)
                                                                 Return $"{bot}: {uptime}"
                                                             End Function))}"
        }

        Await ReplyAsync(embed:=builder.Build)
    End Function

    <Command("codename")>
    <[Alias]("alias", "nickname")>
    <Summary("Sets the bot's Nickname in the server")>
    <Remarks("CodeName Secret Squirrel")>
    Public Async Function Nickname(<Remainder> ByVal codename As String) As Task
        If codename.Length > 32 Then Await ReplyAsync("Invalid name. Maximum length is 32 characters") : Return

        Await Context.Guild.CurrentUser.ModifyAsync(Sub(bot) bot.Nickname = codename)
        Await ReplyAsync($"Codename Confirmed! New designation - **{codename}**")
    End Function
End Class