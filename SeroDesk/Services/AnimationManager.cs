using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Animation;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages animation settings and provides centralized animation configuration for SeroDesk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The AnimationManager provides centralized animation management that:
    /// <list type="bullet">
    /// <item>Controls global animation speed multiplier</item>
    /// <item>Enables/disables animations application-wide</item>
    /// <item>Provides consistent animation durations across components</item>
    /// <item>Applies user animation preferences from settings</item>
    /// <item>Creates reusable animation templates</item>
    /// </list>
    /// </para>
    /// <para>
    /// All animations in SeroDesk should use this manager to ensure consistent
    /// timing and respect user preferences for animation speed and enablement.
    /// </para>
    /// </remarks>
    public class AnimationManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Singleton instance of the AnimationManager.
        /// </summary>
        private static AnimationManager? _instance;
        
        /// <summary>
        /// Reference to the centralized configuration manager.
        /// </summary>
        private readonly CentralConfigurationManager _configManager;
        
        /// <summary>
        /// Whether animations are globally enabled.
        /// </summary>
        private bool _animationsEnabled = true;
        
        /// <summary>
        /// Global animation speed multiplier (0.5 to 2.0).
        /// </summary>
        private double _speedMultiplier = 1.0;
        
        /// <summary>
        /// Base animation speed in milliseconds.
        /// </summary>
        private int _baseAnimationSpeed = 300;
        
        /// <summary>
        /// Gets the singleton instance of the AnimationManager.
        /// </summary>
        public static AnimationManager Instance => _instance ??= new AnimationManager();
        
        /// <summary>
        /// Gets or sets whether animations are globally enabled.
        /// </summary>
        public bool AnimationsEnabled
        {
            get => _animationsEnabled;
            set { _animationsEnabled = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the global animation speed multiplier (0.5 to 2.0).
        /// </summary>
        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set { _speedMultiplier = Math.Max(0.5, Math.Min(2.0, value)); OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the base animation speed in milliseconds.
        /// </summary>
        public int BaseAnimationSpeed
        {
            get => _baseAnimationSpeed;
            set { _baseAnimationSpeed = Math.Max(100, Math.Min(1000, value)); OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets the effective animation duration based on speed settings.
        /// </summary>
        /// <param name="baseDuration">The base duration in milliseconds.</param>
        /// <returns>The effective duration adjusted for speed multiplier.</returns>
        public TimeSpan GetEffectiveDuration(int baseDuration)
        {
            if (!_animationsEnabled)
                return TimeSpan.Zero;
                
            var effectiveDuration = baseDuration / _speedMultiplier;
            return TimeSpan.FromMilliseconds(effectiveDuration);
        }
        
        /// <summary>
        /// Gets the effective animation duration using the base animation speed.
        /// </summary>
        /// <returns>The effective duration adjusted for speed multiplier.</returns>
        public TimeSpan GetEffectiveDuration()
        {
            return GetEffectiveDuration(_baseAnimationSpeed);
        }
        
        /// <summary>
        /// Creates a fade-in animation with the current settings.
        /// </summary>
        /// <param name="baseDuration">The base duration in milliseconds (optional).</param>
        /// <returns>A configured fade-in animation.</returns>
        public DoubleAnimation CreateFadeInAnimation(int? baseDuration = null)
        {
            var duration = baseDuration.HasValue ? GetEffectiveDuration(baseDuration.Value) : GetEffectiveDuration();
            
            return new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
        }
        
        /// <summary>
        /// Creates a fade-out animation with the current settings.
        /// </summary>
        /// <param name="baseDuration">The base duration in milliseconds (optional).</param>
        /// <returns>A configured fade-out animation.</returns>
        public DoubleAnimation CreateFadeOutAnimation(int? baseDuration = null)
        {
            var duration = baseDuration.HasValue ? GetEffectiveDuration(baseDuration.Value) : GetEffectiveDuration();
            
            return new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
        }
        
        /// <summary>
        /// Creates a slide animation with the current settings.
        /// </summary>
        /// <param name="from">Starting position.</param>
        /// <param name="to">Ending position.</param>
        /// <param name="baseDuration">The base duration in milliseconds (optional).</param>
        /// <returns>A configured slide animation.</returns>
        public DoubleAnimation CreateSlideAnimation(double from, double to, int? baseDuration = null)
        {
            var duration = baseDuration.HasValue ? GetEffectiveDuration(baseDuration.Value) : GetEffectiveDuration();
            
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
        }
        
        /// <summary>
        /// Creates a scale animation with the current settings.
        /// </summary>
        /// <param name="from">Starting scale.</param>
        /// <param name="to">Ending scale.</param>
        /// <param name="baseDuration">The base duration in milliseconds (optional).</param>
        /// <returns>A configured scale animation.</returns>
        public DoubleAnimation CreateScaleAnimation(double from, double to, int? baseDuration = null)
        {
            var duration = baseDuration.HasValue ? GetEffectiveDuration(baseDuration.Value) : GetEffectiveDuration();
            
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
        }
        
        /// <summary>
        /// Applies a fade-in animation to a UI element.
        /// </summary>
        /// <param name="element">The element to animate.</param>
        /// <param name="baseDuration">The base duration in milliseconds (optional).</param>
        public void ApplyFadeIn(UIElement element, int? baseDuration = null)
        {
            if (!_animationsEnabled)
            {
                element.Opacity = 1;
                return;
            }
            
            element.Opacity = 0;
            var animation = CreateFadeInAnimation(baseDuration);
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        
        /// <summary>
        /// Applies a fade-out animation to a UI element.
        /// </summary>
        /// <param name="element">The element to animate.</param>
        /// <param name="baseDuration">The base duration in milliseconds (optional).</param>
        /// <param name="onCompleted">Action to execute when animation completes (optional).</param>
        public void ApplyFadeOut(UIElement element, int? baseDuration = null, Action? onCompleted = null)
        {
            if (!_animationsEnabled)
            {
                element.Opacity = 0;
                onCompleted?.Invoke();
                return;
            }
            
            var animation = CreateFadeOutAnimation(baseDuration);
            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        
        /// <summary>
        /// Loads animation settings from the centralized configuration.
        /// </summary>
        public void LoadAnimationSettings()
        {
            var config = _configManager.Configuration;
            
            _animationsEnabled = config.UISettings.EnableAnimations;
            _speedMultiplier = config.UISettings.AnimationSpeedMultiplier;
            _baseAnimationSpeed = config.UISettings.AnimationSpeed;
            
            OnPropertyChanged(nameof(AnimationsEnabled));
            OnPropertyChanged(nameof(SpeedMultiplier));
            OnPropertyChanged(nameof(BaseAnimationSpeed));
        }
        
        private AnimationManager()
        {
            _configManager = CentralConfigurationManager.Instance;
            LoadAnimationSettings();
            
            // Subscribe to configuration changes
            _configManager.PropertyChanged += OnConfigurationChanged;
        }
        
        /// <summary>
        /// Handles configuration changes from CentralConfigurationManager.
        /// </summary>
        private void OnConfigurationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CentralConfigurationManager.Configuration))
            {
                LoadAnimationSettings();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}