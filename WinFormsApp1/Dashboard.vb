Imports System.Drawing
Imports System.Windows.Forms

Public Class Dashboard

    Private ReadOnly ActiveMenuColor As Color = Color.FromArgb(114, 142, 109)
    Private ReadOnly InactiveMenuColor As Color = Color.FromArgb(46, 82, 72)

    Public Property Username As String
    Public Property LoginForm As Form

    Private loggingOut As Boolean
    Private navigating As Boolean

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Async Sub Dashboard_Load(
        sender As Object,
        e As EventArgs
    ) Handles MyBase.Load

        For Each card As Panel In {
    cardIpk,
    cardSemester,
    cardCourse
}
            AddHandler card.Paint,
    Sub(paintSender As Object, paintArgs As PaintEventArgs)
        Using borderPen As New Pen(Color.FromArgb(190, 205, 198), 2)
            paintArgs.Graphics.DrawRectangle(
                borderPen,
                0,
                0,
                card.Width - 1,
                card.Height - 1
            )
        End Using
    End Sub
        Next

        lblAnnouncement.BorderStyle = BorderStyle.FixedSingle
        lblAnnouncement.Padding = New Padding(12)

        ' Tampilan tabel evaluasi
        dgvSchedule.RowTemplate.Height = 38
        dgvSchedule.ColumnHeadersHeight = 40
        dgvSchedule.RowHeadersVisible = False
        dgvSchedule.BorderStyle = BorderStyle.None
        dgvSchedule.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        dgvSchedule.GridColor = Color.FromArgb(230, 234, 230)

        dgvSchedule.AlternatingRowsDefaultCellStyle.BackColor =
    Color.FromArgb(248, 250, 248)

        If String.IsNullOrWhiteSpace(Username) Then
            Username = "Mahasiswa"
        End If

        lblWelcome.Text = $"Selamat datang, {Username}"
        SetActiveMenuButton(btnDashboard)

        dgvSchedule.Rows.Clear()

        Try
            Dim profil As UserProfilItem =
        Await Task.Run(Function() DatabaseHelper.GetUserProfil(Username))

            Dim displayName As String =
        If(String.IsNullOrWhiteSpace(profil.Nama), Username, profil.Nama)

            lblWelcome.Text = $"Selamat datang, {displayName}"

            lblInfo.Text =
        $"NIM: {Username}  |  Kelas: {profil.Kelas}  |  Ringkasan evaluasi dosen"

            Await LoadEdomDashboardAsync()

        Catch ex As Exception
            dgvSchedule.Rows.Clear()

            lblIpkValue.Text = "—"
            lblSemesterValue.Text = "—"
            lblCourseValue.Text = "—"

            MessageBox.Show(
        "Gagal memuat dashboard: " & ex.Message,
        "Informasi",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning
    )
        End Try

        dgvSchedule.ClearSelection()

        dgvSchedule.ClearSelection()
    End Sub

    Private Sub LoadFallbackSchedule()
        dgvSchedule.Rows.Clear()
    End Sub

    Public Sub SetActiveMenuButton(activeButton As Button)
        navigating = False
        btnDashboard.BackColor = If(activeButton Is btnDashboard, ActiveMenuColor, InactiveMenuColor)
        Button1.BackColor = If(activeButton Is Button1, ActiveMenuColor, InactiveMenuColor)
    End Sub

    Private Sub btnDashboard_Click(
        sender As Object,
        e As EventArgs
    ) Handles btnDashboard.Click

        SetActiveMenuButton(btnDashboard)
    End Sub

    Private Sub Button1_Click(
        sender As Object,
        e As EventArgs
    ) Handles Button1.Click

        SetActiveMenuButton(Button1)

        Dim edomForm As New pengisianEdom With {
            .Username = Username,
            .LoginForm = LoginForm,
            .DashboardForm = Me,
            .StartPosition = FormStartPosition.Manual,
            .Location = Location,
            .Size = Size,
            .WindowState = WindowState
        }

        navigating = True
        Hide()
        edomForm.Show()
    End Sub

    Private Sub btnLogout_Click(
        sender As Object,
        e As EventArgs
    ) Handles btnLogout.Click

        btnLogout.BackColor = Color.FromArgb(176, 42, 55)

        Dim result As DialogResult =
            MessageBox.Show(
                "Apakah Anda yakin ingin keluar?",
                "Konfirmasi Keluar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            )

        If result <> DialogResult.Yes Then
            btnLogout.BackColor = Color.FromArgb(220, 53, 69)
            Return
        End If

        loggingOut = True

        If LoginForm IsNot Nothing Then
            LoginForm.Show()
        End If

        Close()
    End Sub

    Private Sub Dashboard_FormClosed(
        sender As Object,
        e As FormClosedEventArgs
    ) Handles MyBase.FormClosed

        If Not loggingOut AndAlso Not navigating Then
            Application.Exit()
        End If
    End Sub

    Private Async Function LoadEdomDashboardAsync() As Task
        Dim items As List(Of EdomMahasiswaItem) =
            Await Task.Run(Function() DatabaseHelper.GetEdomMahasiswa(Username))

        dgvSchedule.Rows.Clear()

        Dim totalTersedia As Integer = 0
        Dim totalSudahDiisi As Integer = 0
        Dim totalBelumDiisi As Integer = 0

        For Each item As EdomMahasiswaItem In items

            If Not item.MataKuliahId.HasValue Then
                Continue For
            End If

            totalTersedia += 1

            If item.SudahDievaluasi Then
                totalSudahDiisi += 1
            Else
                totalBelumDiisi += 1

                Dim rowIndex As Integer = dgvSchedule.Rows.Add(
                    item.DosenNama,
                    item.MataKuliahNama,
                    "Belum Dievaluasi",
                    "Isi Evaluasi"
                )

                dgvSchedule.Rows(rowIndex).Tag = item
            End If

        Next

        lblIpkValue.Text = totalTersedia.ToString()
        lblSemesterValue.Text = totalSudahDiisi.ToString()
        lblCourseValue.Text = totalBelumDiisi.ToString()

        dgvSchedule.ClearSelection()
    End Function

    Private Async Sub dgvSchedule_CellClick(
    sender As Object,
    e As DataGridViewCellEventArgs
) Handles dgvSchedule.CellClick

        If e.RowIndex < 0 OrElse e.ColumnIndex <> 3 Then Return

        Dim item As EdomMahasiswaItem =
            TryCast(dgvSchedule.Rows(e.RowIndex).Tag, EdomMahasiswaItem)

        If item Is Nothing Then Return

        Using dlg As New FormEvaluasiEdom(Username, item)
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Await LoadEdomDashboardAsync()
            End If
        End Using

    End Sub

End Class