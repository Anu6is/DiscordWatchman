Imports System.ConsoleColor
Imports Discord
Imports Discord.Commands

Module ColorConsole
    Private Const date_time_format As String = "MM/dd hh:mm:ss tt"

    Public Sub Append(text As String, Optional foreground As ConsoleColor = White, Optional background As ConsoleColor = Black)
        Console.ForegroundColor = foreground
        Console.BackgroundColor = background

        Console.Write(text)
    End Sub

    Public Sub WriteLine(Optional text As String = "", Optional foreground As ConsoleColor = White, Optional background As ConsoleColor = Black)
        Console.ForegroundColor = foreground
        Console.BackgroundColor = background

        Console.Write(Environment.NewLine & text)
    End Sub

    Public Sub Log(severity As LogSeverity, source As String, message As String)
        ColorConsole.WriteLine($"{DateTimeOffset.Now.ToString(date_time_format)} ", DarkGray)
        ColorConsole.Append($"[{severity,-8}] ", GetSeverityColor(severity))
        ColorConsole.Append($"[{source,-7}]: ", DarkGreen)
        ColorConsole.Append(message, White)
    End Sub

    Public Sub Log(ctx As ICommandContext)
        ColorConsole.WriteLine($"{DateTimeOffset.Now.ToString(date_time_format)} ", Gray)

        If TypeOf ctx.Message.Channel Is IDMChannel Then Append($"[DM] ", Magenta) Else Append($"[{ctx.Guild.Name} | #{ctx.Channel.Name}] ", DarkGreen)

        ColorConsole.Append($"{ctx.User}: ", Green)
        ColorConsole.Append(ctx.Message.Content, White)
    End Sub

    Public Sub Log(message As LogMessage)
        ColorConsole.WriteLine(message.ToString())
    End Sub

    Private Function GetSeverityColor(ByVal severity As LogSeverity) As ConsoleColor
        Select Case severity
            Case LogSeverity.Critical
                Return DarkRed
            Case LogSeverity.[Error]
                Return Red
            Case LogSeverity.Warning
                Return Yellow
            Case LogSeverity.Info
                Return Cyan
            Case LogSeverity.Verbose
                Return DarkCyan
            Case LogSeverity.Debug
                Return DarkMagenta
            Case Else
                Return White
        End Select
    End Function
End Module