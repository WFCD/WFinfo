Imports System
Imports System.Windows.Forms
Imports System.Runtime.InteropServices

Public Class DoubleBufferedTreeView
    Inherits TreeView

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        Win32.SendMessage(Me.Handle, TVM_SETEXTENDEDSTYLE, TVS_EX_DOUBLEBUFFER, TVS_EX_DOUBLEBUFFER)
        MyBase.OnHandleCreated(e)
    End Sub


    Private Const TVM_SETEXTENDEDSTYLE As Integer = &H1100 + 44
    Private Const TVS_EX_DOUBLEBUFFER As Integer = &H4
End Class