Imports Discord
Imports Discord.Commands

<TypeReader(GetType(IGuild))>
Public Class GuildTypeReader
    Inherits TypeReader

    Public Overrides Async Function ReadAsync(context As ICommandContext, input As String, services As IServiceProvider) As Task(Of TypeReaderResult)
        Dim result As IGuild = Nothing
        Dim guildId As ULong = 0

        If ULong.TryParse(input, guildId) Then
            result = Await context.Client.GetGuildAsync(guildId)
        End If

        If result IsNot Nothing Then Return TypeReaderResult.FromSuccess(result)

        Dim guilds = Await context.Client.GetGuildsAsync()
        Dim guild = guilds.FirstOrDefault(Function(g) g.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase))

        If result IsNot Nothing Then Return TypeReaderResult.FromSuccess(result)

        Return TypeReaderResult.FromError(CommandError.ObjectNotFound, "Guild not found.")
    End Function
End Class