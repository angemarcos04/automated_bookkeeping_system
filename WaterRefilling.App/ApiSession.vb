Imports System
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Collections.Generic

Public NotInheritable Class ApiSession

    Private Sub New()
    End Sub

    Public Shared ReadOnly Client As New HttpClient() With {
        .BaseAddress = New Uri("http://127.0.0.1:8000/api/"),
        .Timeout = TimeSpan.FromSeconds(30)
    }

    Public Shared Property BearerToken As String

    Public Shared Property CurrentUserId As Integer

    Public Shared Property CurrentUserName As String

    Public Shared Property CurrentUserEmail As String

    Public Shared Property CurrentUserRoles As New List(Of String)

    Public Shared Sub ApplyAuthorization()
        Client.DefaultRequestHeaders.Authorization = Nothing

        If Not String.IsNullOrWhiteSpace(BearerToken) Then
            Client.DefaultRequestHeaders.Authorization =
                New AuthenticationHeaderValue("Bearer", BearerToken)
        End If
    End Sub

    Public Shared Sub Clear()
        BearerToken = Nothing
        CurrentUserId = 0
        CurrentUserName = Nothing
        CurrentUserEmail = Nothing
        CurrentUserRoles = New List(Of String)()
        ApplyAuthorization()
    End Sub

End Class