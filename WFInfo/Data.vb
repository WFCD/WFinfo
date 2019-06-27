Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Class Data

    Public market_items As Dictionary(Of String, String)                 ' warframe.market item listing                  {<id>: "<name>|<url_name>", ...}
    Private ReadOnly market_items_path As String = Path.Combine(appData, "WFInfo\market_items.json")
    Public market_data As JObject                                        ' contains warframe.market ducatonator listing  {<partName>: {"ducats": <ducat_val>,"plat": <plat_val>}, ...}
    Private ReadOnly market_data_path As String = Path.Combine(appData, "WFInfo\market_data.json")

    Public relic_data As JObject                                        ' Contains relic_data from Warframe PC Drops     {<Era>: {"A1":{"vaulted": true,<rare1/uncommon[12]/common[123]>: <part>}, ...}, "Meso": ..., "Neo": ..., "Axi": ...}
    Private ReadOnly relic_data_path As String = Path.Combine(appData, "WFInfo\relic_data.json")
    Public eqmt_data As JObject                                         ' Contains eqmt_data from Warframe PC Drops      {<EQMT>: {"vaulted": true, "PARTS": {<NAME>:{"relic_name":<name>|"","count":<num>}, ...}},  ...}
    Private ReadOnly eqmt_data_path As String = Path.Combine(appData, "WFInfo\eqmt_data.json")
    Public name_data As Dictionary(Of String, String)                   ' Contains relic to market name translation      {<relic_name>: <market_name>}
    Private ReadOnly name_data_path As String = Path.Combine(appData, "WFInfo\name_data.json")

    Private ReadOnly debug_path As String = Path.Combine(appData, "WFInfo\debug.json")

    Private save_count As Integer = 0
    Private webClient As WebClient
    Public WithEvents watcher As New FileSystemWatcher()


    Public Sub New()
        Main.addLog("CREATING DATABASE")
        If Not My.Computer.FileSystem.DirectoryExists(appData + "\WFInfo") Then
            Directory.CreateDirectory(appData + "\WFInfo")
        End If

        webClient = New WebClient
        webClient.Headers.Add("platform", "pc")
        webClient.Headers.Add("language", "en")
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12


        Dim path As String = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) & "\Warframe"
        If Not My.Computer.FileSystem.DirectoryExists(path) Then
            Directory.CreateDirectory(path)
        End If
        watcher.Path = path
        watcher.EnableRaisingEvents = True

        Update()
    End Sub

    Public Sub Save_JObject(data As JObject)
        Main.addLog("SAVING DEBUG JSON: debug" & save_count.ToString() & ".json")
        File.WriteAllText(Path.Combine(appData, "WFInfo\debug" & save_count.ToString() & ".json"), JsonConvert.SerializeObject(data, Formatting.Indented))
        save_count += 1
    End Sub

    Public Sub Save_JArray(data As JArray)
        Main.addLog("SAVING DEBUG JSON: debug" & save_count.ToString() & ".json")
        File.WriteAllText(Path.Combine(appData, "WFInfo\debug" & save_count.ToString() & ".json"), JsonConvert.SerializeObject(data, Formatting.Indented))
        save_count += 1
    End Sub

    Public Sub Save_Market()
        Main.addLog("SAVING MARKET DATABASE")
        File.WriteAllText(market_items_path, JsonConvert.SerializeObject(market_items, Formatting.Indented))
        File.WriteAllText(market_data_path, JsonConvert.SerializeObject(market_data, Formatting.Indented))
    End Sub

    Public Sub Save_Relics()
        Main.addLog("SAVING RELIC DATABASE")
        File.WriteAllText(relic_data_path, JsonConvert.SerializeObject(relic_data, Formatting.Indented))
    End Sub

    Public Sub Save_Names()
        Main.addLog("SAVING NAME DATABASE")
        File.WriteAllText(name_data_path, JsonConvert.SerializeObject(name_data, Formatting.Indented))
    End Sub

    Public Sub Save_Eqmt()
        Main.addLog("SAVING EQMT DATABASE")
        File.WriteAllText(eqmt_data_path, JsonConvert.SerializeObject(eqmt_data, Formatting.Indented))
    End Sub

    Public Function Get_Current_Version() As Integer
        webClient.Headers.Add("User-Agent", "random-facades")
        Dim github As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.github.com/repos/random-facades/WFInfo/releases/latest"))
        If github.TryGetValue("tag_name", Nothing) Then
            Return VersionToInteger(github("tag_name").ToString())
        End If
        Return Main.Instance.versionNum
    End Function

    Public Function Load_Nexus_Items() As Boolean
        Dim nexus_db As JArray = JsonConvert.DeserializeObject(Of JArray)(webClient.DownloadString("https://api.nexus-stats.com/warframe/v1/items?data=prices"))

        For Each item As JObject In nexus_db
            If item("type").ToString() = "Prime" Then
                For Each part As JObject In item("components")
                    If part("name").ToString() <> "Set" Then
                        Dim str_name As String = item("name").ToString() & " " & part("name").ToString()
                        Dim check As JValue = part("combined")("avg")
                        If check.Value Is Nothing Then
                            Dim str_url As String = Regex.Replace(str_name.ToLower(), " ", "_")
                            Load_Market_Item(str_name, str_url)
                        Else
                            Dim plat As Double = check
                            Dim mark As Double = market_data(str_name)("plat")
                            If mark < plat Then
                                plat = part("combined")("median")
                                If mark < plat Then
                                    plat = part("combined")("min")
                                    If mark < plat Then
                                        plat = mark
                                    End If
                                End If
                            End If
                            market_data(str_name)("plat") = Math.Round(plat, 2)
                        End If
                    End If
                Next

            End If
        Next

        Return True
    End Function

    Private Function Load_Market_Items(Optional force As Boolean = False) As Boolean
        Main.addLog("LOADING MARKET ITEM DATABASE")
        If Not force AndAlso File.Exists(market_items_path) Then
            If market_items Is Nothing Then
                market_items = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(File.ReadAllText(market_items_path))
            End If
            Main.addLog("MARKET ITEM DATABASE EXISTS")
            Return False
        End If
        Main.addLog("LOADING NEW MARKET ITEM DATABASE")
        Dim m_i_temp As JObject
        Try
            m_i_temp = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items"))
        Catch ex As System.Net.WebException
            Main.addLog(ex.Status.ToString())
            Main.addLog(ex.InnerException.ToString())
            Return False
        End Try
        market_items = New Dictionary(Of String, String)()
