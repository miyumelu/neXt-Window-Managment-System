Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.IO
Imports System.Linq

Public Class Window
    ' - Windows APIs
    <DllImport("user32.dll")> Private Shared Function ReleaseCapture() As Boolean : End Function
    <DllImport("user32.dll")> Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer : End Function
    <DllImport("Gdi32.dll", EntryPoint:="CreateRoundRectRgn")> Private Shared Function CreateRoundRectRgn(nLeftRect As Integer, nTopRect As Integer, nRightRect As Integer, nBottomRect As Integer, nWidthEllipse As Integer, nHeightEllipse As Integer) As IntPtr : End Function

    Private _parentForm As Form
    Public _isMaxed As Boolean = False
    Private _isAnimating As Boolean = False
    Private _forcedEdges As Boolean = False
    Private _cornerRadius As Integer = 0

    Private _stripTop As Integer = 0
    Private _dockSize As Integer = 0
    Private _dockLocation As String = "right"

    Private Const HTBOTTOMLEFT As Integer = 16
    Private Const HTBOTTOMRIGHT As Integer = 17
    Public Property DefaultWidth As Integer
    Public Property DefaultHeight As Integer

    Public Sub New(parentForm As Form)
        _parentForm = parentForm
        _DefaultWidth = parentForm.Width
        _DefaultHeight = parentForm.Height

        LoadFilesOnly()

        AddHandler _parentForm.LocationChanged, AddressOf OnLocationChanged
        AddHandler _parentForm.SizeChanged, AddressOf OnSizeChanged
    End Sub

    Private Sub LoadFilesOnly()
        Try
            Dim baseDir As String = "C:\Users\deniz\Documents\GitHub\XenDesk-Tree\System\dictionary.arc\SYSTEM_APPLICATION\system.xendesk.dict\"
            Dim fStrip = Path.Combine(baseDir, "strip_size.val")
            Dim fDock = Path.Combine(baseDir, "dock_size.val")
            Dim fDockLoc = Path.Combine(baseDir, "dock_location.word")

            If File.Exists(fStrip) Then Integer.TryParse(File.ReadAllText(fStrip).Trim(), _stripTop)
            If File.Exists(fDock) Then Integer.TryParse(File.ReadAllText(fDock).Trim(), _dockSize)
            If File.Exists(fDockLoc) Then
                _dockLocation = File.ReadAllText(fDockLoc).Trim().ToLower()
            End If
        Catch : End Try
    End Sub

    Private Function GetAvailableArea(s As Rectangle) As Rectangle
        Dim x = s.X
        Dim y = s.Y + _stripTop
        Dim w = s.Width
        Dim h = s.Height - _stripTop

        Select Case _dockLocation
            Case "bottom" : h -= _dockSize
            Case "left" : x += _dockSize : w -= _dockSize
            Case "right" : w -= _dockSize
        End Select
        Return New Rectangle(x, y, w, h)
    End Function

    ' - Rounded Corners
    Private Sub OnSizeChanged(sender As Object, e As EventArgs)
        If Not _isAnimating Then _isMaxed = False
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

    ' - Forced Edges
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

            If _parentForm.Right < scr.Left + minVisibleWidth Then
                targetX = scr.Left : shouldTeleport = True
            ElseIf _parentForm.Left > scr.Right - minVisibleWidth Then
                targetX = scr.Right - _parentForm.Width : shouldTeleport = True
            End If

            If _parentForm.Bottom < scr.Top + _stripTop + minVisibleHeight Then
                targetY = scr.Top + _stripTop : shouldTeleport = True
            ElseIf _parentForm.Top > scr.Bottom - minVisibleHeight Then
                targetY = scr.Bottom - _parentForm.Height : shouldTeleport = True
            End If

            If shouldTeleport Then
                _isAnimating = True
                _parentForm.Location = New Point(targetX, targetY)
                _isAnimating = False
            End If
        End If
    End Sub

    ' - Snap Method
    Public Sub MaximizeFull()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X, area.Y, area.Width, area.Height)
    End Sub

    Public Sub SnapToLeft()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X, area.Y, area.Width \ 2, area.Height)
    End Sub

    Public Sub SnapToRight()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X + (area.Width \ 2), area.Y, area.Width \ 2, area.Height)
    End Sub

    Public Sub SnapToBottomHalf()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X, area.Y + (area.Height \ 2), area.Width, area.Height \ 2)
    End Sub

    Public Sub SnapToLeftTop()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X, area.Y, area.Width \ 2, area.Height \ 2)
    End Sub

    Public Sub SnapToRightTop()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X + (area.Width \ 2), area.Y, area.Width \ 2, area.Height \ 2)
    End Sub

    Public Sub SnapToLeftBottom()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X, area.Y + (area.Height \ 2), area.Width \ 2, area.Height \ 2)
    End Sub

    Public Sub SnapToRightBottom()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = True
        AnimateSnap(area.X + (area.Width \ 2), area.Y + (area.Height \ 2), area.Width \ 2, area.Height \ 2)
    End Sub

    Public Sub OriginalSize()
        Dim area = GetAvailableArea(Screen.FromPoint(Cursor.Position).WorkingArea)
        _isMaxed = False
        AnimateSnap(area.X + (area.Width - DefaultWidth) \ 2, area.Y + 10, DefaultWidth, DefaultHeight)
    End Sub

    ' - Animation Core
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

    ' - Drag & Resize Management
    Public Sub RegisterResizer()
        AddHandler _parentForm.MouseDown, AddressOf HandleResizeMouseDown
    End Sub

    Private Sub HandleResizeMouseDown(sender As Object, e As MouseEventArgs)
        Dim gripSize As Integer = 20
        Dim mousePos = _parentForm.PointToClient(Cursor.Position)
        If mousePos.Y >= _parentForm.Height - gripSize Then
            If mousePos.X >= _parentForm.Width - gripSize Then
                ReleaseCapture() : SendMessage(_parentForm.Handle, &HA1, HTBOTTOMRIGHT, 0)
            ElseIf mousePos.X <= gripSize Then
                ReleaseCapture() : SendMessage(_parentForm.Handle, &HA1, HTBOTTOMLEFT, 0)
            End If
        End If
    End Sub

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
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        Dim area = GetAvailableArea(s)
        Dim mX = Cursor.Position.X
        Dim mY = Cursor.Position.Y
        Dim edgeSize As Integer = 45

        If mY <= s.Top + _stripTop + 15 Then
            If mX <= s.Left + edgeSize Then : SnapToLeftTop()
            ElseIf mX >= s.Right - edgeSize Then : SnapToRightTop()
            Else : MaximizeFull() : End If
        ElseIf mY >= (s.Bottom - If(_dockLocation = "bottom", _dockSize, 0) - 15) Then
            If mX <= s.Left + edgeSize Then : SnapToLeftBottom()
            ElseIf mX >= s.Right - edgeSize Then : SnapToRightBottom()
            Else : SnapToBottomHalf() : End If
        ElseIf mX <= s.Left + 5 + If(_dockLocation = "left", _dockSize, 0) Then : SnapToLeft()
        ElseIf mX >= s.Right - 5 - If(_dockLocation = "right", _dockSize, 0) Then : SnapToRight()
        End If
    End Sub
End Class