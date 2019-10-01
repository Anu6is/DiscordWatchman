Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("Target")>
Public Class Target
    <Key>
    <DatabaseGenerated(DatabaseGeneratedOption.Identity)>
    Public Property TargetId As Integer
    Public Property GuildId As ULong
    Public Property BotId As ULong
    Public Property Status As Discord.UserStatus
    Public Property LastOffline As Long = DateTimeOffset.MaxValue.UtcTicks
    Public Property LastOnline As Long

    Public Property Contracts As IEnumerable(Of Contract)

    <NotMapped>
    Public ReadOnly Property LastOfflineDate As DateTimeOffset
        Get
            Return New DateTimeOffset(LastOffline, DateTimeOffset.UtcNow.Offset)
        End Get
    End Property

    <NotMapped>
    Public ReadOnly Property LastOnlineDate As DateTimeOffset
        Get
            Return New DateTimeOffset(LastOnline, DateTimeOffset.UtcNow.Offset)
        End Get
    End Property
End Class