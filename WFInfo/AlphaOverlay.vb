'
' Based on the Source from Rui Godinho Lopes <email>rui@ruilopes.com</email>
' Link: https://www.codeproject.com/Articles/1822/Per-Pixel-Alpha-Blend-in-C
'
' Modified from original c# source into vb.net code
'   Then further modifications for WFInfo uses
'
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

Public Class AlphaOverlay
    Inherits Form

    Friend WithEvents tHide As Timer
    Private savedPos As Point
    Public Shared topBrush As Brush = New SolidBrush(Color.FromArgb(130, 140, 150))

    Private components As System.ComponentModel.IContainer

    Public Sub New()
        InitializeComponent()
        FormBorderStyle = FormBorderStyle.None
        TopMost = True
        ShowInTaskbar = False
    End Sub

    Public Function TestBitmap(partName As String, wid As Integer) As Bitmap
        Dim pad As Integer = 15 * parser2.totalScaling
        Dim curve As Integer = 50 * parser2.totalScaling

        Dim font As New System.Drawing.Font(privateFonts.Families(0), CInt(14 * parser2.totalScaling))
        Dim smol As New System.Drawing.Font(privateFonts.Families(0), CInt(11 * parser2.totalScaling))
        Dim strFormat As New StringFormat()
        strFormat.Alignment = StringAlignment.Center
        Dim flipFormat As New StringFormat()
        flipFormat.Alignment = StringAlignment.Far


        Dim job As Newtonsoft.Json.Linq.JObject = db.market_data(partName)
        Dim valuted As Boolean = (Not partName.Equals("Forma Blueprint")) AndAlso db.IsPartVaulted(partName)
        Dim owned As String = ""
        If Not partName.Equals("Forma Blueprint") Then
            owned = db.IsPartOwned(partName) & " OWNED"
            If owned.Chars(0) = CChar("0") Then
                owned = ""
            End If
        End If

        Dim volumeTxt As String = job("volume").ToString() & " sold last 48hrs"
        Dim platWid As Integer = 0
        Dim ducatWid As Integer = 0
        Dim smolWid As Integer = 0
        Dim textSep As Single = 0
        Dim oneLine As Single = 0
        Dim hei As Integer = 0

        Using fake As New Bitmap(1, 1)
            Using g As Graphics = Graphics.FromImage(fake)

                textSep = g.MeasureString(partName, font, wid - pad * 2, strFormat).Height
                oneLine = g.MeasureString("text", font, wid - pad * 2, strFormat).Height
                smolWid = g.MeasureString("text", smol).Height
                hei = pad * 2 + textSep * 1.5 + oneLine * 3
                platWid = oneLine * 0.8 + g.MeasureString(job("plat").ToString(), font).Width
                ducatWid = oneLine * 0.8 + g.MeasureString(job("ducats").ToString(), font).Width

            End Using
        End Using

        Dim layout As New RectangleF(pad, pad + smolWid, wid - pad * 2, hei - pad * 2)

        Dim bmp As New Bitmap(wid, hei)

        Dim clrs As Color() = {Color.FromArgb(0, 0, 0, 0), Color.FromArgb(150, 0, 0, 0), Color.FromArgb(150, 0, 0, 0)}
        Dim segs As Single() = {0.0, pad * 2 / wid, 1.0}
        Dim blender As New ColorBlend()
        blender.Colors = clrs
        blender.Positions = segs

        Dim gp As New GraphicsPath()
        gp.StartFigure()

        ' 0,0 w/size 40,40      top-left
        gp.AddArc(0, 0, curve, curve, 180, 90)
        ' 60,0 w/size 40,40     top-right
        gp.AddArc(wid - curve, 0, curve, curve, -90, 90)
        ' 60,60 w/size 40,40    bottom-right
        gp.AddArc(wid - curve, hei - curve, curve, curve, 0, 90)
        ' 0,60 w/size 40,40     bottom-left
        gp.AddArc(0, hei - curve, curve, curve, 90, 90)

        gp.CloseFigure()

        Using g As Graphics = Graphics.FromImage(bmp)
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic

            Using brushie As New PathGradientBrush(gp)
                brushie.InterpolationColors = blender
                brushie.CenterPoint = New PointF(wid / 2, hei / 2)

                g.FillRectangle(brushie, 0, 0, wid, hei)
                'g.DrawRectangle(New Pen(Glob.rareBrush), New Rectangle(pad, pad, wid - pad * 2, hei - pad * 2))

                If valuted Then
                    g.DrawString("VAULTED", smol, topBrush, wid - pad * 1.5, pad * 0.75, flipFormat)
                End If
                g.DrawString(owned, smol, topBrush, pad * 1.5, pad * 0.75)

                g.DrawString(partName, font, Glob.textBrush, layout, strFormat)
                layout.Y += textSep * 1.5

                Dim imgY As Single = layout.Y + oneLine / 20

                Dim tempDim As Single = oneLine * 0.8
                Dim platX As Single = wid / 3 - platWid / 2 + 1

                g.DrawImage(My.Resources.plat, platX, imgY, tempDim, tempDim)
                g.DrawString(job("plat").ToString(), font, textBrush, platX + tempDim + 1, layout.Y)

                Dim ducatX As Single = 2 * wid / 3 - platWid / 2 + 1
                g.DrawImage(My.Resources.ducat_w, ducatX, imgY, tempDim, tempDim)
                g.DrawString(job("ducats").ToString(), font, textBrush, ducatX + tempDim + 1, layout.Y)


                'g.DrawString(job("plat"), font, Glob.textBrush, layout, strFormat)
                layout.Y += oneLine
                g.DrawString(volumeTxt, font, Glob.textBrush, layout, strFormat)

            End Using
        End Using

        Return bmp
    End Function


    Public Sub SetBitmap(bitmap As Bitmap)
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
            Dim posPoint As New WinPoint(savedPos.X, savedPos.Y)
            Dim pointSource As New WinPoint(0, 0)
            Dim Blend As New BLENDFUNCTION With {
                .BlendOp = Win32.AC_SRC_OVER,
                .BlendFlags = 0,
                .SourceConstantAlpha = 255,
                .AlphaFormat = Win32.AC_SRC_ALPHA
            }

            Win32.UpdateLayeredWindow(Handle, screenDc, posPoint, Size, memDc, pointSource, 0, Blend, Win32.ULW_ALPHA)
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

    '("Loki Prime Systems", New Point(100, 100), 200)
    Public Sub ShowAtLocation(partName As String, pos As Point, wid As Integer)
        Me.Show()
        savedPos = pos
        Me.SetBitmap(TestBitmap(partName, wid))
    End Sub

    Private Sub tHide_Tick(sender As Object, e As EventArgs) Handles tHide.Tick
        Me.Hide()
        tHide.Stop()
    End Sub

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.tHide = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'tHide
        '
        Me.tHide.Interval = 10000
        '
        'PerPixelAlphaForm
        '
        Me.ClientSize = New System.Drawing.Size(284, 262)
        Me.Name = "AlphaOverlay"
        Me.ResumeLayout(False)

    End Sub
End Class