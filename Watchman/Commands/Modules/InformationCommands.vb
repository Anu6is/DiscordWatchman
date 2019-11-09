Imports Discord
Imports Discord.Commands
Imports Humanizer
Imports Humanizer.Localisation

<ListOrder(3)>
<Name("Information Commands")>
Public Class InformationCommands
    Inherits ModuleBase(Of SocketCommandContext)

    Public Const ZERO_WIDTH_SPACE As Char = ChrW(&H200B)
    Private ReadOnly Property Provider As IServiceProvider
    Private ReadOnly Property Commands As CommandService
    Private ReadOnly Property PrefixService As PrefixService

    Public Sub New(commands As CommandService, prefixService As PrefixService, provider As IServiceProvider)
        Me.Commands = commands
        Me.PrefixService = prefixService
        Me.Provider = provider
    End Sub

    <Command("Help")>
    <[Alias]("commands", "command", "cmds", "cmd")>
    <Summary("Displays information about a specific command")>
    <Remarks("help command name")>
    Public Async Function Help() As Task
        Dim builder As New EmbedBuilder With {
            .Title = "Watchman Command List",
            .Description = "**NOTE:** Commands are restricted to users with manage guild permissions",
            .Color = Color.DarkBlue
        }
        Dim orderedModules = Commands.Modules.Where(Function([module]) Not [module].IsSubmodule) _
                                             .OrderBy(Function(info)
                                                          Dim sort As ListOrderAttribute = info.Attributes.FirstOrDefault(Function(a) TypeOf a Is ListOrderAttribute)
                                                          If sort Is Nothing Then Return 999 Else Return sort.Order
                                                      End Function)
        For Each commandModule In orderedModules
            If commandModule.Attributes.Any(Function(attribute) TypeOf attribute Is HiddenAttribute) Then Continue For

            Dim commands As New List(Of String)
            Dim executable As IReadOnlyCollection(Of CommandInfo)

            executable = Await commandModule.GetExecutableCommandsAsync(Context, Provider)

            If executable.Count = 0 Then Continue For

            commands.AddRange(executable.Where(Function(info) info.Attributes.All(Function(a) TypeOf a IsNot HiddenAttribute)) _
                                        .Select(Function(info)
                                                    Dim name = If(info.Module.Group = "", info.Name, $"{info.Module.Group} {info.Name}")
                                                    Return $":white_small_square:**{name}** {info.Summary}"
                                                End Function))

            builder.AddField(commandModule.Name, $"{String.Join(vbNewLine, commands)}{vbNewLine}{ZERO_WIDTH_SPACE}")
        Next

        builder.WithFooter("For additional details on a command, type cr:help <command>")

        Await ReplyAsync(embed:=builder.Build)
    End Function

    <Hidden>
    <Command("Help")>
    <[Alias]("commands", "command", "cmds", "cmd")>
    <Summary("Displays information about a specific command")>
    <Remarks("help command name")>
    Public Async Function Help(<Remainder> ByVal command As CommandInfo) As Task
        Dim parentGroup As String = String.Empty
        Dim group As String = command.Module.Group
        If command.Module.IsSubmodule Then parentGroup = command.Module.Parent.Group
        Dim builder As New EmbedBuilder With {
            .Title = $"Command: {parentGroup} {group} {command.Name}",
            .Description = Format.Italics(command.Summary),
            .Color = Color.DarkBlue
        }

        Dim params = command.Parameters.Select(Function(p) $"<{p.Name}>{If(p.IsOptional, "(optional)", "")}")

        Dim name = $"{parentGroup} {group} {command.Name}"
        Dim prefix = PrefixService.GetPrefix(Context.Guild)

        builder.AddField("Usage", $"`{prefix}{name.Trim} {String.Join(" ", params)}`")
        If Not String.IsNullOrEmpty(command.Remarks) Then builder.AddField("Example", $"`{prefix}{command.Remarks}`")
        If command.Aliases.Count > 1 Then builder.AddField("Can be executed using", String.Join(" | ", command.Aliases))

        Await ReplyAsync(embed:=builder.Build)
    End Function

    <Command("Prefix")>
    <Summary("Set a custom command prefix")>
    <Remarks("prefix !")>
    Public Async Function BotPrefix(Optional prefix As String = Nothing) As Task
        If prefix Is Nothing Then
            Await ReplyAsync($"The current prefix is `{PrefixService.GetPrefix(Context.Guild)}`")
        Else
            Await PrefixService.SetPrefixAsync(Context.Guild, prefix)
            Await ReplyAsync($"The prefix has been updated to `{prefix}`")
        End If
    End Function

    <Command("Ping")>
    <Summary("Check the bot's response time")>
    <Remarks("ping")>
    Public Async Function Ping() As Task
        Dim watch = Stopwatch.StartNew
        Dim message = Await ReplyAsync("Ping")
        watch.Stop()
        Await message.ModifyAsync(Sub(msg) msg.Content = $"Pong ~ {watch.Elapsed.Humanize(2, maxUnit:=TimeUnit.Second, minUnit:=TimeUnit.Millisecond)}")
    End Function
End Class