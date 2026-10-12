Imports System.Drawing
Imports System.Net.Http
Imports System.Net.Http.Json
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Globalization
Imports System.Windows.Forms
Imports System.Linq
Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Class CashReceiptsForm
    Inherits Form

    Private ReadOnly receiptsGrid As New DataGridView()
    Private ReadOnly receiptDateInput As New DateTimePicker()
    Private ReadOnly orNumberInput As New TextBox()
    Private ReadOnly payorInput As New TextBox()
    Private ReadOnly accountCombo As New ComboBox()
    Private ReadOnly depositAccountCombo As New ComboBox()
    Private ReadOnly amountInput As New TextBox()
    Private ReadOnly bankInput As New TextBox()
    Private ReadOnly remarksInput As New TextBox()

    Private ReadOnly statusLabel As New Label()
    Private ReadOnly saveButton As New Button()
    Private ReadOnly refreshButton As New Button()
    Private ReadOnly postButton As New Button()

    ' Search controls
    Private ReadOnly searchInput As New TextBox()
    Private ReadOnly clearSearchButton As New Button()
    Private ReadOnly resultCountLabel As New Label()

    Private ReadOnly listPanel As New Panel()
    Private ReadOnly listTitle As New Label()
    Private ReadOnly searchPanel As New FlowLayoutPanel()

    Private allReceiptRows As New List(Of ReceiptDisplayRow)()

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
        .PropertyNameCaseInsensitive = True
    }

    Private Class ApiResponse(Of T)
        <JsonPropertyName("success")>
        Public Property Success As Boolean

        <JsonPropertyName("message")>
        Public Property Message As String

        <JsonPropertyName("data")>
        Public Property Data As T
    End Class

    Private Class AccountItem
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("account_code")>
        Public Property Code As String

        <JsonPropertyName("account_name")>
        Public Property Name As String

        <JsonPropertyName("account_type")>
        Public Property AccountType As String

        <JsonPropertyName("is_active")>
        Public Property IsActive As Boolean

        Public Overrides Function ToString() As String
            Return $"{Code} - {Name}"
        End Function
    End Class

    Private Class ReceiptItem
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("receipt_date")>
        Public Property ReceiptDate As String

        <JsonPropertyName("or_number")>
        Public Property OrNumber As String

        <JsonPropertyName("payor")>
        Public Property Payor As String

        <JsonPropertyName("account_id")>
        Public Property AccountId As Integer

        <JsonPropertyName("deposit_account_id")>
        Public Property DepositAccountId As Integer?

        <JsonPropertyName("amount")>
        Public Property Amount As JsonElement

        <JsonPropertyName("status")>
        Public Property Status As String

        <JsonPropertyName("bank_deposited")>
        Public Property BankDeposited As String

        <JsonPropertyName("remarks")>
        Public Property Remarks As String

        <JsonPropertyName("account")>
        Public Property Account As AccountItem

        <JsonPropertyName("deposit_account")>
        Public Property DepositAccount As AccountItem

        <JsonIgnore>
        Public ReadOnly Property AmountValue As Decimal
            Get
                Try
                    If Amount.ValueKind = JsonValueKind.Number Then
                        Return Amount.GetDecimal()
                    End If

                    If Amount.ValueKind = JsonValueKind.String Then
                        Dim parsedAmount As Decimal

                        If Decimal.TryParse(
                            Amount.GetString(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            parsedAmount) Then

                            Return parsedAmount
                        End If
                    End If
                Catch
                End Try

                Return 0D
            End Get
        End Property
    End Class

    Private Class ReceiptDisplayRow
        Public Property Id As Integer
        Public Property ReceiptDate As String
        Public Property OrNumber As String
        Public Property Payor As String
        Public Property GlAccount As String
        Public Property DepositAccount As String
        Public Property Amount As Decimal
        Public Property Bank As String
        Public Property Status As String
        Public Property Remarks As String
    End Class

    Public Sub New()
        Text = "Cash Receipts Book"
        StartPosition = FormStartPosition.CenterScreen
        Size = New Size(1350, 800)
        MinimumSize = New Size(1100, 680)
        BackColor = Color.FromArgb(245, 248, 246)
        Font = New Font("Segoe UI", 9.0F)

        BuildInterface()

        AddHandler Shown, AddressOf FormShown
    End Sub

    Private Sub BuildInterface()
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(18),
            .BackColor = BackColor
        }

        root.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 100.0F))

        root.RowStyles.Add(
            New RowStyle(SizeType.Absolute, 70.0F))

        root.RowStyles.Add(
            New RowStyle(SizeType.Absolute, 265.0F))

        root.RowStyles.Add(
            New RowStyle(SizeType.Percent, 100.0F))

        Controls.Add(root)

        ' Header
        Dim headerPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = BackColor
        }

        Dim titleLabel As New Label With {
            .Text = "Cash Receipts Book",
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(35, 75, 55),
            .AutoSize = True,
            .Location = New Point(0, 0)
        }

        Dim subtitleLabel As New Label With {
            .Text = "Record and manage money received by the organization.",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(100, 110, 104),
            .AutoSize = True,
            .Location = New Point(2, 39)
        }

        headerPanel.Controls.Add(titleLabel)
        headerPanel.Controls.Add(subtitleLabel)

        root.Controls.Add(headerPanel, 0, 0)

        ' Receipt entry panel
        Dim entryPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 5,
            .Padding = New Padding(14),
            .BackColor = Color.White
        }

        For i As Integer = 0 To 3
            entryPanel.ColumnStyles.Add(
                New ColumnStyle(SizeType.Percent, 25.0F))
        Next

        For i As Integer = 0 To 3
            entryPanel.RowStyles.Add(
                New RowStyle(SizeType.Percent, 22.0F))
        Next

        entryPanel.RowStyles.Add(
            New RowStyle(SizeType.Percent, 12.0F))

        root.Controls.Add(entryPanel, 0, 1)

        ConfigureInput(receiptDateInput)
        receiptDateInput.Format = DateTimePickerFormat.Custom
        receiptDateInput.CustomFormat = "yyyy-MM-dd"

        ConfigureInput(orNumberInput)
        ConfigureInput(payorInput)
        ConfigureInput(accountCombo)
        ConfigureInput(depositAccountCombo)
        ConfigureInput(amountInput)
        ConfigureInput(bankInput)
        ConfigureInput(remarksInput)

        accountCombo.DropDownStyle = ComboBoxStyle.DropDownList
        depositAccountCombo.DropDownStyle = ComboBoxStyle.DropDownList

        amountInput.TextAlign = HorizontalAlignment.Right

        AddField(entryPanel, "Receipt Date", receiptDateInput, 0, 0)
        AddField(entryPanel, "OR Number", orNumberInput, 1, 0)
        AddField(entryPanel, "Payor", payorInput, 2, 0)
        AddField(entryPanel, "Income Account", accountCombo, 3, 0)

        AddField(entryPanel, "Deposit Account", depositAccountCombo, 0, 1)
        AddField(entryPanel, "Amount", amountInput, 1, 1)
        AddField(entryPanel, "Bank Deposited", bankInput, 2, 1)
        AddField(entryPanel, "Remarks", remarksInput, 3, 1)

        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.FromArgb(75, 85, 80)
        statusLabel.AutoEllipsis = True

        saveButton.Text = "Save Receipt"
        saveButton.Width = 115
        saveButton.Height = 32
        saveButton.BackColor = Color.FromArgb(36, 112, 76)
        saveButton.ForeColor = Color.White
        saveButton.FlatStyle = FlatStyle.Flat
        saveButton.FlatAppearance.BorderSize = 0
        AddHandler saveButton.Click, AddressOf SaveReceiptClicked

        refreshButton.Text = "Refresh"
        refreshButton.Width = 90
        refreshButton.Height = 32
        AddHandler refreshButton.Click, AddressOf RefreshClicked

        postButton.Text = "Post Selected"
        postButton.Width = 115
        postButton.Height = 32
        postButton.Enabled = False
        postButton.BackColor = Color.FromArgb(40, 90, 150)
        postButton.ForeColor = Color.White
        postButton.FlatStyle = FlatStyle.Flat
        postButton.FlatAppearance.BorderSize = 0
        AddHandler postButton.Click, AddressOf PostReceiptClicked

        Dim buttonPanel As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False,
            .BackColor = Color.White,
            .Padding = New Padding(0, 3, 0, 0)
        }

        buttonPanel.Controls.Add(postButton)
        buttonPanel.Controls.Add(refreshButton)
        buttonPanel.Controls.Add(saveButton)

        entryPanel.Controls.Add(statusLabel, 0, 4)
        entryPanel.SetColumnSpan(statusLabel, 1)

        entryPanel.Controls.Add(buttonPanel, 1, 4)
        entryPanel.SetColumnSpan(buttonPanel, 3)

        ' Receipt history grid
        ConfigureGrid()

        ' History panel and search bar
        listPanel.Dock = DockStyle.Fill
        listPanel.Padding = New Padding(0, 12, 0, 0)
        listPanel.BackColor = BackColor

        listTitle.Text = "Receipt History"
        listTitle.Dock = DockStyle.Top
        listTitle.Height = 35
        listTitle.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        listTitle.ForeColor = Color.FromArgb(35, 75, 55)
        listTitle.TextAlign = ContentAlignment.MiddleLeft

        searchPanel.Dock = DockStyle.Top
        searchPanel.Height = 42
        searchPanel.WrapContents = False
        searchPanel.FlowDirection = FlowDirection.LeftToRight
        searchPanel.Padding = New Padding(0, 3, 0, 5)
        searchPanel.BackColor = BackColor

        searchInput.Width = 330
        searchInput.Height = 28
        searchInput.Font = New Font("Segoe UI", 9.0F)
        searchInput.PlaceholderText =
            "Search OR number, payor, account, bank, status..."

        clearSearchButton.Text = "Clear Search"
        clearSearchButton.AutoSize = True
        clearSearchButton.Height = 28
        clearSearchButton.Margin = New Padding(8, 0, 8, 0)

        resultCountLabel.AutoSize = True
        resultCountLabel.Text = "0 receipts"
        resultCountLabel.TextAlign = ContentAlignment.MiddleLeft
        resultCountLabel.Margin = New Padding(8, 6, 0, 0)
        resultCountLabel.ForeColor = Color.FromArgb(85, 95, 90)

        AddHandler searchInput.TextChanged, AddressOf SearchReceipts
        AddHandler clearSearchButton.Click, AddressOf ClearSearchClicked

        searchPanel.Controls.Add(searchInput)
        searchPanel.Controls.Add(clearSearchButton)
        searchPanel.Controls.Add(resultCountLabel)

        listPanel.Controls.Add(receiptsGrid)
        listPanel.Controls.Add(searchPanel)
        listPanel.Controls.Add(listTitle)

        receiptsGrid.BringToFront()
        searchPanel.BringToFront()
        listTitle.BringToFront()

        root.Controls.Add(listPanel, 0, 2)
    End Sub

    Private Sub ConfigureInput(control As Control)
        control.Dock = DockStyle.Fill
        control.Margin = New Padding(3, 5, 10, 5)
        control.Font = New Font("Segoe UI", 9.0F)
    End Sub

    Private Sub AddField(
        parent As TableLayoutPanel,
        caption As String,
        input As Control,
        column As Integer,
        row As Integer)

        Dim fieldPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = Color.White,
            .Margin = New Padding(3, 0, 8, 0)
        }

        fieldPanel.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 100.0F))

        fieldPanel.RowStyles.Add(
            New RowStyle(SizeType.Absolute, 22.0F))

        fieldPanel.RowStyles.Add(
            New RowStyle(SizeType.Percent, 100.0F))

        Dim label As New Label With {
            .Text = caption,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = Color.FromArgb(65, 75, 69),
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        }

        input.Margin = New Padding(0, 2, 0, 5)

        fieldPanel.Controls.Add(label, 0, 0)
        fieldPanel.Controls.Add(input, 0, 1)

        parent.Controls.Add(fieldPanel, column, row)
    End Sub

    Private Sub ConfigureGrid()
        receiptsGrid.Dock = DockStyle.Fill
        receiptsGrid.BackgroundColor = Color.White
        receiptsGrid.BorderStyle = BorderStyle.None
        receiptsGrid.ReadOnly = True
        receiptsGrid.AllowUserToAddRows = False
        receiptsGrid.AllowUserToDeleteRows = False
        receiptsGrid.AllowUserToResizeRows = False
        receiptsGrid.MultiSelect = False
        receiptsGrid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect
        receiptsGrid.AutoGenerateColumns = False
        receiptsGrid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill
        receiptsGrid.RowHeadersVisible = False
        receiptsGrid.RowTemplate.Height = 30
        receiptsGrid.EnableHeadersVisualStyles = False
        receiptsGrid.ColumnHeadersHeight = 36
        receiptsGrid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(229, 238, 232)
        receiptsGrid.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.FromArgb(35, 75, 55)
        receiptsGrid.ColumnHeadersDefaultCellStyle.Font =
            New Font("Segoe UI", 9.0F, FontStyle.Bold)
        receiptsGrid.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(215, 235, 222)
        receiptsGrid.DefaultCellStyle.SelectionForeColor =
            Color.Black

        receiptsGrid.Columns.Clear()

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Id",
            .DataPropertyName = "Id",
            .Visible = False
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "ReceiptDate",
            .HeaderText = "Receipt Date",
            .DataPropertyName = "ReceiptDate",
            .FillWeight = 90
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "OrNumber",
            .HeaderText = "OR Number",
            .DataPropertyName = "OrNumber",
            .FillWeight = 90
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Payor",
            .HeaderText = "Payor",
            .DataPropertyName = "Payor",
            .FillWeight = 115
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "GlAccount",
            .HeaderText = "Income Account",
            .DataPropertyName = "GlAccount",
            .FillWeight = 125
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "DepositAccount",
            .HeaderText = "Deposit Account",
            .DataPropertyName = "DepositAccount",
            .FillWeight = 125
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Amount",
            .HeaderText = "Amount",
            .DataPropertyName = "Amount",
            .FillWeight = 85,
            .DefaultCellStyle = New DataGridViewCellStyle With {
                .Format = "N2",
                .Alignment = DataGridViewContentAlignment.MiddleRight
            }
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Bank",
            .HeaderText = "Bank",
            .DataPropertyName = "Bank",
            .FillWeight = 90
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Status",
            .HeaderText = "Status",
            .DataPropertyName = "Status",
            .FillWeight = 75
        })

        AddHandler receiptsGrid.SelectionChanged,
            AddressOf ReceiptSelectionChanged
    End Sub

    Private Async Sub FormShown(sender As Object, e As EventArgs)
        Await LoadAccountsAsync()
        Await LoadReceiptsAsync()
    End Sub

    Private Async Function LoadAccountsAsync() As Task
        Try
            Dim response = Await ApiSession.Client.GetAsync("accounts?is_active=1")

            If Not response.IsSuccessStatusCode Then
                SetStatus("Unable to load accounts.")
                Return
            End If

            Dim result = Await response.Content.ReadFromJsonAsync(
                Of ApiResponse(Of List(Of AccountItem)))(jsonOptions)

            If result Is Nothing OrElse result.Data Is Nothing Then
                SetStatus("No account data was returned.")
                Return
            End If

            accountCombo.Items.Clear()
            depositAccountCombo.Items.Clear()

            For Each account In result.Data
                If String.Equals(
                    account.AccountType,
                    "Asset",
                    StringComparison.OrdinalIgnoreCase) Then

                    depositAccountCombo.Items.Add(account)
                Else
                    accountCombo.Items.Add(account)
                End If
            Next

            If accountCombo.Items.Count > 0 Then
                accountCombo.SelectedIndex = 0
            End If

            If depositAccountCombo.Items.Count > 0 Then
                depositAccountCombo.SelectedIndex = 0
            End If

            SetStatus("Accounts loaded.")
        Catch ex As Exception
            SetStatus("Account loading failed: " & ex.Message)
        End Try
    End Function

    Private Async Function LoadReceiptsAsync() As Task
        Try
            Dim response = Await ApiSession.Client.GetAsync("cash-receipts")

            If Not response.IsSuccessStatusCode Then
                SetStatus("Unable to load receipts.")
                Return
            End If

            Dim result = Await response.Content.ReadFromJsonAsync(
                Of ApiResponse(Of List(Of ReceiptItem)))(jsonOptions)

            Dim receipts As List(Of ReceiptItem) =
                If(result IsNot Nothing AndAlso result.Data IsNot Nothing,
                   result.Data,
                   New List(Of ReceiptItem)())

            allReceiptRows = receipts.Select(
                Function(r) New ReceiptDisplayRow With {
                    .Id = r.Id,
                    .ReceiptDate = If(r.ReceiptDate, ""),
                    .OrNumber = If(r.OrNumber, ""),
                    .Payor = If(r.Payor, ""),
                    .GlAccount = If(
                        r.Account IsNot Nothing,
                        r.Account.Code & " - " & r.Account.Name,
                        r.AccountId.ToString()),
                    .DepositAccount = If(
                        r.DepositAccount IsNot Nothing,
                        r.DepositAccount.Code & " - " & r.DepositAccount.Name,
                        If(
                            r.DepositAccountId.HasValue,
                            r.DepositAccountId.Value.ToString(),
                            "")),
                    .Amount = r.AmountValue,
                    .Bank = If(r.BankDeposited, ""),
                    .Status = If(
                        String.IsNullOrWhiteSpace(r.Status),
                        "Unknown",
                        r.Status),
                    .Remarks = If(r.Remarks, "")
                }).ToList()

            SearchReceipts(searchInput, EventArgs.Empty)

            SetStatus($"{receipts.Count} receipt(s) loaded.")
        Catch ex As Exception
            SetStatus("Receipt loading failed: " & ex.Message)
        End Try
    End Function

    Private Sub SearchReceipts(sender As Object, e As EventArgs)
        Dim keyword As String = searchInput.Text.Trim()

        Dim filteredRows As List(Of ReceiptDisplayRow)

        If String.IsNullOrWhiteSpace(keyword) Then
            filteredRows = allReceiptRows.ToList()
        Else
            filteredRows = allReceiptRows.Where(
                Function(r)
                    Return ContainsText(r.OrNumber, keyword) OrElse
                        ContainsText(r.Payor, keyword) OrElse
                        ContainsText(r.GlAccount, keyword) OrElse
                        ContainsText(r.DepositAccount, keyword) OrElse
                        ContainsText(r.Bank, keyword) OrElse
                        ContainsText(r.Status, keyword) OrElse
                        ContainsText(r.ReceiptDate, keyword) OrElse
                        ContainsText(r.Remarks, keyword) OrElse
                        ContainsText(r.Amount.ToString(
                            CultureInfo.InvariantCulture), keyword) OrElse
                        ContainsText(r.Amount.ToString("N2"), keyword)
                End Function
            ).ToList()
        End If

        receiptsGrid.DataSource = Nothing
        receiptsGrid.DataSource = filteredRows

        resultCountLabel.Text =
            $"{filteredRows.Count} of {allReceiptRows.Count} receipt(s)"

        UpdatePostButton()
    End Sub

    Private Function ContainsText(value As String, keyword As String) As Boolean
        Return If(value, "").IndexOf(
            keyword,
            StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Sub ClearSearchClicked(sender As Object, e As EventArgs)
        searchInput.Clear()
    End Sub

    Private Sub ReceiptSelectionChanged(sender As Object, e As EventArgs)
        UpdatePostButton()
    End Sub

    Private Sub UpdatePostButton()
        postButton.Enabled = False

        If receiptsGrid.CurrentRow Is Nothing Then
            Return
        End If

        If receiptsGrid.CurrentRow.IsNewRow Then
            Return
        End If

        Dim statusValue As Object =
            receiptsGrid.CurrentRow.Cells("Status").Value

        If statusValue Is Nothing Then
            Return
        End If

        postButton.Enabled = String.Equals(
            statusValue.ToString(),
            "Draft",
            StringComparison.OrdinalIgnoreCase)
    End Sub

    Private Async Sub SaveReceiptClicked(sender As Object, e As EventArgs)
        Dim selectedAccount = TryCast(accountCombo.SelectedItem, AccountItem)
        Dim selectedDepositAccount =
            TryCast(depositAccountCombo.SelectedItem, AccountItem)

        Dim amount As Decimal

        If String.IsNullOrWhiteSpace(orNumberInput.Text) Then
            SetStatus("Enter the OR number.")
            Return
        End If

        If String.IsNullOrWhiteSpace(payorInput.Text) Then
            SetStatus("Enter the payor.")
            Return
        End If

        If selectedAccount Is Nothing Then
            SetStatus("Select an income account.")
            Return
        End If

        If selectedDepositAccount Is Nothing Then
            SetStatus("Select a deposit account.")
            Return
        End If

        If selectedAccount.Id = selectedDepositAccount.Id Then
            SetStatus("The income account and deposit account must be different.")
            Return
        End If

        If Not Decimal.TryParse(
            amountInput.Text,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            amount) OrElse amount <= 0D Then

            SetStatus("Enter a valid amount greater than zero.")
            Return
        End If

        Dim payload = New With {
            .receipt_date = receiptDateInput.Value.ToString("yyyy-MM-dd"),
            .or_number = orNumberInput.Text.Trim(),
            .payor = payorInput.Text.Trim(),
            .account_id = selectedAccount.Id,
            .deposit_account_id = selectedDepositAccount.Id,
            .amount = amount,
            .bank_deposited = bankInput.Text.Trim(),
            .remarks = remarksInput.Text.Trim()
        }

        saveButton.Enabled = False

        Try
            Dim response = Await ApiSession.Client.PostAsJsonAsync(
                "cash-receipts",
                payload)

            Dim responseText = Await response.Content.ReadAsStringAsync()

            If Not response.IsSuccessStatusCode Then
                SetStatus("Unable to save receipt: " & responseText)
                Return
            End If

            SetStatus("Receipt saved successfully.")
            ClearInputs()
            Await LoadReceiptsAsync()

        Catch ex As Exception
            SetStatus("Saving receipt failed: " & ex.Message)
        Finally
            saveButton.Enabled = True
        End Try
    End Sub

    Private Async Sub PostReceiptClicked(sender As Object, e As EventArgs)
        If receiptsGrid.CurrentRow Is Nothing Then
            SetStatus("Select a draft receipt first.")
            Return
        End If

        Dim idValue As Object = receiptsGrid.CurrentRow.Cells("Id").Value
        Dim receiptId As Integer

        If idValue Is Nothing OrElse
           Not Integer.TryParse(idValue.ToString(), receiptId) Then

            SetStatus("The selected receipt ID is invalid.")
            Return
        End If

        Dim statusValue As Object =
            receiptsGrid.CurrentRow.Cells("Status").Value

        If statusValue Is Nothing OrElse
           Not String.Equals(
               statusValue.ToString(),
               "Draft",
               StringComparison.OrdinalIgnoreCase) Then

            SetStatus("Only draft receipts can be posted.")
            Return
        End If

        Dim confirmation = MessageBox.Show(
            "Post this receipt? Make sure its details are correct.",
            "Confirm Posting",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If confirmation <> DialogResult.Yes Then
            Return
        End If

        postButton.Enabled = False

        Try
            Dim response = Await ApiSession.Client.PostAsync(
                $"cash-receipts/{receiptId}/post",
                Nothing)

            Dim responseText = Await response.Content.ReadAsStringAsync()

            If Not response.IsSuccessStatusCode Then
                SetStatus("Unable to post receipt: " & responseText)
                Return
            End If

            SetStatus("Receipt posted successfully.")
            Await LoadReceiptsAsync()

        Catch ex As Exception
            SetStatus("Posting receipt failed: " & ex.Message)
        Finally
            UpdatePostButton()
        End Try
    End Sub

    Private Async Sub RefreshClicked(sender As Object, e As EventArgs)
        refreshButton.Enabled = False

        Try
            Await LoadAccountsAsync()
            Await LoadReceiptsAsync()
            SetStatus("Receipt history refreshed.")
        Finally
            refreshButton.Enabled = True
        End Try
    End Sub

    Private Sub ClearInputs()
        receiptDateInput.Value = DateTime.Today
        orNumberInput.Clear()
        payorInput.Clear()
        amountInput.Clear()
        bankInput.Clear()
        remarksInput.Clear()

        If accountCombo.Items.Count > 0 Then
            accountCombo.SelectedIndex = 0
        End If

        If depositAccountCombo.Items.Count > 0 Then
            depositAccountCombo.SelectedIndex = 0
        End If
    End Sub

    Private Sub SetStatus(message As String)
        statusLabel.Text = message

        If message.IndexOf(
            "failed",
            StringComparison.OrdinalIgnoreCase) >= 0 OrElse
           message.IndexOf(
            "unable",
            StringComparison.OrdinalIgnoreCase) >= 0 OrElse
           message.IndexOf(
            "invalid",
            StringComparison.OrdinalIgnoreCase) >= 0 Then

            statusLabel.ForeColor = Color.Firebrick
        Else
            statusLabel.ForeColor = Color.FromArgb(75, 85, 80)
        End If
    End Sub

End Class