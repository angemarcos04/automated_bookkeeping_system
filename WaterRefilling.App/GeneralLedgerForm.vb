Imports System.Net.Http
Imports System.Text.Json
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class GeneralLedgerForm
    Inherits Form

    Private ReadOnly accountCombo As New ComboBox()
    Private ReadOnly startDatePicker As New DateTimePicker()
    Private ReadOnly endDatePicker As New DateTimePicker()
    Private ReadOnly searchButton As New Button()
    Private ReadOnly accountsGrid As New DataGridView()
    Private ReadOnly entriesGrid As New DataGridView()
    Private ReadOnly statusLabel As New Label()

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
    .PropertyNameCaseInsensitive = True,
    .PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
}

    Private accounts As New List(Of LedgerAccountItem)()

    Public Sub New()
        Me.Text = "General Ledger"
        Me.Size = New Size(1200, 780)
        Me.MinimumSize = New Size(950, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(245, 249, 246)
        Me.Font = New Font("Segoe UI", 9.0F)

        BuildInterface()

        AddHandler Me.Load, AddressOf LoadAccountsAsync
        AddHandler searchButton.Click, AddressOf SearchButtonClicked
        AddHandler accountsGrid.SelectionChanged, AddressOf AccountSelectionChanged
    End Sub

    Private Sub BuildInterface()
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .Padding = New Padding(16),
            .BackColor = Me.BackColor
        }

        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 55))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 48))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 52))

        Dim title As New Label With {
            .Text = "General Ledger",
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.FromArgb(37, 91, 64),
            .TextAlign = ContentAlignment.MiddleLeft
        }

        Dim filters As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 2,
            .Padding = New Padding(0, 5, 0, 5)
        }

        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))

        filters.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        filters.RowStyles.Add(New RowStyle(SizeType.Absolute, 35))

        Dim accountLabel As New Label With {
            .Text = "Account",
            .Dock = DockStyle.Fill
        }

        Dim startLabel As New Label With {
            .Text = "Start Date",
            .Dock = DockStyle.Fill
        }

        Dim endLabel As New Label With {
            .Text = "End Date",
            .Dock = DockStyle.Fill
        }

        accountCombo.Dock = DockStyle.Fill
        accountCombo.DropDownStyle = ComboBoxStyle.DropDownList

        startDatePicker.Dock = DockStyle.Fill
        startDatePicker.Format = DateTimePickerFormat.Short
        startDatePicker.Value = New Date(Date.Today.Year, 1, 1)

        endDatePicker.Dock = DockStyle.Fill
        endDatePicker.Format = DateTimePickerFormat.Short
        endDatePicker.Value = Date.Today

        searchButton.Text = "Load Ledger"
        searchButton.Dock = DockStyle.Fill
        searchButton.BackColor = Color.FromArgb(45, 125, 82)
        searchButton.ForeColor = Color.White
        searchButton.FlatStyle = FlatStyle.Flat
        searchButton.FlatAppearance.BorderSize = 0

        filters.Controls.Add(accountLabel, 0, 0)
        filters.Controls.Add(startLabel, 1, 0)
        filters.Controls.Add(endLabel, 2, 0)

        filters.Controls.Add(accountCombo, 0, 1)
        filters.Controls.Add(startDatePicker, 1, 1)
        filters.Controls.Add(endDatePicker, 2, 1)
        filters.Controls.Add(searchButton, 3, 1)

        ConfigureGrid(accountsGrid)
        ConfigureGrid(entriesGrid)

        accountsGrid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect
        accountsGrid.MultiSelect = False

        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.FromArgb(55, 90, 65)
        statusLabel.Text = "Choose an account and load the ledger."

        Dim accountPanel As New Panel With {.Dock = DockStyle.Fill}
        Dim accountTitle As New Label With {
            .Text = "Account Balances",
            .Dock = DockStyle.Top,
            .Height = 28,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.FromArgb(37, 91, 64)
        }
        accountPanel.Controls.Add(accountsGrid)
        accountPanel.Controls.Add(accountTitle)
        accountsGrid.BringToFront()

        Dim entryPanel As New Panel With {.Dock = DockStyle.Fill}
        Dim entryTitle As New Label With {
            .Text = "Posted Transactions",
            .Dock = DockStyle.Top,
            .Height = 28,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.FromArgb(37, 91, 64)
        }
        entryPanel.Controls.Add(entriesGrid)
        entryPanel.Controls.Add(entryTitle)
        entriesGrid.BringToFront()

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(filters, 0, 1)
        root.Controls.Add(accountPanel, 0, 2)
        root.Controls.Add(entryPanel, 0, 3)

        Me.Controls.Add(root)

        Dim footer As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 28,
            .BackColor = Me.BackColor
        }
        footer.Controls.Add(statusLabel)
        Me.Controls.Add(footer)
        footer.BringToFront()
    End Sub

    Private Sub ConfigureGrid(grid As DataGridView)
        grid.Dock = DockStyle.Fill
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill
        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False
        grid.RowHeadersVisible = False
        grid.BackgroundColor = Color.White
        grid.BorderStyle = BorderStyle.FixedSingle
        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(224, 240, 229)
        grid.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.FromArgb(35, 70, 48)
        grid.ColumnHeadersDefaultCellStyle.Font =
            New Font("Segoe UI", 9, FontStyle.Bold)
        grid.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(248, 251, 249)
    End Sub

    Private Async Sub LoadAccountsAsync(
        sender As Object,
        e As EventArgs
    )
        Await LoadLedgerAsync()
    End Sub

    Private Async Sub SearchButtonClicked(
        sender As Object,
        e As EventArgs
    )
        Await LoadLedgerAsync()
    End Sub

    Private Async Function LoadLedgerAsync() As Task
        Try
            searchButton.Enabled = False
            statusLabel.Text = "Loading general ledger..."
            accountsGrid.DataSource = Nothing
            entriesGrid.DataSource = Nothing

            If endDatePicker.Value.Date < startDatePicker.Value.Date Then
                MessageBox.Show(
                    "End date must be on or after start date.",
                    "Invalid Date Range",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If

            ApiSession.ApplyAuthorization()

            Dim url As String =
                "general-ledger?start_date=" &
                startDatePicker.Value.ToString("yyyy-MM-dd") &
                "&end_date=" &
                endDatePicker.Value.ToString("yyyy-MM-dd")

            If accountCombo.SelectedValue IsNot Nothing Then
                Dim accountId As Integer
                If Integer.TryParse(
                    accountCombo.SelectedValue.ToString(),
                    accountId) AndAlso accountId > 0 Then

                    url &= "&account_id=" & accountId.ToString()
                End If
            End If

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync(url)

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    statusLabel.Text =
                        "API error: " &
                        CInt(response.StatusCode).ToString() &
                        " - " & body
                    Return
                End If

                Dim result =
                    JsonSerializer.Deserialize(
                        Of ApiResponse(Of List(Of LedgerAccountItem)))(
                            body, jsonOptions)

                If result Is Nothing OrElse result.Data Is Nothing Then
                    statusLabel.Text =
                        "The API returned no ledger data."
                    Return
                End If

                accounts = result.Data

                If accountCombo.DataSource Is Nothing Then
                    accountCombo.DataSource = accounts.Select(
                        Function(a) New AccountChoice With {
                            .Id = a.AccountId,
                            .DisplayName =
                                a.AccountCode & " - " & a.AccountName
                        }).ToList()

                    accountCombo.DisplayMember = "DisplayName"
                    accountCombo.ValueMember = "Id"
                End If

                Dim accountRows = accounts.Select(
                    Function(a) New With {
                        .AccountCode = a.AccountCode,
                        .AccountName = a.AccountName,
                        .AccountType = a.AccountType,
                        .TotalDebits = ToDecimal(a.TotalDebits),
                        .TotalCredits = ToDecimal(a.TotalCredits),
                        .ClosingBalance = ToDecimal(a.ClosingBalance)
                    }).ToList()

                accountsGrid.DataSource = accountRows

                If accountsGrid.Columns.Contains("AccountCode") Then
                    accountsGrid.Columns("AccountCode").HeaderText =
                        "Account Code"
                End If

                If accountsGrid.Columns.Contains("AccountName") Then
                    accountsGrid.Columns("AccountName").HeaderText =
                        "Account Name"
                End If

                If accountsGrid.Columns.Contains("AccountType") Then
                    accountsGrid.Columns("AccountType").HeaderText =
                        "Account Type"
                End If

                FormatMoneyColumn(accountsGrid, "TotalDebits", "Total Debits")
                FormatMoneyColumn(accountsGrid, "TotalCredits", "Total Credits")
                FormatMoneyColumn(accountsGrid, "ClosingBalance", "Closing Balance")

                statusLabel.Text =
                    $"Loaded {accounts.Count} accounts for " &
                    $"{startDatePicker.Value:yyyy-MM-dd} to " &
                    $"{endDatePicker.Value:yyyy-MM-dd}."

                If accountsGrid.Rows.Count > 0 Then
    accountsGrid.CurrentCell = accountsGrid.Rows(0).Cells(0)
    ShowSelectedAccountEntries()
                End If
            End Using

        Catch ex As Exception
            statusLabel.Text = "Error: " & ex.Message
        Finally
            searchButton.Enabled = True
        End Try
    End Function

    Private Sub AccountSelectionChanged(
        sender As Object,
        e As EventArgs
    )
        ShowSelectedAccountEntries()
    End Sub

    Private Sub ShowSelectedAccountEntries()
        If accountsGrid.CurrentRow Is Nothing Then Return

        Dim codeValue = accountsGrid.CurrentRow.Cells("AccountCode").Value
        If codeValue Is Nothing Then Return

        Dim selectedAccount = accounts.FirstOrDefault(
            Function(a) a.AccountCode =
                codeValue.ToString())

        If selectedAccount Is Nothing Then Return

        Dim entryRows = selectedAccount.Entries.Select(
            Function(entry) New With {
                .Date = entry.JournalDate,
                .JournalNumber = entry.JournalNumber,
                .Description = entry.EntryDescription,
                .LineDescription = entry.LineDescription,
                .Debit = ToDecimal(entry.Debit),
                .Credit = ToDecimal(entry.Credit),
                .RunningBalance = ToDecimal(entry.RunningBalance)
            }).ToList()

        entriesGrid.DataSource = entryRows

        If entriesGrid.Columns.Contains("JournalNumber") Then
            entriesGrid.Columns("JournalNumber").HeaderText =
                "Journal Number"
        End If

        If entriesGrid.Columns.Contains("LineDescription") Then
            entriesGrid.Columns("LineDescription").HeaderText =
                "Line Description"
        End If

        FormatMoneyColumn(entriesGrid, "Debit", "Debit")
        FormatMoneyColumn(entriesGrid, "Credit", "Credit")
        FormatMoneyColumn(entriesGrid, "RunningBalance", "Running Balance")

        statusLabel.Text =
            $"{selectedAccount.AccountCode} - " &
            $"{selectedAccount.AccountName} | " &
            $"Opening: {ToDecimal(selectedAccount.OpeningBalance):N2} | " &
            $"Debits: {ToDecimal(selectedAccount.TotalDebits):N2} | " &
            $"Credits: {ToDecimal(selectedAccount.TotalCredits):N2} | " &
            $"Closing: {ToDecimal(selectedAccount.ClosingBalance):N2}"
        End Sub

    Private Sub FormatMoneyColumn(
        grid As DataGridView,
        columnName As String,
        header As String
    )
        If grid.Columns.Contains(columnName) Then
            grid.Columns(columnName).HeaderText = header
            grid.Columns(columnName).DefaultCellStyle.Format = "N2"
            grid.Columns(columnName).DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleRight
        End If
    End Sub

    Private Function ToDecimal(value As String) As Decimal
        Dim amount As Decimal
        Decimal.TryParse(
            value,
            Globalization.NumberStyles.Any,
            Globalization.CultureInfo.InvariantCulture,
            amount)
        Return amount
    End Function

    Private Class ApiResponse(Of T)
        Public Property Data As T
    End Class

    Private Class AccountChoice
        Public Property Id As Integer
        Public Property DisplayName As String
    End Class

    Private Class LedgerAccountItem
        Public Property AccountId As Integer
        Public Property AccountCode As String
        Public Property AccountName As String
        Public Property AccountType As String
        Public Property OpeningBalance As String
        Public Property TotalDebits As String
        Public Property TotalCredits As String
        Public Property ClosingBalance As String
        Public Property Entries As List(Of LedgerEntryItem) =
            New List(Of LedgerEntryItem)
    End Class

    Private Class LedgerEntryItem
        Public Property JournalEntryId As Integer
        Public Property JournalNumber As String
        Public Property JournalDate As String
        Public Property EntryDescription As String
        Public Property LineDescription As String
        Public Property Debit As String
        Public Property Credit As String
        Public Property RunningBalance As String
    End Class
End Class