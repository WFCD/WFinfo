Imports System.Drawing.Drawing2D

Public Class Overlay2
    Private brushie As PathGradientBrush
    Private pencil As Pen

    Dim drag As Boolean = False
    Dim mouseX As Integer
    Dim mouseY As Integer

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Dim pnts As Point() = {New Point(0, 0), New Point(100, 0), New Point(100, 100), New Point(0, 100)}
        Dim clrs As Color() = {Color.FromArgb(0, 125, 125, 125), Color.FromArgb(150, 125, 125, 125), Color.FromArgb(150, 125, 125, 125)}
        Dim segs As Single() = {0.0, 0.4, 1.0}


        Dim blender As New ColorBlend()
        blender.Colors = clrs
        blender.Positions = segs

        brushie = New PathGradientBrush(pnts)
        brushie.InterpolationColors = blender

        pencil = New Pen(Color.Red)
    End Sub

    Public Sub Overlay_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.TransparencyKey = Color.Black
        Me.BackColor = Color.Black
    End Sub

    Public Sub Custom_Display(sender As Object, e As PaintEventArgs) Handles Panel1.Paint
        e.Graphics.FillRectangle(brushie, 20, 20, 100, 100)
        e.Graphics.DrawRectangle(pencil, 10, 10, 120, 120)
    End Sub





    Private Sub startDRAGnDROP(sender As Object, e As MouseEventArgs) Handles Panel1.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub DRAGnDROP(sender As Object, e As MouseEventArgs) Handles Panel1.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub stopDRAGnDROP(sender As Object, e As MouseEventArgs) Handles Panel1.MouseUp
        drag = False
    End Sub
End Class


' Source from Rui Godinho Lopes <email>rui@ruilopes.com</email>
' Link: https://www.codeproject.com/Articles/1822/Per-Pixel-Alpha-Blend-in-C
'
' Modified from original source (was c# and changes were needed)
'
Public Class PerPixelAlphaForm
    Inherits Form


End Class