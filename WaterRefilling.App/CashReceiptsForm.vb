
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
    Private ReadOnly receiptDatePicker As New DateTimePicker()
    Private ReadOnly orNumberInput As New TextBox()
    Private ReadOnly payorInput As New TextBox()
    Private ReadOnly glAccountCombo As New ComboBox()
    Private ReadOnly depositAccountCombo As New ComboBox()
    Private ReadOnly amountInput As New TextBox()
    Private ReadOnly bankInput As New TextBox()
    Private ReadOnly remarksInput As New TextBox()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly saveButton As New Button()
    Private ReadOnly refreshButton As New Button()

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
            Return $"{Code} - {Name} ({AccountType})"
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

        ' Accept the API amount as either a JSON number or a numeric string.
        <JsonPropertyName("amount")>
        Public Property Amount As JsonElement

        Public ReadOnly Property AmountValue As Decimal
            Get
                If Amount.ValueKind = JsonValueKind.Number Then
                    Return Amount.GetDecimal()
                End If

                If Amount.ValueKind = JsonValueKind.String Then
                    Dim parsed As Decimal

                    If Decimal.TryParse(
                        Amount.GetString(),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        parsed) Then
                        Return parsed
                    End If
                End If

                Return 0D
            End Get
        End Property

        <JsonPropertyName("bank_deposited")>
        Public Property BankDeposited As String

        <JsonPropertyName("remarks")>
        Public Property Remarks As String

        <JsonPropertyName("account")>
        Public Property Account As AccountItem

        <JsonPropertyName("deposit_account")>
        Public Property DepositAccount As AccountItem
    End Class

    Public Sub New()
        Text = "Cash Receipts Book"
        StartPosition = FormStartPosition.CenterScreen
        Size = New Size(1250, 780)
        MinimumSize = New Size(1050, 650)
        BackColor = Color.FromArgb(245, 250, 246)
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

        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 265))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        Controls.Add(root)

        Dim header As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = BackColor
        }

        Dim title As New Label With {
            .Text = "Cash Receipts Book",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.FromArgb(38, 95, 65),
            .AutoSize = True,
            .Location = New Point(0, 0)
        }

        Dim subtitle As New Label With {
            .Text = "Record incoming funds and automatically create the accounting entry.",
            .ForeColor = Color.FromArgb(90, 110, 98),
            .AutoSize = True,
            .Location = New Point(2, 40)
        }

        header.Controls.Add(title)
        header.Controls.Add(subtitle)
        root.Controls.Add(header, 0, 0)

        Dim entryPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 5,
            .Padding = New Padding(14),
            .BackColor = Color.White,
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        }

        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 16))
        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34))
        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 16))
        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34))

        For i As Integer = 0 To 3
            entryPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 20))
        Next
        entryPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))

        ConfigureInput(receiptDatePicker)
        receiptDatePicker.Format = DateTimePickerFormat.Short
        ConfigureInput(orNumberInput)
        ConfigureInput(payorInput)
        ConfigureInput(glAccountCombo)
        ConfigureInput(depositAccountCombo)
        ConfigureInput(amountInput)
        ConfigureInput(bankInput)
        ConfigureInput(remarksInput)

        glAccountCombo.DropDownStyle = ComboBoxStyle.DropDownList
        depositAccountCombo.DropDownStyle = ComboBoxStyle.DropDownList
        remarksInput.Multiline = True
        remarksInput.ScrollBars = ScrollBars.Vertical

        AddField(entryPanel, "Receipt Date", receiptDatePicker, 0, 0)
        AddField(entryPanel, "OR Number", orNumberInput, 1, 0)
        AddField(entryPanel, "Payor", payorInput, 0, 1)
        AddField(entryPanel, "GL / Credit Account", glAccountCombo, 1, 1)
        AddField(entryPanel, "Deposit / Cash Account", depositAccountCombo, 0, 2)
        AddField(entryPanel, "Amount", amountInput, 1, 2)
        AddField(entryPanel, "Bank Deposited", bankInput, 0, 3)
        AddField(entryPanel, "Remarks", remarksInput, 1, 3)

        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.FromArgb(65, 85, 72)

        saveButton.Text = "Save Receipt"
        saveButton.Width = 130
        saveButton.Height = 32
        saveButton.BackColor = Color.FromArgb(42, 125, 82)
        saveButton.ForeColor = Color.White
        saveButton.FlatStyle = FlatStyle.Flat
        saveButton.FlatAppearance.BorderSize = 0
        AddHandler saveButton.Click, AddressOf SaveReceiptClicked

        refreshButton.Text = "Refresh List"
        refreshButton.Width = 110
        refreshButton.Height = 32
        AddHandler refreshButton.Click, AddressOf RefreshClicked

        Dim buttons As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False
        }

        buttons.Controls.Add(saveButton)
        buttons.Controls.Add(refreshButton)

        entryPanel.Controls.Add(statusLabel, 0, 4)
        entryPanel.SetColumnSpan(statusLabel, 2)
        entryPanel.Controls.Add(buttons, 2, 4)
        entryPanel.SetColumnSpan(buttons, 2)

        root.Controls.Add(entryPanel, 0, 1)

        receiptsGrid.Dock = DockStyle.Fill
        receiptsGrid.ReadOnly = True
        receiptsGrid.AllowUserToAddRows = False
        receiptsGrid.AllowUserToDeleteRows = False
        receiptsGrid.AutoGenerateColumns = False
        receiptsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        receiptsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        receiptsGrid.MultiSelect = False
        receiptsGrid.BackgroundColor = Color.White
        receiptsGrid.BorderStyle = BorderStyle.None
        receiptsGrid.RowHeadersVisible = False

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "ReceiptDate",
            .HeaderText = "Receipt Date",
            .DataPropertyName = "ReceiptDate"
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "OrNumber",
            .HeaderText = "OR Number",
            .DataPropertyName = "OrNumber"
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Payor",
            .HeaderText = "Payor",
            .DataPropertyName = "Payor"
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "GlAccount",
            .HeaderText = "GL / Credit Account",
            .DataPropertyName = "GlAccount"
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "DepositAccount",
            .HeaderText = "Deposit Account",
            .DataPropertyName = "DepositAccount"
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Amount",
            .HeaderText = "Amount",
            .DataPropertyName = "Amount",
            .DefaultCellStyle = New DataGridViewCellStyle With {
                .Format = "N2"
            }
        })

        receiptsGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Bank",
            .HeaderText = "Bank Deposited",
            .DataPropertyName = "Bank"
        })

        Dim listPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(0, 12, 0, 0),
            .BackColor = BackColor
        }

        Dim listTitle As New Label With {
            .Text = "Receipt History",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = Color.FromArgb(38, 95, 65),
            .Dock = DockStyle.Top,
            .Height = 35
        }

        listPanel.Controls.Add(receiptsGrid)
        listPanel.Controls.Add(listTitle)
        receiptsGrid.BringToFront()
        root.Controls.Add(listPanel, 0, 2)
    End Sub

    Private Sub ConfigureInput(control As Control)
        control.Dock = DockStyle.Fill
        control.Margin = New Padding(5)
    End Sub

    Private Sub AddField(
        panel As TableLayoutPanel,
        caption As String,
        input As Control,
        pairIndex As Integer,
        rowIndex As Integer)

        Dim labelColumn As Integer = pairIndex * 2
        Dim inputColumn As Integer = labelColumn + 1

        Dim label As New Label With {
            .Text = caption,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = Color.FromArgb(50, 75, 60),
            .Margin = New Padding(4)
        }

        panel.Controls.Add(label, labelColumn, rowIndex)
        panel.Controls.Add(input, inputColumn, rowIndex)
    End Sub

    Private Async Sub FormShown(sender As Object, e As EventArgs)
        Await LoadAccountsAsync()
        Await LoadReceiptsAsync()
    End Sub

    Private Async Function LoadAccountsAsync() As Task
        Try
            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync("accounts?is_active=1")

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(body)
                End If

                Dim result = JsonSerializer.Deserialize(
                    Of ApiResponse(Of List(Of AccountItem)))(body, jsonOptions)

                Dim accounts As List(Of AccountItem) =
                    If(result?.Data, New List(Of AccountItem)())

                glAccountCombo.Items.Clear()
                depositAccountCombo.Items.Clear()

                For Each account As AccountItem In accounts
                    If account.AccountType <> "Asset" Then
                        glAccountCombo.Items.Add(account)
                    End If

                    If account.AccountType = "Asset" Then
                        depositAccountCombo.Items.Add(account)
                    End If
                Next

                If glAccountCombo.Items.Count > 0 Then
                    glAccountCombo.SelectedIndex = 0
                End If

                If depositAccountCombo.Items.Count > 0 Then
                    depositAccountCombo.SelectedIndex = 0
                End If

                SetStatus("Active accounts loaded.")
            End Using

        Catch ex As Exception
            SetStatus("Could not load accounts: " & ex.Message, True)
        End Try
    End Function

    Private Async Function LoadReceiptsAsync() As Task
        Try
            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync("cash-receipts")

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(body)
                End If

                Dim result = JsonSerializer.Deserialize(
                    Of ApiResponse(Of List(Of ReceiptItem)))(body, jsonOptions)

                Dim receipts As List(Of ReceiptItem) =
                    If(result?.Data, New List(Of ReceiptItem)())

                Dim displayRows = receipts.Select(
                    Function(r) New With {
                        .ReceiptDate = r.ReceiptDate,
                        .OrNumber = r.OrNumber,
                        .Payor = r.Payor,
                        .GlAccount = If(r.Account IsNot Nothing,
                                        r.Account.Code & " - " & r.Account.Name,
                                        r.AccountId.ToString()),
                        .DepositAccount = If(r.DepositAccount IsNot Nothing,
                                             r.DepositAccount.Code & " - " & r.DepositAccount.Name,
                                             If(r.DepositAccountId.HasValue,
                                                r.DepositAccountId.Value.ToString(),
                                                "")),
                        .Amount = r.AmountValue,
                        .Bank = r.BankDeposited
                    }).ToList()

                receiptsGrid.DataSource = Nothing
                receiptsGrid.DataSource = displayRows

                SetStatus($"{receipts.Count} receipt(s) loaded.")
            End Using

        Catch ex As Exception
            SetStatus("Could not load receipts: " & ex.Message, True)
        End Try
    End Function

    Private Async Sub SaveReceiptClicked(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(orNumberInput.Text) Then
            MessageBox.Show("Enter the OR number.", "Required Field",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            orNumberInput.Focus()
            Return
        End If

        If String.IsNullOrWhiteSpace(payorInput.Text) Then
            MessageBox.Show("Enter the payor.", "Required Field",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            payorInput.Focus()
            Return
        End If

        If glAccountCombo.SelectedItem Is Nothing OrElse
           depositAccountCombo.SelectedItem Is Nothing Then
            MessageBox.Show("Select both the GL account and the deposit account.",
                            "Required Field", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim amount As Decimal
        If Not Decimal.TryParse(
            amountInput.Text,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            amount) OrElse amount <= 0D Then

            MessageBox.Show("Enter a valid amount greater than zero.",
                            "Invalid Amount",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
            amountInput.Focus()
            Return
        End If

        Dim glAccount As AccountItem =
            DirectCast(glAccountCombo.SelectedItem, AccountItem)

        Dim depositAccount As AccountItem =
            DirectCast(depositAccountCombo.SelectedItem, AccountItem)

        If glAccount.Id = depositAccount.Id Then
            MessageBox.Show("Choose different GL and deposit accounts.",
                            "Invalid Account Selection",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
            Return
        End If

        Dim payload As New Dictionary(Of String, Object) From {
            {"receipt_date", receiptDatePicker.Value.ToString("yyyy-MM-dd")},
            {"or_number", orNumberInput.Text.Trim()},
            {"payor", payorInput.Text.Trim()},
            {"account_id", glAccount.Id},
            {"deposit_account_id", depositAccount.Id},
            {"amount", amount},
            {"bank_deposited", If(String.IsNullOrWhiteSpace(bankInput.Text),
                                  Nothing, bankInput.Text.Trim())},
            {"remarks", If(String.IsNullOrWhiteSpace(remarksInput.Text),
                           Nothing, remarksInput.Text.Trim())}
        }

        Try
            saveButton.Enabled = False
            SetStatus("Saving receipt...")

            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.PostAsJsonAsync("cash-receipts", payload)

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    If response.StatusCode =
                        Net.HttpStatusCode.UnprocessableEntity AndAlso
                        body.Contains("""or_number""") AndAlso
                        body.Contains("already been taken") Then

                        MessageBox.Show(
                            "This OR number already exists. Enter a unique OR number and try again.",
                            "Duplicate OR Number",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)

                        SetStatus("Duplicate OR number. Enter a unique number.", True)
                        Return
                    End If

                    Throw New Exception(body)
                End If

                Dim result = JsonSerializer.Deserialize(
                    Of ApiResponse(Of ReceiptItem))(body, jsonOptions)

                MessageBox.Show(
                    If(String.IsNullOrWhiteSpace(result?.Message),
                       "Receipt saved successfully.",
                       result.Message),
                    "Cash Receipt Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

                ClearInputs()
                Await LoadReceiptsAsync()
            End Using

        Catch ex As Exception
            SetStatus("Could not save receipt: " & ex.Message, True)
            MessageBox.Show(ex.Message, "Save Failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            saveButton.Enabled = True
        End Try
    End Sub

    Private Async Sub RefreshClicked(sender As Object, e As EventArgs)
        Await LoadAccountsAsync()
        Await LoadReceiptsAsync()
    End Sub

    Private Sub ClearInputs()
        receiptDatePicker.Value = Date.Today
        orNumberInput.Clear()
        payorInput.Clear()
        amountInput.Clear()
        bankInput.Clear()
        remarksInput.Clear()

        If glAccountCombo.Items.Count > 0 Then
            glAccountCombo.SelectedIndex = 0
        End If

        If depositAccountCombo.Items.Count > 0 Then
            depositAccountCombo.SelectedIndex = 0
        End If
    End Sub

    Private Sub SetStatus(message As String,
                          Optional isError As Boolean = False)
        statusLabel.Text = message
        statusLabel.ForeColor =
            If(isError, Color.Firebrick, Color.FromArgb(65, 85, 72))
    End Sub
End Class