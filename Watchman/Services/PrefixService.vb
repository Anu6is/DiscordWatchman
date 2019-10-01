Imports Discord
Imports Microsoft.Extensions.Configuration

<Service(ServiceScope.Singleton)>
Public Class PrefixService
    Private ReadOnly _prefix As String
    Private ReadOnly _prefixes As New Dictionary(Of ULong, String)

    Public Sub New(config As IConfigurationRoot)
        _prefix = config("prefix")
    End Sub

    Public Function GetPrefix(guild As IGuild) As String
        If guild Is Nothing Then Return _prefix

        _prefixes.TryGetValue(guild.Id, _prefix)

        Return _prefix
    End Function
End Class