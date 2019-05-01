Imports System.Windows.Forms

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Picker
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Picker))
        Me.lbSideBar = New System.Windows.Forms.Label()
        Me.lbText = New System.Windows.Forms.Label()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.pbBackground = New System.Windows.Forms.PictureBox()
        Me.pbTray = New System.Windows.Forms.PictureBox()
        Me.lbTray = New System.Windows.Forms.Label()
        Me.lbBackground = New System.Windows.Forms.Label()
        Me.pbTitleBar = New System.Windows.Forms.PictureBox()
        Me.lbTitleBar = New System.Windows.Forms.Label()
        Me.pbText = New System.Windows.Forms.PictureBox()
        Me.pbSideBar = New System.Windows.Forms.PictureBox()
        Me.ColorPicker = New System.Windows.Forms.ColorDialog()
        Me.tGetColor = New System.Windows.Forms.Timer(Me.components)
        Me.tGetShade = New System.Windows.Forms.Timer(Me.components)
        Me.pbPreview = New System.Windows.Forms.PictureBox()
        Me.pbColorWheel = New System.Windows.Forms.PictureBox()
        Me.pbColorWheelBG = New System.Windows.Forms.PictureBox()
        Me.pbSliderFG = New System.Windows.Forms.PictureBox()
        Me.pbSliderBG = New System.Windows.Forms.PictureBox()
        Me.pTitle = New System.Windows.Forms.Panel()
        Me.pbIcon = New System.Windows.Forms.PictureBox()
        Me.lbTitle = New System.Windows.Forms.Label()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.btnSet = New System.Windows.Forms.Button()
        Me.Panel3 = New System.Windows.Forms.Panel()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnDefault = New System.Windows.Forms.Button()
        Me.Panel4 = New System.Windows.Forms.Panel()
        Me.Panel1.SuspendLayout()
        CType(Me.pbBackground, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbTray, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbTitleBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbText, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbSideBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbPreview, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbColorWheel, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbColorWheelBG, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbSliderFG, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbSliderBG, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.pTitle.SuspendLayout()
        CType(Me.pbIcon, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel2.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.Panel4.SuspendLayout()
        Me.SuspendLayout()
        '
        'lbSideBar
        '
        Me.lbSideBar.AutoSize = True
        Me.lbSideBar.Font = New System.Drawing.Font("Calibri", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbSideBar.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbSideBar.Location = New System.Drawing.Point(48, 82)
        Me.lbSideBar.Name = "lbSideBar"
        Me.lbSideBar.Size = New System.Drawing.Size(58, 18)
        Me.lbSideBar.TabIndex = 10
        Me.lbSideBar.Text = "Side Bar"
        '
        'lbText
        '
        Me.lbText.AutoSize = True
        Me.lbText.Font = New System.Drawing.Font("Calibri", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbText.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbText.Location = New System.Drawing.Point(48, 117)
        Me.lbText.Name = "lbText"
        Me.lbText.Size = New System.Drawing.Size(34, 18)
        Me.lbText.TabIndex = 11
        Me.lbText.Text = "Text"
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer))
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.pbBackground)
        Me.Panel1.Controls.Add(Me.pbTray)
        Me.Panel1.Controls.Add(Me.lbTray)
        Me.Panel1.Controls.Add(Me.lbBackground)
        Me.Panel1.Controls.Add(Me.pbTitleBar)
        Me.Panel1.Controls.Add(Me.lbTitleBar)
        Me.Panel1.Controls.Add(Me.pbText)
        Me.Panel1.Controls.Add(Me.lbSideBar)
        Me.Panel1.Controls.Add(Me.pbSideBar)
        Me.Panel1.Controls.Add(Me.lbText)
        Me.Panel1.Location = New System.Drawing.Point(298, 6)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(142, 185)
        Me.Panel1.TabIndex = 16
        '
        'pbBackground
        '
        Me.pbBackground.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer))
        Me.pbBackground.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pbBackground.Location = New System.Drawing.Point(20, 47)
        Me.pbBackground.Name = "pbBackground"
        Me.pbBackground.Size = New System.Drawing.Size(20, 20)
        Me.pbBackground.TabIndex = 22
        Me.pbBackground.TabStop = False
        '
        'pbTray
        '
        Me.pbTray.BackColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(203, Byte), Integer), CType(CType(215, Byte), Integer))
        Me.pbTray.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pbTray.Location = New System.Drawing.Point(20, 152)
        Me.pbTray.Name = "pbTray"
        Me.pbTray.Size = New System.Drawing.Size(20, 20)
        Me.pbTray.TabIndex = 18
        Me.pbTray.TabStop = False
        '
        'lbTray
        '
        Me.lbTray.AutoSize = True
        Me.lbTray.Font = New System.Drawing.Font("Calibri", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbTray.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbTray.Location = New System.Drawing.Point(48, 152)
        Me.lbTray.Name = "lbTray"
        Me.lbTray.Size = New System.Drawing.Size(33, 18)
        Me.lbTray.TabIndex = 17
        Me.lbTray.Text = "Tray"
        '
        'lbBackground
        '
        Me.lbBackground.AutoSize = True
        Me.lbBackground.Font = New System.Drawing.Font("Calibri", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbBackground.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbBackground.Location = New System.Drawing.Point(48, 47)
        Me.lbBackground.Name = "lbBackground"
        Me.lbBackground.Size = New System.Drawing.Size(80, 18)
        Me.lbBackground.TabIndex = 21
        Me.lbBackground.Text = "Background"
        '
        'pbTitleBar
        '
        Me.pbTitleBar.BackColor = System.Drawing.Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.pbTitleBar.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.pbTitleBar.Location = New System.Drawing.Point(20, 12)
        Me.pbTitleBar.Name = "pbTitleBar"
        Me.pbTitleBar.Size = New System.Drawing.Size(20, 20)
        Me.pbTitleBar.TabIndex = 20
        Me.pbTitleBar.TabStop = False
        '
        'lbTitleBar
        '
        Me.lbTitleBar.AutoSize = True
        Me.lbTitleBar.Font = New System.Drawing.Font("Calibri", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbTitleBar.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbTitleBar.Location = New System.Drawing.Point(48, 12)
        Me.lbTitleBar.Name = "lbTitleBar"
        Me.lbTitleBar.Size = New System.Drawing.Size(59, 18)
        Me.lbTitleBar.TabIndex = 19
        Me.lbTitleBar.Text = "Title Bar"
        '
        'pbText
        '
        Me.pbText.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.pbText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pbText.Location = New System.Drawing.Point(20, 117)
        Me.pbText.Name = "pbText"
        Me.pbText.Size = New System.Drawing.Size(20, 20)
        Me.pbText.TabIndex = 13
        Me.pbText.TabStop = False
        '
        'pbSideBar
        '
        Me.pbSideBar.BackColor = System.Drawing.Color.DimGray
        Me.pbSideBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pbSideBar.Location = New System.Drawing.Point(20, 81)
        Me.pbSideBar.Name = "pbSideBar"
        Me.pbSideBar.Size = New System.Drawing.Size(20, 20)
        Me.pbSideBar.TabIndex = 12
        Me.pbSideBar.TabStop = False
        '
        'ColorPicker
        '
        Me.ColorPicker.AnyColor = True
        Me.ColorPicker.FullOpen = True
        '
        'tGetColor
        '
        Me.tGetColor.Interval = 1
        '
        'tGetShade
        '
        Me.tGetShade.Interval = 1
        '
        'pbPreview
        '
        Me.pbPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pbPreview.Location = New System.Drawing.Point(154, 14)
        Me.pbPreview.Name = "pbPreview"
        Me.pbPreview.Size = New System.Drawing.Size(128, 128)
        Me.pbPreview.TabIndex = 26
        Me.pbPreview.TabStop = False
        '
        'pbColorWheel
        '
        Me.pbColorWheel.Image = CType(resources.GetObject("pbColorWheel.Image"), System.Drawing.Image)
        Me.pbColorWheel.Location = New System.Drawing.Point(9, 14)
        Me.pbColorWheel.Name = "pbColorWheel"
        Me.pbColorWheel.Size = New System.Drawing.Size(128, 128)
        Me.pbColorWheel.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pbColorWheel.TabIndex = 24
        Me.pbColorWheel.TabStop = False
        '
        'pbColorWheelBG
        '
        Me.pbColorWheelBG.Image = CType(resources.GetObject("pbColorWheelBG.Image"), System.Drawing.Image)
        Me.pbColorWheelBG.Location = New System.Drawing.Point(9, 14)
        Me.pbColorWheelBG.Name = "pbColorWheelBG"
        Me.pbColorWheelBG.Size = New System.Drawing.Size(128, 128)
        Me.pbColorWheelBG.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pbColorWheelBG.TabIndex = 28
        Me.pbColorWheelBG.TabStop = False
        '
        'pbSliderFG
        '
        Me.pbSliderFG.Location = New System.Drawing.Point(154, 163)
        Me.pbSliderFG.Name = "pbSliderFG"
        Me.pbSliderFG.Size = New System.Drawing.Size(128, 22)
        Me.pbSliderFG.TabIndex = 29
        Me.pbSliderFG.TabStop = False
        '
        'pbSliderBG
        '
        Me.pbSliderBG.Location = New System.Drawing.Point(154, 163)
        Me.pbSliderBG.Name = "pbSliderBG"
        Me.pbSliderBG.Size = New System.Drawing.Size(128, 22)
        Me.pbSliderBG.TabIndex = 25
        Me.pbSliderBG.TabStop = False
        '
        'pTitle
        '
        Me.pTitle.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.pTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pTitle.Controls.Add(Me.pbIcon)
        Me.pTitle.Controls.Add(Me.lbTitle)
        Me.pTitle.Controls.Add(Me.btnClose)
        Me.pTitle.Location = New System.Drawing.Point(0, 0)
        Me.pTitle.Name = "pTitle"
        Me.pTitle.Size = New System.Drawing.Size(454, 27)
        Me.pTitle.TabIndex = 30
        '
        'pbIcon
        '
        Me.pbIcon.BackColor = System.Drawing.Color.Transparent
        Me.pbIcon.Image = Global.WFInfo.My.Resources.Resources.WFLogo
        Me.pbIcon.Location = New System.Drawing.Point(0, -1)
        Me.pbIcon.Name = "pbIcon"
        Me.pbIcon.Size = New System.Drawing.Size(25, 25)
        Me.pbIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.pbIcon.TabIndex = 22
        Me.pbIcon.TabStop = False
        '
        'lbTitle
        '
        Me.lbTitle.AutoSize = True
        Me.lbTitle.BackColor = System.Drawing.Color.Transparent
        Me.lbTitle.Font = New System.Drawing.Font("Cambria", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbTitle.Location = New System.Drawing.Point(32, 4)
        Me.lbTitle.Name = "lbTitle"
        Me.lbTitle.Size = New System.Drawing.Size(98, 17)
        Me.lbTitle.TabIndex = 17
        Me.lbTitle.Text = "Customize UI"
        '
        'btnClose
        '
        Me.btnClose.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnClose.Font = New System.Drawing.Font("Tahoma", 11.0!, System.Drawing.FontStyle.Bold)
        Me.btnClose.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnClose.Location = New System.Drawing.Point(423, -1)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(30, 27)
        Me.btnClose.TabIndex = 17
        Me.btnClose.TabStop = False
        Me.btnClose.Text = "×"
        Me.btnClose.UseVisualStyleBackColor = False
        '
        'Panel2
        '
        Me.Panel2.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.Panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel2.Controls.Add(Me.btnSet)
        Me.Panel2.Controls.Add(Me.pbColorWheel)
        Me.Panel2.Controls.Add(Me.pbPreview)
        Me.Panel2.Controls.Add(Me.pbSliderFG)
        Me.Panel2.Controls.Add(Me.pbColorWheelBG)
        Me.Panel2.Controls.Add(Me.Panel1)
        Me.Panel2.Controls.Add(Me.pbSliderBG)
        Me.Panel2.ForeColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.Panel2.Location = New System.Drawing.Point(0, 0)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(452, 199)
        Me.Panel2.TabIndex = 31
        '
        'btnSet
        '
        Me.btnSet.BackColor = System.Drawing.Color.FromArgb(CType(CType(22, Byte), Integer), CType(CType(22, Byte), Integer), CType(CType(22, Byte), Integer))
        Me.btnSet.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSet.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold)
        Me.btnSet.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnSet.Location = New System.Drawing.Point(9, 162)
        Me.btnSet.Name = "btnSet"
        Me.btnSet.Size = New System.Drawing.Size(128, 23)
        Me.btnSet.TabIndex = 18
        Me.btnSet.Text = "Set"
        Me.btnSet.TextAlign = System.Drawing.ContentAlignment.TopCenter
        Me.btnSet.UseVisualStyleBackColor = False
        '
        'Panel3
        '
        Me.Panel3.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.Panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel3.Controls.Add(Me.btnSave)
        Me.Panel3.Controls.Add(Me.btnDefault)
        Me.Panel3.Location = New System.Drawing.Point(0, 197)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(452, 68)
        Me.Panel3.TabIndex = 32
        '
        'btnSave
        '
        Me.btnSave.BackColor = System.Drawing.Color.FromArgb(CType(CType(22, Byte), Integer), CType(CType(22, Byte), Integer), CType(CType(22, Byte), Integer))
        Me.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSave.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold)
        Me.btnSave.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnSave.Location = New System.Drawing.Point(8, 36)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(433, 24)
        Me.btnSave.TabIndex = 17
        Me.btnSave.Text = "Save && Apply"
        Me.btnSave.TextAlign = System.Drawing.ContentAlignment.TopCenter
        Me.btnSave.UseVisualStyleBackColor = False
        '
        'btnDefault
        '
        Me.btnDefault.BackColor = System.Drawing.Color.FromArgb(CType(CType(22, Byte), Integer), CType(CType(22, Byte), Integer), CType(CType(22, Byte), Integer))
        Me.btnDefault.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnDefault.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold)
        Me.btnDefault.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnDefault.Location = New System.Drawing.Point(8, 7)
        Me.btnDefault.Name = "btnDefault"
        Me.btnDefault.Size = New System.Drawing.Size(433, 24)
        Me.btnDefault.TabIndex = 16
        Me.btnDefault.Text = "Default"
        Me.btnDefault.TextAlign = System.Drawing.ContentAlignment.TopCenter
        Me.btnDefault.UseVisualStyleBackColor = False
        '
        'Panel4
        '
        Me.Panel4.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.Panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel4.Controls.Add(Me.Panel2)
        Me.Panel4.Controls.Add(Me.Panel3)
        Me.Panel4.Location = New System.Drawing.Point(0, 26)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(454, 267)
        Me.Panel4.TabIndex = 33
        '
        'Picker
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(454, 293)
        Me.Controls.Add(Me.Panel4)
        Me.Controls.Add(Me.pTitle)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Picker"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Settings"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.pbBackground, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbTray, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbTitleBar, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbText, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbSideBar, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbPreview, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbColorWheel, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbColorWheelBG, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbSliderFG, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbSliderBG, System.ComponentModel.ISupportInitialize).EndInit()
        Me.pTitle.ResumeLayout(False)
        Me.pTitle.PerformLayout()
        CType(Me.pbIcon, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.Panel4.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents lbSideBar As Label
    Friend WithEvents lbText As Label
    Friend WithEvents pbSideBar As PictureBox
    Friend WithEvents pbText As PictureBox
    Friend WithEvents Panel1 As Panel
    Friend WithEvents pbTray As PictureBox
    Friend WithEvents lbTray As Label
    Friend WithEvents pbBackground As PictureBox
    Friend WithEvents lbBackground As Label
    Friend WithEvents pbTitleBar As PictureBox
    Friend WithEvents lbTitleBar As Label
    Friend WithEvents ColorPicker As ColorDialog
    Friend WithEvents pbColorWheel As PictureBox
    Friend WithEvents pbSliderBG As PictureBox
    Friend WithEvents pbPreview As PictureBox
    Friend WithEvents tGetColor As Timer
    Friend WithEvents pbColorWheelBG As PictureBox
    Friend WithEvents tGetShade As Timer
    Friend WithEvents pbSliderFG As PictureBox
    Friend WithEvents pTitle As Panel
    Friend WithEvents pbIcon As PictureBox
    Friend WithEvents lbTitle As Label
    Friend WithEvents btnClose As Button
    Friend WithEvents Panel2 As Panel
    Friend WithEvents Panel3 As Panel
    Friend WithEvents Panel4 As Panel
    Friend WithEvents btnDefault As Button
    Friend WithEvents btnSave As Button
    Friend WithEvents btnSet As Button
End Class
