Imports Discord
Imports Discord.Commands
Imports Discord.WebSocket

<TypeReader(GetType(IUser))>
Public Class DiscordUserTypeReader
    Inherits UserTypeReader(Of IUser)

    Public Overrides Async Function ReadAsync(context As ICommandContext,
                                               input As String,
                                               services As IServiceProvider) As Task(Of TypeReaderResult)
        Dim result = Await MyBase.ReadAsync(context, input, services)
        If result.IsSuccess Then Return result

        Dim restClient = DirectCast(context.Client, DiscordSocketClient).Rest

        Dim userId As ULong = 0

        If ULong.TryParse(input, userId) OrElse MentionUtils.TryParseUser(input, userId) Then
            Dim guildUser = Await restClient.GetGuildUserAsync(context.Guild.Id, userId)
            If guildUser IsNot Nothing Then Return TypeReaderResult.FromSuccess(guildUser)

            Dim user = Await restClient.GetUserAsync(userId)

            Return If(user IsNot Nothing, TypeReaderResult.FromSuccess(user), result)
        End If

        Return result
    End Function
End Class