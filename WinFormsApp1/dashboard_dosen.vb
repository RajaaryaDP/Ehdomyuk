Imports System.Drawing
Imports System.Windows.Forms
Imports MySql.Data.MySqlClient

Public Class dashboard_dosen

    Private ReadOnly ActiveMenuColor As Color = Color.FromArgb(114, 142, 109)
    Private ReadOnly InactiveMenuColor As Color = Color.FromArgb(46, 82, 72)
    Private ReadOnly LowProgressColor As Color = Color.FromArgb(176, 42, 55)

    Public Property Username As String
    Public Property LoginForm As Form

    Private currentDosen As DosenItem
    Private currentMataKuliahList As New List(Of MataKuliahItem)
    Private loggingOut As Boolean
    Private loadingData As Boolean
    Private isFiltering As Boolean

    Public Sub New()
        InitializeComponent()

        StyleGrid(dgvFeedback)
        StyleGrid(dgvMataKuliahDiajar)
    End Sub

    Private Async Sub dashboard_dosen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If String.IsNullOrWhiteSpace(Username) Then
            Username = "dosen"
        End If

        Await LoadDashboardAsync()
    End Sub

    ' ---------- Inisialisasi & Pengambilan Data ----------

    Private Async Function LoadDashboardAsync() As Task
        ShowState("Memuat data profil dosen dan daftar mata kuliah...", showRetry:=False)

        Try
            ' 1. Ambil data profil dosen berdasarkan akun login
            currentDosen = Await Task.Run(Function() DatabaseHelper.GetDosenByUsername(Username))

            If currentDosen Is Nothing Then
                ShowState("Profil dosen tidak ditemukan. Pastikan akun terdaftar di sistem.", showRetry:=True)
                Return
            End If

            lblDosenName.Text = currentDosen.Nama
            lblDosenNidn.Text = If(String.IsNullOrWhiteSpace(currentDosen.Nidn), "NIDN: -", $"NIDN: {currentDosen.Nidn}")

            ' 2. Ambil daftar mata kuliah yang diajar oleh dosen ini
            currentMataKuliahList = Await Task.Run(Function() DatabaseHelper.GetMataKuliahByDosen(currentDosen.Id))

            ' 3. Isi menu dropdown list kelas yang diajar
            PopulateDropdownMataKuliah()

            ' 4. Ambil statistik hasil pengisian EDOM (default: seluruh kelas)
            Await LoadStatistikAsync(Nothing)

        Catch ex As Exception
            ShowState(DatabaseHelper.DescribeError(ex), showRetry:=True)
        End Try
    End Function

    Private Sub PopulateDropdownMataKuliah()
        isFiltering = True
        cmbMataKuliah.Items.Clear()

        ' Item pertama: opsi melihat rekapitulasi seluruh kelas yang diajar
        cmbMataKuliah.Items.Add(New MataKuliahComboItem With {
            .Id = 0,
            .Kelas = "",
            .DisplayText = "Seluruh Kelas"
        })

        ' Tambahkan daftar kelas yang diajar oleh dosen
        Dim daftarKelas As List(Of String) = currentMataKuliahList _
            .Select(Function(mk) If(mk.Kelas, "").Trim()) _
            .Where(Function(k) Not String.IsNullOrEmpty(k)) _
            .Distinct(StringComparer.OrdinalIgnoreCase) _
            .OrderBy(Function(k) k, StringComparer.OrdinalIgnoreCase) _
            .ToList()

        For Each k In daftarKelas
            cmbMataKuliah.Items.Add(New MataKuliahComboItem With {
                .Id = 0,
                .Kelas = k,
                .DisplayText = k
            })
        Next

        cmbMataKuliah.SelectedIndex = 0
        isFiltering = False
    End Sub

    Private Async Function LoadStatistikAsync(kelasFilter As String) As Task
        If loadingData OrElse currentDosen Is Nothing Then Return
        loadingData = True
        btnRefresh.Enabled = False

        Try
            Dim stats As DosenEdomStatistik = Await Task.Run(Function() DatabaseHelper.GetStatistikEdomDosen(currentDosen.Id, kelasFilter))
            RenderStatistik(stats, kelasFilter)
            lblLastUpdated.Text = $"Diperbarui pukul {DateTime.Now:HH.mm}"
            HideState()
        Catch ex As Exception
            ShowState(DatabaseHelper.DescribeError(ex), showRetry:=True)
        Finally
            loadingData = False
            btnRefresh.Enabled = True
        End Try
    End Function

    Private Sub RenderStatistik(stats As DosenEdomStatistik, selectedKelas As String)
        ' 1. Kartu Ringkasan Metrik Pengisian EDOM
        lblTotalMahasiswa.Text = stats.TotalMahasiswa.ToString()
        lblTotalSub.Text = If(
            String.IsNullOrWhiteSpace(selectedKelas),
            "Total mahasiswa yang diajar",
            $"Total mahasiswa kelas {selectedKelas}"
        )
        lblSudahMengisi.Text = stats.SudahMengisi.ToString()
        lblBelumMengisi.Text = stats.BelumMengisi.ToString()
        lblPersentase.Text = $"{stats.Persentase:0.0}%"

        If stats.TotalMahasiswa > 0 AndAlso stats.Persentase < 50 Then
            lblPersentase.ForeColor = LowProgressColor
            lblPersenSub.Text = "Partisipasi rendah (< 50%)"
        Else
            lblPersentase.ForeColor = Color.FromArgb(46, 82, 72)
            lblPersenSub.Text = "Target partisipasi: >=75%"
        End If

        ' 2. Kartu Indeks Kinerja Dosen (Nilai Crisp & Kategori Fuzzy Tsukamoto dari edom.py)
        lblRataRataSkor.Text = If(stats.NilaiFuzzy > 0, stats.NilaiFuzzy.ToString("0.00"), "0.00")
        lblScoreScale.Left = lblRataRataSkor.Right + 4

        If stats.NilaiFuzzy > 0 Then
            lblPredikatBadge.Text = stats.PredikatFuzzy
            Select Case If(stats.PredikatFuzzy, "").Trim().ToLowerInvariant()
                Case "baik"
                    lblPredikatBadge.BackColor = Color.FromArgb(236, 241, 234)
                    lblPredikatBadge.ForeColor = Color.FromArgb(30, 59, 51)
                Case "cukup"
                    lblPredikatBadge.BackColor = Color.FromArgb(254, 243, 199)
                    lblPredikatBadge.ForeColor = Color.FromArgb(146, 64, 14)
                Case Else
                    lblPredikatBadge.BackColor = Color.FromArgb(254, 226, 226)
                    lblPredikatBadge.ForeColor = LowProgressColor
            End Select
        Else
            lblPredikatBadge.Text = "Belum Ada Nilai"
            lblPredikatBadge.BackColor = Color.FromArgb(241, 239, 232)
            lblPredikatBadge.ForeColor = Color.FromArgb(108, 117, 125)
        End If

        lblPedagogikVal.Text = stats.SkorPedagogik.ToString("0.00")
        lblProfesionalVal.Text = stats.SkorProfesional.ToString("0.00")
        lblKepribadianVal.Text = stats.SkorKepribadian.ToString("0.00")
        lblSosialVal.Text = stats.SkorSosial.ToString("0.00")

        ' 3. Tabel Detail Respon Pengisian & Feedback Mahasiswa (Kolom Kelas menggantikan Mahasiswa NIM)
        dgvFeedback.Rows.Clear()
        Dim noF As Integer = 1
        For Each item In stats.DaftarFeedback
            Dim rowIndex As Integer = dgvFeedback.Rows.Add(
                noF,
                item.MataKuliah,
                If(String.IsNullOrWhiteSpace(item.Kelas), "-", item.Kelas),
                item.DiisiPada.ToString("dd MMM yyyy, HH:mm"),
                item.NilaiRataRata.ToString("0.00"),
                If(String.IsNullOrWhiteSpace(item.Saran), "-", item.Saran)
            )
            noF += 1
        Next
        dgvFeedback.ClearSelection()

        ' 4. Tabel Mata Kuliah yang Diajar
        dgvMataKuliahDiajar.Rows.Clear()
        Dim noMk As Integer = 1
        For Each mk In stats.PerMataKuliah
            Dim kelasRuang As String = If(
                String.IsNullOrWhiteSpace(mk.Ruang) OrElse mk.Ruang = "-",
                mk.Kelas,
                $"{mk.Kelas} ({mk.Ruang})"
            )
            Dim rowIndex As Integer = dgvMataKuliahDiajar.Rows.Add(
                noMk,
                mk.Nama,
                kelasRuang,
                mk.TotalMahasiswa,
                mk.Sudah,
                mk.Belum,
                $"{mk.Persentase:0.0}%",
                If(mk.RataRataSkor > 0, mk.RataRataSkor.ToString("0.00"), "-"),
                mk.Status
            )
            dgvMataKuliahDiajar.Rows(rowIndex).Tag = mk.Kelas
            noMk += 1
        Next
        dgvMataKuliahDiajar.ClearSelection()

        ' Update status label jika tidak ada data pengisian
        If stats.DaftarFeedback.Count = 0 Then
            lblFeedbackSub.Text = "Belum ada mahasiswa yang mengisi EDOM untuk filter kelas ini."
        Else
            lblFeedbackSub.Text = $"Menampilkan {stats.DaftarFeedback.Count} respon evaluasi mahasiswa."
        End If
    End Sub

    ' ---------- Event Handlers: Dropdown List & Filter ----------

    Private Async Sub cmbMataKuliah_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMataKuliah.SelectedIndexChanged
        If isFiltering Then Return

        Dim selected = TryCast(cmbMataKuliah.SelectedItem, MataKuliahComboItem)
        If selected Is Nothing Then Return

        Dim kelasFilter As String = If(Not String.IsNullOrWhiteSpace(selected.Kelas), selected.Kelas, Nothing)
        Await LoadStatistikAsync(kelasFilter)
    End Sub

    Private Async Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        Dim selected = TryCast(cmbMataKuliah.SelectedItem, MataKuliahComboItem)
        Dim kelasFilter As String = If(selected IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(selected.Kelas), selected.Kelas, Nothing)
        Await LoadStatistikAsync(kelasFilter)
    End Sub

    Private Async Sub btnRetry_Click(sender As Object, e As EventArgs) Handles btnRetry.Click
        Await LoadDashboardAsync()
    End Sub

    ' Membuka rata-rata nilai setiap butir instrumen EDOM, mengikuti filter kelas yang dipilih.
    Private Sub btnRincianButir_Click(sender As Object, e As EventArgs) Handles btnRincianButir.Click
        If currentDosen Is Nothing Then Return

        Dim selected = TryCast(cmbMataKuliah.SelectedItem, MataKuliahComboItem)
        Dim kelasFilter As String = If(selected IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(selected.Kelas), selected.Kelas, Nothing)
        Dim cakupan As String = If(Not String.IsNullOrWhiteSpace(kelasFilter), $"Kelas {kelasFilter}", "Seluruh kelas yang diajar")

        Using dlg As New FormRincianButir(currentDosen, kelasFilter, cakupan)
            dlg.ShowDialog(Me)
        End Using
    End Sub

    ' ---------- Navigasi Menu Sidebar ----------

    Private Sub btnMenuStatistik_Click(sender As Object, e As EventArgs) Handles btnMenuStatistik.Click
        SwitchSection(isStatistik:=True)
    End Sub

    Private Sub btnMenuMataKuliah_Click(sender As Object, e As EventArgs) Handles btnMenuMataKuliah.Click
        SwitchSection(isStatistik:=False)
    End Sub

    Private Sub SwitchSection(isStatistik As Boolean)
        btnMenuStatistik.BackColor = If(isStatistik, ActiveMenuColor, InactiveMenuColor)
        btnMenuMataKuliah.BackColor = If(Not isStatistik, ActiveMenuColor, InactiveMenuColor)

        pnlStatistikView.Visible = isStatistik
        pnlMataKuliahView.Visible = Not isStatistik

        If isStatistik Then
            lblHeaderTitle.Text = "Statistik Hasil Evaluasi Dosen (EDOM)"
            lblHeaderSubtitle.Text = "Rekapitulasi partisipasi dan evaluasi pembelajaran oleh mahasiswa semester ini"
        Else
            lblHeaderTitle.Text = "Menu Mata Kuliah yang Diajar"
            lblHeaderSubtitle.Text = "Daftar mata kuliah yang Anda ampu dan status evaluasi EDOM masing-masing kelas"
        End If
    End Sub

    ' Jika memilih baris di tabel mata kuliah, dropdown otomatis menyesuaikan ke kelas tersebut
    Private Sub dgvMataKuliahDiajar_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvMataKuliahDiajar.CellDoubleClick
        If e.RowIndex < 0 Then Return

        Dim targetKelas As String = TryCast(dgvMataKuliahDiajar.Rows(e.RowIndex).Tag, String)
        If Not String.IsNullOrWhiteSpace(targetKelas) Then
            For i As Integer = 0 To cmbMataKuliah.Items.Count - 1
                Dim item = TryCast(cmbMataKuliah.Items(i), MataKuliahComboItem)
                If item IsNot Nothing AndAlso String.Equals(item.Kelas, targetKelas, StringComparison.OrdinalIgnoreCase) Then
                    cmbMataKuliah.SelectedIndex = i
                    SwitchSection(isStatistik:=True)
                    Exit For
                End If
            Next
        End If
    End Sub

    ' ---------- UI States & Styling ----------

    Private Sub ShowState(message As String, showRetry As Boolean)
        lblStateMessage.Text = message
        btnRetry.Visible = showRetry
        pnlStatistikView.Visible = False
        pnlMataKuliahView.Visible = False
        pnlState.Visible = True
    End Sub

    Private Sub HideState()
        pnlState.Visible = False
        pnlStatistikView.Visible = (btnMenuStatistik.BackColor = ActiveMenuColor)
        pnlMataKuliahView.Visible = (btnMenuMataKuliah.BackColor = ActiveMenuColor)
    End Sub

    Friend Shared Sub StyleGrid(grid As DataGridView)
        Dim ink As Color = Color.FromArgb(33, 37, 41)

        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AllowUserToResizeRows = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.BackgroundColor = Color.White
        grid.BorderStyle = BorderStyle.None
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        grid.GridColor = Color.FromArgb(230, 228, 222)
        grid.MultiSelect = False
        grid.ReadOnly = True
        grid.RowHeadersVisible = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.RowTemplate.Height = 40

        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        grid.ColumnHeadersHeight = 42
        With grid.ColumnHeadersDefaultCellStyle
            .BackColor = Color.FromArgb(241, 239, 232)
            .ForeColor = ink
            .SelectionBackColor = Color.FromArgb(241, 239, 232)
            .SelectionForeColor = ink
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
            .Padding = New Padding(10, 0, 0, 0)
        End With

        With grid.DefaultCellStyle
            .BackColor = Color.White
            .ForeColor = ink
            .SelectionBackColor = Color.FromArgb(236, 241, 234)
            .SelectionForeColor = ink
            .Font = New Font("Segoe UI", 9.5F)
            .Padding = New Padding(10, 0, 0, 0)
        End With
    End Sub

    ' ---------- Logout & Window LifeCycle ----------

    Private Sub btnLogout_Click(sender As Object, e As EventArgs) Handles btnLogout.Click
        Dim result As DialogResult = MessageBox.Show(
            "Apakah Anda yakin ingin keluar?",
            "Konfirmasi Keluar",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        )

        If result <> DialogResult.Yes Then Return

        loggingOut = True

        If LoginForm IsNot Nothing Then
            LoginForm.Show()
        End If

        Close()
    End Sub

    Private Sub dashboard_dosen_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        If Not loggingOut Then
            Application.Exit()
        End If
    End Sub

End Class

' Helper Item untuk Dropdown ComboBox Kelas / Mata Kuliah
Public Class MataKuliahComboItem
    Public Property Id As Integer
    Public Property Kelas As String
    Public Property DisplayText As String

    Public Overrides Function ToString() As String
        Return DisplayText
    End Function
End Class