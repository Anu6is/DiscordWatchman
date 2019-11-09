Imports System.IO
Imports System.Data.SQLite
Imports System.Text
Imports Microsoft.Extensions.Configuration

Public Class DatabaseDesigner
    Private ReadOnly _config As IConfigurationRoot

    Private ReadOnly Property TargetTableCreate As String
        Get
            Dim sql As New StringBuilder()
            'Formatting using string builder is strictly for aesthetics
            With sql
                .Append("CREATE TABLE IF NOT EXISTS Target")
                .Append("([TargetId] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,")
                .Append("[GuildId] INTEGER NOT NULL,")
                .Append("[BotId] INTEGER NOT NULL,")
                .Append("[Status] INTEGER NOT NULL,")
                .Append("[LastOffline] INTEGER DEFAULT ").Append(DateTimeOffset.MaxValue.UtcTicks).Append(",")
                .Append("[LastOnline] INTEGER DEFAULT ").Append(DateTimeOffset.MaxValue.UtcTicks).Append(");")
            End With

            Return sql.ToString
        End Get
    End Property

    Private ReadOnly Property ContractTableCreate As String
        Get
            Dim sql As New StringBuilder()
            'Formatting using string builder is strictly for aesthetics
            With sql
                .Append("CREATE TABLE IF NOT EXISTS Contract")
                .Append("([ContractId] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,")
                .Append("[TargetId] INTEGER NOT NULL,")
                .Append("[UserId] INTEGER NOT NULL,")
                .Append("[Delay] INTEGER NOT NULL,")
                .Append("[DirectMessage] BOOLEAN NOT NULL,")
                .Append("FOREIGN KEY (TargetId)")
                .Append(" REFERENCES Target (TargetId));")
            End With

            Return sql.ToString
        End Get
    End Property

    Private ReadOnly Property GuildAlertTableCreate As String
        Get
            Dim sql As New StringBuilder()
            'Formatting using string builder is strictly for aesthetics
            With sql
                .Append("CREATE TABLE IF NOT EXISTS GuildAlert")
                .Append("([GuildId] INTEGER NOT NULL PRIMARY KEY UNIQUE,")
                .Append("[ChannelId] INTEGER NOT NULL,")
                .Append("[RoleId] INTEGER NOT NULL,")
                .Append("[Delay] INTEGER NOT NULL);")
            End With

            Return sql.ToString
        End Get
    End Property

    Private ReadOnly Property GuildSettingsTableCreate As String
        Get
            Dim sql As New StringBuilder()
            'Formatting using string builder is strictly for aesthetics
            With sql
                .Append("CREATE TABLE IF NOT EXISTS GuildSettings")
                .Append("([GuildId] INTEGER NOT NULL PRIMARY KEY UNIQUE,")
                .Append("[Prefix] TEXT NOT NULL);")
            End With

            Return sql.ToString
        End Get
    End Property

    Private ReadOnly Property TargetTableIndex As String
        Get
            Return "CREATE INDEX IF NOT EXISTS target_ids ON Target (GuildId, BotId);"
        End Get
    End Property

    Private ReadOnly Property ContractTableIndex As String
        Get
            Return "CREATE INDEX IF NOT EXISTS contract_owner ON Contract (UserId);"
        End Get
    End Property

    Private ReadOnly Property ContractTrigger As String
        Get
            Dim sql As New StringBuilder()
            'Formatting using string builder is strictly for aesthetics
            With sql
                .Append("CREATE TRIGGER IF NOT EXISTS delete_target")
                .Append(" AFTER")
                .Append("     DELETE ON Contract")
                .Append(" BEGIN")
                .Append("     DELETE FROM Target")
                .Append("     WHERE TargetId = OLD.TargetId")
                .Append("     AND (SELECT COUNT(*) FROM Contract WHERE TargetId = Old.TargetId) = 0;")
                .Append(" END;")
            End With

            Return sql.ToString
        End Get
    End Property

    Private ReadOnly Property TargetTrigger As String
        Get
            Dim sql As New StringBuilder()
            'Formatting using string builder is strictly for aesthetics
            With sql
                .Append("CREATE TRIGGER IF NOT EXISTS delete_contracts")
                .Append(" AFTER")
                .Append("     DELETE ON Target")
                .Append(" BEGIN")
                .Append("     DELETE FROM Contract")
                .Append("     WHERE TargetId = OLD.TargetId;")
                .Append(" END;")
            End With

            Return sql.ToString
        End Get
    End Property

    Public Sub New(config As IConfigurationRoot)
        _config = config
    End Sub

    Public Sub CreateDb(Optional overwrite As Boolean = False)
        Dim database = _config("database")

        If File.Exists(database) Then
            If Not overwrite Then Return
            File.Delete(database)
        End If

        SQLiteConnection.CreateFile(database)
    End Sub

    Public Sub BuildTables()
        Using connection = New SQLiteConnection(_config("connectionString"))
            Using command As New SQLiteCommand(connection)
                connection.Open()

                command.CommandText = TargetTableCreate : command.ExecuteNonQuery()
                command.CommandText = ContractTableCreate : command.ExecuteNonQuery()
                command.CommandText = GuildAlertTableCreate : command.ExecuteNonQuery()
                command.CommandText = GuildSettingsTableCreate : command.ExecuteNonQuery()

                command.CommandText = TargetTableIndex : command.ExecuteNonQuery()
                command.CommandText = ContractTableIndex : command.ExecuteNonQuery()

                command.CommandText = ContractTrigger : command.ExecuteNonQuery()
                command.CommandText = TargetTrigger : command.ExecuteNonQuery()

                connection.Close()
            End Using
        End Using
    End Sub
End Class