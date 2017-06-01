<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Overlay
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Overlay))
        Me.lbDisplay = New System.Windows.Forms.Label()
        Me.tHide = New System.Windows.Forms.Timer(Me.components)
        Me.tAnimate = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'lbDisplay
        '
        Me.lbDisplay.AutoSize = True
        Me.lbDisplay.BackColor = System.Drawing.Color.Transparent
        Me.lbDisplay.Font = New System.Drawing.Font("Cambria", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbDisplay.ForeColor = System.Drawing.Color.FromArgb(CType(CType(147, Byte), Integer), CType(CType(178, Byte), Integer), CType(CType(187, Byte), Integer))
        Me.lbDisplay.Location = New System.Drawing.Point(25, 20)
        Me.lbDisplay.Name = "lbDisplay"
        Me.lbDisplay.Size = New System.Drawing.Size(0, 19)
        Me.lbDisplay.TabIndex = 0
        '
        'tHide
        '
        Me.tHide.Interval = 10000
        '
        'tAnimate
        '
        Me.tAnimate.Interval = 1
        '
        'Overlay
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Black
        Me.BackgroundImage = CType(resources.GetObject("$this.BackgroundImage"), System.Drawing.Image)
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.ClientSize = New System.Drawing.Size(297, 350)
        Me.Controls.Add(Me.lbDisplay)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "Overlay"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Overlay"
        Me.TopMost = True
        Me.TransparencyKey = System.Drawing.SystemColors.HotTrack
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents lbDisplay As Label
    Friend WithEvents tHide As Timer
    Friend WithEvents tAnimate As Timer
End Class
