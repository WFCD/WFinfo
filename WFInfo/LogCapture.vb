Imports System
Imports System.IO
Imports System.IO.MemoryMappedFiles
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports System.Text
Imports System.Threading

Public Class LogCapture
    Implements IDisposable
    Private memoryMappedFile As MemoryMappedFile = Nothing
    Private bufferReadyEvent As EventWaitHandle = Nothing
    Private dataReadyEvent As EventWaitHandle = Nothing
    Private tokenSource As New CancellationTokenSource()
    Private token As CancellationToken = tokenSource.Token

    Public Sub New()
        Main.addLog("STARTING LogCapture")
        memoryMappedFile = MemoryMappedFile.CreateOrOpen("DBWIN_BUFFER", 4096L)
        Dim created As Boolean

        bufferReadyEvent = New EventWaitHandle(False, EventResetMode.AutoReset, "DBWIN_BUFFER_READY", created)

        If Not created Then
            Main.addLog("The DBWIN_BUFFER_READY event exists.")
            Return
        End If

        dataReadyEvent = New EventWaitHandle(False, EventResetMode.AutoReset, "DBWIN_DATA_READY", created)

        If Not created Then
            Main.addLog("The DBWIN_DATA_READY event exists.")
            Return
        End If

        Task.Factory.StartNew(AddressOf Run)
    End Sub

    Public Sub Run()
        Dim proc As Process = parser2.GetWFProc()
        Try
            Dim Timeout = TimeSpan.FromSeconds(1.0)
            bufferReadyEvent.Set()
            While Not token.IsCancellationRequested

                If Not dataReadyEvent.WaitOne(Timeout) Then
                    Continue While
                End If
                If proc Is Nothing OrElse proc.HasExited Then
                    proc = parser2.GetWFProc()
                End If
                If proc IsNot Nothing Then
                    Using stream = memoryMappedFile.CreateViewStream()
                        Using reader = New BinaryReader(stream, Encoding.Default)
                            Dim processId = reader.ReadUInt32()
                            If processId = proc.Id Then
                                Dim chars = reader.ReadChars(4092)
                                Dim index = Array.IndexOf(chars, Chr(0))
                                Dim message = New String(chars, 0, index)
                                RaiseEvent TextChanged(Me, message.Trim)
                            End If
                        End Using
                    End Using
                End If

                ' The message has been processed, so trigger the
                ' DBWIN_BUFFER_READY event in order to receive the next message
                ' from the process being debugged.
                bufferReadyEvent.Set()
            End While
        Catch ex As Exception
            Console.WriteLine(ex.ToString())
        Finally
            If memoryMappedFile IsNot Nothing Then
                memoryMappedFile.Dispose()
            End If

            If bufferReadyEvent IsNot Nothing Then
                bufferReadyEvent.Dispose()
            End If

            If dataReadyEvent IsNot Nothing Then
                dataReadyEvent.Dispose()
            End If
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        tokenSource.Cancel()
        tokenSource.Dispose()
        Main.addLog("STOPPING LogCapture")
    End Sub

    Public Delegate Sub LogWatcherEventHandler(sender As Object, text As String)

    ''Event that is fired when the file is changed
    Public Event TextChanged As LogWatcherEventHandler
End Class
