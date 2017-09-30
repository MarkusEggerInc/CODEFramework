using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace CODE.Framework.Wpf.Theme.Newsroom.Controls
{
    /// <summary>Simple rendering mechanism to render a standard Newsroom loading animatin (circular)</summary>
    public class LinearProgressAnimation : Grid
    {
        /// <summary>Constructor</summary>
        public LinearProgressAnimation()
        {
            ClipToBounds = false;

            IsVisibleChanged += (o, args) =>
                                    {
                                        if ((bool) args.NewValue && IsActive) StartAnimation();
                                        else StopAnimation();
                                    };

            SizeChanged += (o, e) => TriggerVisualRefresh(this, new DependencyPropertyChangedEventArgs());
        }

        /// <summary>Defines the number of actual dots in the animation</summary>
        public int DotCount
        {
            get { return (int) GetValue(DotCountProperty); }
            set { SetValue(DotCountProperty, value); }
        }

        /// <summary>Defines the number of actual dots in the animation</summary>
        public static readonly DependencyProperty DotCountProperty = DependencyProperty.Register("DotCount", typeof (int), typeof (LinearProgressAnimation), new UIPropertyMetadata(5, TriggerVisualRefresh));

        /// <summary>Defines the diameter of each dot within the animation</summary>
        public double DotDiameter
        {
            get { return (double) GetValue(DotDiameterProperty); }
            set { SetValue(DotDiameterProperty, value); }
        }

        /// <summary>Defines the diameter of each dot within the animation</summary>
        public static readonly DependencyProperty DotDiameterProperty = DependencyProperty.Register("DotDiameter", typeof (double), typeof (LinearProgressAnimation), new UIPropertyMetadata(6d, TriggerVisualRefresh));

        /// <summary>Brush used to draw each dot</summary>
        public Brush DotBrush
        {
            get { return (Brush) GetValue(DotBrushProperty); }
            set { SetValue(DotBrushProperty, value); }
        }

        /// <summary>Brush used to draw each dot</summary>
        public static readonly DependencyProperty DotBrushProperty = DependencyProperty.Register("DotBrush", typeof (Brush), typeof (LinearProgressAnimation), new UIPropertyMetadata(Brushes.Black, TriggerVisualRefresh));

        /// <summary>determines the spacing of the individual dots (1 = neutral)</summary>
        public double DotSpaceFactor
        {
            get { return (double) GetValue(DotSpaceFactorProperty); }
            set { SetValue(DotSpaceFactorProperty, value); }
        }

        /// <summary>determines the spacing of the individual dots (1 = neutral)</summary>
        public static readonly DependencyProperty DotSpaceFactorProperty = DependencyProperty.Register("DotSpaceFactor", typeof (double), typeof (LinearProgressAnimation), new UIPropertyMetadata(1d, TriggerVisualRefresh));

        /// <summary>Sets the speed of the animation (factor 1 = neutral speed, lower factors are faster, larger factors slower, as it increases the time the animation has to perform)(</summary>
        public double DotAnimationSpeedFactor
        {
            get { return (double) GetValue(DotAnimationSpeedFactorProperty); }
            set { SetValue(DotAnimationSpeedFactorProperty, value); }
        }

        /// <summary>Sets the speed of the animation (factor 1 = neutral speed, lower factors are faster, larger factors slower, as it increases the time the animation has to perform)(</summary>
        public static readonly DependencyProperty DotAnimationSpeedFactorProperty = DependencyProperty.Register("DotAnimationSpeedFactor", typeof (double), typeof (LinearProgressAnimation), new UIPropertyMetadata(1d, TriggerVisualRefresh));

        /// <summary>Indicates whether the progress animation is active</summary>
        /// <value>True if active</value>
        /// <remarks>For the progress animation to be displayed, the IsActive must be true, and the control must have its visibility set to visible.</remarks>
        public bool IsActive
        {
            get { return (bool) GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        /// <summary>Indicates whether the progress animation is active</summary>
        /// <value>True if active</value>
        /// <remarks>For the progress animation to be displayed, the IsActive must be true, and the control must have its visibility set to visible.</remarks>
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof (bool), typeof (LinearProgressAnimation),
                                                                                                 new UIPropertyMetadata(false, (s, e) =>
                                                                                                                                   {
                                                                                                                                       var progress = s as LinearProgressAnimation;
                                                                                                                                       if (progress != null && (bool) e.NewValue && progress.Visibility == Visibility.Visible) progress.StartAnimation();
                                                                                                                                       else if (progress != null) progress.StopAnimation();
                                                                                                                                   }));

        /// <summary>Triggers a re-creation of all the child elements that make up the animation</summary>
        /// <param name="o">Dependency Object</param>
        /// <param name="e">Event arguments</param>
        private static void TriggerVisualRefresh(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var o2 = o as LinearProgressAnimation;
            if (o2 != null && o2.IsActive)
            {
                o2.StopAnimation();
                o2.CreateVisuals(o2.DotCount);
                if (o2.IsActive && o2.Visibility == Visibility.Visible)
                    o2.StartAnimation();
            }
        }

        /// <summary>Creates the actual visual elements that make up the animation</summary>
        /// <param name="circleCount">Number of circles to use in the animation</param>
        private void CreateVisuals(int circleCount)
        {
            Children.Clear();
            _storyboards.Clear();

            var slowDistance = DotCount * (DotDiameter + 10);
            var fastDistance = (ActualWidth - slowDistance) / 2;
            var widthSpeedFactor = 1d;
            if (fastDistance < slowDistance*2) widthSpeedFactor = .5d;
            else if (fastDistance < slowDistance * 3) widthSpeedFactor = .6d;
            // Additional speed adjustment needed for number of dots
            widthSpeedFactor = widthSpeedFactor*(7d/DotCount);

            for (int counter = 0; counter < circleCount; counter++)
            {
                var ellipse = new Ellipse
                                  {
                                      Height = DotDiameter,
                                      Width = DotDiameter,
                                      Fill = DotBrush,
                                      HorizontalAlignment = HorizontalAlignment.Left,
                                      VerticalAlignment = VerticalAlignment.Center,
                                      Margin = new Thickness(DotDiameter*-1, 0, 0, 0)
                                  };
                Children.Add(ellipse);

                var a1 = new ThicknessAnimationUsingKeyFrames
                             {
                                 RepeatBehavior = RepeatBehavior.Forever,
                                 BeginTime = TimeSpan.FromMilliseconds(DotSpaceFactor*175*counter*widthSpeedFactor)
                             };
                Storyboard.SetTargetProperty(a1, new PropertyPath("(FrameworkElement.Margin)"));
                Storyboard.SetTarget(a1, ellipse);
                var storyboard = new Storyboard();
                storyboard.Children.Add(a1);
                a1.KeyFrames.Add(new SplineThicknessKeyFrame
                                     {
                                         KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 0)),
                                         Value = new Thickness(DotDiameter*-1, 0, 0, 0)
                                     });
                var keytime = (long)(15000000 * DotAnimationSpeedFactor * widthSpeedFactor);
                a1.KeyFrames.Add(new SplineThicknessKeyFrame
                                     {
                                         KeyTime = KeyTime.FromTimeSpan(new TimeSpan(keytime)),
                                         Value = new Thickness(fastDistance, 0, 0, 0)
                                     });
                keytime += (long)(10000000 * DotAnimationSpeedFactor); // No speec factor here!!!
                a1.KeyFrames.Add(new SplineThicknessKeyFrame
                                     {
                                         KeyTime = KeyTime.FromTimeSpan(new TimeSpan(keytime)),
                                         Value = new Thickness(fastDistance + slowDistance, 0, 0, 0)
                                     });
                keytime += (long) (10000000*DotAnimationSpeedFactor*widthSpeedFactor);
                a1.KeyFrames.Add(new SplineThicknessKeyFrame
                                     {
                                         KeyTime = KeyTime.FromTimeSpan(new TimeSpan(keytime)),
                                         Value = new Thickness(fastDistance + slowDistance + fastDistance, 0, 0, 0)
                                     });
                keytime += (long) (10000000*DotAnimationSpeedFactor*widthSpeedFactor);
                a1.KeyFrames.Add(new SplineThicknessKeyFrame
                                     {
                                         KeyTime = KeyTime.FromTimeSpan(new TimeSpan(keytime)),
                                         Value = new Thickness(ActualWidth + DotDiameter, 0, 0, 0)
                                     });
                _storyboards.Add(storyboard);
            }
        }

        /// <summary>
        /// Starts the animation.
        /// </summary>
        public void StartAnimation()
        {
            if (_animationInStartMode) return; // Preventing accidental recursive calls

            _animationInStartMode = true;
            CreateVisuals(DotCount);
            foreach (var sb in _storyboards)
                sb.Begin();
            if (!IsActive) IsActive = true;
            _animationInStartMode = false;
        }

        /// <summary>Internal field used to prevent recursive calls</summary>
        private bool _animationInStartMode;

        /// <summary>
        /// Stops the animation.
        /// </summary>
        public void StopAnimation()
        {
            foreach (var sb in _storyboards)
                sb.Stop();
        }

        private readonly List<Storyboard> _storyboards = new List<Storyboard>();
    }
}
