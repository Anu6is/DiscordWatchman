Imports Discord
Imports Discord.Commands
Imports Discord.WebSocket
Imports Discord.Commands.CommandError

<Service(ServiceScope.Singleton)>
Public Class LogService
    Private ReadOnly Client As DiscordSocketClient
    Private ReadOnly Commands As CommandService
    Private ReadOnly LogLock As New Object

    Public Sub New(ByVal client As DiscordSocketClient, ByVal commands As CommandService)
        Me.Client = client
        Me.Commands = commands

        AddHandler client.Log, AddressOf ClientLog
        AddHandler commands.CommandExecuted, AddressOf CommandExecuted
    End Sub

    Private Function ClientLog(message As LogMessage) As Task
        SyncLock LogLock
            If message.Severity > LogSeverity.Error Then Log(message.Severity, message.Source, message.Message) Else Log(message)
        End SyncLock

        Return Task.CompletedTask
    End Function

    Private Function CommandExecuted(info As [Optional](Of CommandInfo), context As ICommandContext, result As IResult) As Task
        If result.IsSuccess Then Return Task.CompletedTask

        SyncLock LogLock
            Select Case result.Error.Value
                Case UnknownCommand : Return Task.CompletedTask
                Case ParseFailed, BadArgCount, ObjectNotFound, MultipleMatches, UnmetPrecondition : context.Channel.SendMessageAsync(result.ErrorReason)
                Case Exception : Log(LogSeverity.Error, "Command", DirectCast(result, ExecuteResult).Exception.ToString)
                Case Unsuccessful : Return Task.CompletedTask
            End Select
        End SyncLock

        Return Task.CompletedTask
    End Function

    Public Sub LogDebug(ByVal source As String, ByVal message As String)
        SyncLock LogLock
            Log(LogSeverity.Debug, source, message)
        End SyncLock
    End Sub

    Public Sub LogInfo(ByVal source As String, ByVal message As String)
        SyncLock LogLock
            Log(LogSeverity.Info, source, message)
        End SyncLock
    End Sub

    Public Sub LogError(ByVal source As String, ByVal message As String)
        SyncLock LogLock
            Log(LogSeverity.Error, source, message)
        End SyncLock
    End Sub
End Class