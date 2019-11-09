Imports System.Threading
Imports Discord
Imports Discord.WebSocket

<Service(ServiceScope.Singleton)>
Public Class WatchDogService
    Private ReadOnly Property Client As DiscordSocketClient
    Private ReadOnly Property Timeout As TimeSpan = TimeSpan.FromSeconds(20)

    Private _cancellationToken As CancellationTokenSource

    Public Sub New(client As DiscordSocketClient)
        Me.Client = client
        _cancellationToken = New CancellationTokenSource()
    End Sub

    Public Sub Initialize()
        AddHandler Client.Connected, AddressOf Connected
        AddHandler Client.Disconnected, AddressOf Disconnected
        AddHandler Client.LatencyUpdated, AddressOf LatencyUpdated
    End Sub

    Private Async Function LatencyUpdated(previous As Integer, current As Integer) As Task
        Select Case current
            Case >= 1000 : If Client.Status <> UserStatus.DoNotDisturb Then Await Client.SetStatusAsync(UserStatus.DoNotDisturb)
            Case >= 300 : If Client.Status <> UserStatus.Idle Then Await Client.SetStatusAsync(UserStatus.Idle)
            Case < 300 : If Client.Status <> UserStatus.Online Then Await Client.SetStatusAsync(UserStatus.Online)
        End Select
    End Function

    Private Function Connected() As Task
        _cancellationToken.Cancel()
        _cancellationToken = New CancellationTokenSource()
        Return Task.CompletedTask
    End Function

    Private Function Disconnected(arg As Exception) As Task
        Dim client = Me.Client
        Dim delay = Task.Delay(Timeout, _cancellationToken.Token).ContinueWith(Async Function(tsk)
                                                                                   Await CheckStateAsync(client)
                                                                               End Function)
        Return Task.CompletedTask
    End Function

    Private Async Function CheckStateAsync(client As DiscordSocketClient) As Task
        If client.ConnectionState = ConnectionState.Connected AndAlso client.Status = UserStatus.Online Then Return
        If client.ConnectionState = ConnectionState.Connected AndAlso client.Status <> UserStatus.Online Then
            Await LatencyUpdated(client.Latency, client.Latency)
            Return
        End If

        Dim timeout = Task.Delay(Me.Timeout)
        Dim connect = client.StartAsync()
        Dim reset = Await Task.WhenAny(timeout, connect)

        If reset.Equals(timeout) OrElse connect.IsFaulted Then
            FailFast()
        ElseIf client.ConnectionState = ConnectionState.Connected Then
            Return
        End If

        FailFast()
    End Function

    Private Sub FailFast()
        Environment.Exit(1)
    End Sub
End Class
