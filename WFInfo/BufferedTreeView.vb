Imports System
Imports System.Windows.Forms
Imports System.Runtime.InteropServices

Public Class DoubleBufferedTreeView
    Inherits TreeView

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        SendMessage(Me.Handle, TVM_SETEXTENDEDSTYLE, TVS_EX_DOUBLEBUFFER, TVS_EX_DOUBLEBUFFER)
        MyBase.OnHandleCreated(e)
    End Sub


    Private Const TVM_SETEXTENDEDSTYLE As Integer = &H1100 + 44
    Private Const TVS_EX_DOUBLEBUFFER As Integer = &H4
    <DllImport("User32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wp As IntPtr, lp As IntPtr) As IntPtr
    End Function
End Class