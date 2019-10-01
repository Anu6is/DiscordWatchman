Imports Discord
Imports Discord.Commands
Imports Humanizer
Imports Humanizer.Localisation.TimeUnit

<Group("alert")>
Public Class NotificationCommands
    Inherits ModuleBase(Of SocketCommandContext)

    Private ReadOnly _data As DataService
    Private ReadOnly _surveillance As SurveillanceService

    Public Sub New(data As DataService)
        _data = data
    End Sub

    <Hidden>
    <Command("delay")>
    <Summary("Time delay before offline status updates are sent (Max 10 minutes)")>
    <Remarks("Note: This sets the delay for all your contracts across all guilds")>
    Public Async Function AlertDelay(delay As TimeSpan) As Task
        If delay > TimeSpan.FromMinutes(10) Then delay = TimeSpan.FromMinutes(10)

        If Await _data.BulkUpdateAsync(New Contract With {.Delay = delay.TotalMilliseconds},
                                       {NameOf(Contract.Delay)},
                                       $"{NameOf(Contract.UserId):C}= {Context.User.Id}") Then
            Await ReplyAsync($"You will be notified if any bot goes offline for at least {delay.Humanize(maxUnit:=Minute, minUnit:=Second)}")
        Else
            Await ReplyAsync("There are no bots currently under surveillance")
        End If
    End Function

    <Command("delay")>
    <Summary("Time delay before offline status updates are sent (Max 10 minutes)")>
    <Remarks("alert delay @MrRobot @Bimo 10s")>
    Public Async Function AlertDelay(delay As TimeSpan, <OverrideTypeReader(GetType(TargetUserTypeReader))> ParamArray bots() As IGuildUser) As Task
        If delay > TimeSpan.FromMinutes(10) Then delay = TimeSpan.FromMinutes(10)

        Dim ids = String.Join(",", bots.Select(Function(bot) bot.Id))
        Dim contracts = Await _data.GetJointListAsync(Of Contract, Target)($"{NameOf(Contract.UserId):C}=@UserIdParam",
                                                                           New With {Key .UserIdParam = Context.User.Id, Key .GuildIdParam = Context.Guild.Id},
                                                                           $"{NameOf(Target.GuildId):C}=@GuildIdParam AND {NameOf(Target.BotId):C} IN ({ids})")

        Dim contractIds = String.Join(",", contracts.Select(Function(contract) contract.ContractId))
        If Await _data.BulkUpdateAsync(New Contract With {.Delay = delay.TotalMilliseconds},
                               {NameOf(Contract.Delay)},
                               $"{NameOf(Contract.ContractId):C} IN ({contractIds})") Then
            Await ReplyAsync($"You will be notified if any monitored bot goes offline for at least {delay.Humanize(maxUnit:=Minute, minUnit:=Second)}")
        Else
            Await ReplyAsync("These bots are not currently under surveillance")
        End If
    End Function

    <Command("channel")>
    <Summary("Enable alerts in specified channel")>
    <Remarks("alert channel #botAlerts")>
    Public Async Function AlertChannel(Optional channel As ITextChannel = Nothing) As Task
        channel = If(channel, Context.Channel)
    End Function

    Public Async Function AlertRole() As Task

    End Function

    Public Async Function ToggleDirectMessage() As Task

    End Function
End Class