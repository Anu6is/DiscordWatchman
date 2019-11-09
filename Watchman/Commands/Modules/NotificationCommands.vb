Imports Discord
Imports Discord.Commands
Imports Humanizer
Imports Humanizer.Localisation.TimeUnit

<ListOrder(2)>
<Group("Alert")>
<Name("Notification Commands")>
<RequireUserPermission(GuildPermission.ManageGuild)>
Public Class NotificationCommands
    Inherits ModuleBase(Of SocketCommandContext)

    Private ReadOnly _surveillance As SurveillanceService

    Public Sub New(surveillance As SurveillanceService)
        _surveillance = surveillance
    End Sub

    <Name("")>
    <Command>
    <Summary("Disable the specified alert option (channel, role, message)")>
    <Remarks("alert channel disable | alert role disable")>
    Public Async Function Disable(style As AlertType, term As CancellationTerm) As Task
        Select Case style
            Case AlertType.Channel
                If Await _surveillance.DeleteAsync(New GuildAlert With {.GuildId = Context.Guild.Id}) Then
                    Await ReplyAsync("Alert channel removed")
                    Return
                End If
            Case AlertType.Role
                Dim guildAlert = Await _surveillance.GetByIdAsync(New GuildAlert With {.GuildId = Context.Guild.Id})

                If guildAlert IsNot Nothing Then
                    guildAlert.RoleId = 0
                    Await _surveillance.UpdateAsync(guildAlert)
                    Await ReplyAsync("Alert role disabled")
                    Return
                End If
            Case AlertType.Message
                Dim contracts = Await _surveillance.GetUserContractsForGuildAsync(Context.Guild.Id, Context.User.Id)

                If contracts.Count > 0 Then
                    Await _surveillance.BulkUpdateAsync(New Contract With {.DirectMessage = False}, {NameOf(Contract.DirectMessage)},
                                                $"{NameOf(Contract.ContractId):C} IN ({contracts.Select(Function(c) c.ContractId)})")
                    Await ReplyAsync("Direct messages disabled or all contracts")
                End If
        End Select

        Await ReplyAsync($"No alert {style.ToString} exists!")
    End Function

    <Hidden>
    <Command("Override")>
    <Summary("Time delay before offline status updates are sent (Max 10 minutes)")>
    <Remarks("Note: This sets the delay for all your contracts across all guilds")>
    Public Async Function AlertDelay(delay As TimeSpan) As Task
        If delay > TimeSpan.FromMinutes(10) Then delay = TimeSpan.FromMinutes(10)

        If Await _surveillance.BulkUpdateAsync(New Contract With {.Delay = delay.TotalMilliseconds},
                                       {NameOf(Contract.Delay)},
                                       $"{NameOf(Contract.UserId):C}= {Context.User.Id}") Then
            Await ReplyAsync($"You will be notified if any bot goes offline for at least {delay.Humanize(maxUnit:=Minute, minUnit:=Second)}")
        Else
            Await ReplyAsync("There are no bots currently under surveillance")
        End If
    End Function

    <Command("Delay")>
    <Summary("Time delay before offline status updates are sent (Max 10 minutes)")>
    <Remarks("alert delay 10s @MrRobot @Bimo | alert delay 10s #AlertChannel")>
    Public Async Function AlertDelay(delay As TimeSpan, <OverrideTypeReader(GetType(TargetUserTypeReader))> ParamArray bots() As IGuildUser) As Task
        If delay > TimeSpan.FromMinutes(10) Then delay = TimeSpan.FromMinutes(10)

        Dim ids = bots.Select(Function(bot) bot.Id)
        Dim contracts = Await _surveillance.GetSelectedContractsForGuildAsync(Context.Guild.Id, Context.User.Id, ids)
        Dim contractIds = String.Join(",", contracts.Select(Function(contract) contract.ContractId))

        If Await _surveillance.BulkUpdateAsync(New Contract With {.Delay = delay.TotalMilliseconds},
                                       {NameOf(Contract.Delay)},
                                       $"{NameOf(Contract.ContractId):C} IN ({contractIds})") Then
            Await ReplyAsync($"You will be notified if any monitored bot goes offline for at least {delay.Humanize(maxUnit:=Minute, minUnit:=Second)}")
        Else
            Await ReplyAsync("These bots are not currently under surveillance")
        End If
    End Function

    <Hidden>
    <Command("Delay")>
    <Summary("Time delay before offline status updates are sent (Max 10 minutes)")>
    <Remarks("alert delay 10s @MrRobot @Bimo | alert delay 10s #AlertChannel")>
    Public Function AlertDelay(delay As TimeSpan, Optional channel As ITextChannel = Nothing) As Task
        Return AlertChannel(channel, delay)
    End Function

    <Command("Channel")>
    <Summary("Enable alerts in specified channel")>
    <Remarks("alert channel #botAlerts")>
    Public Async Function AlertChannel(Optional channel As ITextChannel = Nothing, Optional delay As TimeSpan = Nothing) As Task
        channel = If(channel, Context.Channel)
        delay = If(delay = Nothing, delay, TimeSpan.FromSeconds(5))

        Dim alert = New GuildAlert With {.GuildId = Context.Guild.Id}

        Dim guildAlert = Await _surveillance.GetByIdAsync(alert)

        If guildAlert Is Nothing Then
            alert.ChannelId = Context.Channel.Id
            alert.Delay = delay.TotalMilliseconds
            Await _surveillance.InsertAsync(alert)
        ElseIf guildAlert.ChannelId <> channel.Id Then
            guildAlert.ChannelId = channel.Id
            Await _surveillance.UpdateAsync(guildAlert)
        Else
            guildAlert.Delay = delay.TotalMilliseconds
            Await _surveillance.UpdateAsync(guildAlert)
            Await ReplyAsync("To disable alerts, use the command `alert channel off`" & Environment.NewLine)
        End If

        Await ReplyAsync($"Bot alerts will now be posted in {channel.Mention}!{Environment.NewLine}Offline delay: {delay.Humanize(2, maxUnit:=Minute, minUnit:=Second)}")
    End Function

    <Command("Role")>
    <Summary("Define a role to be mentioned when status updates are sent")>
    <Remarks("alert role stat updates")>
    Public Async Function AlertRole(<Remainder> ByVal role As IRole) As Task
        Dim alert = New GuildAlert With {.GuildId = Context.Guild.Id}
        Dim guildAlert = Await _surveillance.GetByIdAsync(alert)

        If guildAlert Is Nothing Then
            alert.RoleId = role.Id
            alert.Delay = TimeSpan.FromSeconds(5).TotalMilliseconds
            Await _surveillance.InsertAsync(alert)
        Else
            guildAlert.RoleId = role.Id
            Await _surveillance.UpdateAsync(guildAlert)
        End If

        Dim channelRequired = If(guildAlert.ChannelId = 0, $"{vbNewLine}**An alert channel is required!** Use `toggle alerts` in the desired channel.", String.Empty)

        Await ReplyAsync($"The **{role.Name}** role will be pinged for status updates.{channelRequired}")
    End Function

    <Command("Message")>
    <Summary("Select your preferred notification option")>
    <Remarks("alert message @TargetBot")>
    Public Async Function ToggleDirectMessage(<OverrideTypeReader(GetType(TargetUserTypeReader))> bot As IGuildUser) As Task
        Dim botContracts = Await _surveillance.GetSelectedContractsForGuildAsync(Context.Guild.Id, Context.User.Id, {bot.Id})

        If botContracts.Count = 0 Then Await ReplyAsync("You do not have an existing contract for this bot!") : Return

        Dim botContract = botContracts.First

        botContract.DirectMessage = Not botContract.DirectMessage

        Await _surveillance.UpdateAsync(botContract)

        Await ReplyAsync($"Direct messages **{If(botContract.DirectMessage, "ENABLED", "DISABLED")}** for {bot.Mention}")
    End Function
End Class