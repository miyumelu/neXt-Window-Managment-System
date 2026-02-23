Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.IO

Public Class Window
    ' --- Windows APIs ---
    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function
    <DllImport("Gdi32.dll", EntryPoint:="CreateRoundRectRgn")>
    Private Shared Function CreateRoundRectRgn(nLeftRect As Integer, nTopRect As Integer, nRightRect As Integer, nBottomRect As Integer, nWidthEllipse As Integer, nHeightEllipse As Integer) As IntPtr
    End Function

    ' --- Variablen ---
    Private _parentForm As Form
    Public _isMaxed As Boolean = False
    Private _isAnimating As Boolean = False
    Private _forcedEdges As Boolean = False
    Private _cornerRadius As Integer = 0

    Private _stripTop As Integer = 0
    Private _dockBottom As Integer = 0

    ' --- Weitere Windows Konstanten für Resizing ---
    Private Const WM_NCHITTEST As Integer = &H84
    Private Const HTBOTTOMLEFT As Integer = 16
    Private Const HTBOTTOMRIGHT As Integer = 17
    Public Property DefaultWidth As Integer
    Public Property DefaultHeight As Integer

    ' --- Konstruktor ---
    Public Sub New(parentForm As Form)
        _parentForm = parentForm
        _DefaultWidth = parentForm.Width
        _DefaultHeight = parentForm.Height

        ' WICHTIG: Dateien beim Start lesen, damit die Werte bekannt sind,
        ' aber das Fenster bleibt in der Originalgröße.
        LoadFilesOnly()

        AddHandler _parentForm.LocationChanged, AddressOf OnLocationChanged
        AddHandler _parentForm.SizeChanged, AddressOf OnSizeChanged
    End Sub

    ' --- Hilfsmethode: Nur Daten laden ---
    Private Sub LoadFilesOnly()
        Try
            Dim baseDir As String = "C:\Users\deniz\Documents\GitHub\XenDesk-Tree\System\dictionary.arc\SYSTEM_APPLICATION\system.xendesk.dict\"
            Dim fStrip = Path.Combine(baseDir, "strip_size.val")
            Dim fDock = Path.Combine(baseDir, "dock_size.val")

            If File.Exists(fStrip) Then Integer.TryParse(File.ReadAllText(fStrip).Trim(), _stripTop)
            If File.Exists(fDock) Then Integer.TryParse(File.ReadAllText(fDock).Trim(), _dockBottom)
        Catch : End Try
    End Sub

    ' --- Management von abgerundeten Ecken ---
    Private Sub OnSizeChanged(sender As Object, e As EventArgs)
        If Not _isAnimating Then
            _isMaxed = False
        End If

        UpdateRegion()
    End Sub

    Private Sub UpdateRegion()
        If _cornerRadius > 0 Then
            _parentForm.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, _parentForm.Width, _parentForm.Height, _cornerRadius, _cornerRadius))
        Else
            _parentForm.Region = Nothing
        End If
    End Sub

    Public Sub ApplyRoundedCorners(radius As Integer)
        _cornerRadius = radius
        _parentForm.FormBorderStyle = FormBorderStyle.None
        UpdateRegion()
    End Sub

    ' --- XWMS Layout Logik ---
    Public Sub ApplyXWMSLayout()
        Try
            LoadFilesOnly() ' Werte frisch laden

            Dim baseDir As String = "C:\Users\deniz\Documents\GitHub\XenDesk-Tree\System\dictionary.arc\SYSTEM_APPLICATION\system.xendesk.dict\"
            Dim fPos = Path.Combine(baseDir, "dock_position.word")
            Dim dockPos As String = "center"
            If File.Exists(fPos) Then dockPos = File.ReadAllText(fPos).Trim().ToLower()

            Dim screenArea = Screen.PrimaryScreen.WorkingArea
            Dim finalY = screenArea.Top + _stripTop
            Dim finalHeight = screenArea.Height - _stripTop - _dockBottom
            Dim finalX = _parentForm.Left

            Select Case dockPos
                Case "left" : finalX = screenArea.Left
                Case "right" : finalX = screenArea.Right - _parentForm.Width
                Case "center" : finalX = screenArea.Left + (screenArea.Width - _parentForm.Width) \ 2
            End Select

            _parentForm.Bounds = New Rectangle(finalX, finalY, _parentForm.Width, finalHeight)
            _isMaxed = True
            UpdateRegion()
        Catch : End Try
    End Sub

    ' --- Forced Edges (Teleport bei 95% außerhalb) ---
    Public Sub SetForcedEdges(enable As Boolean)
        _forcedEdges = enable
    End Sub

    Private Sub OnLocationChanged(sender As Object, e As EventArgs)
        If _forcedEdges AndAlso Not _isAnimating AndAlso Not _isMaxed Then
            Dim scr = Screen.FromControl(_parentForm).WorkingArea
            Dim minVisibleWidth As Integer = CInt(_parentForm.Width * 0.05)
            Dim minVisibleHeight As Integer = CInt(_parentForm.Height * 0.05)

            Dim shouldTeleport As Boolean = False
            Dim targetX As Integer = _parentForm.Left
            Dim targetY As Integer = _parentForm.Top

            ' Horizontal
            If _parentForm.Right < scr.Left + minVisibleWidth Then
                targetX = scr.Left : shouldTeleport = True
            ElseIf _parentForm.Left > scr.Right - minVisibleWidth Then
                targetX = scr.Right - _parentForm.Width : shouldTeleport = True
            End If

            ' Vertikal (Respektiert Strip und Dock)
            If _parentForm.Bottom < scr.Top + _stripTop + minVisibleHeight Then
                targetY = scr.Top + _stripTop : shouldTeleport = True
            ElseIf _parentForm.Top > scr.Bottom - _dockBottom - minVisibleHeight Then
                targetY = scr.Bottom - _dockBottom - _parentForm.Height : shouldTeleport = True
            End If

            If shouldTeleport Then _parentForm.Location = New Point(targetX, targetY)
        End If
    End Sub

    ' Diese Sub muss in der Window-Klasse stehen
    Public Sub RegisterResizer()
        ' Wir weisen dem Parent-Form eine Logik zu, um auf Maus-Treffer zu reagieren
        AddHandler _parentForm.MouseDown, AddressOf HandleResizeMouseDown
    End Sub

    ' Prüft, ob die Maus in den unteren Ecken ist und startet das Resizing
    Private Sub HandleResizeMouseDown(sender As Object, e As MouseEventArgs)
        Dim gripSize As Integer = 20 ' Größe des Bereichs in Pixeln

        ' Position relativ zum Fenster
        Dim mousePos = _parentForm.PointToClient(Cursor.Position)

        If mousePos.Y >= _parentForm.Height - gripSize Then
            ' Rechts Unten
            If mousePos.X >= _parentForm.Width - gripSize Then
                ReleaseCapture()
                SendMessage(_parentForm.Handle, &HA1, HTBOTTOMRIGHT, 0)
                ' Links Unten
            ElseIf mousePos.X <= gripSize Then
                ReleaseCapture()
                SendMessage(_parentForm.Handle, &HA1, HTBOTTOMLEFT, 0)
            End If
        End If
    End Sub

    ' --- Drag & Snap ---
    Public Sub AddControl(ctrl As Control)
        AddHandler ctrl.MouseDown, AddressOf OnMouseDown
    End Sub

    Private Sub OnMouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            If e.Clicks = 2 Then
                If _isMaxed Then OriginalSize() Else MaximizeFull()
                Return
            End If

            If _isMaxed Then
                _isMaxed = False
                _parentForm.Size = New Size(DefaultWidth, DefaultHeight)
                _parentForm.Location = New Point(Cursor.Position.X - (DefaultWidth \ 2), Cursor.Position.Y - e.Y)
                Application.DoEvents()
            End If

            ReleaseCapture()
            SendMessage(_parentForm.Handle, &HA1, 2, 0)
            CheckSnapPosition()
        End If
    End Sub

    Private Sub CheckSnapPosition()
        Dim screenArea = Screen.FromPoint(Cursor.Position).WorkingArea
        Dim mX = Cursor.Position.X
        Dim mY = Cursor.Position.Y
        Dim edgeSize As Integer = 45

        ' TOP SNAPPING
        If mY <= screenArea.Top + _stripTop + 15 Then
            If mX <= screenArea.Left + edgeSize Then
                SnapToLeftTop()
            ElseIf mX >= screenArea.Right - edgeSize Then
                SnapToRightTop()
            Else
                MaximizeFull()
            End If
            ' BOTTOM SNAPPING
        ElseIf mY >= screenArea.Bottom - _dockBottom - 15 Then
            If mX <= screenArea.Left + edgeSize Then
                SnapToLeftBottom()
            ElseIf mX >= screenArea.Right - edgeSize Then
                SnapToRightBottom()
            Else
                SnapToBottomHalf()
            End If
            ' SIDE SNAPPING
        ElseIf mX <= screenArea.Left + 5 Then
            SnapToLeft()
        ElseIf mX >= screenArea.Right - 5 Then
            SnapToRight()
        End If
    End Sub

    ' --- Snap Methoden ---
    Public Sub OriginalSize()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = False
        AnimateSnap(s.Left + (s.Width - DefaultWidth) \ 2, s.Top + _stripTop + 10, DefaultWidth, DefaultHeight)
    End Sub

    Public Sub MaximizeFull()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        AnimateSnap(s.Left, s.Top + _stripTop, s.Width, s.Height - _stripTop - _dockBottom)
    End Sub

    Public Sub SnapToLeft()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        AnimateSnap(s.Left, s.Top + _stripTop, s.Width \ 2, s.Height - _stripTop - _dockBottom)
    End Sub

    Public Sub SnapToRight()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        AnimateSnap(s.Right - (s.Width \ 2), s.Top + _stripTop, s.Width \ 2, s.Height - _stripTop - _dockBottom)
    End Sub

    Public Sub SnapToBottomHalf()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        Dim h = (s.Height - _stripTop - _dockBottom) \ 2
        AnimateSnap(s.Left, s.Bottom - _dockBottom - h, s.Width, h)
    End Sub

    Public Sub SnapToLeftTop()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        AnimateSnap(s.Left, s.Top + _stripTop, s.Width \ 2, (s.Height - _stripTop - _dockBottom) \ 2)
    End Sub

    Public Sub SnapToRightTop()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        AnimateSnap(s.Right - (s.Width \ 2), s.Top + _stripTop, s.Width \ 2, (s.Height - _stripTop - _dockBottom) \ 2)
    End Sub

    Public Sub SnapToLeftBottom()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        Dim h = (s.Height - _stripTop - _dockBottom) \ 2
        AnimateSnap(s.Left, s.Bottom - _dockBottom - h, s.Width \ 2, h)
    End Sub

    Public Sub SnapToRightBottom()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        _isMaxed = True
        Dim h = (s.Height - _stripTop - _dockBottom) \ 2
        AnimateSnap(s.Right - (s.Width \ 2), s.Bottom - _dockBottom - h, s.Width \ 2, h)
    End Sub

    ' --- Animation Core ---
    Private Async Sub AnimateSnap(tX As Integer, tY As Integer, tW As Integer, tH As Integer)
        If _isAnimating Then Return
        _isAnimating = True
        Dim steps As Integer = 8
        Dim sX = _parentForm.Left, sY = _parentForm.Top, sW = _parentForm.Width, sH = _parentForm.Height

        For i As Integer = 1 To steps
            If _parentForm.IsDisposed Then Exit Sub
            _parentForm.Bounds = New Rectangle(sX + (tX - sX) * i \ steps, sY + (tY - sY) * i \ steps, sW + (tW - sW) * i \ steps, sH + (tH - sH) * i \ steps)
            If i Mod 2 = 0 Then UpdateRegion()
            Await Task.Delay(5)
        Next

        _parentForm.Bounds = New Rectangle(tX, tY, tW, tH)
        UpdateRegion()
        _isAnimating = False
    End Sub
End Class