Imports Newtonsoft.Json.Linq

Public Class Equipment
    Private drag As Boolean = False
    Private resizing As Boolean = False
    Private mouseX As Integer
    Private mouseY As Integer
    Private AddItem As TreeNode = Nothing
    Public Tree1Sorter As New EqmtSorter()
    Public Tree2Sorter As New EqmtSorter()
    Public Tree3Sorter As New EqmtSorter()
    Public types As String() = {"Warframe", "Primary", "Secondary", "Melee", "Archwing", "Companion"}

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    ' DRAG n DROP code

    Private Sub startDRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseDown, lbTitle.MouseDown, pbIcon.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub DRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseMove, lbTitle.MouseMove, pbIcon.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub stopDRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseUp, lbTitle.MouseUp, pbIcon.MouseUp
        drag = False
        My.Settings.EqmtWinLoc = Me.Location
    End Sub

    ' Resize Code

    Private Sub startResize(sender As Object, e As EventArgs) Handles BottomResize.MouseDown
        resizing = True
        mouseY = Cursor.Position.Y - Size.Height
    End Sub

    Private Sub contResize(sender As Object, e As EventArgs) Handles BottomResize.MouseMove
        If resizing Then
            Dim newSize As Integer = Cursor.Position.Y - mouseY
            If newSize < 200 Then
                newSize = 200
            End If
            Size = New Size(Size.Width, newSize)
            BottomResize.Location = New Point(BottomResize.Location.X, newSize - 87)

            Line1.Size = New Size(1, newSize)
            Line2.Size = Line1.Size
            Line3.Size = Line1.Size
            Panel1.Size = New Size(Panel1.Size.Width, newSize - 27)
            Panel3.Size = New Size(Panel3.Size.Width, newSize - 58)
            EqmtTree1.Size = New Size(EqmtTree1.Size.Width, newSize - 81)
            EqmtTree2.Size = New Size(EqmtTree2.Size.Width, newSize - 81)
        End If

    End Sub

    Private Sub stopResize(sender As Object, e As EventArgs) Handles BottomResize.MouseUp
        resizing = False
    End Sub

    ' Form Button/ETC Code

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Private Sub btnSwap_Click(sender As Object, e As EventArgs) Handles btnSwap.Click
        EqmtTree2.Visible = My.Settings.EqmtOne
        My.Settings.EqmtOne = Not My.Settings.EqmtOne
        EqmtTree1.Visible = My.Settings.EqmtOne
        lbTitle.Select()
        If My.Settings.EqmtOne Then
            EqmtTree1.Sort()
            EqmtTree1.Select()
            btnSwap.Text = "Equipment Groups"
        Else
            EqmtTree2.Sort()
            EqmtTree2.Select()
            btnSwap.Text = "All Equipment"
        End If
    End Sub

    Private Sub SortSelection_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortSelection.SelectedIndexChanged
        My.Settings.EqmtSort = SortSelection.SelectedIndex
        Tree1Sorter.type = My.Settings.EqmtSort
        Tree2Sorter.type = My.Settings.EqmtSort
        Tree3Sorter.type = My.Settings.EqmtSort
        EqmtTree1.Sort()
        EqmtTree2.Sort()
        EqmtTree3.Sort()
        lbTitle.Select()

    End Sub

    Private Sub btnCollapse_Click(sender As Object, e As EventArgs) Handles btnCollapse.Click
        lbTitle.Select()
        If EqmtTree3.Visible Then
            EqmtTree3.Visible = False
            EqmtTree3.CollapseAll()
            EqmtTree3.Visible = True
            EqmtTree3.Nodes(0).EnsureVisible()
        ElseIf EqmtTree2.Visible Then
            EqmtTree2.Visible = False
            EqmtTree2.CollapseAll()
            EqmtTree2.Visible = True
            EqmtTree2.Nodes(0).EnsureVisible()
        Else
            EqmtTree1.Visible = False
            EqmtTree1.CollapseAll()
            EqmtTree1.Visible = True
            EqmtTree1.Nodes(0).EnsureVisible()
        End If
    End Sub

    Private Sub btnExpand_Click(sender As Object, e As EventArgs) Handles btnExpand.Click
        If EqmtTree3.Visible Then
            EqmtTree3.Visible = False
            EqmtTree3.ExpandAll()
            EqmtTree3.Visible = True
            EqmtTree3.Nodes(0).EnsureVisible()
            EqmtTree3.Select()
        ElseIf EqmtTree2.Visible Then
            EqmtTree2.Visible = False
            EqmtTree2.ExpandAll()
            EqmtTree2.Visible = True
            EqmtTree2.Nodes(0).EnsureVisible()
            EqmtTree2.Select()
        Else
            EqmtTree1.Visible = False
            EqmtTree1.ExpandAll()
            EqmtTree1.Visible = True
            EqmtTree1.Nodes(0).EnsureVisible()
            EqmtTree1.Select()
        End If
    End Sub

    Private Sub VaultCheck_CheckedChanged(sender As Object, e As EventArgs) Handles VaultCheck.CheckedChanged
        If Me.Visible Then
            If VaultCheck.Checked Then
                Load_Eqmt_Tree()
            Else
                Remove_Vaulted()
            End If

            If EqmtTree3.Visible Then
                UpdateEqmtTree3(current_filters)
            End If
            EqmtTree1.Nodes(0).EnsureVisible()
            EqmtTree2.Nodes(0).EnsureVisible()
            If EqmtTree3.Nodes.Count > 0 Then
                EqmtTree3.Nodes(0).EnsureVisible()
            End If
            If EqmtTree3.Visible Then
                EqmtTree3.Select()
            ElseIf EqmtTree2.Visible Then
                EqmtTree2.Select()
            Else
                EqmtTree1.Select()
            End If
        End If
    End Sub

    Private Sub EqmtTree_MouseEnter(sender As Object, e As EventArgs) Handles EqmtTree1.MouseEnter, EqmtTree2.MouseEnter, EqmtTree3.MouseEnter
        Dim treeCast As TreeView = sender
        treeCast.Select()
        treeCast.SelectedNode = Nothing
    End Sub

    ' Right Click Menu

    Private Sub ToolStripItem_MouseEnter(sender As Object, e As EventArgs)
        If sender.GetType() Is GetType(ToolStripMenuItem) Then
            Console.WriteLine(sender.forecolor.ToString())
            sender.forecolor = BackColor
        End If
    End Sub

    Private Sub ToolStripItem_MouseLeave(sender As Object, e As EventArgs)
        If sender.GetType() Is GetType(ToolStripMenuItem) Then
            sender.forecolor = textColor
        End If
    End Sub

    Private Sub AddMenu_Click(sender As Object, e As ToolStripItemClickedEventArgs) Handles AddMenu.ItemClicked
        If e.ClickedItem.Name = "0" Then
            For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data(AddItem.Text)("parts").ToObject(Of JObject)
                db.eqmt_data(AddItem.Text)("parts")(kvp.Key)("owned") = kvp.Value.Item("count")
            Next

        ElseIf e.ClickedItem.Name = "1" Then
            For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data(AddItem.Text)("parts").ToObject(Of JObject)
                db.eqmt_data(AddItem.Text)("parts")(kvp.Key)("owned") = 0
            Next

        ElseIf e.ClickedItem.Name = "2" Then
            Dim owned As Integer = db.eqmt_data(AddItem.Parent.Text)("parts")(AddItem.Name)("owned")
            db.eqmt_data(AddItem.Parent.Text)("parts")(AddItem.Name)("owned") = owned + 1
            AddItem = AddItem.Parent

        ElseIf e.ClickedItem.Name = "3" Then
            Dim owned As Integer = db.eqmt_data(AddItem.Parent.Text)("parts")(AddItem.Name)("owned")
            db.eqmt_data(AddItem.Parent.Text)("parts")(AddItem.Name)("owned") = owned - 1
            AddItem = AddItem.Parent
        End If
        If My.Settings.EqmtSort = 2 OrElse My.Settings.EqmtSort = 3 Then
            AddItem.TreeView.BeginUpdate()
            ' Find bottom part
            Dim bot = AddItem
            If AddItem.Nodes.Count > 0 Then
                bot = AddItem.Nodes(0)
                For i As Integer = 1 To AddItem.Nodes.Count - 1
                    If Tree1Sorter.IComparer_Compare(bot, AddItem.Nodes(i)) < 0 Then
                        bot = AddItem.Nodes(i)
                    End If
                Next
            End If

            AddItem.TreeView.Sort()
            AddItem.TreeView.EndUpdate()
            bot.EnsureVisible()
        Else
            AddItem.TreeView.Refresh()
        End If
        AddItem = Nothing
        db.Save_Eqmt()
    End Sub

    Private Sub EqmtTree_Click(sender As Object, e As TreeNodeMouseClickEventArgs) Handles EqmtTree1.NodeMouseClick, EqmtTree2.NodeMouseClick, EqmtTree3.NodeMouseClick
        e.Node.TreeView.SelectedNode = e.Node
        AddMenu.Items.Clear()
        If e.Button <> MouseButtons.Right OrElse types.Contains(e.Node.Text) Then
            AddItem = Nothing
            Return
        End If
        AddItem = e.Node

        If AddItem IsNot Nothing Then
            If AddItem.Nodes.Count = 0 Then
                ' PART!!! IT'S A PART!
                Dim count As Integer = db.eqmt_data(AddItem.Parent.Text)("parts")(AddItem.Name)("count")
                Dim owned As Integer = db.eqmt_data(AddItem.Parent.Text)("parts")(AddItem.Name)("owned")
                If owned < count Then
                    Dim tsi As ToolStripItem = AddMenu.Items.Add("Add a Part")
                    tsi.ForeColor = textColor
                    tsi.BackColor = BackColor
                    tsi.Name = "2"
                End If
                If owned > 0 Then
                    Dim tsi As ToolStripItem = AddMenu.Items.Add("Remove a Part")
                    tsi.ForeColor = textColor
                    tsi.BackColor = BackColor
                    tsi.Name = "3"
                End If
                AddMenu.Show(EqmtTree1, e.Location)

            Else
                Dim count As Integer = 0
                Dim owned As Integer = 0
                For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data(AddItem.Text)("parts").ToObject(Of JObject)
                    count += kvp.Value.Item("count").ToObject(Of Integer)
                    owned += kvp.Value.Item("owned").ToObject(Of Integer)
                Next

                If owned <> count Then
                    Dim tsi As ToolStripItem = AddMenu.Items.Add("Mark Owned")
                    tsi.ForeColor = textColor
                    tsi.BackColor = BackColor
                    tsi.Name = "0"
                    AddHandler tsi.MouseLeave, AddressOf ToolStripItem_MouseLeave
                    AddHandler tsi.MouseEnter, AddressOf ToolStripItem_MouseEnter
                End If
                If owned <> 0 Then
                    Dim tsi As ToolStripItem = AddMenu.Items.Add("Mark Not Owned")
                    tsi.ForeColor = textColor
                    tsi.BackColor = BackColor
                    tsi.Name = "1"
                    AddHandler tsi.MouseLeave, AddressOf ToolStripItem_MouseLeave
                    AddHandler tsi.MouseEnter, AddressOf ToolStripItem_MouseEnter
                End If
                AddMenu.Show(EqmtTree1, e.Location)

            End If
        End If
    End Sub

    ' Collapse/Expand code

    Private Sub RelicTree_Collapse(sender As Object, e As TreeViewEventArgs) Handles EqmtTree1.AfterCollapse, EqmtTree2.AfterCollapse
        Dim temp As String = "|" + e.Node.FullPath + "|"
        If My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics = My.Settings.ExpandedRelics.Replace(temp, "")
        End If

        Dim other As TreeView = EqmtTree1
        If sender.Equals(EqmtTree1) Then
            other = EqmtTree2
        End If

        For Each node As TreeNode In other.Nodes.Find(e.Node.Name, True)
            If node.IsExpanded AndAlso node.Name = e.Node.Name Then
                node.Collapse()
            End If
        Next
    End Sub

    Private Sub RelicTree_Expand(sender As Object, e As TreeViewEventArgs) Handles EqmtTree1.AfterExpand, EqmtTree2.AfterExpand
        Dim temp As String = "|" + e.Node.FullPath + "|"
        If Not My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics += temp
        End If

        Dim other As TreeView = EqmtTree1
        If sender.Equals(EqmtTree1) Then
            other = EqmtTree2
        End If
        For Each node As TreeNode In other.Nodes.Find(e.Node.Name, True)
            If Not node.IsExpanded AndAlso node.Name = e.Node.Name Then
                node.Expand()
            End If
        Next
    End Sub

    Private Sub CheckIfExpand(node As TreeNode)
        Dim temp As String = "|" + node.FullPath + "|"
        If My.Settings.ExpandedRelics.Contains(temp) Then
            node.Expand()
        End If
    End Sub

    ' Startup/Show Code

    Private Sub Eqmt_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Location = My.Settings.EqmtWinLoc
        If Me.Location.X = 0 And Me.Location.Y = 0 Then
            Me.Location = New Point(Main.Location.X + Main.Width + 25, Main.Location.Y)
        End If
        If My.Settings.EqmtOne Then
            EqmtTree2.Visible = False
            EqmtTree1.Select()
            btnSwap.Text = "Equipment Groups"
        Else
            EqmtTree1.Visible = False
            EqmtTree2.Select()
            btnSwap.Text = "All Equipment"
        End If
        SortSelection.SelectedIndex = My.Settings.EqmtSort
    End Sub

    Private Sub Eqmt_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then
            If Not IsWindowMoveable(Me) Then
                Dim scr As Screen = GetMainScreen()
                Me.Location = New Point(scr.WorkingArea.X + 200, scr.WorkingArea.Y + 200)
            End If
            VaultCheck.Checked = True
        End If
    End Sub

    Public Sub Load_Eqmt_Tree()
        btnSwap.BackColor = bgColor
        For Each node As TreeNode In EqmtTree1.Nodes
            CheckIfExpand(node)
        Next

        Dim cast As JObject = Nothing
        Dim eqmt As TreeNode = Nothing
        For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data
            If Not kvp.Key.Contains("timestamp") AndAlso kvp.Key <> "version" Then
                cast = kvp.Value
                Dim kids As TreeNode() = EqmtTree1.Nodes.Find(cast("type"), False)(0).Nodes.Find(kvp.Key, False)
                If kids.Length = 0 Then
                    eqmt = New TreeNode(kvp.Key)
                    eqmt.Name = kvp.Key
                    For Each part As KeyValuePair(Of String, JToken) In cast("parts").ToObject(Of JObject)
                        Dim node As TreeNode = eqmt.Nodes.Add(part.Key)
                        node.Name = part.Key
                        If node.Text.Contains("Prime ") Then
                            node.Text = node.Text.Substring(node.Text.IndexOf("Prime ") + 6)
                        End If
                    Next
                    EqmtTree1.Nodes.Find(cast("type"), False)(0).Nodes.Add(eqmt)

                    CheckIfExpand(eqmt)

                    eqmt = eqmt.Clone()
                    EqmtTree2.Nodes.Add(eqmt)
                    CheckIfExpand(eqmt)
                End If
            End If
        Next

        EqmtTree1.TreeViewNodeSorter = Tree1Sorter
        EqmtTree2.TreeViewNodeSorter = Tree2Sorter
        EqmtTree3.TreeViewNodeSorter = Tree3Sorter
        EqmtTree1.Sort()
        EqmtTree2.Sort()
        EqmtTree3.Sort()
    End Sub

    Public Sub Remove_Vaulted()
        For Each prime As KeyValuePair(Of String, JToken) In db.eqmt_data
            If Not prime.Key.Contains("timestamp") AndAlso Not prime.Key.Contains("version") Then
                Dim vaulted As Boolean = False
                If prime.Value.ToObject(Of JObject).TryGetValue("vaulted", Nothing) Then
                    vaulted = prime.Value("vaulted").ToObject(Of Boolean)
                Else
                    For Each kvp As KeyValuePair(Of String, JToken) In prime.Value("parts").ToObject(Of JObject)
                        If kvp.Value("vaulted").ToObject(Of Boolean) Then
                            vaulted = True
                            Exit For
                        End If
                    Next

                End If

                If vaulted Then
                    Dim matches As TreeNode() = EqmtTree1.Nodes.Find(prime.Key, True)
                    If matches.Length > 0 Then
                        matches(0).Remove()
                    End If
                    matches = EqmtTree2.Nodes.Find(prime.Key, True)
                    If matches.Length > 0 Then
                        matches(0).Remove()
                    End If
                    matches = EqmtTree3.Nodes.Find(prime.Key, True)
                    If matches.Length > 0 Then
                        matches(0).Remove()
                    End If
                End If

            End If
        Next
    End Sub

    ' Custom Draw

    Private Sub EqmtTree_DrawItem(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DrawTreeNodeEventArgs) Handles EqmtTree1.DrawNode, EqmtTree2.DrawNode, EqmtTree3.DrawNode
        'e.DrawDefault = True
        If e.Bounds.Width = 0 OrElse e.Bounds.X = -1 Then
            ' BOUNDS ARE INCORRECT
            Return
        End If
        e.Graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic


        Dim depth As Integer = GetTreeViewDepth(e.Node)
        Dim rgb As Integer = 20 + 10 * depth + 5 * (e.Node.Index Mod 2)

        Dim altBrush As New SolidBrush(Color.FromArgb(rgb, rgb, rgb))

        e.Graphics.FillRectangle(altBrush, e.Bounds.X, e.Bounds.Y, EqmtTree1.Width, 16)

        e.Graphics.DrawString(e.Node.Text, tahoma9_bold, textBrush, New PointF(e.Bounds.X, e.Bounds.Y))

        If types.Contains(e.Node.Text) Then
            Return
        End If

        Dim vaulted As Boolean = False
        If e.Node.Text.Contains("Prime") Then
            ' EQMT

            Dim eqmt As JObject = Nothing
            If db.eqmt_data.TryGetValue(e.Node.Name, eqmt) Then
                For Each kvp As KeyValuePair(Of String, JToken) In eqmt("parts").ToObject(Of JObject)
                    If kvp.Value("vaulted").ToObject(Of Boolean) Then
                        vaulted = True
                        Exit For
                    End If
                Next
            End If

        Else
            ' PART
            '   node.Name = full part (matches db.market_data)
            Dim job As JObject = Nothing
            If db.eqmt_data.TryGetValue(e.Node.Parent.Name, job) Then
                If job("parts").ToObject(Of JObject).TryGetValue(e.Node.Name, job) Then
                    If job("vaulted").ToObject(Of Boolean) Then
                        vaulted = True
                    End If
                End If
            End If
        End If

        If vaulted Then
            Dim rght As Integer = e.Bounds.Right
            If e.Node.Text.Contains("&") Then
                rght += 8
            End If
            e.Graphics.DrawString("Vaulted", tahoma9_bold, stealthBrush, New PointF(rght, e.Bounds.Y))
        End If

        Dim brush As Brush = textBrush

        Dim sf As New StringFormat With {.Alignment = StringAlignment.Near}
        ' COLUMNS: Count(210-290), Ducat(290-370), Plat(370-450)
        If e.Node.Parent IsNot Nothing AndAlso e.Node.Parent.Text.Contains("Prime") Then
            ' PART with a PARENT PRIME      i.e. Warframe/Ash Prime/Systems  OR  Ash Prime/Systems
            sf.Alignment = StringAlignment.Far
            Dim count As String = "1"
            Dim owned As String = "0"
            Dim ducat As String = "0"
            Dim plat As String = "UNK"
            Dim job As JObject = Nothing
            If db.market_data.TryGetValue(e.Node.Name, job) Then
                plat = job("plat").ToObject(Of Double).ToString("N1")
                ducat = job("ducats")
            ElseIf db.eqmt_data.TryGetValue(e.Node.Name, job) Then
                plat = db.GetSetPlat(job).ToString("N1")
            End If

            If ducat = "0" Then
                ducat = "---"
            End If

            If db.eqmt_data.TryGetValue(e.Node.Parent.Text, job) Then
                job = job("parts")
                If job.TryGetValue(e.Node.Name, job) Then
                    count = job("count")
                    owned = job("owned")
                End If
            End If

            If (My.Settings.EqmtSort = 2 Or My.Settings.EqmtSort = 3) AndAlso count = owned Then
                brush = stealthBrush
                e.Node.ForeColor = stealthColor
            Else
                e.Node.ForeColor = textColor
            End If
            e.Graphics.DrawString(owned + "/" + count, EqmtTree1.Font, brush, 285, e.Bounds.Top + 1, sf)
            e.Graphics.DrawString(ducat, EqmtTree1.Font, brush, 360, e.Bounds.Top + 1, sf)
            e.Graphics.DrawString(plat, EqmtTree1.Font, brush, 445, e.Bounds.Top + 1, sf)
        Else
            ' ONLY PRIME SETS
            Dim job As JObject = Nothing
            If db.eqmt_data.TryGetValue(e.Node.Name, job) Then

                Dim plat As String = db.GetSetPlat(job, (My.Settings.EqmtSort = 2 Or My.Settings.EqmtSort = 3)).ToString("N1")
                job = job("parts")
                Dim count As Integer = 0
                Dim owned As Integer = 0
                For Each kvp As KeyValuePair(Of String, JToken) In job
                    count += kvp.Value.Item("count").ToObject(Of Integer)
                    owned += kvp.Value.Item("owned").ToObject(Of Integer)
                Next
                If (My.Settings.EqmtSort = 2 Or My.Settings.EqmtSort = 3) AndAlso count = owned Then
                    brush = stealthBrush
                    e.Node.ForeColor = stealthColor
                Else
                    e.Node.ForeColor = textColor
                End If
                e.Graphics.DrawString(owned.ToString() + "/" + count.ToString(), EqmtTree1.Font, brush, 225, e.Bounds.Top + 1, sf)
                e.Graphics.DrawString(plat, EqmtTree1.Font, brush, 375, e.Bounds.Top + 1, sf)
            Else
                Console.WriteLine("Missing plat: " & e.Node.Name)
            End If
        End If
    End Sub

    ' Treeview Filter code

    Private current_filters As String() = Nothing
    Private showAll As Boolean = True


    Private Sub FilterText_Enter(sender As Object, e As EventArgs) Handles FilterText.Enter
        If FilterText.Text = "Filter Terms..." Then
            FilterText.Text = ""
            FilterText.ForeColor = textColor
        End If
    End Sub

    Private Sub FilterText_Exit(sender As Object, e As EventArgs) Handles FilterText.Leave
        If FilterText.Text = "" Then
            FilterText.Text = "Filter Terms..."
            FilterText.ForeColor = stealthColor
        End If
    End Sub

    Private Sub FilterText_EnterKey(sender As Object, e As KeyPressEventArgs) Handles FilterText.KeyPress
        If e.KeyChar = Chr(Keys.Enter) Then
            EqmtTree3.Select()
            e.Handled = True
        End If
    End Sub

    Private Sub FilterText_TextChanged(sender As Object, e As EventArgs) Handles FilterText.TextChanged
        EqmtTree3.Visible = False
        If FilterText.Text.Length <> 0 AndAlso FilterText.Text <> "Filter Terms..." Then
            Dim textTemp As String = FilterText.Text.Trim
            If FilterText.Text.Chars(0) = "!"c Then
                textTemp = textTemp.Substring(1)
                showAll = False
            Else
                showAll = True
            End If
            If textTemp.Length > 0 Then
                current_filters = textTemp.ToLower().Split(" ")
                UpdateEqmtTree3(current_filters)
            Else
                EqmtTree3.Nodes.Clear()
            End If
        End If
    End Sub

    Private Sub RecursiveGetExpandedNodes(nodes As TreeNodeCollection, ByRef arr As List(Of String))
        For Each kid As TreeNode In nodes
            If kid.IsExpanded Then
                arr.Add(kid.FullPath)
            End If
            If kid.Nodes.Count > 0 Then
                RecursiveGetExpandedNodes(kid.Nodes, arr)
            End If
        Next
    End Sub

    Private Function GetExpandedNodes(tree As TreeView) As List(Of String)
        Dim ret As New List(Of String)
        RecursiveGetExpandedNodes(tree.Nodes, ret)
        Return ret
    End Function

    Private Sub RecursiveExpandNodes(nodes As TreeNodeCollection, arr As List(Of String))
        For Each kid As TreeNode In nodes
            If arr.Contains(kid.FullPath) Then
                kid.Expand()
            End If
            If kid.Nodes.Count > 0 Then
                RecursiveExpandNodes(kid.Nodes, arr)
            End If
        Next
    End Sub

    Private Function CheckNodes(nodes As TreeNodeCollection, filters() As String) As Boolean
        Dim matchesAll As Boolean = True
        For Each node As TreeNode In nodes
            If CheckNode(node, filters) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Function CheckNode(node As TreeNode, filters() As String) As Boolean
        Dim checkStr As String = node.FullPath.ToLower()
        For Each filt As String In filters
            If Not checkStr.Contains(filt) Then
                Return False
            End If
        Next
        Return True
    End Function

    Private Function CombineSegments(arr As List(Of Integer())) As List(Of Integer())
        Dim ret As New List(Of Integer())
        Dim i As Integer = 0
        While i < arr.Count
            Dim hold As Integer() = arr(i)
            i += 1
            While i < arr.Count AndAlso arr(i)(0) <= hold(1)
                If arr(i)(1) > hold(1) Then
                    hold(1) = arr(i)(1)
                End If
                i += 1
            End While
            ret.Add(hold)
        End While

        Return ret
    End Function

    Private Function GetMatchedText(node As TreeNode, filters As String()) As List(Of Integer())
        Dim ret As New List(Of Integer())
        Dim checkStr As String = node.Text.ToLower()
        For Each filt As String In filters
            If checkStr.Contains(filt) Then
                Dim first As Integer = checkStr.IndexOf(filt)
                Do
                    Dim last As Integer = first + filt.Length
                    ret.Add({first, last})
                    first = checkStr.IndexOf(filt, last)
                Loop While first <> -1
            End If
        Next
        ret.Sort(Function(x, y) x(0).CompareTo(y(0)))
        Return ret
    End Function

    Private Sub UpdateEqmtTree3(filters As String())
        EqmtTree3.Visible = False
        EqmtTree2.Visible = False
        EqmtTree1.Visible = False
        Dim nodes As List(Of String) = Nothing
        If showAll Then
            If My.Settings.EqmtOne Then
                nodes = GetExpandedNodes(EqmtTree1)
            Else
                nodes = GetExpandedNodes(EqmtTree2)
            End If
        End If

        EqmtTree3.Nodes.Clear()


        If My.Settings.EqmtOne Then
            'RelicTree1
            For Each eqmtType As TreeNode In EqmtTree1.Nodes
                Dim typeNode As TreeNode = Nothing
                For Each eqmtNode As TreeNode In eqmtType.Nodes
                    If showAll Then
                        If CheckNodes(eqmtNode.Nodes, filters) Then
                            If typeNode Is Nothing Then
                                typeNode = New TreeNode(eqmtType.Text)
                                typeNode.Name = eqmtType.Name
                                EqmtTree3.Nodes.Add(typeNode)
                            End If
                            typeNode.Nodes.Add(eqmtNode.Clone())
                        End If
                    Else
                        Dim tempNode As TreeNode = Nothing
                        For Each part As TreeNode In eqmtNode.Nodes
                            If CheckNode(part, filters) Then
                                If typeNode Is Nothing Then
                                    typeNode = New TreeNode(eqmtType.Text)
                                    typeNode.Name = eqmtType.Name
                                    EqmtTree3.Nodes.Add(typeNode)
                                End If
                                If tempNode Is Nothing Then
                                    tempNode = New TreeNode(eqmtNode.Text)
                                    tempNode.Name = eqmtNode.Name
                                    typeNode.Nodes.Add(tempNode)
                                End If
                                tempNode.Nodes.Add(part.Clone())
                            End If
                        Next
                    End If
                Next
            Next
        Else
            'RelicTree2
            ' {Relics, Hidden:{Relics}}
            For Each node As TreeNode In EqmtTree2.Nodes
                If showAll Then
                    If CheckNodes(node.Nodes, filters) Then
                        EqmtTree3.Nodes.Add(node.Clone())
                    End If
                Else
                    Dim tempNode As TreeNode = Nothing
                    For Each part As TreeNode In node.Nodes
                        If CheckNode(part, filters) Then
                            If tempNode Is Nothing Then
                                tempNode = New TreeNode(node.Text)
                                tempNode.Name = node.Name
                                EqmtTree3.Nodes.Add(tempNode)
                            End If
                            tempNode.Nodes.Add(part.Clone())
                        End If
                    Next
                End If
            Next
        End If

        EqmtTree3.Sort()

        If EqmtTree3.Nodes.Count > 0 Then
            If showAll Then
                RecursiveExpandNodes(EqmtTree3.Nodes, nodes)
            Else
                EqmtTree3.ExpandAll()
            End If
            EqmtTree3.Nodes(0).EnsureVisible()
        End If
        EqmtTree3.Visible = True
        EqmtTree2.Visible = Not My.Settings.EqmtOne
        EqmtTree1.Visible = My.Settings.EqmtOne
    End Sub
