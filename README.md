# neXt Window Management System (XWMS)

**neXt Window Management System** is a lightweight, high-performance window management library for VB.NET. It enables borderless WinForms applications to behave like modern Windows 11/10 windows, featuring smooth Aero-Snap functionality, quadrant snapping, and asynchronous resize animations.

---

## Features

*  **Advanced Aero-Snap**: Drag windows to edges or corners to snap them into 50% or 25% screen segments.
*  **Async Animation Engine**: Smoothly transitions window bounds using an internal asynchronous system for a premium UI feel.
*  **Universal Dragging**: Turn any UI element (Panels, Labels, PictureBoxes) into a functional drag handle for your form.
*  **Smart Un-Snap**: Pulling a maximized or snapped window automatically restores it to its original dimensions.
*  **Multi-Quadrant Support**: Supports Top-Left, Top-Right, Bottom-Left, and Bottom-Right corner snapping for efficient screen organization.

---

## Integration Guide

### 1. Add Reference
Simply add the `neXt Window Management System` DLL as a reference to your project solution.

### 2. Setup
To use the system, you initialize the manager within your Form and register the specific controls that should handle the dragging and snapping logic. 

### 3. Recommended Form Settings
For the best visual experience, the following settings are recommended in the Visual Studio Property Grid:
* **FormBorderStyle**: Set to `None`.
* **DoubleBuffered**: Set to `True` to eliminate flickering during transitions.

---

## Snap Layouts & Zones

The system detects the cursor position relative to the screen's working area (respecting the taskbar) to trigger the following layouts:



| Trigger Zone | Snap Result | Description |
| :--- | :--- | :--- |
| **Top Edge** | **Full Maximize** | Fills the entire working area. |
| **Left/Right Edges** | **Vertical Half** | Snaps to 50% width and 100% height. |
| **Top Corners** | **Quadrant Snap** | Snaps to 50% width and 50% height in corners. |
| **Bottom Edge** | **Bottom Half** | Snaps to 100% width and 50% height at the bottom. |
| **Bottom Corners** | **Quadrant Snap** | Snaps to 50% width and 50% height in bottom corners. |

---

##  API Documentation

### Core Methods
* **AddControl**: Attaches the snap and drag logic to a specific UI element (e.g., a Header Panel).
* **MaximizeFull**: Animates the form to fill the entire working area of the current monitor.
* **OriginalSize**: Returns the form to its defined default dimensions and centers it.
* **SnapToLeft / SnapToRight**: Manually triggers vertical half-screen snaps via code.
* **SnapToLeftTop / SnapToRightTop**: Triggers 25% quadrant snaps.
* **SnapToBottomHalf**: Snaps the window to the lower horizontal half of the screen.

### Configuration Properties
* **DefaultWidth**: The width the window returns to when un-snapping.
* **DefaultHeight**: The height the window returns to when un-snapping.
* **_isMaxed**: A boolean status indicating if the window is currently in a snapped or maximized state.

---

##  Technical Requirements
* **Target Framework**: .NET 6.0/7.0/8.0+
* **OS**: Windows (Utilizes `user32.dll` for native window messaging).
* **Environment**: Compatible with all WinForms-supported development environments.

---

##  Example Usage

    Imports System.Drawing

    Public Class MainForm
        Private xwms As Window
        
        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            xwms = New Window(Me)
        
            If drag_panel IsNot Nothing Then
                xwms.AddControl(drag_panel)
            End If

            If name_label IsNot Nothing Then
                xwms.AddControl(name_label)
            End If
        End Sub
        
        Private Sub btnMaximize_Click(sender As Object, e As EventArgs) Handles btnMaximize.Click
            If xwms._isMaxed Then
                xwms.OriginalSize()
            Else
                xwms.MaximizeFull()
            End If
        End Sub
    End Class
