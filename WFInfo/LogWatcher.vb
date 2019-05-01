Imports System.IO

'' Source: https://www.codeproject.com/Tips/371229/Live-Log-Viewer

Public Class LogWatcher
    Inherits FileSystemWatcher

    ''The name of the file to monitor
    Private FileName As String

    ''The FileStream for reading the text from the file
    Public Stream As FileStream
    ''The StreamReader for reading the text from the FileStream
    Public Reader As StreamReader

    ''Constructor for the LogWatcher class
    Public Sub New(filename As String)

        ''Set the filename of the file to watch
        Me.FileName = filename

        ''Create the FileStream And StreamReader object for the file
        Stream = New System.IO.FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        Reader = New System.IO.StreamReader(Stream)

        ''Set the position of the stream to the end of the file
        Stream.Position = Stream.Length
    End Sub


    Public Overloads Sub OnChanged(sender As Object, e As FileSystemEventArgs) Handles Me.Changed

        ''Read the new text from the file
        Dim contents As String = Reader.ReadToEnd()

        ''Fire the TextChanged event
        RaiseEvent TextChanged(Me, contents)
    End Sub

    Public Delegate Sub LogWatcherEventHandler(sender As Object, text As String)

    ''Event that is fired when the file is changed
    Public Event TextChanged As LogWatcherEventHandler
End Class
