Imports System.Data
Imports System.Data.SQLite
Imports Dapper.FastCrud
Imports Microsoft.Extensions.Configuration

<Service(ServiceScope.Singleton)>
Public Class DataService
    Private ReadOnly _connectionString As String

    Public Sub New(config As IConfigurationRoot)
        _connectionString = config("connectionString")
        OrmConfiguration.DefaultDialect = SqlDialect.SqLite
    End Sub

    Public Async Function GetByIdAsync(Of T)(entity As T) As Task(Of T)
        Using conn = Await GetOpenConnectionAsync()
            Return Await conn.GetAsync(entity)
        End Using
    End Function

    Public Async Function GetListAsync(Of T)(condition As FormattableString, parameters As Object) As Task(Of List(Of T))
        Using conn = Await GetOpenConnectionAsync()
            Return (Await conn.FindAsync(Of T)(Sub(clause)
                                                   clause.Where(condition)
                                                   clause.WithParameters(parameters)
                                               End Sub)).ToList
        End Using
    End Function

    Public Async Function GetJointListAsync(Of T, T2)(condition As FormattableString, parameters As Object,
                                                      Optional joinCondition As FormattableString = Nothing) As Task(Of List(Of T))
        Using conn = Await GetOpenConnectionAsync()
            Return (Await conn.FindAsync(Of T)(Sub(clause)
                                                   clause.Where(condition)
                                                   clause.WithParameters(parameters)
                                                   clause.Include(Of T2)(Sub(join) join.InnerJoin().Where(joinCondition))
                                               End Sub)).ToList
        End Using
    End Function

    Public Async Function GetJointEntityAsync(Of T, T2)(condition As FormattableString, parameters As Object,
                                                        Optional joinCondition As FormattableString = Nothing) As Task(Of T)
        Using conn = Await GetOpenConnectionAsync()
            Return (Await conn.FindAsync(Of T)(Sub(clause)
                                                   clause.Where(condition)
                                                   clause.WithParameters(parameters)
                                                   clause.Include(Of T2)(Sub(join) join.InnerJoin().Where(joinCondition))
                                               End Sub)).SingleOrDefault
        End Using
    End Function

    Public Async Function InsertAsync(Of T)(entity As T) As Task(Of T)
        Using conn = Await GetOpenConnectionAsync()
            Await Task.Run(Async Function()
                               Await conn.InsertAsync(entity)
                           End Function)
            Return entity
        End Using
    End Function

    Public Async Function UpdateAsync(Of T)(entity As T) As Task(Of Boolean)
        Using conn = Await GetOpenConnectionAsync()
            Return Await conn.UpdateAsync(entity)
        End Using
    End Function

    Public Async Function BulkUpdateAsync(Of T)(entity As T, columns As String(), condition As FormattableString) As Task(Of Boolean)
        Using conn = Await GetOpenConnectionAsync()
            Dim updateMapping = OrmConfiguration.GetDefaultEntityMapping(Of T).Clone().RemoveAllPropertiesExcluding(columns)

            Return (Await conn.BulkUpdateAsync(entity, Sub(clause)
                                                           clause.Where(condition)
                                                           clause.WithEntityMappingOverride(updateMapping)
                                                       End Sub)) <> 0
        End Using
    End Function

    Public Async Function DeleteAsync(Of T)(entity As T) As Task(Of Boolean)
        Using conn = Await GetOpenConnectionAsync()
            Return Await conn.DeleteAsync(entity)
        End Using
    End Function

    Public Async Function BulkDeleteAsync(Of T)(condition As FormattableString) As Task(Of Boolean)
        Using conn = Await GetOpenConnectionAsync()
            Return (Await conn.BulkDeleteAsync(Of T)(Sub(clause) clause.Where(condition))) <> 0
        End Using
    End Function

    Private Async Function GetOpenConnectionAsync() As Task(Of IDbConnection)
        Dim connection As New SQLiteConnection(_connectionString)
        Await connection.OpenAsync
        Return connection
    End Function
End Class