Module Program
    Sub Main(args As String())
        Watchman.StartAsync().GetAwaiter().GetResult()
    End Sub
End Module