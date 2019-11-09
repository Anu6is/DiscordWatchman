Imports Discord.Commands

<TypeReader(GetType(CommandInfo))>
Public Class CommandInfoTypeReader
    Inherits TypeReader

    Public Overrides Function ReadAsync(context As ICommandContext, input As String, services As IServiceProvider) As Task(Of TypeReaderResult)
        Dim cmdService As CommandService = services.GetService(GetType(CommandService))
        Dim cmd = cmdService.Commands.FirstOrDefault(Function(info)
                                                         Return info.Aliases.Any(Function([alias])
                                                                                     Return [alias].Equals(input, StringComparison.OrdinalIgnoreCase)
                                                                                 End Function)
                                                     End Function)
        If cmd Is Nothing Then Return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, "Command not found"))

        Return Task.FromResult(TypeReaderResult.FromSuccess(cmd))
    End Function
End Class