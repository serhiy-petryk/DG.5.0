using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Common;
using WpfSpLib.Helpers;
using WpfSpLib.Themes;

namespace WpfSpLib.Controls
{
    /// <summary>
    /// Interaction logic for MwiContainer.xaml
    /// </summary>
    [ContentProperty("Children")]
    public partial class MwiContainer: ContentControl, INotifyPropertyChanged, IColorThemeSupport
    {
        const int WINDOW_OFFSET_STEP = 25;
        private static int controlId = 0;
        internal int _controlId = controlId++;

        static MwiContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MwiContainer), new FrameworkPropertyMetadata(typeof(MwiContainer)));
            // KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(MwiContainer), new FrameworkPropertyMetadata(false));
            FocusableProperty.OverrideMetadata(typeof(MwiContainer), new FrameworkPropertyMetadata(false));
        }

        public MwiContainer()
        {
            CmdSetLayout = new RelayCommand(ExecuteWindowsMenuOption, CanExecuteWindowsMenuOption);
            Children.CollectionChanged += OnChildrenCollectionChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (this.IsElementDisposing())
            {
                if (Children != null)
                {
                    Children.CollectionChanged -= OnChildrenCollectionChanged;
                    foreach (MwiChild mwiChild in Children.ToArray())
                        mwiChild.Close();
                }
            }
        }

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var o in e.NewItems)
                    {
                        if (!(o is MwiChild mwiChild))
                            throw new Exception($"All children of MwiContainer object have to be MwiChild type but it is '{o.GetType().Name}' type");
                        mwiChild.MwiContainer = this;
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (VisualTreeHelper.GetParent(mwiChild) is Panel parent)  // suppress VS designer error: InvalidOperationException: Specified element is already the logical child of another element. Disconnect it first
                                parent.Children.Remove(mwiChild);
                            if (!mwiChild.Position.HasValue)
                                mwiChild.Position = GetStartPositionForMwiChild(mwiChild);
                            MwiPanel?.Children.Add(mwiChild);
                            mwiChild.Activate();
                        }), DispatcherPriority.Background);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (MwiChild oldChild in e.OldItems)
                        MwiPanel.Children.Remove(oldChild);

                    GetTopChild(Children)?.Activate(false);
                    InvalidateLayout();

                    break;

                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:  // only for correct view in VS designer
                    break;

                default: throw new Exception($"Please, check code for {e.Action} action. New items: {e.NewItems}. OldItems: {e.OldItems}");
            }
        }

        private Point GetStartPositionForMwiChild(MwiChild mwiChild)
        {
            if (MwiPanel == null) return new Point();

            _windowOffset += WINDOW_OFFSET_STEP;
            if ((_windowOffset + mwiChild.ActualWidth > MwiPanel.ActualWidth) || (_windowOffset + mwiChild.ActualHeight > MwiPanel.ActualHeight))
                _windowOffset = 0;
            return new Point(_windowOffset, _windowOffset);
        }

        internal void InvalidateLayout()
        {
            var maximizedFlag = false;
            foreach (var mwiChild in InternalWindows.Where(w => w.WindowState != WindowState.Minimized).OrderByDescending(Panel.GetZIndex))
            {
                if (maximizedFlag)
                {
                    if (mwiChild.Visibility != Visibility.Collapsed)
                    {
                        mwiChild.RefreshThumbnail();
                        mwiChild.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    if (mwiChild.Visibility != Visibility.Visible) mwiChild.Visibility = Visibility.Visible;
                    maximizedFlag = mwiChild.WindowState == WindowState.Maximized;
                }
            }
        }

        #region ============  Override  ====================
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            MwiPanel = GetTemplateChild("MwiPanel") as Grid;
            _leftPanelContainer = GetTemplateChild("LeftPanelContainer") as Grid;
            _leftPanelButton = GetTemplateChild("LeftPanelButton") as ToggleButton;
            if (_leftPanelButton != null)
            {
                _leftPanelButton.Checked += LeftPanelButton_OnCheckedChanged;
                _leftPanelButton.Unchecked += LeftPanelButton_OnCheckedChanged;
            }

            if (GetTemplateChild("LeftPanelDragThumb") is Thumb leftPanelDragThumb)
                leftPanelDragThumb.DragDelta += LeftPanel_OnDragDelta;

            if (GetTemplateChild("WindowsMenuButton") is ToggleButton windowsMenuButton)
                windowsMenuButton.Checked += OnWindowsMenuButtonChecked;

            if (Window.GetWindow(this) is Window wnd) // need to check because an error in VS wpf designer
            {
                wnd.Activated += OnWindowActivated;
                wnd.Deactivated += OnWindowDeactivated;
            }

            if (DesignerProperties.GetIsInDesignMode(this)) // only for correct view in VS designer
                Dispatcher.BeginInvoke(new Action(() => OnChildrenCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Children))), DispatcherPriority.Background);
            // To fix VS correct view of MwiStartup: DesignerProperties.GetIsInDesignMode(this) ? DispatcherPriority.Background : DispatcherPriority.Normal)

            UpdateColorTheme(false, true);
            // =======================
            void OnWindowActivated(object sender, EventArgs e)
            {
                if (ActiveMwiChild != null && !ActiveMwiChild.IsWindowed)
                    ActiveMwiChild.Activate();
            }
            void OnWindowDeactivated(object sender, EventArgs e)
            {
                if (ActiveMwiChild != null && !ActiveMwiChild.IsWindowed)
                    ActiveMwiChild.IsActive = false;
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (_leftPanelButton!= null && Equals(_leftPanelButton.IsChecked, true) && !_leftPanelContainer.IsMouseOver && !_leftPanelButton.IsMouseOver)
                HideLeftPanel();
        }

        #endregion

        #region =======  Properties  =========
        public ScrollViewer ScrollViewer;
        public Grid MwiPanel;
        public ObservableCollection<object> Children { get; } = new ObservableCollection<object>(); // if define as ObservableCollection<MwiChild>: there are vs designer errors in test forms "A value of type 'MwiChild' cannot be added to a collection or dictionary of type 'ObservableCollection`1'"

        internal IEnumerable<MwiChild> InternalWindows => Children.Cast<MwiChild>().Where(w => !w.IsWindowed);
        internal MwiChild GetTopChild(IEnumerable<object> items) => items.Cast<MwiChild>().OrderByDescending(Panel.GetZIndex).FirstOrDefault();

        // Offset for new window.
        private double _windowOffset = -WINDOW_OFFSET_STEP;

        private MwiChild _activeMwiChild;
        public MwiChild ActiveMwiChild
        {
            get => _activeMwiChild;
            set
            {
                if (!Equals(_activeMwiChild, value)) _activeMwiChild = value;
                OnPropertiesChanged(nameof(ActiveMwiChild), nameof(ScrollBarKind));
            }
        }

        public ScrollBarVisibility ScrollBarKind =>
            ActiveMwiChild != null && ActiveMwiChild.WindowState == WindowState.Maximized
                ? ScrollBarVisibility.Disabled
                : ScrollBarVisibility.Auto;

        //==============================
        public static readonly DependencyProperty MwiContainerProperty = DependencyProperty.RegisterAttached("MwiContainer", typeof(MwiContainer), typeof(MwiContainer));
        public static void SetMwiContainer(DependencyObject element, MwiContainer value) => element?.SetValue(MwiContainerProperty, value); // NotNull propagation need to prevent VS designer error
        public static MwiContainer GetMwiContainer(DependencyObject element) => element?.GetValue(MwiContainerProperty) as MwiContainer; // NotNull propagation need to prevent VS designer error

        #endregion

        #region =================  INotifyPropertyChanged  ==================
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region ===============  IColorThemeSupport  =================
        public MwiThemeInfo ActualTheme => this.GetActualTheme();
        public Color ActualThemeColor => this.GetActualThemeColor();
        public IColorThemeSupport ColorThemeParent => this.GetVisualParents().OfType<MwiChild>().FirstOrDefault();
        //=================
        public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register("Theme",
            typeof(MwiThemeInfo), typeof(MwiContainer),
            new FrameworkPropertyMetadata(null, (d, e) => ((MwiContainer)d).UpdateColorTheme(false, true)));

        public MwiThemeInfo Theme
        {
            get => (MwiThemeInfo)GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }
        //=================
        public static readonly DependencyProperty ThemeColorProperty = DependencyProperty.Register("ThemeColor",
            typeof(Color?), typeof(MwiContainer),
            new FrameworkPropertyMetadata(null, (d, e) => ((MwiContainer) d).UpdateColorTheme(true, true)));

        public Color? ThemeColor
        {
            get => (Color?)GetValue(ThemeColorProperty);
            set => SetValue(ThemeColorProperty, value);
        }
        //================
        public void UpdateColorTheme(bool colorChanged, bool processChildren)
        {
            // Не виконується: if (this.IsElementDisposing()) return;

            UpdateResources(false);
            OnPropertiesChanged(nameof(ActualTheme), nameof(ActualThemeColor));

            if (processChildren)
                foreach (MwiChild element in Children)
                    element.UpdateColorTheme(colorChanged, true);
        }
        private void UpdateResources(bool colorChanged)
        {
            if (!colorChanged && ActualTheme != null)
            {
                foreach (var f1 in ActualTheme.GetResources())
                    FillResources(this, f1);
            }

            if (TryFindResource("Mwi.Container.BaseColorProxy") is BindingProxy colorProxy)
                colorProxy.Value = ActualThemeColor;
            (GetTemplateChild("WindowsBar") as MwiBar)?.UpdateTabItems();
        }
        private static void FillResources(FrameworkElement fe, ResourceDictionary resources)
        {
            foreach (var rd in resources.MergedDictionaries)
                FillResources(fe, rd);
            foreach (var key in resources.Keys.OfType<string>().Where(key => key.StartsWith("Mwi.Container") || key.StartsWith("Mwi.Bar")))
                fe.Resources[key] = resources[key];
        }

        #endregion
    }
}
