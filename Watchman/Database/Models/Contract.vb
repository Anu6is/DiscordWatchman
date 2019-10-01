Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("Contract")>
Public Class Contract
    <Key>
    <DatabaseGenerated(DatabaseGeneratedOption.Identity)>
    Public Property ContractId As Integer
    Public Property UserId As ULong
    Public Property DirectMessage As Boolean = True
    Public Property Delay As Long = TimeSpan.FromSeconds(5).Ticks
    <ForeignKey("Target")>
    Public Property TargetId As Integer
    Public Property Target As Target
End Class