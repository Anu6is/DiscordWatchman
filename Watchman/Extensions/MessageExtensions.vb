Imports System.Runtime.CompilerServices
Imports Discord

Module MessageExtensions

    <Extension>
    Public Function Guild(message As IMessage) As IGuild
        Return TryCast(message.Channel, IGuildChannel)?.Guild
    End Function

    <Extension>
    Public Function IsDirectMessage(message As IMessage) As Boolean
        Return TypeOf message.Channel Is IDMChannel
    End Function

    <Extension>
    Public Async Function TryDeleteAsync(message As IMessage) As Task
        If message.IsDirectMessage AndAlso message.Author.IsBot Then Dim ___ = message.DeleteAsync : Return

        Dim guild = message.Guild
        Dim bot = Await guild.GetCurrentUserAsync

        If bot.GetPermissions(message.Channel).Has(ChannelPermission.ManageMessages) Then Dim __ = message.DeleteAsync
    End Function

    <Extension>
    Public Function IsUserMention(ByVal message As IMessage, ByVal user As IUser) As Boolean
        Dim userId As ULong

        If Not MentionUtils.TryParseUser(message.Content.Trim, userId) Then Return False

        Return user.Id = userId
    End Function
End Module