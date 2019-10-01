Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("GuildAlert")>
Public Class GuildAlert
    <Key, Required>
    Public Property GuildId As ULong
    Public Property ChannelId As ULong
    Public Property RoleId As ULong
    Public Property Delay As Long
End Class