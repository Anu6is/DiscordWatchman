<AttributeUsage(AttributeTargets.Class, AllowMultiple:=False, Inherited:=True)>
Public Class ListOrderAttribute
    Inherits Attribute

    Public ReadOnly Property Order As Integer

    Public Sub New(ByVal order As Integer)
        Me.Order = order
    End Sub
End Class