For Each elem As JObject In m_i_temp("payload")("items")("en")
            Dim name As String = elem("item_name")
            If name.Contains("Prime ") Then
                market_items(elem("id")) = name + "|" + elem("url_name").ToString()
            End If
        Next
        Return True
    End Function

    Private Function Load_Market_Data(Optional force As Boolean = False) As Boolean
        Main.addLog("LOADING MARKET DATABASE")
        If Not force AndAlso File.Exists(market_data_path) Then
            If market_data Is Nothing Then
                market_data = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(market_data_path))
            End If
            Dim timestamp As Date = DateTime.Parse(market_data("timestamp"))
            Dim dayAgo As Date = Date.Now.AddDays(-1)
            If timestamp > dayAgo Then
                Main.addLog("MARKET DATABASE EXISTS")
                Return False
            End If
        End If

        Main.addLog("LOADING NEW MARKET DATABASE")


        Dim market_temp As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/tools/ducats"))
        market_data = New JObject()
        For Each elem As JObject In market_temp("payload")("previous_day")
            Dim item_name As String = ""
            If Not market_items.TryGetValue(elem("item"), item_name) Then
                Load_Market_Items(True)
            End If

            If market_items.TryGetValue(elem("item"), item_name) Then
                item_name = item_name.Split("|")(0)

                If Not item_name.Contains("Set") Then
                    market_data(item_name) = New JObject()
                    market_data(item_name)("ducats") = elem("ducats")
                    market_data(item_name)("plat") = elem("wa_price")
                End If
            Else
                Main.addLog("CANNOT FIND:" & elem("item").ToString())
            End If
        Next

        Dim job As New JObject()
        job("ducats") = 0
        job("plat") = 0
        market_data("Forma Blueprint") = job
        market_data("timestamp") = Date.Now.ToString("R")

        ' OVERWRITE ALL DATA FROM DUCANATOR CAUSE IT SUCKS
        '    :'(
        '                   except the ducat values, those are nice
        Load_Nexus_Items()
        Return True
    End Function

    Friend Function GetSetPlat(job As JObject, Optional unowned As Boolean = False) As Double
        Dim temp As JObject = Nothing
        Dim ret As Double = 0
        For Each kvp As KeyValuePair(Of String, JToken) In job("parts").ToObject(Of JObject)
            Dim count As Integer = kvp.Value.Item("count")
            Dim owned As Integer = kvp.Value.Item("owned")
            If unowned Then
                count -= owned
            End If
            If db.market_data.TryGetValue(kvp.Key, temp) Then
                ' THAR BE PLAT/DUCAT VALUES!
                ret += count * temp("plat").ToObject(Of Double)

            ElseIf db.eqmt_data.TryGetValue(kvp.Key, temp) Then
                ' So... it ain't got no values, but it can?
                db.market_data(kvp.Key) = New JObject()
                db.market_data(kvp.Key)("ducats") = 0
                db.market_data(kvp.Key)("plat") = 0
                Dim plat As Double = GetSetPlat(temp)
                db.market_data(kvp.Key)("plat") = plat
                Save_Market()

                ret += count * plat
            End If
        Next
        Return ret
    End Function

    Private Sub Load_Market_Item(item_name As String, url As String)
        Main.addLog("LOADING MISSING MARKET ITEM -- " & item_name)

        Dim stats As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + url + "/statistics"))
        stats = stats("payload")("statistics_closed")("90days").Last

        Dim ducats As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + url))
        ducats = ducats("payload")("item")
        Dim id As String = ducats("id")
        For Each part As JObject In ducats("items_in_set")
            If part("id").ToString() = id Then
                ducats = part
                Exit For
            End If
        Next
        Dim ducat As String = Nothing
        If Not ducats.TryGetValue("ducats", ducat) Then
            ducat = "0"
        End If

        market_data(item_name) = New JObject()
        market_data(item_name)("ducats") = ducat
        market_data(item_name)("plat") = stats("avg_price")
    End Sub

    Private Function Load_Drop_Data(Optional force As Boolean = False) As Boolean
        Main.addLog("LOADING DROP DATABASE")
        Dim request As WebRequest = Nothing
        If eqmt_data Is Nothing Then
            If File.Exists(eqmt_data_path) Then
                eqmt_data = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(eqmt_data_path))
            Else
                eqmt_data = New JObject()
            End If
        End If

        If Not force AndAlso File.Exists(relic_data_path) AndAlso File.Exists(eqmt_data_path) Then
            request = WebRequest.Create("https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html")
            request.Method = "HEAD"
            ' Move last_mod back one hour, so that it doesn't equal timestamp
            Using resp As WebResponse = request.GetResponse()
                Dim last_mod As Date = DateTime.Parse(resp.Headers.Get("Last-Modified")).AddHours(-1)

                If relic_data Is Nothing Then
                    relic_data = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(relic_data_path))
                End If
                If name_data Is Nothing Then
                    name_data = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(File.ReadAllText(name_data_path))
                End If

                If relic_data.TryGetValue("timestamp", Nothing) AndAlso
                    eqmt_data.TryGetValue("timestamp", Nothing) AndAlso
                    eqmt_data("timestamp").ToString() = relic_data("timestamp").ToString() AndAlso
                    last_mod < DateTime.Parse(relic_data("timestamp")) Then
                    Main.addLog("DROP DATABASE EXISTS")
                    Return False
                End If
            End Using
        End If
        Main.addLog("LOADING NEW DROP DATABASE")

        relic_data = New JObject()
        name_data = New Dictionary(Of String, String)()

        request = WebRequest.Create("https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html")
        Dim response As WebResponse = request.GetResponse()
        relic_data("timestamp") = response.Headers.Get("Last-Modified")
        eqmt_data("timestamp") = response.Headers.Get("Last-Modified")

        Dim drop_data As String = Nothing
        Using reader As New StreamReader(response.GetResponseStream(), Text.Encoding.ASCII)
            drop_data = reader.ReadToEnd()
        End Using

        ' Load Relic Info
        ' Get table start + end locations
        Dim first As Integer = drop_data.IndexOf("id=""relicRewards""")
        first = drop_data.IndexOf("<table>", first)
        Dim last As Integer = drop_data.IndexOf("</table>", first)

        ' Loop through each row
        '   Get start > while not at end > get last > parse > get start > goto while
        Dim index As Integer = drop_data.IndexOf("<tr>", first)
        Dim tr_stop As Integer = 0
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            ' If this is a Relic, then parse drops
            If sub_str.Contains("Relic") AndAlso sub_str.Contains("Intact") Then
                sub_str = Regex.Replace(sub_str, "<[^>]+>|\([^\)]+\)", "")
                Dim split As String() = sub_str.Split(" ")
                Dim era As String = split(0)
                Dim relic As String = split(1)
                If Not relic_data.TryGetValue(era, Nothing) Then
                    relic_data(era) = New JObject()
                End If
                relic_data(era)(relic) = New JObject()
                ' Will check if not vaulted in future
                relic_data(era)(relic)("vaulted") = True

                Dim cmnNum As Integer = 1
                Dim uncNum As Integer = 1
                index = drop_data.IndexOf("<tr", tr_stop)
                tr_stop = drop_data.IndexOf("</tr>", index)
                sub_str = drop_data.Substring(index, tr_stop - index)
                While Not sub_str.Contains("blank-row")
                    sub_str = sub_str.Replace("<tr><td>", "").Replace("</td>", "").Replace("td>", "")
                    split = sub_str.Split("<")
                    Dim name As String = split(0)
                    If name.Contains("Kavasa") Then
                        If name.Contains("Kubrow") Then
                            name = name.Replace("Kubrow ", "")
                        Else
                            name = name.Replace("Prime", "Prime Collar")
                        End If
                    ElseIf Not name.Contains("Prime Blueprint") And Not name.Contains("Forma") Then
                        name = name.Replace(" Blueprint", "")
                    End If
                    If split(1).Contains("2.") Then
                        relic_data(era)(relic)("rare1") = name
                    ElseIf split(1).Contains("11") Then
                        relic_data(era)(relic)("uncommon" + uncNum.ToString()) = name
                        uncNum += 1
                    Else
                        relic_data(era)(relic)("common" + cmnNum.ToString()) = name
                        cmnNum += 1
                    End If

                    Dim prime As String = name
                    If prime.IndexOf("Prime") <> -1 Then
                        prime = prime.Substring(0, prime.IndexOf("Prime") + 5)
                        If Not eqmt_data.TryGetValue(prime, Nothing) Then
                            eqmt_data(prime) = New JObject()
                            eqmt_data(prime)("parts") = New JObject()
                            eqmt_data(prime)("type") = ""
                        End If

                        Dim job As JObject = eqmt_data(prime)("parts")

                        If Not job.TryGetValue(name, Nothing) Then
                            job = New JObject()
                            job("count") = 1
                            job("owned") = 0
                            job("vaulted") = True
                            eqmt_data(prime)("parts")(name) = job
                        End If

                        If name.Contains("Harness") Then
                            eqmt_data(prime)("type") = "Archwing"
                        ElseIf name.Contains("Chassis") Then
                            eqmt_data(prime)("type") = "Warframe"
                        ElseIf name.Contains("Carapace") OrElse name.Contains("Collar Blueprint") Then
                            eqmt_data(prime)("type") = "Companion"
                        End If

                    End If
                    If Not name_data.ContainsKey(split(0)) Then
                        name_data(split(0)) = name
                    End If


                    index = drop_data.IndexOf("<tr", tr_stop)
                    tr_stop = drop_data.IndexOf("</tr>", index)
                    sub_str = drop_data.Substring(index, tr_stop - index)
                End While

            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While

        ' Find NOT Vauled Relics in Missions
        last = drop_data.IndexOf("id=""relicRewards""")
        index = drop_data.IndexOf("<tr>")
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            index = sub_str.IndexOf("Relic")
            If index <> -1 Then
                sub_str = sub_str.Substring(0, index - 1)
                index = sub_str.LastIndexOf(">") + 1
                sub_str = sub_str.Substring(index)
                Dim split As String() = sub_str.Split(" ")
                If relic_data.TryGetValue(split(0), Nothing) Then
                    relic_data(split(0))(split(1))("vaulted") = False
                    Mark_Eqmt_Unvaulted(split(0), split(1))
                End If
            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While

        ' Find NOT Vauled Relics in Special Rewards
        last = drop_data.IndexOf("id=""modByAvatar""")
        index = drop_data.IndexOf("id=""keyRewards""")
        index = drop_data.IndexOf("<tr>", index)
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            index = sub_str.IndexOf("Relic")
            If index <> -1 Then
                sub_str = sub_str.Substring(0, index - 1)
                index = sub_str.LastIndexOf(">") + 1
                sub_str = sub_str.Substring(index)
                Dim split As String() = sub_str.Split(" ")
                If relic_data.TryGetValue(split(0), Nothing) Then
                    relic_data(split(0))(split(1))("vaulted") = False
                    Mark_Eqmt_Unvaulted(split(0), split(1))
                End If
            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While
        Return True
    End Function

    Private Sub Mark_Eqmt_Unvaulted(era As String, name As String)
        Dim job As JObject = relic_data(era)(name)
        For Each kvp As KeyValuePair(Of String, JToken) In job
            Dim str As String = kvp.Value.ToString()
            If str.IndexOf("Prime") <> -1 Then
                Dim eqmt As String = str.Substring(0, str.IndexOf("Prime") + 5)
                If eqmt_data.TryGetValue(eqmt, Nothing) Then
                    eqmt_data(eqmt)("parts")(str)("vaulted") = False
                Else
                    Console.WriteLine("CANNOT FIND """ + eqmt + """ IN eqmt_data")
                End If
            End If
        Next
    End Sub

    Private Function Load_Eqmt_Rqmts() As Boolean
        ' Load wiki data on prime eqmt requirements
        ' Mainly weapons
        ' https://warframe.fandom.com/wiki/Special:Export/Module:Weapons/data

        Main.addLog("LOADING NEW WIKI DATABASE")
        Dim data As String = webClient.DownloadString("https://warframe.fandom.com/wiki/Special:Export/Module:Weapons/data")

        Dim start As Integer = data.IndexOf("<timestamp>") + 11
        Dim last As Integer = data.IndexOf("<", start)
        eqmt_data("rqmts_timestamp") = DateTime.Parse(data.Substring(start, last - start)).ToString("R")
        data = data.Substring(data.IndexOf("{", data.IndexOf("<text")))
        data = data.Substring(0, data.LastIndexOf("}") + 1)
        data = Regex.Replace(data, "&quot;", """")
        data = Regex.Replace(data, "&amp;", "&")
        data = Regex.Replace(data, "--\[\[[\s\S]*?--\]\]", "")
        data = Regex.Replace(data, "--([^\[\]][^\n]*)?\n", "")
        data = Regex.Replace(data, "\[|\]", "")
        data = Regex.Replace(data, "\s*=\s*", "=")
        data = Regex.Replace(data, "\{([^\}=]+)\}", "[$1]")
        data = Regex.Replace(data, "\{((?:[^\}\{=]*\{[^\}\{]*\}[^\}\{=]*)*)\}", "[$1]")
        data = Regex.Replace(data, "([\{,]\s*)([^\{\}\[\]\""=,]+?)(=\s*)", "$1""$2""$3")
        data = Regex.Replace(data, "([\[=,]\s*)([^\{\}\[\]\""=,]+?)(,\s*)", "$1""$2""$3")
        data = Regex.Replace(data, ",(\s*[\]\}])", "$1")
        data = Regex.Replace(data, "=", ":")
        data = Regex.Replace(data, """""([^,])", """$1")


        Dim data_job As JObject = JsonConvert.DeserializeObject(Of JObject)(data)("Weapons")
        'File.WriteAllText(debug_path, JsonConvert.SerializeObject(data_job, Formatting.Indented))
        For Each kvp As KeyValuePair(Of String, JToken) In eqmt_data
            If Not kvp.Key.Contains("timestamp") Then
                If data_job.TryGetValue(kvp.Key, Nothing) Then
                    eqmt_data(kvp.Key)("type") = data_job(kvp.Key)("Type")
                    Dim temp As New Dictionary(Of String, Integer)()
                    For Each part As JObject In data_job(kvp.Key)("Cost")("Parts")
                        If part("Type").ToString() = "PrimePart" Then
                            For Each relic_part As KeyValuePair(Of String, JToken) In kvp.Value.Item("parts").ToObject(Of JObject)
                                If relic_part.Key.Contains(part("Name").ToString()) Then
                                    eqmt_data(kvp.Key)("parts")(relic_part.Key)("count") = part("Count")
                                    Exit For
                                End If
                            Next
                        ElseIf part("Type").ToString() = "Weapon" AndAlso part("Name").ToString().Contains("Prime") Then
                            If Not temp.ContainsKey(part("Name")) Then
                                temp(part("Name")) = 0
                            End If
                            temp(part("Name")) += part("Count").ToObject(Of Integer)
                        End If
                    Next

                    If temp.Count > 0 Then
                        For Each entry As KeyValuePair(Of String, Integer) In temp
                            Dim job As JObject = eqmt_data(kvp.Key)("parts")
                            If Not job.TryGetValue(entry.Key, Nothing) Then
                                job = New JObject()
                                job("owned") = 0
                                job("vaulted") = False
                                eqmt_data(kvp.Key)("parts")(entry.Key) = job
                            End If
                            eqmt_data(kvp.Key)("parts")(entry.Key)("count") = entry.Value
                        Next
                    End If
                End If
            End If
        Next
        Return True
    End Function

    Public Function Update() As Boolean
        Main.addLog("UPDATING DATABASES")
        Dim save_market As Boolean = Load_Market_Items() Or Load_Market_Data()

        For Each elem As KeyValuePair(Of String, String) In market_items
            Dim name As String = elem.Value.Split("|")(0)
            If Not name.Contains(" Set") AndAlso Not market_data.TryGetValue(name, Nothing) Then
                Dim split As String() = elem.Value.Split("|")
                Load_Market_Item(split(0), split(1))
                save_market = True
            End If
        Next
        If save_market Then
            Me.Save_Market()
        End If
        Dim save_drop As Boolean = Load_Drop_Data()
        If save_drop Then
            Load_Eqmt_Rqmts()
            Save_Eqmt()
            Save_Relics()
            Save_Names()
        End If
        If save_market Or save_drop Then
            Main.addLog("DATABASES NEEDED UPDATES")
        Else
            Main.addLog("DATABASES DID NOT NEED UPDATES")
        End If
        Return save_market Or save_drop
    End Function

    Public Sub ForceMarketUpdate()
        Main.addLog("FORCING MARKET UPDATE")
        Load_Market_Items(True)
        Load_Market_Data(True)

        For Each elem As KeyValuePair(Of String, String) In market_items
            Dim name As String = elem.Value.Split("|")(0)
            If Not name.Contains(" Set") AndAlso Not market_data.TryGetValue(name, Nothing) Then
                Dim split As String() = elem.Value.Split("|")
                Load_Market_Item(split(0), split(1))
            End If
        Next
        Save_Market()
    End Sub

    Public Sub ForceEqmtUpdate()
        Main.addLog("FORCING EQMT UPDATE")
        Load_Drop_Data(True)
        Load_Eqmt_Rqmts()
        Save_Eqmt()
        Save_Relics()
        Save_Names()

    End Sub

    Public Sub ForceWikiUpdate()
        Main.addLog("FORCING WIKI UPDATE")
        Load_Eqmt_Rqmts()
        Save_Eqmt()
    End Sub

    Public temp_timer As Long = 0
    Public Function GetPlatLive(item_url As String) As JArray
        temp_timer = clock.Elapsed.TotalMilliseconds
        Dim stats As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + item_url + "/orders")) 'Get initial list of orders
        temp_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("Time taken to download all listings: " & -temp_timer)
        temp_timer = clock.Elapsed.TotalMilliseconds

        Dim sellers As New JArray
        For Each listing In stats("payload")("orders")
            If listing("order_type").ToString = "buy" Or listing("user")("status").ToString = "offline" Then 'check if order is a: a sell, b: acvtive 
                Continue For
            End If
            sellers.Add(listing)
        Next

        'sellers = sellers.Sort(Function(jo1, jo2) CDec(jo1.Item("Rate")).CompareTo(CDec(jo2.Item("Rate"))))
        'sellers = sellers.OrderBy(Of Integer)(Function(jo1, jo2) CDec(jo1.Item("Rate")).CompareTo(CDec(jo2.Item("Rate"))))
        'Dim sorted As JArray = (sellers.OrderBy(Of platinum))
        temp_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("Time taken to process sell and online listings: " & -temp_timer)
        Console.WriteLine(sellers)
        Return sellers 'return list of *recent* sellers. Price may fluctuate 

    End Function


    Public Function IsPartVaulted(name As String) As Boolean
        Dim eqmt As String = name.Substring(0, name.IndexOf("Prime") + 5)
        Return db.eqmt_data(eqmt)("parts")(name)("vaulted")
    End Function

    Public Function GetPartName(string1 As String) As String
        '_________________________________________________________________________
        'Checks the levDist of a string and returns the index in Names() of the closest part
        '_________________________________________________________________________
        Dim lowest As String = Nothing
        Dim low As Integer = 9999
        Dim str As String = Nothing
        Dim job As JObject = Nothing
        For Each prop As KeyValuePair(Of String, String) In name_data
            Dim val As Integer = LevDist(prop.Key, string1)
            If val < low Then
                low = val
                lowest = prop.Value
            End If
        Next
        Main.addLog("FOUND PART: " & lowest & vbTab & vbTab & "FROM: " & string1)
        Return lowest
    End Function

    Public Function GetSetName(string1 As String) As String
        '_________________________________________________________________________
        'Returns a string of a set given a part name
        '_________________________________________________________________________
        string1 = string1.ToLower()
        string1 = string1.Replace("*", "")
        Dim rStr As String = Nothing
        Dim low As Integer = 9999
        ' Modified
        For Each prop As KeyValuePair(Of String, JToken) In market_data
            Dim str As String = prop.Key.ToLower
            str = str.Replace("neuroptics", "")
            str = str.Replace("chassis", "")
            str = str.Replace("sytems", "")
            str = str.Replace("carapace", "")
            str = str.Replace("cerebrum", "")
            str = str.Replace("blueprint", "")
            str = str.Replace("harness", "")
            str = str.Replace("blade", "")
            str = str.Replace("pouch", "")
            str = str.Replace("barrel", "")
            str = str.Replace("receiver", "")
            str = str.Replace("stock", "")
            str = str.Replace("disc", "")
            str = str.Replace("grip", "")
            str = str.Replace("string", "")
            str = str.Replace("handle", "")
            str = str.Replace("ornament", "")
            str = str.Replace("wings", "")
            str = str.Replace("blades", "")
            str = str.Replace("hilt", "")
            str = RTrim(str)
            Dim val As Integer = LevDist(str, string1)
            If val < low Then
                low = val
                rStr = prop.Key
            End If
        Next
        rStr = rStr.ToLower
        rStr = rStr.Replace("neuroptics", "")
        rStr = rStr.Replace("chassis", "")
        rStr = rStr.Replace("sytems", "")
        rStr = rStr.Replace("carapace", "")
        rStr = rStr.Replace("cerebrum", "")
        rStr = rStr.Replace("blueprint", "")
        rStr = rStr.Replace("harness", "")
        rStr = rStr.Replace("blade", "")
        rStr = rStr.Replace("pouch", "")
        rStr = rStr.Replace("head", "")
        rStr = rStr.Replace("barrel", "")
        rStr = rStr.Replace("receiver", "")
        rStr = rStr.Replace("stock", "")
        rStr = rStr.Replace("disc", "")
        rStr = rStr.Replace("grip", "")
        rStr = rStr.Replace("string", "")
        rStr = rStr.Replace("handle", "")
        rStr = rStr.Replace("ornament", "")
        rStr = rStr.Replace("wings", "")
        rStr = rStr.Replace("blades", "")
        rStr = rStr.Replace("hilt", "")
        rStr = RTrim(rStr) & " set"
        rStr = StrConv(rStr, VbStrConv.ProperCase)
        Return rStr
    End Function

    Public Function GetRelicName(string1 As String) As String
        Dim lowest As String = Nothing
        Dim low As Integer = 999
        Dim temp As Integer = 0
        Dim str As String = Nothing
        Dim job As JObject = Nothing
        For Each era As KeyValuePair(Of String, JToken) In relic_data
            If Not era.Key.Contains("timestamp") Then
                temp = LevDist2(string1, era.Key & "??RELIC", low)
                If temp < low Then
                    job = era.Value
                    str = era.Key
                    low = temp
                End If
            End If
        Next
        low = 999
        For Each relic As KeyValuePair(Of String, JToken) In job
            temp = LevDist2(string1, str & relic.Key & "RELIC", low)
            If temp < low Then
                lowest = str & " " & relic.Key
                low = temp
            End If
        Next
        Return lowest
    End Function

    Private waiting As Boolean = False

    Private Sub watcher_Created(ByVal sender As Object, ByVal e As FileSystemEventArgs) Handles watcher.Created
        waiting = True
    End Sub

    Private Sub watcher_Changed(ByVal sender As Object, ByVal e As FileSystemEventArgs) Handles watcher.Changed
        If waiting Then
            waiting = False
            Threading.Thread.Sleep(500)
            parser2.ParseFile(e.FullPath)
        End If
    End Sub
End Class
