Imports Newtonsoft.Json.Linq

Public Class Equipment
    Private drag As Boolean = False
    Private mouseX As Integer
    Private mouseY As Integer
    Private AddItem As TreeNode = Nothing
    Public Tree1Sorter As New EqmtSorter(0)
    Public Tree2Sorter As New EqmtSorter(1)
    Public types As String() = {"Warframe", "Primary", "Secondary", "Melee", "Archwing", "Companion"}

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Public Shared Function GetScrollPos(hWnd As IntPtr, nBar As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Public Shared Function SetScrollPos(hWnd As IntPtr, nBar As Integer, nPos As Integer, bRedraw As Boolean) As Integer
    End Function

    Private Sub pTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles pTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub pTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles pTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub pTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles pTitle.MouseUp
        drag = False
        My.Settings.EqmtWinLoc = Me.Location
    End Sub

    Private Sub lbTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub lbTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub lbTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseUp
        drag = False
        My.Settings.EqmtWinLoc = Me.Location
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Private Sub Label2_MouseEnter(sender As Object, e As EventArgs) Handles Label2.MouseEnter
        Label2.BackColor = Color.FromArgb(50, 50, 50)
    End Sub

    Private Sub Label2_MouseLeave(sender As Object, e As EventArgs) Handles Label2.MouseLeave
        Label2.BackColor = Color.Transparent
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
            Dim bot = AddItem.Nodes(0)
            For i As Integer = 1 To AddItem.Nodes.Count - 1
                If Tree1Sorter.IComparer_Compare(bot, AddItem.Nodes(i)) < 0 Then
                    bot = AddItem.Nodes(i)
                End If
            Next

            AddItem.TreeView.Sort()
            AddItem.TreeView.EndUpdate()
            bot.EnsureVisible()
        Else
            AddItem.TreeView.Refresh()
        End If
        AddItem = Nothing
        db.Save_Eqmt()
    End Sub

    Private Sub EqmtTree_Click(sender As Object, e As TreeNodeMouseClickEventArgs) Handles EqmtTree1.NodeMouseClick, EqmtTree2.NodeMouseClick
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
                    tsi.Name = "2"
                End If
                If owned > 0 Then
                    Dim tsi As ToolStripItem = AddMenu.Items.Add("Remove a Part")
                    tsi.ForeColor = textColor
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
                    tsi.Name = "0"
                End If
                If owned <> 0 Then
                    Dim tsi As ToolStripItem = AddMenu.Items.Add("Mark Not Owned")
                    tsi.ForeColor = textColor
                    tsi.Name = "1"
                End If
                AddMenu.Show(EqmtTree1, e.Location)

            End If
        End If
    End Sub

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

    Private Sub Eqmt_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UpdateColors(Me)
    End Sub

    Private Sub Eqmt_Opening(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Location = My.Settings.EqmtWinLoc
        If My.Settings.EqmtOne Then
            EqmtTree2.Visible = False
            EqmtTree1.Select()
            Label2.Text = "Equipment Groups"
        Else
            EqmtTree1.Visible = False
            EqmtTree2.Select()
            Label2.Text = "All Equipment"
        End If
        SortSelection.SelectedIndex = My.Settings.EqmtSort
    End Sub

    Private Sub EqmtTree_DrawItem(ByVal sender As System.Object, ByVal e As DrawTreeNodeEventArgs) Handles EqmtTree1.DrawNode, EqmtTree2.DrawNode
        e.DrawDefault = True
        If e.Bounds.Width = 0 Then
            ' BOUNDS ARE INCORRECT
            Return
        End If
        If types.Contains(e.Node.Text) Then
            e.Graphics.DrawLine(New Pen(Color.FromArgb(40, 40, 40)), 50, e.Bounds.Top, 450, e.Bounds.Top)
            e.Graphics.DrawLine(New Pen(Color.FromArgb(40, 40, 40)), 50, e.Bounds.Bottom, 450, e.Bounds.Bottom)
            Return
        End If
        e.Graphics.DrawLine(New Pen(Color.FromArgb(40, 40, 40)), 60, e.Bounds.Top, 450, e.Bounds.Top)
        e.Graphics.DrawLine(New Pen(Color.FromArgb(40, 40, 40)), 60, e.Bounds.Bottom, 450, e.Bounds.Bottom)

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
            e.Graphics.DrawString(owned + "/" + count, EqmtTree1.Font, brush, 280, e.Bounds.Top + 1, sf)
            e.Graphics.DrawString(ducat, EqmtTree1.Font, brush, 360, e.Bounds.Top + 1, sf)
            e.Graphics.DrawString(plat, EqmtTree1.Font, brush, 440, e.Bounds.Top + 1, sf)
        Else
            ' ONLY PRIME SETS
            Dim job As JObject = db.eqmt_data(e.Node.Name)
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
            e.Graphics.DrawString(owned.ToString() + "/" + count.ToString(), EqmtTree1.Font, brush, 215, e.Bounds.Top + 1, sf)
            e.Graphics.DrawString(plat, EqmtTree1.Font, brush, 375, e.Bounds.Top + 1, sf)
        End If
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        EqmtTree2.Visible = My.Settings.EqmtOne
        My.Settings.EqmtOne = Not My.Settings.EqmtOne
        EqmtTree1.Visible = My.Settings.EqmtOne
        If My.Settings.EqmtOne Then
            EqmtTree1.Sort()
            EqmtTree1.Select()
            Label2.Text = "Equipment Groups"
        Else
            EqmtTree2.Sort()
            EqmtTree2.Select()
            Label2.Text = "All Equipment"
        End If
    End Sub

    Private Sub SortSelection_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortSelection.SelectedIndexChanged
        My.Settings.EqmtSort = SortSelection.SelectedIndex
        Tree1Sorter.type = My.Settings.EqmtSort
        Tree2Sorter.type = My.Settings.EqmtSort
        EqmtTree1.Sort()
        EqmtTree2.Sort()
        If EqmtTree1.Visible Then
            EqmtTree1.Select()
        Else
            EqmtTree2.Select()
        End If

    End Sub

    Public Sub Load_Eqmt_Tree()
        If EqmtTree1.Nodes(0).Nodes.Count > 1 Then
            Return
        End If
        For Each node As TreeNode In EqmtTree1.Nodes
            CheckIfExpand(node)
        Next

        Dim cast As JObject = Nothing
        Dim eqmt As TreeNode = Nothing
        For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data
            If Not kvp.Key.Contains("timestamp") Then
                cast = kvp.Value
                eqmt = EqmtTree1.Nodes.Find(cast("type"), False)(0).Nodes.Add(kvp.Key)
                eqmt.Name = kvp.Key
                cast = cast("parts")
                For Each part As KeyValuePair(Of String, JToken) In cast
                    Dim node As TreeNode = eqmt.Nodes.Add(part.Key)
                    node.Name = part.Key
                    If node.Text.Contains("Prime ") Then
                        node.Text = node.Text.Substring(node.Text.IndexOf("Prime ") + 6)
                    End If
                Next
                CheckIfExpand(eqmt)

                eqmt = eqmt.Clone()
                EqmtTree2.Nodes.Add(eqmt)
                CheckIfExpand(eqmt)
            End If
        Next

        EqmtTree1.TreeViewNodeSorter = Tree1Sorter
        EqmtTree2.TreeViewNodeSorter = Tree2Sorter
        EqmtTree1.Sort()
        EqmtTree2.Sort()
    End Sub

    Private Sub CheckIfExpand(node As TreeNode)
        Dim temp As String = "|" + node.FullPath + "|"
        If My.Settings.ExpandedRelics.Contains(temp) Then
            node.Expand()
        End If
    End Sub

    Friend Sub RefreshData()
        Dim cast As JObject = Nothing
        Dim eqmt As TreeNode = Nothing
        For Each kvp As KeyValuePair(Of String, JToken) In db.eqmt_data
            If Not kvp.Key.Contains("timestamp") Then
                If EqmtTree1.Nodes.Find(kvp.Key, True).Length = 0 Then
                    cast = kvp.Value
                    eqmt = EqmtTree1.Nodes.Find(cast("type"), False)(0).Nodes.Add(kvp.Key)
                    eqmt.Name = kvp.Key
                    cast = cast("parts")
                    For Each part As KeyValuePair(Of String, JToken) In cast
                        Dim node As TreeNode = eqmt.Nodes.Add(part.Key)
                        node.Name = part.Key
                        If node.Text.Contains("Prime ") Then
                            node.Text = node.Text.Substring(node.Text.IndexOf("Prime ") + 6)
                        End If
                    Next

                    eqmt = eqmt.Clone()
                    EqmtTree2.Nodes.Add(eqmt)
                End If
            End If
        Next
    End Sub
End Class

Public Class EqmtSorter
    Implements IComparer
    Dim group As Integer = 0
    Public type As Integer = 0 ' 0: Name, 1: Cost

    Public Sub New(x As Integer)
        Me.group = x
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