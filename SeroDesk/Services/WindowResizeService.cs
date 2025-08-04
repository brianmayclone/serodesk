using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace SeroDesk.Services
{
    /// <summary>
    /// Service für automatische Fenstergrößenanpassung wenn StatusBar ein-/ausgeklappt wird
    /// </summary>
    public class WindowResizeService
    {
        private static WindowResizeService? _instance;
        public static WindowResizeService Instance => _instance ??= new WindowResizeService();
        
        private readonly Dictionary<IntPtr, WindowState> _trackedWindows = new();
        private bool _statusBarExpanded = false;
        private double _statusBarHeight = 60; // Standard StatusBar Höhe
        
        /// <summary>
        /// Gespeicherter Zustand eines Fensters
        /// </summary>
        private class WindowState
        {
            public RECT OriginalRect { get; set; }
            public bool IsAdjusted { get; set; }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        
        private WindowResizeService()
        {
            System.Diagnostics.Debug.WriteLine("WindowResizeService initialized");
        }
        
        /// <summary>
        /// Wird aufgerufen wenn die StatusBar ein-/ausgeklappt wird
        /// </summary>
        public void OnStatusBarToggled(bool isExpanded, double statusBarHeight = 60)
        {
            System.Diagnostics.Debug.WriteLine($"StatusBar toggled - Expanded: {isExpanded}, Height: {statusBarHeight}");
            
            _statusBarExpanded = isExpanded;
            _statusBarHeight = statusBarHeight;
            
            if (isExpanded)
            {
                AdjustWindowsForStatusBar();
            }
            else
            {
                RestoreOriginalWindowSizes();
            }
        }
        
        /// <summary>
        /// Passt alle sichtbaren Fenster an die StatusBar an
        /// </summary>
        private void AdjustWindowsForStatusBar()
        {
            System.Diagnostics.Debug.WriteLine("Adjusting windows for expanded StatusBar");
            
            // Alle sichtbaren Fenster finden und anpassen
            var visibleWindows = GetVisibleWindows();
            
            foreach (var hwnd in visibleWindows)
            {
                try
                {
                    // Aktuellen Zustand speichern falls noch nicht gespeichert
                    if (!_trackedWindows.ContainsKey(hwnd))
                    {
                        if (GetWindowRect(hwnd, out RECT rect))
                        {
                            _trackedWindows[hwnd] = new WindowState
                            {
                                OriginalRect = rect,
                                IsAdjusted = false
                            };
                        }
                    }
                    
                    var windowState = _trackedWindows[hwnd];
                    if (!windowState.IsAdjusted)
                    {
                        // Fenster anpassen
                        AdjustSingleWindow(hwnd, windowState);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adjusting window {hwnd}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Passt ein einzelnes Fenster an
        /// </summary>
        private void AdjustSingleWindow(IntPtr hwnd, WindowState windowState)
        {
            var original = windowState.OriginalRect;
            
            // Prüfen ob Fenster von StatusBar überdeckt wird
            if (original.Top < _statusBarHeight)
            {
                System.Diagnostics.Debug.WriteLine($"Adjusting window at top {original.Top} (StatusBar height: {_statusBarHeight})");
                
                // Neue Position und Größe berechnen
                int newTop = (int)_statusBarHeight;
                int newHeight = original.Height - (newTop - original.Top);
                
                // Sicherstellen dass Fenster noch sichtbar ist
                if (newHeight > 100) // Mindesthöhe
                {
                    bool success = SetWindowPos(hwnd, IntPtr.Zero, 
                        original.Left, newTop, 
                        original.Width, newHeight, 
                        SWP_NOZORDER | SWP_NOACTIVATE);
                    
                    if (success)
                    {
                        windowState.IsAdjusted = true;
                        System.Diagnostics.Debug.WriteLine($"Successfully adjusted window to {original.Left},{newTop} {original.Width}x{newHeight}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to adjust window position");
                    }
                }
            }
        }
        
        /// <summary>
        /// Stellt alle Fenster auf ihre ursprüngliche Größe zurück
        /// </summary>
        private void RestoreOriginalWindowSizes()
        {
            System.Diagnostics.Debug.WriteLine("Restoring original window sizes");
            
            foreach (var kvp in _trackedWindows)
            {
                var hwnd = kvp.Key;
                var windowState = kvp.Value;
                
                if (windowState.IsAdjusted)
                {
                    try
                    {
                        var original = windowState.OriginalRect;
                        
                        bool success = SetWindowPos(hwnd, IntPtr.Zero,
                            original.Left, original.Top,
                            original.Width, original.Height,
                            SWP_NOZORDER | SWP_NOACTIVATE);
                        
                        if (success)
                        {
                            windowState.IsAdjusted = false;
                            System.Diagnostics.Debug.WriteLine($"Successfully restored window to {original.Left},{original.Top} {original.Width}x{original.Height}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error restoring window {hwnd}: {ex.Message}");
                    }
                }
            }
            
            // Cleanup - entferne nicht mehr existierende Fenster
            CleanupTrackedWindows();
        }
        
        /// <summary>
        /// Findet alle sichtbaren Fenster
        /// </summary>
        private List<IntPtr> GetVisibleWindows()
        {
            var windows = new List<IntPtr>();
            var currentProcessId = (uint)Process.GetCurrentProcess().Id;
            
            // Alle Top-Level Fenster durchlaufen 
            SeroDesk.Platform.NativeMethods.EnumWindows((hwnd, lParam) =>
            {
                try
                {
                    // Nur sichtbare, nicht minimierte Fenster
                    if (IsWindowVisible(hwnd) && !IsIconic(hwnd))
                    {
                        // Eigene Fenster (SeroDesk) ausschließen
                        GetWindowThreadProcessId(hwnd, out uint windowProcessId);
                        if (windowProcessId != currentProcessId)
                        {
                            // Titel prüfen um System-Fenster zu filtern
                            var title = GetWindowTitle(hwnd);
                            if (!string.IsNullOrEmpty(title) && 
                                !title.Contains("Program Manager") && 
                                !title.Contains("Desktop Window Manager"))
                            {
                                windows.Add(hwnd);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignoriere Fehler bei einzelnen Fenstern
                }
                
                return true; // Weitermachen
            }, IntPtr.Zero);
            
            System.Diagnostics.Debug.WriteLine($"Found {windows.Count} visible windows to potentially adjust");
            return windows;
        }
        
        /// <summary>
        /// Holt den Titel eines Fensters
        /// </summary>
        private string GetWindowTitle(IntPtr hwnd)
        {
            var title = new System.Text.StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            return title.ToString();
        }
        
        /// <summary>
        /// Entfernt nicht mehr existierende Fenster aus der Tracking-Liste
        /// </summary>
        private void CleanupTrackedWindows()
        {
            var toRemove = new List<IntPtr>();
            
            foreach (var hwnd in _trackedWindows.Keys)
            {
                if (!IsWindowVisible(hwnd))
                {
                    toRemove.Add(hwnd);
                }
            }
            
            foreach (var hwnd in toRemove)
            {
                _trackedWindows.Remove(hwnd);
            }
            
            if (toRemove.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Cleaned up {toRemove.Count} no longer visible windows");
            }
        }
        
        /// <summary>
        /// Setzt alle Verfolgungen zurück (z.B. beim Herunterfahren)
        /// </summary>
        public void Reset()
        {
            System.Diagnostics.Debug.WriteLine("WindowResizeService reset - restoring all windows");
            
            if (_statusBarExpanded)
            {
                RestoreOriginalWindowSizes();
            }
            
            _trackedWindows.Clear();
            _statusBarExpanded = false;
        }
    }
}