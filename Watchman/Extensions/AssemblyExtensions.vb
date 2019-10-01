Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Microsoft.Extensions.DependencyInjection

Module AssemblyExtensions
    <Extension>
    Public Function LoadCustomServices(ByVal assembly As Assembly, ByVal collection As ServiceCollection) As ServiceCollection
        Dim services = assembly.GetTypesWithCustomAttributes(Of ServiceAttribute).ToImmutableArray

        For Each service In services
            If collection.Any(Function(s) s.ServiceType = service) Then Continue For
            Dim attribute = service.GetCustomAttribute(Of ServiceAttribute)

            Select Case attribute.Scope
                Case ServiceScope.Singleton : collection.AddSingleton(service)
                Case ServiceScope.Transient : collection.AddTransient(service)
                Case ServiceScope.Scoped : collection.AddScoped(service)
                Case Else : Throw New ArgumentOutOfRangeException
            End Select
        Next

        Return collection
    End Function

    <Extension>
    Public Function GetTypesWithCustomAttributes(Of T As Attribute)(ByVal assembly As Assembly) As IEnumerable(Of Type)
        Return assembly.GetTypes().Where(Function(type) type.GetCustomAttributes(GetType(T), True).Length > 0).ToImmutableArray
    End Function

    <Extension>
    Public Function GetCustomTypeReaders(ByVal assembly As Assembly) As IEnumerable(Of Type)
        Return assembly.GetTypesWithCustomAttributes(Of TypeReaderAttribute)
    End Function
End Module