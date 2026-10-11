Imports System.Net.Http
Imports System.Text.Json
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Public Class TrialBalanceForm
    Inherits Form

    Private ReadOnly reportGrid As New DataGridView()
    Private ReadOnly refreshButton As New Button()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly totalsLabel As New Label()

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
        .PropertyNameCaseInsensitive = True
    }

    Public Sub New()
        Me.Text = "Trial Balance"
        Me.Size = New Size(1100, 720)
        Me.MinimumSize = New Size(850, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(245, 249, 246)
        Me.Font = New Font("Segoe UI", 9.0F)

        BuildInterface()

        AddHandler Me.Load, AddressOf FormLoaded
        AddHandler refreshButton.Click, AddressOf RefreshButtonClicked
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
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 45))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 75))

        Dim title As New Label With {
            .Text = "Trial Balance",
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.FromArgb(37, 91, 64),
            .TextAlign = ContentAlignment.MiddleLeft
        }

        refreshButton.Text = "Refresh Report"
        refreshButton.Size = New Size(140, 34)
        refreshButton.Anchor = AnchorStyles.Right Or AnchorStyles.Top
        refreshButton.BackColor = Color.FromArgb(45, 125, 82)
        refreshButton.ForeColor = Color.White
        refreshButton.FlatStyle = FlatStyle.Flat
        refreshButton.FlatAppearance.BorderSize = 0

        Dim toolbar As New Panel With {
            .Dock = DockStyle.Fill
        }
        toolbar.Controls.Add(refreshButton)

        ConfigureGrid(reportGrid)

        Dim summaryPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = Color.FromArgb(232, 244, 235),
            .Padding = New Padding(10)
        }

        summaryPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
        summaryPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 50))

        totalsLabel.Dock = DockStyle.Fill
        totalsLabel.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        totalsLabel.ForeColor = Color.FromArgb(37, 91, 64)
        totalsLabel.TextAlign = ContentAlignment.MiddleLeft
        totalsLabel.Text = "Debit total: 0.00 | Credit total: 0.00"

        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.FromArgb(55, 90, 65)
        statusLabel.Text = "Load the report to check the balances."

        summaryPanel.Controls.Add(totalsLabel, 0, 0)
        summaryPanel.Controls.Add(statusLabel, 0, 1)

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(toolbar, 0, 1)
        root.Controls.Add(reportGrid, 0, 2)
        root.Controls.Add(summaryPanel, 0, 3)

        Me.Controls.Add(root)
    End Sub

    Private Sub ConfigureGrid(grid As DataGridView)
        grid.Dock = DockStyle.Fill
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
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

    Private Async Sub FormLoaded(sender As Object, e As EventArgs)
        Await LoadTrialBalanceAsync()
    End Sub

    Private Async Sub RefreshButtonClicked(
        sender As Object,
        e As EventArgs
    )
        Await LoadTrialBalanceAsync()
    End Sub

    Private Async Function LoadTrialBalanceAsync() As Task
        Try
            refreshButton.Enabled = False
            statusLabel.Text = "Loading trial balance..."
            reportGrid.DataSource = Nothing

            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync("trial-balance")

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
                        Of TrialBalanceResponse)(
                            body, jsonOptions)

                If result Is Nothing OrElse result.Data Is Nothing Then
                    statusLabel.Text =
                        "The API returned no trial balance data."
                    Return
                End If

                Dim rows = result.Data.Select(
                    Function(item) New With {
                        .AccountCode = item.AccountCode,
                        .AccountName = item.AccountName,
                        .AccountType = item.AccountType,
                        .DebitBalance = ToDecimal(item.DebitBalance),
                        .CreditBalance = ToDecimal(item.CreditBalance)
                    }).ToList()

                reportGrid.DataSource = rows

                SetColumnHeader("AccountCode", "Account Code")
                SetColumnHeader("AccountName", "Account Name")
                SetColumnHeader("AccountType", "Account Type")
                SetColumnHeader("DebitBalance", "Debit Balance")
                SetColumnHeader("CreditBalance", "Credit Balance")

                FormatMoneyColumn("DebitBalance")
                FormatMoneyColumn("CreditBalance")

                Dim totalDebits As Decimal =
                    ToDecimal(result.Totals.Debits)

                Dim totalCredits As Decimal =
                    ToDecimal(result.Totals.Credits)

                totalsLabel.Text =
                    $"Total Debits: {totalDebits:N2}    " &
                    $"Total Credits: {totalCredits:N2}"

                If result.Totals.IsBalanced Then
                    statusLabel.Text =
                        $"BALANCED — Total debits equal total credits. " &
                        $"Accounts displayed: {result.Data.Count}."
                    statusLabel.ForeColor =
                        Color.FromArgb(37, 91, 64)
                Else
                    statusLabel.Text =
                        $"NOT BALANCED — Difference: " &
                        $"{Math.Abs(totalDebits - totalCredits):N2}. " &
                        $"Accounts displayed: {result.Data.Count}."
                    statusLabel.ForeColor = Color.DarkRed
                End If
            End Using

        Catch ex As Exception
            statusLabel.Text = "Error: " & ex.Message
            statusLabel.ForeColor = Color.DarkRed
        Finally
            refreshButton.Enabled = True
        End Try
    End Function

    Private Sub SetColumnHeader(
        columnName As String,
        header As String
    )
        If reportGrid.Columns.Contains(columnName) Then
            reportGrid.Columns(columnName).HeaderText = header
        End If
    End Sub

    Private Sub FormatMoneyColumn(columnName As String)
        If reportGrid.Columns.Contains(columnName) Then
            reportGrid.Columns(columnName).DefaultCellStyle.Format = "N2"
            reportGrid.Columns(columnName).DefaultCellStyle.Alignment =
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

    Private Class TrialBalanceResponse
        Public Property Success As Boolean
        Public Property Data As List(Of TrialBalanceItem)
        Public Property Totals As TrialBalanceTotals
    End Class

    Private Class TrialBalanceItem
        Public Property AccountId As Integer
        Public Property AccountCode As String
        Public Property AccountName As String
        Public Property AccountType As String
        Public Property DebitBalance As String
        Public Property CreditBalance As String
    End Class

    Private Class TrialBalanceTotals
        Public Property Debits As String
        Public Property Credits As String
        Public Property IsBalanced As Boolean
    End Class

End Class