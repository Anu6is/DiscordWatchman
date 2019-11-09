Imports Discord
Imports Discord.Commands
Imports Discord.WebSocket
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection

<TypeReader(GetType(IUser))>
Public Class TargetUserTypeReader
    Inherits DiscordUserTypeReader

    Public Overrides Async Function ReadAsync(context As ICommandContext, input As String, services As IServiceProvider) As Task(Of TypeReaderResult)
        Dim config As IConfiguration = Nothing
        Dim result = Await MyBase.ReadAsync(context, input, services)

        Using scope = services.CreateScope()
            config = scope.ServiceProvider.GetRequiredService(Of IConfigurationRoot)
        End Using

        If result.IsSuccess Then
            Dim target = DirectCast(result.BestMatch, IUser)

            If target.IsBot Then Return result

            Return TypeReaderResult.FromError(CommandError.ParseFailed, $"Surveillance target [**{target}**] must be a Bot account {config("emote:bot")}")
        Else
            Dim userId As ULong = 0
            Dim restClient = DirectCast(context.Client, DiscordSocketClient).Rest

            If ULong.TryParse(input, userId) OrElse MentionUtils.TryParseUser(input, userId) Then
                Dim guildUser = Await restClient.GetGuildUserAsync(context.Guild.Id, userId)

                If guildUser IsNot Nothing Then
                    If guildUser.IsBot Then Return TypeReaderResult.FromSuccess(guildUser)

                    Return TypeReaderResult.FromError(CommandError.ParseFailed, $"Surveillance target [**{guildUser}**] must be a Bot account {config("emote:bot")}")
                End If
            End If
        End If

        Return TypeReaderResult.FromError(CommandError.ObjectNotFound, $"Requested target is not present in this server {config("emote:spy")}")
    End Function
End Class