End Class

Public Class EqmtSorter
    Implements IComparer
    Public type As Integer = 0 ' 0: Name, 1: Cost

    Public Sub New()
    End Sub

    Public Function IComparer_Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Dim tx As TreeNode = x
        Dim ty As TreeNode = y
        Dim strx As String = tx.Text
        Dim stry As String = ty.Text
        Dim erax As String = ""
        Dim eray As String = ""

        If Equipment.types.Contains(strx) Then
            ' TYPE LEVEL
            Return Array.IndexOf(Equipment.types, strx) - Array.IndexOf(Equipment.types, stry)
        End If

        If Me.type = 1 Then
            ' Sort By COST
            Dim job As JObject = Nothing
            Dim xPlat As Double = 0
            Dim yPlat As Double = 0
            If db.market_data.TryGetValue(tx.Name, job) Then
                xPlat = job("plat")
            ElseIf db.eqmt_data.TryGetValue(tx.Name, job) Then
                xPlat = db.GetSetPlat(job)
            End If

            If db.market_data.TryGetValue(ty.Name, job) Then
                yPlat = job("plat")
            ElseIf db.eqmt_data.TryGetValue(ty.Name, job) Then
                yPlat = db.GetSetPlat(job)
            End If
            If xPlat <> yPlat Then
                Return yPlat - xPlat
            End If
        ElseIf Me.type = 3 Then
            ' Sort By COST

            Dim xPlat As Double = 0
            Dim yPlat As Double = 0
            Dim job As JObject = Nothing
            If tx.Parent Is Nothing OrElse Equipment.types.Contains(tx.Parent.Text) Then
                If db.eqmt_data.TryGetValue(tx.Name, job) Then
                    xPlat = db.GetSetPlat(job, True)
                End If
                If db.eqmt_data.TryGetValue(ty.Name, job) Then
                    yPlat = db.GetSetPlat(job, True)
                End If
            Else
                Dim xCount As Integer = db.eqmt_data(tx.Parent.Text)("parts")(tx.Name)("count")
                xCount -= db.eqmt_data(tx.Parent.Text)("parts")(tx.Name)("owned").ToObject(Of Integer)

                If db.market_data.TryGetValue(tx.Name, job) Then
                    xPlat = job("plat")
                ElseIf db.eqmt_data.TryGetValue(tx.Name, job) Then
                    xPlat = db.GetSetPlat(job)
                End If
                xPlat *= xCount

                Dim yCount As Integer = db.eqmt_data(ty.Parent.Text)("parts")(ty.Name)("count")
                yCount -= db.eqmt_data(ty.Parent.Text)("parts")(ty.Name)("owned").ToObject(Of Integer)

                If db.market_data.TryGetValue(ty.Name, job) Then
                    yPlat = job("plat")
                ElseIf db.eqmt_data.TryGetValue(ty.Name, job) Then
                    yPlat = db.GetSetPlat(job)
                End If
                yPlat *= yCount
            End If

            If xPlat <> yPlat Then
                Return yPlat - xPlat
            End If
        ElseIf Me.type = 2 Then
            ' Sort By UNOWNED
            If tx.Nodes.Count = 0 Then
                ' PART LEVEL
                Dim xCount As Integer = db.eqmt_data(tx.Parent.Text)("parts")(tx.Name)("count")
                Dim xOwned As Integer = db.eqmt_data(tx.Parent.Text)("parts")(tx.Name)("owned")
                Dim xMiss As Double = (10.0 * xOwned) / xCount

                Dim yCount As Integer = db.eqmt_data(ty.Parent.Text)("parts")(ty.Name)("count")
                Dim yOwned As Integer = db.eqmt_data(ty.Parent.Text)("parts")(ty.Name)("owned")
                Dim yMiss As Double = (10.0 * yOwned) / yCount

                ' NEED TO REVERSE ORDER 
                '   BECAUSE MORE MISSING MEANS FIRST
                If xMiss <> yMiss Then
                    Return xMiss - yMiss
                End If
            Else
                ' EQMT LEVEL
                Dim xCount As Integer = 0
                Dim xOwned As Integer = 0
                For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data(tx.Text)("parts").ToObject(Of JObject)
                    xCount += kvp.Value.Item("count").ToObject(Of Integer)
                    xOwned += kvp.Value.Item("owned").ToObject(Of Integer)
                Next
                Dim xMiss As Double = (10.0 * xOwned) / xCount

                Dim yCount As Integer = 0
                Dim yOwned As Integer = 0
                For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data(ty.Text)("parts").ToObject(Of JObject)
                    yCount += kvp.Value.Item("count").ToObject(Of Integer)
                    yOwned += kvp.Value.Item("owned").ToObject(Of Integer)
                Next
                Dim yMiss As Double = (10.0 * yOwned) / yCount

                If xMiss <> yMiss Then
                    Return xMiss - yMiss
                End If
            End If
        End If

        ' SORT BY NAME
        Return String.Compare(strx, stry)
    End Function
End Class