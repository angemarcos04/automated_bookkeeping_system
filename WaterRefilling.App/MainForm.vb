Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Net.Http
Imports System.Text.Json
Imports System.Globalization
Imports System.Threading.Tasks

Public Class MainForm
    Inherits Form

    Private ReadOnly primaryGreen As Color = Color.FromArgb(46, 157, 88)
    Private ReadOnly deepGreen As Color = Color.FromArgb(23, 107, 58)
    Private ReadOnly pageBackground As Color = Color.FromArgb(245, 250, 245)
    Private ReadOnly textColor As Color = Color.FromArgb(32, 51, 40)

    Private sidebar As Panel
    Private workspace As Panel
    Private pageTitle As Label

    Public Sub New()
        InitializeDashboard()
    End Sub

    Private Sub InitializeDashboard()
        Text = "Aqua ARC - Water Station Bookkeeping"
        StartPosition = FormStartPosition.CenterScreen
        Size = New Size(1440, 900)
        MinimumSize = New Size(1100, 700)
        BackColor = pageBackground
        Font = New Font("Segoe UI", 10)

        BuildSidebar()
        BuildHeader()
        BuildDashboard()
    End Sub

    Private Sub BuildSidebar()
        sidebar = New Panel With {
            .Dock = DockStyle.Left,
            .Width = 245,
            .BackColor = Color.White,
            .Padding = New Padding(12)
        }
        Controls.Add(sidebar)

        Dim logo As New Panel With {
            .Location = New Point(12, 15),
            .Size = New Size(42, 42),
            .BackColor = Color.FromArgb(228, 245, 232)
        }
        MakeRounded(logo, 12)
        sidebar.Controls.Add(logo)

        Dim logoText As New Label With {
            .Text = "AA",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = deepGreen,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        logo.Controls.Add(logoText)

        Dim appName As New Label With {
            .Text = "AQUA ARC" & vbCrLf & "BOOKKEEPING",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Location = New Point(65, 17),
            .Size = New Size(155, 40)
        }
        sidebar.Controls.Add(appName)

        Dim subtitle As New Label With {
            .Text = "WATER STATION BOOKKEEPING",
            .Font = New Font("Segoe UI", 7, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .Location = New Point(12, 72),
            .AutoSize = True
        }
        sidebar.Controls.Add(subtitle)

        Dim menuItems As String() = {
        "Dashboard",
        "Authentication & Security",
        "System Setup",
        "Master Data",
        "Fixed Assets",
        "Transactions",
        "Journal Entries",
        "General Ledger",
        "Financial Reports",
        "Audit Trail",
        "Settings"
}

        Dim y As Integer = 112

        For Each item As String In menuItems
            Dim selectedItem As String = item

            Dim menuButton As New Button With {
                .Text = "    " & selectedItem,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Location = New Point(5, y),
                .Size = New Size(215, 43),
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 9.5F),
                .Cursor = Cursors.Hand,
                .BackColor = If(selectedItem = "Dashboard",
                                Color.FromArgb(228, 245, 232),
                                Color.White),
                .ForeColor = If(selectedItem = "Dashboard",
                                deepGreen,
                                Color.FromArgb(80, 100, 85))
            }

            menuButton.FlatAppearance.BorderSize = 0

            AddHandler menuButton.Click,
                Sub(sender, e)
                    HandleNavigation(selectedItem)
                End Sub

            sidebar.Controls.Add(menuButton)
            y += 48
        Next

        Dim userPanel As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 65,
            .BackColor = Color.FromArgb(245, 250, 245)
        }
        sidebar.Controls.Add(userPanel)

        Dim userName As New Label With {
            .Text = "Administrator",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = textColor,
            .Location = New Point(12, 12),
            .AutoSize = True
        }
        userPanel.Controls.Add(userName)

        Dim userRole As New Label With {
            .Text = "System User",
            .Font = New Font("Segoe UI", 8),
            .ForeColor = Color.Gray,
            .Location = New Point(12, 34),
            .AutoSize = True
        }
        userPanel.Controls.Add(userRole)
    End Sub

    Private Sub BuildHeader()
        Dim header As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 75,
            .BackColor = Color.White,
            .Padding = New Padding(25, 12, 25, 12)
        }
        Controls.Add(header)

        pageTitle = New Label With {
            .Text = "Dashboard",
            .Font = New Font("Segoe UI", 19, FontStyle.Bold),
            .ForeColor = textColor,
            .Location = New Point(20, 17),
            .AutoSize = True
        }
        header.Controls.Add(pageTitle)

        Dim period As New Label With {
            .Text = "Fiscal Year " & DateTime.Now.Year.ToString(),
            .Font = New Font("Segoe UI", 9),
            .ForeColor = deepGreen,
            .BackColor = Color.FromArgb(238, 248, 240),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(145, 35),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .Location = New Point(1000, 18)
        }

        MakeRounded(period, 10)
        header.Controls.Add(period)
    End Sub

    Private Sub BuildDashboard()
        workspace = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = pageBackground,
            .AutoScroll = True,
            .Padding = New Padding(25)
        }

        Controls.Add(workspace)
        workspace.BringToFront()

        BuildDashboardContent()
    End Sub

    Private Sub BuildDashboardContent()
        workspace.Controls.Clear()

        Dim welcome As New Label With {
            .Text = "Welcome to Aqua ARC!",
            .Font = New Font("Segoe UI", 23, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Location = New Point(25, 20),
            .AutoSize = True
        }
        workspace.Controls.Add(welcome)

        Dim description As New Label With {
            .Text = "Monitor your finances, accounts, and transactions in one place.",
            .Font = New Font("Segoe UI", 10),
            .ForeColor = Color.FromArgb(113, 132, 119),
            .Location = New Point(28, 65),
            .AutoSize = True
        }
        workspace.Controls.Add(description)

        Dim notice As New Label With {
            .Text = "Financial information will be displayed here.",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = deepGreen,
            .BackColor = Color.FromArgb(228, 245, 232),
            .Location = New Point(28, 100),
            .Size = New Size(430, 32),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        MakeRounded(notice, 8)
        workspace.Controls.Add(notice)

        AddSummaryPanel("CASH BALANCE", "Not connected",
                        "Available cash", 25, 155, 360, 140, True)

        AddSummaryPanel("TOTAL INCOME", "Not connected",
                        "Recorded income", 405, 155, 230, 140)

        AddSummaryPanel("TOTAL EXPENSES", "Not connected",
                        "Recorded expenses", 655, 155, 230, 140)

        AddSummaryPanel("RECEIVABLES", "Not connected",
                        "Outstanding amounts", 905, 155, 210, 140)

        AddSummaryPanel("PAYABLES", "Not connected",
                        "Amounts owed", 25, 315, 250, 125)

        AddSummaryPanel("TOTAL ASSETS", "Not connected",
                        "Recorded assets", 295, 315, 250, 125)

        Dim activityTitle As New Label With {
            .Text = "Recent Activity",
            .Font = New Font("Segoe UI", 17, FontStyle.Bold),
            .ForeColor = textColor,
            .Location = New Point(25, 475),
            .AutoSize = True
        }
        workspace.Controls.Add(activityTitle)

        Dim activityDescription As New Label With {
            .Text = "Your latest bookkeeping transactions will appear here.",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.FromArgb(113, 132, 119),
            .Location = New Point(28, 510),
            .AutoSize = True
        }
        workspace.Controls.Add(activityDescription)

        Dim activityGrid As New DataGridView With {
            .Location = New Point(25, 545),
            .Size = New Size(1060, 200),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoGenerateColumns = False
        }

        activityGrid.Columns.Add("reference", "Reference")
        activityGrid.Columns.Add("date", "Date")
        activityGrid.Columns.Add("type", "Type")
        activityGrid.Columns.Add("description", "Description")
        activityGrid.Columns.Add("amount", "Amount")
        activityGrid.Columns.Add("status", "Status")

        activityGrid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(228, 245, 232)
        activityGrid.ColumnHeadersDefaultCellStyle.ForeColor = deepGreen
        activityGrid.EnableHeadersVisualStyles = False
        activityGrid.Rows.Add("—", "—", "—",
                              "No transaction data loaded", "—", "—")

        workspace.Controls.Add(activityGrid)
    End Sub

    Private Sub AddSummaryPanel(caption As String,
                                value As String,
                                detail As String,
                                x As Integer,
                                y As Integer,
                                width As Integer,
                                height As Integer,
                                Optional featured As Boolean = False)

        Dim panel As New Panel With {
            .Location = New Point(x, y),
            .Size = New Size(width, height),
            .BackColor = If(featured,
                            Color.FromArgb(228, 245, 232),
                            Color.White)
        }

        MakeRounded(panel, 14)
        workspace.Controls.Add(panel)

        Dim accent As New Panel With {
            .Location = New Point(0, 0),
            .Size = New Size(5, height),
            .BackColor = primaryGreen
        }
        panel.Controls.Add(accent)

        Dim title As New Label With {
            .Text = caption,
            .Location = New Point(18, 18),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(113, 132, 119)
        }
        panel.Controls.Add(title)

        Dim amount As New Label With {
            .Text = value,
            .Location = New Point(18, 48),
            .AutoSize = True,
            .Font = New Font("Segoe UI", If(featured, 17, 13), FontStyle.Bold),
            .ForeColor = deepGreen
        }
        panel.Controls.Add(amount)

        Dim subtext As New Label With {
            .Text = detail,
            .Location = New Point(18, height - 32),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 8),
            .ForeColor = Color.Gray
        }
        panel.Controls.Add(subtext)
    End Sub

    Private Sub MakeRounded(control As Control, radius As Integer)
        Dim updateRegion As Action =
            Sub()
                If control.Width <= 0 OrElse control.Height <= 0 Then Return

                Using path As New GraphicsPath()
                    Dim rect As New Rectangle(
                        0, 0, control.Width, control.Height)

                    Dim diameter As Integer = Math.Min(
                        radius * 2,
                        Math.Min(rect.Width, rect.Height))

                    path.AddArc(rect.Left, rect.Top,
                                diameter, diameter, 180, 90)
                    path.AddArc(rect.Right - diameter, rect.Top,
                                diameter, diameter, 270, 90)
                    path.AddArc(rect.Right - diameter,
                                rect.Bottom - diameter,
                                diameter, diameter, 0, 90)
                    path.AddArc(rect.Left, rect.Bottom - diameter,
                                diameter, diameter, 90, 90)
                    path.CloseFigure()

                    control.Region = New Region(path)
                End Using
            End Sub

        AddHandler control.SizeChanged,
            Sub(sender, e)
                updateRegion()
            End Sub

        updateRegion()
    End Sub



Private Sub HandleNavigation(item As String)
    Select Case item
        Case "Dashboard"
            pageTitle.Text = "Dashboard"
            BuildDashboardContent()

        Case "Master Data"
            pageTitle.Text = "Chart of Accounts"
            Using accounts As New ChartOfAccountsForm()
                accounts.ShowDialog(Me)
            End Using

        Case "Transactions"
            Dim choice As DialogResult = MessageBox.Show(
                "Yes = Cash Receipts Book" & vbCrLf &
                "No = Cash Disbursements",
                "Transactions",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question)

            If choice = DialogResult.Yes Then
                pageTitle.Text = "Cash Receipts Book"
                Using receipts As New CashReceiptsForm()
                    receipts.ShowDialog(Me)
                End Using
            ElseIf choice = DialogResult.No Then
                pageTitle.Text = "Cash Disbursements"
                Using disbursements As New CashDisbursementsForm()
                    disbursements.ShowDialog(Me)
                End Using
            End If

        Case "Journal Entries"
            pageTitle.Text = "Journal Entries"
            Using journal As New JournalEntriesForm()
                journal.ShowDialog(Me)
            End Using

        Case "Fixed Assets"
            pageTitle.Text = "Fixed Assets"
            Using assets As New FixedAssetsForm()
                assets.ShowDialog(Me)
            End Using

        Case "General Ledger"
            pageTitle.Text = "General Ledger"
            Using ledger As New GeneralLedgerForm()
                ledger.ShowDialog(Me)
            End Using

        Case "Financial Reports"
            Dim choice As DialogResult = MessageBox.Show(
                "Yes = Lapsing Schedule" & vbCrLf &
                "No = Trial Balance",
                "Financial Reports",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question)

            If choice = DialogResult.Yes Then
                pageTitle.Text = "Lapsing Schedule"
                ShowLapsingSchedule()
            ElseIf choice = DialogResult.No Then
                pageTitle.Text = "Trial Balance"
                Using trialBalance As New TrialBalanceForm()
                    trialBalance.ShowDialog(Me)
                End Using
            End If

        Case Else
            MessageBox.Show(
                item & " is not connected yet.",
                "Navigation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
    End Select
End Sub

Private Sub ShowTransactionsMenu()
    Dim choice As DialogResult =
        MessageBox.Show(
            "Choose Yes for Cash Receipts or No for Cash Disbursements.",
            "Transactions",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question)

    If choice = DialogResult.Yes Then
        pageTitle.Text = "Cash Receipts Book"

        Using receipts As New CashReceiptsForm()
            receipts.ShowDialog(Me)
        End Using

    ElseIf choice = DialogResult.No Then
        pageTitle.Text = "Cash Disbursements"

        Using disbursements As New CashDisbursementsForm()
            disbursements.ShowDialog(Me)
        End Using
    End If
End Sub


    Private Async Sub ShowLapsingSchedule()
        workspace.Controls.Clear()

        Dim heading As New Label With {
            .Text = "Fixed Asset Lapsing Schedule",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Location = New Point(25, 20),
            .AutoSize = True
        }
        workspace.Controls.Add(heading)

        Dim description As New Label With {
            .Text = "Depreciation and net book value as of the selected reporting period end.",
            .Font = New Font("Segoe UI", 10),
            .ForeColor = Color.Gray,
            .Location = New Point(28, 65),
            .AutoSize = True
        }
        workspace.Controls.Add(description)

        Dim periodLabel As New Label With {
            .Text = "Period:",
            .Location = New Point(28, 110),
            .AutoSize = True
        }
        workspace.Controls.Add(periodLabel)

        Dim periodPicker As New ComboBox With {
            .Location = New Point(85, 105),
            .Width = 145,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        periodPicker.Items.AddRange(
            New Object() {"Daily", "Weekly", "Monthly", "Yearly"})
        periodPicker.SelectedItem = "Daily"
        workspace.Controls.Add(periodPicker)

        Dim dateLabel As New Label With {
            .Text = "Reference date:",
            .Location = New Point(250, 110),
            .AutoSize = True
        }
        workspace.Controls.Add(dateLabel)

        Dim datePicker As New DateTimePicker With {
            .Location = New Point(355, 105),
            .Width = 180,
            .Format = DateTimePickerFormat.Short,
            .Value = DateTime.Today
        }
        workspace.Controls.Add(datePicker)

        Dim refreshButton As New Button With {
            .Text = "Load Report",
            .Location = New Point(550, 103),
            .Size = New Size(130, 35),
            .BackColor = primaryGreen,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        refreshButton.FlatAppearance.BorderSize = 0
        workspace.Controls.Add(refreshButton)

        Dim statusLabel As New Label With {
            .Text = "Choose a reporting period and date.",
            .Location = New Point(28, 150),
            .AutoSize = True,
            .ForeColor = Color.Gray
        }
        workspace.Controls.Add(statusLabel)

        Dim reportGrid As New DataGridView With {
            .Location = New Point(25, 185),
            .Size = New Size(1100, 420),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or
                      AnchorStyles.Right,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False
        }

        reportGrid.Columns.Add("asset_code", "Asset Code")
        reportGrid.Columns.Add("name", "Asset Name")
        reportGrid.Columns.Add("category", "Category")
        reportGrid.Columns.Add("acquisition_date", "Acquisition Date")
        reportGrid.Columns.Add("acquisition_cost", "Cost")
        reportGrid.Columns.Add("annual_depreciation", "Annual Depreciation")
        reportGrid.Columns.Add("accumulated_depreciation", "Accumulated Depreciation")
        reportGrid.Columns.Add("net_book_value", "Net Book Value")

        reportGrid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(228, 245, 232)
        reportGrid.ColumnHeadersDefaultCellStyle.ForeColor = deepGreen
        reportGrid.EnableHeadersVisualStyles = False
        reportGrid.ColumnHeadersHeight = 40
        reportGrid.RowTemplate.Height = 32

        workspace.Controls.Add(reportGrid)

        Dim loadReport As Func(Of Task) =
            Async Function()
                Dim period As String = periodPicker.SelectedItem.ToString()
                Dim referenceDate As DateTime = datePicker.Value.Date

                Dim range As (StartDate As DateTime, EndDate As DateTime) =
                    GetReportingRange(period, referenceDate)

                Await LoadLapsingScheduleAsync(
                    period,
                    range.StartDate,
                    range.EndDate,
                    reportGrid,
                    statusLabel,
                    refreshButton)
            End Function

        AddHandler refreshButton.Click,
            Async Sub(sender, e)
                Await loadReport()
            End Sub

        AddHandler periodPicker.SelectedIndexChanged,
            Sub(sender, e)
                If periodPicker.SelectedItem IsNot Nothing Then
                    statusLabel.Text =
                        "Selected period: " & periodPicker.SelectedItem.ToString() &
                        ". Click Load Report to refresh."
                End If
            End Sub

        Await loadReport()
    End Sub

    Private Function GetReportingRange(
        period As String,
        referenceDate As DateTime
    ) As (StartDate As DateTime, EndDate As DateTime)

        Select Case period
            Case "Weekly"
                Dim startDate As DateTime =
                    referenceDate.AddDays(
                        -((CInt(referenceDate.DayOfWeek) + 6) Mod 7))

                Return (startDate, startDate.AddDays(6))

            Case "Monthly"
                Dim startDate As New DateTime(
                    referenceDate.Year, referenceDate.Month, 1)

                Return (startDate, startDate.AddMonths(1).AddDays(-1))

            Case "Yearly"
                Return (
                    New DateTime(referenceDate.Year, 1, 1),
                    New DateTime(referenceDate.Year, 12, 31))

            Case Else
                Return (referenceDate.Date, referenceDate.Date)
        End Select
    End Function

    Private Async Function LoadLapsingScheduleAsync(
        period As String,
        startDate As DateTime,
        endDate As DateTime,
        reportGrid As DataGridView,
        statusLabel As Label,
        refreshButton As Button) As Task

        refreshButton.Enabled = False
        statusLabel.Text = "Loading report..."
        reportGrid.Rows.Clear()

        Try
   Dim url As String =
    "reports/lapsing-schedule?period=" &
    Uri.EscapeDataString(period.ToLowerInvariant()) &
    "&date=" & endDate.ToString("yyyy-MM-dd")

            Dim totalCost As Decimal = 0D
            Dim totalAnnual As Decimal = 0D
            Dim totalAccumulated As Decimal = 0D
            Dim totalNetBookValue As Decimal = 0D
            Dim assetCount As Integer = 0

           
ApiSession.ApplyAuthorization()

Using response As HttpResponseMessage =
    Await ApiSession.Client.GetAsync(url)

    Dim json As String =
        Await response.Content.ReadAsStringAsync()

    If Not response.IsSuccessStatusCode Then
        Throw New Exception(
            "API returned HTTP " &
            CInt(response.StatusCode).ToString() &
            ": " & json)
    End If

    Using document As JsonDocument =
        JsonDocument.Parse(json)

        Dim root As JsonElement = document.RootElement
        Dim assets As JsonElement

        If Not root.TryGetProperty("data", assets) OrElse
           assets.ValueKind <> JsonValueKind.Array Then
            Throw New Exception(
                "The API response does not contain a valid data array.")
        End If

        For Each asset As JsonElement In assets.EnumerateArray()
            Dim cost As Decimal =
                ReadJsonDecimal(asset, "acquisition_cost")
            Dim annual As Decimal =
                ReadJsonDecimal(asset, "annual_depreciation")
            Dim accumulated As Decimal =
                ReadJsonDecimal(asset, "accumulated_depreciation")
            Dim netValue As Decimal =
                ReadJsonDecimal(asset, "net_book_value")

            totalCost += cost
            totalAnnual += annual
            totalAccumulated += accumulated
            totalNetBookValue += netValue
            assetCount += 1

            reportGrid.Rows.Add(
                ReadJsonText(asset, "asset_code"),
                ReadJsonText(asset, "name"),
                ReadJsonText(asset, "category"),
                ReadJsonText(asset, "acquisition_date"),
                FormatPeso(cost),
                FormatPeso(annual),
                FormatPeso(accumulated),
                FormatPeso(netValue))
        Next
    End Using
End Using


            If assetCount > 0 Then
                Dim totalRowIndex As Integer = reportGrid.Rows.Add(
                    "",
                    "TOTAL",
                    "",
                    "",
                    FormatPeso(totalCost),
                    FormatPeso(totalAnnual),
                    FormatPeso(totalAccumulated),
                    FormatPeso(totalNetBookValue))

                With reportGrid.Rows(totalRowIndex)
                    .DefaultCellStyle.BackColor =
                        Color.FromArgb(228, 245, 232)
                    .DefaultCellStyle.ForeColor = deepGreen
                    .DefaultCellStyle.Font =
                        New Font("Segoe UI", 9, FontStyle.Bold)
                    .DefaultCellStyle.SelectionBackColor =
                        Color.FromArgb(210, 235, 216)
                    .DefaultCellStyle.SelectionForeColor = deepGreen
                End With
            End If

            statusLabel.Text =
                period & " | " &
                startDate.ToString("MMM dd, yyyy") & " - " &
                endDate.ToString("MMM dd, yyyy") &
                " | Depreciation as of " &
                endDate.ToString("MMM dd, yyyy") &
                " | Assets listed: " & assetCount.ToString()

        Catch ex As Exception
            statusLabel.Text = "Unable to load the report."

            MessageBox.Show(
                ex.Message,
                "Lapsing Schedule Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            refreshButton.Enabled = True
        End Try
    End Function

    Private Function ReadJsonText(
        element As JsonElement,
        propertyName As String) As String

        Dim value As JsonElement

        If Not element.TryGetProperty(propertyName, value) OrElse
           value.ValueKind = JsonValueKind.Null Then
            Return ""
        End If

        Return value.ToString()
    End Function

    Private Function ReadJsonDecimal(
        element As JsonElement,
        propertyName As String) As Decimal

        Dim value As JsonElement

        If Not element.TryGetProperty(propertyName, value) OrElse
           value.ValueKind = JsonValueKind.Null Then
            Return 0D
        End If

        Dim result As Decimal

        If Decimal.TryParse(
            value.ToString(),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            result) Then
            Return result
        End If

        Return 0D
    End Function

    Private Function FormatPeso(amount As Decimal) As String
        Return "₱" & amount.ToString(
            "#,##0.00",
            CultureInfo.GetCultureInfo("en-PH"))
    End Function

    Private Function FormatPeso(value As String) As String
        Dim amount As Decimal

        If Decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            amount) Then
            Return FormatPeso(amount)
        End If

        Return value
    End Function

End Class