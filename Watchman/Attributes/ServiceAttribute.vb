<AttributeUsage(AttributeTargets.Class, AllowMultiple:=False, Inherited:=False)>
Public Class ServiceAttribute
    Inherits Attribute

    Public Scope As ServiceScope

    Public Sub New(Optional ByVal serviceScope As ServiceScope = ServiceScope.Singleton)
        Scope = serviceScope
    End Sub
End Class

Public Enum ServiceScope
    Singleton
    Transient
    Scoped
End Enum