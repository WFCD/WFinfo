Imports System.Windows.Forms

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Equipment
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
        Dim TreeNode1 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Warframe")
        Dim TreeNode2 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Primary")
        Dim TreeNode3 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Secondary")
        Dim TreeNode4 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Melee")
        Dim TreeNode5 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Archwing")
        Dim TreeNode6 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Companion")
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Equipment))
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.Panel4 = New System.Windows.Forms.Panel()
        Me.Panel5 = New System.Windows.Forms.Panel()
        Me.FilterText = New System.Windows.Forms.TextBox()
        Me.VaultCheck = New System.Windows.Forms.CheckBox()
        Me.btnCollapse = New System.Windows.Forms.Button()
        Me.btnExpand = New System.Windows.Forms.Button()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Panel3 = New System.Windows.Forms.Panel()
        Me.Line1 = New System.Windows.Forms.Label()
        Me.Line2 = New System.Windows.Forms.Label()
        Me.Line3 = New System.Windows.Forms.Label()
        Me.BottomResize = New System.Windows.Forms.Panel()
        Me.EqmtTree1 = New WFInfo.DoubleBufferedTreeView()
        Me.EqmtTree2 = New WFInfo.DoubleBufferedTreeView()
        Me.EqmtTree3 = New WFInfo.DoubleBufferedTreeView()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.SortSelection = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.btnSwap = New System.Windows.Forms.Button()
        Me.HideOpt = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.pTitle = New System.Windows.Forms.Panel()
        Me.pbIcon = New System.Windows.Forms.PictureBox()
        Me.lbTitle = New System.Windows.Forms.Label()
        Me.AddMenu = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.Panel1.SuspendLayout()
        Me.Panel4.SuspendLayout()
        Me.Panel5.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.Panel2.SuspendLayout()
        Me.pTitle.SuspendLayout()
        CType(Me.pbIcon, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.Panel4)
        Me.Panel1.Controls.Add(Me.Panel3)
        Me.Panel1.Controls.Add(Me.Panel2)
        Me.Panel1.Location = New System.Drawing.Point(0, 26)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(456, 387)
        Me.Panel1.TabIndex = 24
        '
        'Panel4
        '
        Me.Panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel4.Controls.Add(Me.Panel5)
        Me.Panel4.Controls.Add(Me.VaultCheck)
        Me.Panel4.Controls.Add(Me.btnCollapse)
        Me.Panel4.Controls.Add(Me.btnExpand)
        Me.Panel4.Controls.Add(Me.Label7)
        Me.Panel4.Location = New System.Drawing.Point(-1, 30)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(456, 24)
        Me.Panel4.TabIndex = 26
        '
        'Panel5
        '
        Me.Panel5.BackColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.Panel5.Controls.Add(Me.FilterText)
        Me.Panel5.Location = New System.Drawing.Point(234, -1)
        Me.Panel5.Name = "Panel5"
        Me.Panel5.Size = New System.Drawing.Size(221, 24)
        Me.Panel5.TabIndex = 28
        '
        'FilterText
        '
        Me.FilterText.BackColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.FilterText.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.FilterText.Font = New System.Drawing.Font("Tahoma", 11.5!)
        Me.FilterText.ForeColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer))
        Me.FilterText.Location = New System.Drawing.Point(3, 3)
        Me.FilterText.MaxLength = 255
        Me.FilterText.Name = "FilterText"
        Me.FilterText.Size = New System.Drawing.Size(215, 19)
        Me.FilterText.TabIndex = 26
        Me.FilterText.Text = "Filter Terms..."
        Me.FilterText.WordWrap = False
        '
        'VaultCheck
        '
        Me.VaultCheck.AutoSize = True
        Me.VaultCheck.CheckAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.VaultCheck.Checked = True
        Me.VaultCheck.CheckState = System.Windows.Forms.CheckState.Checked
        Me.VaultCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.VaultCheck.Font = New System.Drawing.Font("Tahoma", 10.0!, System.Drawing.FontStyle.Bold)
        Me.VaultCheck.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.VaultCheck.Location = New System.Drawing.Point(50, 0)
        Me.VaultCheck.Name = "VaultCheck"
        Me.VaultCheck.Size = New System.Drawing.Size(120, 21)
        Me.VaultCheck.TabIndex = 27
        Me.VaultCheck.Text = "Show Vaulted"
        '
        'btnCollapse
        '
        Me.btnCollapse.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.btnCollapse.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer))
        Me.btnCollapse.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.btnCollapse.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCollapse.Font = New System.Drawing.Font("Tahoma", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnCollapse.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnCollapse.Location = New System.Drawing.Point(23, -1)
        Me.btnCollapse.Name = "btnCollapse"
        Me.btnCollapse.Size = New System.Drawing.Size(24, 24)
        Me.btnCollapse.TabIndex = 26
        Me.btnCollapse.Text = "-"
        Me.btnCollapse.UseCompatibleTextRendering = True
        Me.btnCollapse.UseVisualStyleBackColor = False
        '
        'btnExpand
        '
        Me.btnExpand.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.btnExpand.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer))
        Me.btnExpand.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.btnExpand.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnExpand.Font = New System.Drawing.Font("Tahoma", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnExpand.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnExpand.Location = New System.Drawing.Point(-1, -1)
        Me.btnExpand.Name = "btnExpand"
        Me.btnExpand.Size = New System.Drawing.Size(25, 24)
        Me.btnExpand.TabIndex = 25
        Me.btnExpand.Text = "+"
        Me.btnExpand.UseCompatibleTextRendering = True
        Me.btnExpand.UseVisualStyleBackColor = False
        '
        'Label7
        '
        Me.Label7.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.Label7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label7.Font = New System.Drawing.Font("Tahoma", 11.0!, System.Drawing.FontStyle.Bold)
        Me.Label7.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label7.Location = New System.Drawing.Point(179, -3)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(61, 26)
        Me.Label7.TabIndex = 23
        Me.Label7.Text = "Filter:"
        Me.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Panel3
        '
        Me.Panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel3.Controls.Add(Me.Line1)
        Me.Panel3.Controls.Add(Me.Line2)
        Me.Panel3.Controls.Add(Me.Line3)
        Me.Panel3.Controls.Add(Me.BottomResize)
        Me.Panel3.Controls.Add(Me.EqmtTree3)
        Me.Panel3.Controls.Add(Me.EqmtTree1)
        Me.Panel3.Controls.Add(Me.EqmtTree2)
        Me.Panel3.Controls.Add(Me.Label6)
        Me.Panel3.Controls.Add(Me.Label5)
        Me.Panel3.Controls.Add(Me.Label4)
        Me.Panel3.Controls.Add(Me.Label3)
        Me.Panel3.Location = New System.Drawing.Point(-1, 53)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(456, 333)
        Me.Panel3.TabIndex = 25
        '
        'Line1
        '
        Me.Line1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Line1.Location = New System.Drawing.Point(217, 0)
        Me.Line1.Name = "Line1"
        Me.Line1.Size = New System.Drawing.Size(1, 345)
        Me.Line1.TabIndex = 31
        '
        'Line2
        '
        Me.Line2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Line2.Location = New System.Drawing.Point(292, 0)
        Me.Line2.Name = "Line2"
        Me.Line2.Size = New System.Drawing.Size(1, 345)
        Me.Line2.TabIndex = 32
        '
        'Line3
        '
        Me.Line3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Line3.Location = New System.Drawing.Point(367, 0)
        Me.Line3.Name = "Line3"
        Me.Line3.Size = New System.Drawing.Size(1, 345)
        Me.Line3.TabIndex = 33
        '
        'BottomResize
        '
        Me.BottomResize.BackColor = System.Drawing.Color.Transparent
        Me.BottomResize.Cursor = System.Windows.Forms.Cursors.SizeNS
        Me.BottomResize.ForeColor = System.Drawing.Color.Transparent
        Me.BottomResize.Location = New System.Drawing.Point(5, 326)
        Me.BottomResize.Name = "BottomResize"
        Me.BottomResize.Size = New System.Drawing.Size(444, 5)
        Me.BottomResize.TabIndex = 28
        '
        'EqmtTree1
        '
        Me.EqmtTree1.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.EqmtTree1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.EqmtTree1.CausesValidation = False
        Me.EqmtTree1.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText
        Me.EqmtTree1.Font = New System.Drawing.Font("Tahoma", 9.0!, System.Drawing.FontStyle.Bold)
        Me.EqmtTree1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.EqmtTree1.FullRowSelect = True
        Me.EqmtTree1.Location = New System.Drawing.Point(-1, 22)
        Me.EqmtTree1.Name = "EqmtTree1"
        TreeNode1.Name = "warframe"
        TreeNode1.Text = "Warframe"
        TreeNode2.Name = "primary"
        TreeNode2.Text = "Primary"
        TreeNode3.Name = "secondary"
        TreeNode3.Text = "Secondary"
        TreeNode4.Name = "melee"
        TreeNode4.Text = "Melee"
        TreeNode5.Name = "archwing"
        TreeNode5.Text = "Archwing"
        TreeNode6.Name = "companion"
        TreeNode6.Text = "Companion"
        Me.EqmtTree1.Nodes.AddRange(New System.Windows.Forms.TreeNode() {TreeNode1, TreeNode2, TreeNode3, TreeNode4, TreeNode5, TreeNode6})
        Me.EqmtTree1.Size = New System.Drawing.Size(473, 310)
        Me.EqmtTree1.TabIndex = 27
        '
        'EqmtTree2
        '
        Me.EqmtTree2.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.EqmtTree2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.EqmtTree2.CausesValidation = False
        Me.EqmtTree2.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText
        Me.EqmtTree2.Font = New System.Drawing.Font("Tahoma", 9.0!, System.Drawing.FontStyle.Bold)
        Me.EqmtTree2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.EqmtTree2.FullRowSelect = True
        Me.EqmtTree2.Location = New System.Drawing.Point(-1, 22)
        Me.EqmtTree2.Name = "EqmtTree2"
        Me.EqmtTree2.Size = New System.Drawing.Size(473, 310)
        Me.EqmtTree2.TabIndex = 26
        '
        'EqmtTree3
        '
        Me.EqmtTree3.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.EqmtTree3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.EqmtTree3.CausesValidation = False
        Me.EqmtTree3.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText
        Me.EqmtTree3.Font = New System.Drawing.Font("Tahoma", 9.0!, System.Drawing.FontStyle.Bold)
        Me.EqmtTree3.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.EqmtTree3.FullRowSelect = True
        Me.EqmtTree3.Location = New System.Drawing.Point(-1, 22)
        Me.EqmtTree3.Name = "EqmtTree2"
        Me.EqmtTree3.Size = New System.Drawing.Size(473, 310)
        Me.EqmtTree3.TabIndex = 29
        Me.EqmtTree3.Visible = False
        '
        'Label6
        '
        Me.Label6.BackColor = System.Drawing.Color.Transparent
        Me.Label6.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label6.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label6.Location = New System.Drawing.Point(217, -1)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(76, 24)
        Me.Label6.TabIndex = 25
        Me.Label6.Text = "Owned"
        Me.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label5
        '
        Me.Label5.BackColor = System.Drawing.Color.Transparent
        Me.Label5.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label5.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label5.Location = New System.Drawing.Point(292, -1)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(76, 24)
        Me.Label5.TabIndex = 24
        Me.Label5.Text = "Ducat"
        Me.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label4
        '
        Me.Label4.BackColor = System.Drawing.Color.Transparent
        Me.Label4.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label4.Location = New System.Drawing.Point(367, -1)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(88, 24)
        Me.Label4.TabIndex = 23
        Me.Label4.Text = "Plat"
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label3
        '
        Me.Label3.BackColor = System.Drawing.Color.Transparent
        Me.Label3.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label3.Location = New System.Drawing.Point(-1, -1)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(205, 24)
        Me.Label3.TabIndex = 22
        Me.Label3.Text = "Name"
        Me.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Panel2
        '
        Me.Panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel2.Controls.Add(Me.SortSelection)
        Me.Panel2.Controls.Add(Me.Label1)
        Me.Panel2.Controls.Add(Me.btnSwap)
        Me.Panel2.Location = New System.Drawing.Point(-1, -1)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(456, 32)
        Me.Panel2.TabIndex = 19
        '
        'SortSelection
        '
        Me.SortSelection.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.SortSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.SortSelection.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.SortSelection.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold)
        Me.SortSelection.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.SortSelection.FormattingEnabled = True
        Me.SortSelection.Items.AddRange(New Object() {"Name", "Cost", "Unowned", "Complete Cost"})
        Me.SortSelection.Location = New System.Drawing.Point(292, 2)
        Me.SortSelection.Name = "SortSelection"
        Me.SortSelection.Size = New System.Drawing.Size(150, 26)
        Me.SortSelection.TabIndex = 23
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label1.Location = New System.Drawing.Point(218, 6)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(68, 18)
        Me.Label1.TabIndex = 22
        Me.Label1.Text = "Sort By:"
        '
        'btnSwap
        '
        Me.btnSwap.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.btnSwap.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(100, Byte), Integer))
        Me.btnSwap.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.btnSwap.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSwap.Font = New System.Drawing.Font("Tahoma", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnSwap.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnSwap.Location = New System.Drawing.Point(-1, -1)
        Me.btnSwap.Name = "btnSwap"
        Me.btnSwap.Size = New System.Drawing.Size(209, 32)
        Me.btnSwap.TabIndex = 21
        Me.btnSwap.Text = "All Equipment"
        Me.btnSwap.UseVisualStyleBackColor = False
        '
        'HideOpt
        '
        Me.HideOpt.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.HideOpt.Name = "HideOpt"
        Me.HideOpt.ShowShortcutKeys = False
        Me.HideOpt.Size = New System.Drawing.Size(155, 22)
        Me.HideOpt.Text = "Hide"
        Me.HideOpt.TextAlign = System.Drawing.ContentAlignment.TopLeft
        '
        'btnClose
        '
        Me.btnClose.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnClose.Font = New System.Drawing.Font("Tahoma", 11.0!, System.Drawing.FontStyle.Bold)
        Me.btnClose.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnClose.Location = New System.Drawing.Point(425, -1)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(30, 27)
        Me.btnClose.TabIndex = 17
        Me.btnClose.TabStop = False
        Me.btnClose.Text = "×"
        Me.btnClose.UseVisualStyleBackColor = False
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
        Me.pTitle.Size = New System.Drawing.Size(456, 27)
        Me.pTitle.TabIndex = 25
        '
        'pbIcon
        '
        Me.pbIcon.BackColor = System.Drawing.Color.Transparent
        Me.pbIcon.Image = Global.WFInfo.My.Resources.Resources.WFLogo
        Me.pbIcon.Location = New System.Drawing.Point(0, 0)
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
        Me.lbTitle.Location = New System.Drawing.Point(32, 3)
        Me.lbTitle.Name = "lbTitle"
        Me.lbTitle.Size = New System.Drawing.Size(84, 17)
        Me.lbTitle.TabIndex = 17
        Me.lbTitle.Text = "Equipment"
        '
        'AddMenu
        '
        Me.AddMenu.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.AddMenu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.AddMenu.Font = New System.Drawing.Font("Tahoma", 10.0!, System.Drawing.FontStyle.Bold)
        Me.AddMenu.Name = "HideMenu"
        Me.AddMenu.ShowImageMargin = False
        Me.AddMenu.ShowItemToolTips = False
        Me.AddMenu.Size = New System.Drawing.Size(36, 4)
        '
        'Equipment
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(456, 413)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.pTitle)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Equipment"
        Me.Text = "Equipment"
        Me.Panel1.ResumeLayout(False)
        Me.Panel4.ResumeLayout(False)
        Me.Panel4.PerformLayout()
        Me.Panel5.ResumeLayout(False)
        Me.Panel5.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        Me.pTitle.ResumeLayout(False)
        Me.pTitle.PerformLayout()
        CType(Me.pbIcon, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel1 As Panel
    Friend WithEvents pbIcon As PictureBox
    Friend WithEvents btnClose As Button
    Friend WithEvents pTitle As Panel
    Friend WithEvents lbTitle As Label
    Friend WithEvents Panel2 As Panel
    Friend WithEvents btnSwap As Button
    Friend WithEvents HideOpt As ToolStripMenuItem
    Friend WithEvents Label1 As Label
    Friend WithEvents SortSelection As ComboBox
    Friend WithEvents AddMenu As ContextMenuStrip
    Friend WithEvents Panel3 As Panel
    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents EqmtTree3 As DoubleBufferedTreeView
    Friend WithEvents EqmtTree2 As DoubleBufferedTreeView
    Friend WithEvents EqmtTree1 As DoubleBufferedTreeView
    Friend WithEvents BottomResize As Panel
    Friend WithEvents Line1 As Label
    Friend WithEvents Line2 As Label
    Friend WithEvents Line3 As Label
    Friend WithEvents Panel4 As Panel
    Friend WithEvents Panel5 As Panel
    Friend WithEvents FilterText As TextBox
    Friend WithEvents VaultCheck As CheckBox
    Friend WithEvents btnCollapse As Button
    Friend WithEvents btnExpand As Button
    Friend WithEvents Label7 As Label
End Class
