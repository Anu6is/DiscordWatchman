Imports Discord
Imports Discord.Commands

<Hidden>
<RequireOwner>
Public Class OwnerCommands
    Inherits ModuleBase(Of SocketCommandContext)

    <Command("shutdown")>
    <Summary("Close the bot application")>
    Public Async Function Shutdown(Optional exitCode As Integer = 0) As Task
        Await Context.Client.SetStatusAsync(UserStatus.Invisible)
        Environment.Exit(exitCode)
    End Function

    <Command("restart")>
    <Summary("Restart the bot application")>
    <Remarks("This requires an external service to monitor the applications exit code and restart the application when required")>
    Public Async Function Restart() As Task
        Await Shutdown(1)
    End Function

    <Command("exit"), [Alias]("leave")>
    <Summary("Exit the specified guild")>
    Public Async Function ExitGuild(ByVal guild As IGuild) As Task
        Await guild.LeaveAsync()
        Await ReplyAsync($"Exited {guild.Name}")
    End Function

    <Command("status")>
    <Summary("Set the status of the bot")>
    Public Async Function SetStatus(ByVal status As UserStatus) As Task
        Await Context.Client.SetStatusAsync(status)
    End Function
End Class