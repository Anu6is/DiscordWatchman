Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("GuildSettings")>
Public Class GuildSettings
    <Key, Required>
    Public Property GuildId As ULong
    Public Property Prefix As String
End Class
