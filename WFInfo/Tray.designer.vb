Imports System.Windows.Forms

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Tray
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Tray))
        Me.lbDisplay = New System.Windows.Forms.Label()
        Me.tHide = New System.Windows.Forms.Timer(Me.components)
        Me.tAnimate = New System.Windows.Forms.Timer(Me.components)
        Me.lbDDropShadow = New System.Windows.Forms.Label()
        Me.pbBG = New System.Windows.Forms.PictureBox()
        CType(Me.pbBG, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lbDisplay
        '
        Me.lbDisplay.AutoSize = True
        Me.lbDisplay.BackColor = System.Drawing.Color.Transparent
        Me.lbDisplay.Font = New System.Drawing.Font("DejaVu Sans Condensed", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbDisplay.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbDisplay.Location = New System.Drawing.Point(21, 60)
        Me.lbDisplay.Name = "lbDisplay"
        Me.lbDisplay.Size = New System.Drawing.Size(0, 28)
        Me.lbDisplay.TabIndex = 0
        '
        'tHide
        '
        Me.tHide.Interval = 15000
        '
        'tAnimate
        '
        Me.tAnimate.Interval = 1
        '
        'lbDDropShadow
        '
        Me.lbDDropShadow.AutoSize = True
        Me.lbDDropShadow.BackColor = System.Drawing.Color.Transparent
        Me.lbDDropShadow.Font = New System.Drawing.Font("Cambria", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbDDropShadow.ForeColor = System.Drawing.Color.Black
        Me.lbDDropShadow.Location = New System.Drawing.Point(21, 9)
        Me.lbDDropShadow.Name = "lbDDropShadow"
        Me.lbDDropShadow.Size = New System.Drawing.Size(0, 28)
        Me.lbDDropShadow.TabIndex = 2
        '
        'pbBG
        '
        Me.pbBG.Image = CType(resources.GetObject("pbBG.Image"), System.Drawing.Image)
        Me.pbBG.Location = New System.Drawing.Point(68, 36)
        Me.pbBG.Name = "pbBG"
        Me.pbBG.Size = New System.Drawing.Size(166, 183)
        Me.pbBG.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.pbBG.TabIndex = 1
        Me.pbBG.TabStop = False
        '
        'Tray
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Black
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.ClientSize = New System.Drawing.Size(297, 350)
        Me.Controls.Add(Me.lbDDropShadow)
        Me.Controls.Add(Me.lbDisplay)
        Me.Controls.Add(Me.pbBG)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "Tray"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Overlay"
        Me.TopMost = True
        Me.TransparencyKey = System.Drawing.Color.Black
        CType(Me.pbBG, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents lbDisplay As Label
    Friend WithEvents tHide As Timer
    Friend WithEvents tAnimate As Timer
    Friend WithEvents pbBG As PictureBox
    Friend WithEvents lbDDropShadow As Label
End Class
