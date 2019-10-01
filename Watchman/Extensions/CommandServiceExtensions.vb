Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Discord.Commands

Module CommandServiceExtensions
    <Extension>
    Public Function RegisterTypeReadersAsync(commands As CommandService, assembly As Assembly) As Task
        Dim typeReaders = assembly.GetCustomTypeReaders

        For Each typeReader In typeReaders
            Dim attribute = typeReader.GetCustomAttribute(Of TypeReaderAttribute)

            If attribute Is Nothing Then Continue For

            commands.AddTypeReader(attribute.TargetType, DirectCast(Activator.CreateInstance(typeReader), TypeReader))
        Next

        Return Task.CompletedTask
    End Function
End Module