<AttributeUsage(AttributeTargets.Class, AllowMultiple:=False, Inherited:=False)>
Public Class TypeReaderAttribute
    Inherits Attribute

    Public ReadOnly Property TargetType As Type

    Public Sub New(ByVal targetType As Type)
        Me.TargetType = targetType
    End Sub
End Class