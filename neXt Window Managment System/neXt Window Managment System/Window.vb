Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks

Public Class Window
    ' Windows API
    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
    End Function

    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2
    Private Const VK_LBUTTON As Integer = &H1

    ' Variablen
    Private WithEvents _targetPanel As Control
    Private _parentForm As Form
    Private _isMaxed As Boolean = False
    Private _isAnimating As Boolean = False

    Public Property DefaultWidth As Integer
    Public Property DefaultHeight As Integer

    Public Sub New(targetPanel As Control, parentForm As Form)
        _targetPanel = targetPanel
        _parentForm = parentForm
        _DefaultWidth = parentForm.Width
        _DefaultHeight = parentForm.Height

        AddHandler _targetPanel.MouseDown, AddressOf OnMouseDown
        AddHandler _targetPanel.DoubleClick, AddressOf OnDoubleClick
    End Sub

    Private Sub OnDoubleClick(sender As Object, e As EventArgs)
        If _isMaxed Then OriginalSize() Else MaximizeFull()
    End Sub

    Private Async Sub OnMouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            If _isMaxed Then
                _isMaxed = False
                _parentForm.Size = New Size(DefaultWidth, DefaultHeight)
                _parentForm.Location = New Point(Cursor.Position.X - (DefaultWidth \ 2), Cursor.Position.Y - e.Y)
            End If
            ReleaseCapture()
            SendMessage(_parentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
            CheckSnapPosition()
        End If
    End Sub

    Private Sub CheckSnapPosition()
        Dim screenArea = Screen.FromPoint(Cursor.Position).WorkingArea
        Dim mX = Cursor.Position.X
        Dim mY = Cursor.Position.Y

        Dim edgeSize As Integer = 40

        If mY <= screenArea.Top + 5 Then
            If mX <= screenArea.Left + edgeSize Then
                SnapToLeftTop()
            ElseIf mX >= screenArea.Right - edgeSize Then
                SnapToRightTop()
            Else
                MaximizeFull()
            End If
        ElseIf mY >= screenArea.Bottom - 5 Then
            If mX <= screenArea.Left + edgeSize Then
                SnapToLeftBottom()
            ElseIf mX >= screenArea.Right - edgeSize Then
                SnapToRightBottom()
            Else
                SnapToBottomHalf()
            End If
        ElseIf mX <= screenArea.Left + 5 Then
            SnapToLeft()
        ElseIf mX >= screenArea.Right - 5 Then
            SnapToRight()
        End If
    End Sub

    Public Sub OriginalSize()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Left + (s.Width - DefaultWidth) \ 2, s.Top + (s.Height - DefaultHeight) \ 2, DefaultWidth, DefaultHeight)
        _isMaxed = False
    End Sub

    Public Sub MaximizeFull()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Left, s.Top, s.Width, s.Height)
        _isMaxed = True
    End Sub

    Public Sub SnapToLeftTop()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Left, s.Top, s.Width \ 2, s.Height \ 2)
        _isMaxed = True
    End Sub

    Public Sub SnapToRightTop()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Right - (s.Width \ 2), s.Top, s.Width \ 2, s.Height \ 2)
        _isMaxed = True
    End Sub

    Public Sub SnapToLeftBottom()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Left, s.Bottom - (s.Height \ 2), s.Width \ 2, s.Height \ 2)
        _isMaxed = True
    End Sub

    Public Sub SnapToRightBottom()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Right - (s.Width \ 2), s.Bottom - (s.Height \ 2), s.Width \ 2, s.Height \ 2)
        _isMaxed = True
    End Sub

    Public Sub SnapToLeft()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Left, s.Top, s.Width \ 2, s.Height)
        _isMaxed = True
    End Sub

    Public Sub SnapToRight()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Right - (s.Width \ 2), s.Top, s.Width \ 2, s.Height)
        _isMaxed = True
    End Sub

    Public Sub SnapToUpHalf()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Left, s.Top, s.Width, s.Height \ 2)
        _isMaxed = True
    End Sub

    Public Sub SnapToBottomHalf()
        Dim s = Screen.FromPoint(Cursor.Position).WorkingArea
        AnimateSnap(s.Left, s.Bottom - (s.Height \ 2), s.Width, s.Height \ 2)
        _isMaxed = True
    End Sub

    Private Async Sub AnimateSnap(tX As Integer, tY As Integer, tW As Integer, tH As Integer)
        If _isAnimating Then Return
        _isAnimating = True

        Dim steps As Integer = 8
        Dim sX = _parentForm.Left, sY = _parentForm.Top, sW = _parentForm.Width, sH = _parentForm.Height

        For i As Integer = 1 To steps
            If _parentForm.IsDisposed Then Exit Sub
            Dim nX = sX + (tX - sX) * i \ steps
            Dim nY = sY + (tY - sY) * i \ steps
            Dim nW = sW + (tW - sW) * i \ steps
            Dim nH = sH + (tH - sH) * i \ steps
            _parentForm.Bounds = New Rectangle(nX, nY, nW, nH)
            Await Task.Delay(10)
        Next

        _parentForm.Bounds = New Rectangle(tX, tY, tW, tH)
        _isAnimating = False
    End Sub
End Class