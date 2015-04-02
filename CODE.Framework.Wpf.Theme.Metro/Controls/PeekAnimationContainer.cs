using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace CODE.Framework.Wpf.Theme.Metro.Controls
{
    /// <summary>
    /// This control has two content elements which make up the entire surface. Elements are
    /// aligned vertically. Animations move them up and down.
    /// </summary>
    public class PeekAnimationContainer : Grid
    {
        private readonly StackPanel _animationContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeekAnimationContainer"/> class.
        /// </summary>
        public PeekAnimationContainer()
        {
            ClipToBounds = true;

            _animationContainer = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Children.Add(_animationContainer);
        }

        /// <summary>Content positioned at the top</summary>
        public UIElement ContentTop
        {
            get { return (UIElement) GetValue(ContentTopProperty); }
            set { SetValue(ContentTopProperty, value); }
        }

        /// <summary>Content positioned at the top</summary>
        public static readonly DependencyProperty ContentTopProperty = DependencyProperty.Register("ContentTop", typeof (UIElement), typeof (PeekAnimationContainer), new UIPropertyMetadata(null, TriggerRefresh));

        /// <summary>Content positioned at the top</summary>
        public UIElement ContentBottom
        {
            get { return (UIElement) GetValue(ContentBottomProperty); }
            set { SetValue(ContentBottomProperty, value); }
        }

        /// <summary>Content positioned at the top</summary>
        public static readonly DependencyProperty ContentBottomProperty = DependencyProperty.Register("ContentBottom", typeof (UIElement), typeof (PeekAnimationContainer), new UIPropertyMetadata(null, TriggerRefresh));

        /// <summary>Defines how far the animation moves the visuals vertically</summary>
        public double VerticalSlide
        {
            get { return (double) GetValue(VerticalSlideProperty); }
            set { SetValue(VerticalSlideProperty, value); }
        }

        /// <summary>Defines how far the animation moves the visuals vertically</summary>
        public static readonly DependencyProperty VerticalSlideProperty = DependencyProperty.Register("VerticalSlide", typeof (double), typeof (PeekAnimationContainer), new UIPropertyMetadata(151d, TriggerRefresh));

        /// <summary>Time (in milliseconds) the top content should be displayed</summary>
        public int TopContentDisplayDuration
        {
            get { return (int) GetValue(TopContentDisplayDurationProperty); }
            set { SetValue(TopContentDisplayDurationProperty, value); }
        }

        /// <summary>Time (in milliseconds) the top content should be displayed</summary>
        public static readonly DependencyProperty TopContentDisplayDurationProperty = DependencyProperty.Register("TopContentDisplayDuration", typeof (int), typeof (PeekAnimationContainer), new UIPropertyMetadata(8000, TriggerRefresh));

        /// <summary>Time (in milliseconds) the bottom content should be displayed</summary>
        public int BottomContentDisplayDuration
        {
            get { return (int) GetValue(BottomContentDisplayDurationProperty); }
            set { SetValue(BottomContentDisplayDurationProperty, value); }
        }

        /// <summary>Time (in milliseconds) the top content should be displayed</summary>
        public static readonly DependencyProperty BottomContentDisplayDurationProperty = DependencyProperty.Register("BottomContentDisplayDuration", typeof (int), typeof (PeekAnimationContainer), new UIPropertyMetadata(8000, TriggerRefresh));

        /// <summary>Time (in milliseconds) used for the transition from one content to the other</summary>
        public int ContentTransitionDuration
        {
            get { return (int) GetValue(ContentTransitionDurationProperty); }
            set { SetValue(ContentTransitionDurationProperty, value); }
        }

        /// <summary>Time (in milliseconds) the top content should be displayed</summary>
        public static readonly DependencyProperty ContentTransitionDurationProperty = DependencyProperty.Register("ContentTransitionDuration", typeof (int), typeof (PeekAnimationContainer), new UIPropertyMetadata(300, TriggerRefresh));

        /// <summary>Time (in milliseconds) used to delay the start of the animation</summary>
        public int AnimationStartDelay
        {
            get { return (int) GetValue(AnimationStartDelayProperty); }
            set { SetValue(AnimationStartDelayProperty, value); }
        }

        /// <summary>Time (in milliseconds) used to delay the start of the animation</summary>
        public static readonly DependencyProperty AnimationStartDelayProperty = DependencyProperty.Register("AnimationStartDelay", typeof (int), typeof (PeekAnimationContainer), new UIPropertyMetadata(0, TriggerRefresh));

        /// <summary>Maximum random additional duration (in milliseconds) to create a more dynamic animation cycle</summary>
        public int AnimationStartDelayRandomAddition
        {
            get { return (int) GetValue(AnimationStartDelayRandomAdditionProperty); }
            set { SetValue(AnimationStartDelayRandomAdditionProperty, value); }
        }

        /// <summary>Maximum random additional duration (in milliseconds) to create a more dynamic animation cycle</summary>
        public static readonly DependencyProperty AnimationStartDelayRandomAdditionProperty = DependencyProperty.Register("AnimationStartDelayRandomAddition", typeof (int), typeof (PeekAnimationContainer), new UIPropertyMetadata(5000, TriggerRefresh));


        /// <summary>Maximum random additional duration (in milliseconds) to create a more dynamic animation cycle</summary>
        public int TopContentDisplayDurationRandomAddition
        {
            get { return (int) GetValue(TopContentDisplayDurationRandomAdditionProperty); }
            set { SetValue(TopContentDisplayDurationRandomAdditionProperty, value); }
        }

        /// <summary>Maximum random additional duration (in milliseconds) to create a more dynamic animation cycle</summary>
        public static readonly DependencyProperty TopContentDisplayDurationRandomAdditionProperty = DependencyProperty.Register("TopContentDisplayDurationRandomAddition", typeof (int), typeof (PeekAnimationContainer), new UIPropertyMetadata(2500, TriggerRefresh));

        /// <summary>Maximum random additional duration (in milliseconds) to create a more dynamic animation cycle</summary>
        public int BottomContentDisplayDurationRandomAddition
        {
            get { return (int) GetValue(BottomContentDisplayDurationRandomAdditionProperty); }
            set { SetValue(BottomContentDisplayDurationRandomAdditionProperty, value); }
        }

        /// <summary>Maximum random additional duration (in milliseconds) to create a more dynamic animation cycle</summary>
        public static readonly DependencyProperty BottomContentDisplayDurationRandomAdditionProperty = DependencyProperty.Register("BottomContentDisplayDurationRandomAddition", typeof (int), typeof (PeekAnimationContainer), new UIPropertyMetadata(2500, TriggerRefresh));

        /// <summary>Indicates whether the top content is to be displayed initially</summary>
        public bool StartWithTopContent
        {
            get { return (bool) GetValue(StartWithTopContentProperty); }
            set { SetValue(StartWithTopContentProperty, value); }
        }

        /// <summary>Indicates whether the top content is to be displayed initially</summary>
        public static readonly DependencyProperty StartWithTopContentProperty = DependencyProperty.Register("StartWithTopContent", typeof (bool), typeof (PeekAnimationContainer), new UIPropertyMetadata(false, TriggerRefresh));

        private static void TriggerRefresh(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var peek = s as PeekAnimationContainer;
            if (peek != null) peek.PopulateChildren();
        }

        private void PopulateChildren()
        {
            _animationContainer.Children.Clear();
            if (ContentTop != null) _animationContainer.Children.Add(ContentTop);
            if (ContentBottom != null) _animationContainer.Children.Add(ContentBottom);

            CreateAnimations();
        }

        private Storyboard _storyboard;

        private void CreateAnimations()
        {
            if (_storyboard != null) _storyboard.Stop();

            if (ContentTop == null || ContentBottom == null) return;

            _storyboard = new Storyboard();
            var a1 = new ThicknessAnimationUsingKeyFrames {RepeatBehavior = RepeatBehavior.Forever, BeginTime = TimeSpan.FromMilliseconds(AnimationStartDelay + GetRandomAddition(AnimationStartDelayRandomAddition))};
            Storyboard.SetTargetProperty(a1, new PropertyPath("(FrameworkElement.Margin)"));
            Storyboard.SetTarget(a1, _animationContainer);
            _storyboard.Children.Add(a1);

            var position1 = StartWithTopContent ? new Thickness() : new Thickness(0, VerticalSlide*-1, 0, 0);
            var position2 = StartWithTopContent ? new Thickness(0, VerticalSlide*-1, 0, 0) : new Thickness();

            _animationContainer.Margin = position1;

            var move1 = TopContentDisplayDuration + GetRandomAddition(TopContentDisplayDurationRandomAddition);
            var move2 = move1 + ContentTransitionDuration;
            var move3 = move2 + BottomContentDisplayDuration + GetRandomAddition(BottomContentDisplayDurationRandomAddition);
            var move4 = move3 + ContentTransitionDuration;

            a1.KeyFrames.Add(new SplineThicknessKeyFrame(position1, KeyTime.FromTimeSpan(new TimeSpan(0)), new KeySpline(.75d, 0d, 0d, .75d)));
            a1.KeyFrames.Add(new SplineThicknessKeyFrame(position1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(move1)), new KeySpline(.75d, 0d, 0d, .75d)));
            a1.KeyFrames.Add(new SplineThicknessKeyFrame(position2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(move2)), new KeySpline(.75d, 0d, 0d, .75d)));
            a1.KeyFrames.Add(new SplineThicknessKeyFrame(position2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(move3)), new KeySpline(.75d, 0d, 0d, .75d)));
            a1.KeyFrames.Add(new SplineThicknessKeyFrame(position1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(move4)), new KeySpline(.75d, 0d, 0d, .75d)));

            _storyboard.Begin();
        }

        private static int GetRandomAddition(int maxAddition)
        {
            if (maxAddition == 0) return 0;
            var random = new Random();
            return random.Next(maxAddition);
        }
    }
}