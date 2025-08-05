namespace SeroDesk.Constants
{
    /// <summary>
    /// Provides centralized constants for UI dimensions, thresholds, and timing values throughout the SeroDesk application.
    /// </summary>
    /// <remarks>
    /// This class contains all magic numbers used in the UI to ensure consistency and maintainability.
    /// All values are based on iOS/macOS design guidelines adapted for Windows.
    /// </remarks>
    public static class UIConstants
    {
        #region StatusBar Constants
        
        /// <summary>
        /// The height of the collapsed status bar in pixels.
        /// </summary>
        public const int StatusBarHeight = 32;
        
        /// <summary>
        /// The default height of the expanded status bar in pixels.
        /// </summary>
        public const int DefaultStatusBarHeight = 60;
        
        /// <summary>
        /// The height of the mouse activation area at the top of the screen in pixels.
        /// </summary>
        /// <remarks>
        /// When the mouse enters this area, the status bar will automatically expand.
        /// </remarks>
        public const int MouseActivationArea = 5;
        
        #endregion

        #region Window Management Constants
        
        /// <summary>
        /// The minimum height allowed for a window when resized to accommodate the status bar.
        /// </summary>
        /// <remarks>
        /// This prevents windows from becoming too small to be usable.
        /// </remarks>
        public const int MinimumWindowHeight = 100;
        
        /// <summary>
        /// The Z-index used for the status bar to ensure it appears above all other windows.
        /// </summary>
        public const int StatusBarZIndex = 1000;
        
        #endregion

        #region LaunchPad Constants
        
        /// <summary>
        /// The maximum number of application icons displayed per page in the LaunchPad.
        /// </summary>
        /// <remarks>
        /// This value is optimized for a 7x5 grid layout on standard displays.
        /// </remarks>
        public const int ItemsPerPage = 35;
        
        /// <summary>
        /// The size of application icons in pixels (width and height).
        /// </summary>
        public const int IconSize = 64;
        
        /// <summary>
        /// The spacing between icons in the LaunchPad grid in pixels.
        /// </summary>
        public const int IconSpacing = 16;
        
        /// <summary>
        /// The duration of page transition animations in milliseconds.
        /// </summary>
        public const int PageTransitionDurationMs = 300;
        
        #endregion

        #region Touch and Drag Constants
        
        /// <summary>
        /// The movement threshold in pixels for touch input before canceling a tap gesture.
        /// </summary>
        /// <remarks>
        /// This higher threshold accounts for the imprecision of finger input compared to mouse.
        /// </remarks>
        public const int TouchMovementThreshold = 20;
        
        /// <summary>
        /// The movement threshold in pixels for mouse input before canceling a click gesture.
        /// </summary>
        public const int MouseMovementThreshold = 10;
        
        /// <summary>
        /// The minimum movement in pixels required to initiate a drag operation.
        /// </summary>
        public const int DragStartThreshold = 5;
        
        /// <summary>
        /// The duration in milliseconds for a long press gesture to trigger edit mode.
        /// </summary>
        public const int LongPressDurationMs = 800;
        
        #endregion

        #region Animation Constants
        
        /// <summary>
        /// The duration of the wiggle animation cycle in milliseconds.
        /// </summary>
        /// <remarks>
        /// Used when icons are in edit mode, similar to iOS behavior.
        /// </remarks>
        public const int WiggleAnimationDurationMs = 100;
        
        /// <summary>
        /// The scale factor applied to icons during drag operations.
        /// </summary>
        public const double DragScaleFactor = 1.1;
        
        /// <summary>
        /// The rotation angle in degrees applied to icons during drag operations.
        /// </summary>
        public const double DragRotationAngle = 2.0;
        
        /// <summary>
        /// The opacity of icons during drag operations (0.0 to 1.0).
        /// </summary>
        public const double DragOpacity = 0.8;
        
        /// <summary>
        /// The opacity of icons when in edit mode (0.0 to 1.0).
        /// </summary>
        public const double EditModeOpacity = 0.9;
        
        #endregion

        #region Color Constants
        
        /// <summary>
        /// The alpha value for the lightest color in icon background gradients.
        /// </summary>
        public const byte DefaultColorAlpha = 180;
        
        /// <summary>
        /// The alpha value for the medium color in icon background gradients.
        /// </summary>
        public const byte MediumColorAlpha = 120;
        
        /// <summary>
        /// The alpha value for the darkest color in icon background gradients.
        /// </summary>
        public const byte DarkColorAlpha = 80;
        
        /// <summary>
        /// The minimum RGB component value for valid color sampling.
        /// </summary>
        /// <remarks>
        /// Colors darker than this are considered too dark for accurate sampling.
        /// </remarks>
        public const byte MinColorComponent = 30;
        
        /// <summary>
        /// The maximum RGB component value for valid color sampling.
        /// </summary>
        /// <remarks>
        /// Colors brighter than this are considered too light for accurate sampling.
        /// </remarks>
        public const byte MaxColorComponent = 225;
        
        /// <summary>
        /// The minimum alpha value for a pixel to be considered in color sampling.
        /// </summary>
        public const byte MinAlpha = 100;
        
        #endregion

        #region Timer Constants
        
        /// <summary>
        /// The interval in milliseconds for updating the window list in WindowManager.
        /// </summary>
        public const int WindowUpdateIntervalMs = 200;
        
        /// <summary>
        /// The maximum number of pixels to sample when extracting dominant colors from icons.
        /// </summary>
        /// <remarks>
        /// This limit ensures color extraction performance remains acceptable for large images.
        /// </remarks>
        public const int MaxColorSampleCount = 1000;
        
        #endregion
    }
}