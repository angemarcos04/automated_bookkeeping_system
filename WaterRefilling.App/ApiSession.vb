Imports System
Imports System.Net.Http

Public NotInheritable Class ApiSession
    Private Sub New()
    End Sub

    Public Shared ReadOnly Client As New HttpClient() With {
        .BaseAddress = New Uri("http://127.0.0.1:8000/api/"),
        .Timeout = TimeSpan.FromSeconds(30)
    }

    Public Shared Property BearerToken As String

    Public Shared Sub ApplyAuthorization()
        Client.DefaultRequestHeaders.Authorization = Nothing

        If Not String.IsNullOrWhiteSpace(BearerToken) Then
            Client.DefaultRequestHeaders.Authorization =
                New System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    BearerToken)
        End If
    End Sub

    Public Shared Sub Clear()
        BearerToken = Nothing
        ApplyAuthorization()
    End Sub
End Class