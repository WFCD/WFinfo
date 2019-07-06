'
' Based on the Source from Rui Godinho Lopes <email>rui@ruilopes.com</email>
' Link: https://www.codeproject.com/Articles/1822/Per-Pixel-Alpha-Blend-in-C
'
' Modified from original c# source into vb.net code
'   Then further modifications for WFInfo uses
'
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

Public Class PerPixelAlphaForm
    Inherits Form

    Dim drag As Boolean = False      ' Toggle for the custom UI allowing it to drag
    Dim mouseX As Integer
    Dim mouseY As Integer

    Public Sub New()
        FormBorderStyle = FormBorderStyle.None
        TopMost = True
        ShowInTaskbar = False
    End Sub

    Private Sub startDRAGnDROP(sender As Object, e As MouseEventArgs) Handles Me.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub DRAGnDROP(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub stopDRAGnDROP(sender As Object, e As MouseEventArgs) Handles Me.MouseUp
        drag = False
    End Sub

    Public Function TestBitmap() As Bitmap
        Dim bmp As New Bitmap(100, 100)

        Dim clrs As Color() = {Color.FromArgb(0, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), Color.FromArgb(150, 255, 255, 255)}
        Dim segs As Single() = {0.0, 0.1, 1.0}
        Dim blender As New ColorBlend()
        blender.Colors = clrs
        blender.Positions = segs

        Dim gp As New GraphicsPath()
        gp.StartFigure()
        gp.AddArc(0, 0, 10, 10, 180, 90)
        gp.AddLine(10, 0, 90, 0)
        gp.AddArc(90, 0, 10, 10, 270, 90)
        gp.AddLine(100, 10, 100, 90)
        gp.AddArc(90, 90, 10, 10, 0, 90)
        gp.AddLine(90, 100, 10, 100)
        gp.AddArc(0, 90, 10, 10, 90, 90)
        gp.AddLine(0, 90, 0, 10)
        gp.CloseFigure()

        Dim brushie As New PathGradientBrush(gp)
        brushie.InterpolationColors = blender

        Using g As Graphics = Graphics.FromImage(bmp)
            g.FillRectangle(brushie, 0, 0, 100, 100)
        End Using
        Return bmp
    End Function


    Public Sub SetBitmap(bitmap As Bitmap, opacity As Byte)
        If bitmap.PixelFormat <> PixelFormat.Format32bppArgb Then
            Throw New ApplicationException("The bitmap must be 32ppp with alpha-channel.")
        End If

        Dim screenDc As IntPtr = GetDC(IntPtr.Zero)
        Dim memDc As IntPtr = CreateCompatibleDC(screenDc)
        Dim hBitmap As IntPtr = IntPtr.Zero
        Dim oldBitmap As IntPtr = IntPtr.Zero

        Try
            hBitmap = bitmap.GetHbitmap(Color.FromArgb(0))
            oldBitmap = SelectObject(memDc, hBitmap)

            Dim Size As New WinSize(bitmap.Width, bitmap.Height)
            Dim pointSource As New WinPoint(0, 0)
            Dim topPos As New WinPoint(Left, Top)
            Dim Blend As New BLENDFUNCTION With {
                .BlendOp = Win32.AC_SRC_OVER,
                .BlendFlags = 0,
                .SourceConstantAlpha = opacity,
                .AlphaFormat = Win32.AC_SRC_ALPHA
            }

            Win32.UpdateLayeredWindow(Handle, screenDc, topPos, Size, memDc, pointSource, 0, Blend, Win32.ULW_ALPHA)
        Finally
            Win32.ReleaseDC(IntPtr.Zero, screenDc)
            If hBitmap <> IntPtr.Zero Then
                Win32.SelectObject(memDc, oldBitmap)
                Win32.DeleteObject(hBitmap)
            End If
            Win32.DeleteDC(memDc)
        End Try
    End Sub

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or &H80000
            Return cp
        End Get
    End Property

    Friend Sub DoTheThings()
        Me.Show()
        Me.SetBitmap(TestBitmap(), 255)
    End Sub
End Class