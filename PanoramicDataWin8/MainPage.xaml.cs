using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Windows.Devices.Input;
using PanoramicDataWin8.view.vis;
using Windows.UI.Input;
using MathNet.Numerics.LinearAlgebra;
using PanoramicDataWin8.view;
using PanoramicDataWin8.view.common;
using PanoramicDataWin8.utils;
using Windows.UI.Notifications;
using Windows.UI.Core;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents;
using DynamicExpresso;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PanoramicDataWin8.controller.data;
using PanoramicDataWin8.controller.input;
using PanoramicDataWin8.controller.view;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.result;
using PanoramicDataWin8.model.data.sim;
using PanoramicDataWin8.model.data.tuppleware;
using PanoramicDataWin8.model.view;
using PanoramicDataWin8.model.view.tilemenu;
using PanoramicDataWin8.view.setting;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PanoramicDataWin8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PointerManager _mainPointerManager = new PointerManager();
        private Point _mainPointerManagerPreviousPoint = new Point();
        private DispatcherTimer _messageTimer = new DispatcherTimer();

        private DispatcherTimer _flushTimer = new DispatcherTimer();

        private TileMenuItemView _inputMenu = null;
        private TileMenuItemView _visualizationMenu = null;
        private TileMenuItemView _jobMenu = null;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            this.DataContextChanged += MainPage_DataContextChanged;
            //this.KeyUp += MainPage_KeyUp;
            this.KeyDown += MainPage_KeyDown;
            Application.Current.Suspending += Current_Suspending;

            _messageTimer.Interval = TimeSpan.FromMilliseconds(2000);
            _messageTimer.Tick += _messageTimer_Tick;

            _flushTimer.Interval = TimeSpan.FromMilliseconds(500);
            _flushTimer.Tick += _flushTimer_Tick;
            _flushTimer.Start();

            this.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(MainPage_PointerPressed), true);
            this.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(MainPage_PointerReleased), true);
            this.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(MainPage_PointerMoved), true);

        }

        private void _flushTimer_Tick(object sender, object e)
        {
            if (Logger.Instance != null)
            {
                Logger.Instance.Flush();
            }
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (Logger.Instance != null)
            {
                Logger.Instance.Flush();
            }
        }

        private void MainPage_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (Logger.Instance != null)
            {
                var p = e.GetCurrentPoint(this);
                Logger.Instance.LogMouse("Moved", p.Position.X, p.Position.Y);
            }
        }

        private void MainPage_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (Logger.Instance != null)
            {
                bool isRight = false;
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                {
                    var properties = e.GetCurrentPoint(this).Properties;
                    if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
                    {
                        isRight = true;
                    }
                }
                var p = e.GetCurrentPoint(this);
                Logger.Instance.LogMouse("Released", p.Position.X, p.Position.Y, isRight);
            }
        }

        private void MainPage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (Logger.Instance != null)
            {
                bool isRight = false;
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                {
                    var properties = e.GetCurrentPoint(this).Properties;
                    if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
                    {
                        isRight = true;
                    }
                }
                var p = e.GetCurrentPoint(this);
                Logger.Instance.LogMouse("Pressed", p.Position.X, p.Position.Y, isRight);
            }
        }

        void _messageTimer_Tick(object sender, object e)
        {
            msgTextBlock.Opacity = 0;
            _messageTimer.Stop();
        }

        void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var state = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            if ((state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
            {

                if (e.Key == Windows.System.VirtualKey.Q)
                {
                    MainViewController.Instance.MainModel.SampleSize = MainViewController.Instance.MainModel.SampleSize + 100;
                    Debug.WriteLine("SampleSize : " + MainViewController.Instance.MainModel.SampleSize);

                    msgTextBlock.Text = ("SampleSize : " + MainViewController.Instance.MainModel.SampleSize);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                else if (e.Key == Windows.System.VirtualKey.A)
                {
                    MainViewController.Instance.MainModel.SampleSize = Math.Max(MainViewController.Instance.MainModel.SampleSize - 100, 1.0);
                    Debug.WriteLine("SampleSize : " + MainViewController.Instance.MainModel.SampleSize);

                    msgTextBlock.Text = ("SampleSize : " + MainViewController.Instance.MainModel.SampleSize);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == Windows.System.VirtualKey.W)
                {
                    MainViewController.Instance.MainModel.ThrottleInMillis = MainViewController.Instance.MainModel.ThrottleInMillis + 300.0;
                    Debug.WriteLine("Throttle : " + MainViewController.Instance.MainModel.ThrottleInMillis);

                    msgTextBlock.Text = ("Throttle : " + MainViewController.Instance.MainModel.ThrottleInMillis);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                else if (e.Key == Windows.System.VirtualKey.S)
                {
                    MainViewController.Instance.MainModel.ThrottleInMillis = Math.Max(MainViewController.Instance.MainModel.ThrottleInMillis - 300.0, 0.0);
                    Debug.WriteLine("Throttle : " + MainViewController.Instance.MainModel.ThrottleInMillis);

                    msgTextBlock.Text = ("Throttle : " + MainViewController.Instance.MainModel.ThrottleInMillis);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == Windows.System.VirtualKey.E)
                {
                    MainViewController.Instance.MainModel.NrOfXBins = MainViewController.Instance.MainModel.NrOfXBins + 1;
                    Debug.WriteLine("NrOfXBins : " + MainViewController.Instance.MainModel.NrOfXBins);

                    msgTextBlock.Text = ("NrOfXBins : " + MainViewController.Instance.MainModel.NrOfXBins);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                else if (e.Key == Windows.System.VirtualKey.D)
                {
                    MainViewController.Instance.MainModel.NrOfXBins = Math.Max(MainViewController.Instance.MainModel.NrOfXBins - 1, 1.0);
                    Debug.WriteLine("NrOfXBins : " + MainViewController.Instance.MainModel.NrOfXBins);

                    msgTextBlock.Text = ("NrOfXBins : " + MainViewController.Instance.MainModel.NrOfXBins);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == Windows.System.VirtualKey.R)
                {
                    MainViewController.Instance.MainModel.NrOfYBins = MainViewController.Instance.MainModel.NrOfYBins + 1;
                    Debug.WriteLine("NrOfYBins : " + MainViewController.Instance.MainModel.NrOfYBins);

                    msgTextBlock.Text = ("NrOfYBins : " + MainViewController.Instance.MainModel.NrOfYBins);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                else if (e.Key == Windows.System.VirtualKey.F)
                {
                    MainViewController.Instance.MainModel.NrOfYBins = Math.Max(MainViewController.Instance.MainModel.NrOfYBins - 1, 1.0);
                    Debug.WriteLine("NrOfYBins : " + MainViewController.Instance.MainModel.NrOfYBins);

                    msgTextBlock.Text = ("NrOfYBins : " + MainViewController.Instance.MainModel.NrOfYBins);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == Windows.System.VirtualKey.Number1)
                {
                    MainViewController.Instance.MainModel.GraphRenderOption = GraphRenderOptions.Grid;
                    Debug.WriteLine("GraphRenderOption : " + MainViewController.Instance.MainModel.GraphRenderOption.ToString());

                    msgTextBlock.Text = ("GraphRenderOption : " + MainViewController.Instance.MainModel.GraphRenderOption.ToString());
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == Windows.System.VirtualKey.Number2)
                {
                    MainViewController.Instance.MainModel.GraphRenderOption = GraphRenderOptions.Cell;
                    Debug.WriteLine("GraphRenderOption : " + MainViewController.Instance.MainModel.GraphRenderOption.ToString());

                    msgTextBlock.Text = ("GraphRenderOption : " + MainViewController.Instance.MainModel.GraphRenderOption.ToString());
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == Windows.System.VirtualKey.V)
                {
                    MainViewController.Instance.MainModel.Verbose = !MainViewController.Instance.MainModel.Verbose;
                    Debug.WriteLine("Verbose : " + MainViewController.Instance.MainModel.Verbose.ToString());

                    msgTextBlock.Text = ("Verbose : " + MainViewController.Instance.MainModel.Verbose.ToString());
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == VirtualKey.P)
                {
                    Debug.WriteLine("Render Fingers / Pen : " + MainViewController.Instance.MainModel.RenderFingersAndPen);
                    MainViewController.Instance.MainModel.RenderFingersAndPen = !MainViewController.Instance.MainModel.RenderFingersAndPen;
                    msgTextBlock.Text = ("Fingers / Pen : " + MainViewController.Instance.MainModel.RenderFingersAndPen);
                    msgTextBlock.Opacity = 1;
                    _messageTimer.Start();
                }
                if (e.Key == VirtualKey.L)
                {
                    var interpreter = new Interpreter();
                    //Lambda parsedExpression = interpreter.Parse("test == \"tefst\"",
                    //                          new Parameter("test", typeof(string)));
                  Lambda parsedExpression = interpreter.Parse("blabla == 5 and b == 4",
                                            new Parameter("blabla", typeof(string)), new Parameter("b", typeof(double)));
                  var result = parsedExpression.Invoke("asd", 4);
                }
            }
        }
        
        void MainPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                (args.NewValue as MainModel).PropertyChanged += MainPage_PropertyChanged;
                (args.NewValue as MainModel).DatasetConfigurations.CollectionChanged += DatasetConfigurations_CollectionChanged;
            }
        }

        void MainPage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var model = DataContext as MainModel;
            if (model.SchemaModel != null &&
                e.PropertyName == model.GetPropertyName(() => model.SchemaModel))
            {
                loadInputModels();
            }
        }

        void DatasetConfigurations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            /*commandBar.SecondaryCommands.Clear();
            foreach (var datasetConfiguration in (DataContext as MainModel).DatasetConfigurations)
            {
                AppBarButton b = new AppBarButton();
                b.Style =  Application.Current.Resources.MergedDictionaries[0]["AppBarButtonStyle1"] as Style;
                b.Label = datasetConfiguration.Name;
                b.Icon = new SymbolIcon(Symbol.Library);
                b.DataContext = datasetConfiguration;
                b.Click += appBarButton_Click;
                commandBar.SecondaryCommands.Add(b);
            }*/

        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            MainViewController.CreateInstance(layoutRoot, v1, v2, v3, v4, this);
            DataContext = MainViewController.Instance.MainModel;
            

        }
        
        private void loadInputModels()
        {
            MainModel mainModel = (DataContext as MainModel);
            if (_inputMenu == null && mainModel.SchemaModel != null)
            {
                var inputModels =
                    mainModel.SchemaModel.OriginModels.First()
                        .InputModels.Where(am => am.IsDisplayed);

                if (_inputMenu != null)
                {
                    ((TileMenuItemViewModel) _inputMenu.DataContext).AreChildrenExpanded = false;
                    ((TileMenuItemViewModel) _inputMenu.DataContext).IsBeingRemoved = true;
                    _inputMenu.Dispose();
                    menuCanvas.Children.Remove(_inputMenu);
                }

                TileMenuItemViewModel parentModel = new TileMenuItemViewModel(null);
                parentModel.ChildrenNrColumns = (int) Math.Ceiling(inputModels.Count()/8.0);
                parentModel.ChildrenNrRows = (int) Math.Min(8.0, inputModels.Count());

                menuCanvas.Width = (int) Math.Ceiling(inputModels.Count()/8.0) * 50 + ((int)Math.Ceiling(inputModels.Count() / 8.0) -1) * 4;

                parentModel.Alignment = Alignment.LeftOrTop;
                parentModel.AttachPosition = AttachPosition.Right;

                int count = 0;
                foreach (var inputModel in inputModels)
                {
                    TileMenuItemViewModel tileMenuItemViewModel = recursiveCreateTileMenu(inputModel, parentModel);
                    tileMenuItemViewModel.Row = count;
                    tileMenuItemViewModel.Column = parentModel.ChildrenNrColumns - (int) Math.Floor((parentModel.Children.Count - 1)/8.0) - 1;
                    tileMenuItemViewModel.RowSpan = 1;
                    tileMenuItemViewModel.ColumnSpan = 1;
                    Debug.WriteLine(inputModel.Name + " c: " + tileMenuItemViewModel.Column + " r : " + tileMenuItemViewModel.Row);
                    count++;
                    if (count == 8.0)
                    {
                        count = 0;
                    }
                }

                _inputMenu = new TileMenuItemView {MenuCanvas = menuCanvas, DataContext = parentModel};
                menuCanvas.Children.Add(_inputMenu);

                parentModel.CurrentPosition = new Pt(-4,0);
                parentModel.TargetPosition = new Pt(-4,0);
                parentModel.Size = new Vec(0,0);
                parentModel.AreChildrenExpanded = true;
            }
        }

        private TileMenuItemViewModel recursiveCreateTileMenu(object inputModel, TileMenuItemViewModel parent)
        {
            TileMenuItemViewModel currentTileMenuItemViewModel = null;
            if (inputModel is InputGroupModel)
            {
                var inputGroupModel = inputModel as InputGroupModel;
                currentTileMenuItemViewModel = new TileMenuItemViewModel(parent);
                InputGroupViewModel inputGroupViewModel = new InputGroupViewModel(null, inputGroupModel);
                currentTileMenuItemViewModel.TileMenuContentViewModel = new InputGroupViewTileMenuContentViewModel()
                {
                    Name = inputGroupModel.Name,
                    InputGroupViewModel = inputGroupViewModel
                };

                currentTileMenuItemViewModel.ChildrenNrColumns = (int)Math.Ceiling(inputGroupModel.InputModels.Count() / 8.0);
                currentTileMenuItemViewModel.ChildrenNrRows = (int)Math.Min(8.0, inputGroupModel.InputModels.Count());
                currentTileMenuItemViewModel.Alignment = Alignment.Center;
                currentTileMenuItemViewModel.AttachPosition = AttachPosition.Right;

                int count = 0;
                foreach (var childInputModel in inputGroupModel.InputModels/*.OrderBy(am => am.Name)*/)
                {
                    var childTileMenu = recursiveCreateTileMenu(childInputModel, currentTileMenuItemViewModel);
                    childTileMenu.Row = count; // TileMenuItemViewModel.Children.Count;
                    childTileMenu.Column = (currentTileMenuItemViewModel.ChildrenNrColumns - 1) - (int)Math.Floor((currentTileMenuItemViewModel.Children.Count - 1) / 8.0);
                    childTileMenu.RowSpan = 1;
                    childTileMenu.ColumnSpan = 1;
                    //currentTileMenuItemViewModel.Children.Add(childTileMenu);
                    count++;
                    if (count == 8.0)
                    {
                        count = 0;
                    }
                }
            }
            else if (inputModel is InputFieldModel)
            {
                currentTileMenuItemViewModel = new TileMenuItemViewModel(parent);
                InputFieldViewModel inputFieldViewModel = new InputFieldViewModel(null, new InputOperationModel(inputModel as InputFieldModel));
                currentTileMenuItemViewModel.TileMenuContentViewModel = new InputFieldViewTileMenuContentViewModel()
                {
                    Name = (inputModel as InputFieldModel).Name,
                    InputFieldViewModel = inputFieldViewModel
                };
            } 
            else if (inputModel is TaskGroupModel)
            {
                var taskGroupModel = inputModel as TaskGroupModel;
                currentTileMenuItemViewModel = new TileMenuItemViewModel(parent);
                currentTileMenuItemViewModel.TileMenuContentViewModel = new TaskGroupViewTileMenuContentViewModel()
                {
                    Name = taskGroupModel.Name,
                    TaskGroupModel = taskGroupModel
                };

                currentTileMenuItemViewModel.ChildrenNrColumns = (int)Math.Ceiling(taskGroupModel.TaskModels.Count() / 8.0);
                currentTileMenuItemViewModel.ChildrenNrRows = (int)Math.Min(8.0, taskGroupModel.TaskModels.Count());
                currentTileMenuItemViewModel.Alignment = Alignment.Center;
                currentTileMenuItemViewModel.AttachPosition = AttachPosition.Right;

                int count = 0;
                foreach (var childInputModel in taskGroupModel.TaskModels/*.OrderBy(am => am.Name)*/)
                {
                    var childTileMenu = recursiveCreateTileMenu(childInputModel, currentTileMenuItemViewModel);
                    childTileMenu.Row = count; // TileMenuItemViewModel.Children.Count;
                    childTileMenu.Column = (currentTileMenuItemViewModel.ChildrenNrColumns - 1) - (int)Math.Floor((currentTileMenuItemViewModel.Children.Count - 1) / 8.0);
                    childTileMenu.RowSpan = 1;
                    childTileMenu.ColumnSpan = 1;
                    //currentTileMenuItemViewModel.Children.Add(childTileMenu);
                    count++;
                    if (count == 8.0)
                    {
                        count = 0;
                    }
                }
            }
            else if (inputModel is TaskModel)
            {
                currentTileMenuItemViewModel = new TileMenuItemViewModel(parent);
                currentTileMenuItemViewModel.TileMenuContentViewModel = new TaskViewTileMenuContentViewModel()
                {
                    Name = (inputModel as TaskModel).Name,
                    TaskModel = (inputModel as TaskModel)
                };
            }
            parent.Children.Add(currentTileMenuItemViewModel);
            currentTileMenuItemViewModel.Alignment = Alignment.Center;
            currentTileMenuItemViewModel.AttachPosition = AttachPosition.Right;
            return currentTileMenuItemViewModel;
        }


        private void clearAndDisposeMenus()
        {
            if (_jobMenu != null)
            {
                ((TileMenuItemViewModel)_jobMenu.DataContext).AreChildrenExpanded = false;
                ((TileMenuItemViewModel)_jobMenu.DataContext).IsBeingRemoved = true;
                _jobMenu.Dispose();
                menuCanvas.Children.Remove(_jobMenu);
                _jobMenu = null;
            }
            if (_inputMenu != null)
            {
                ((TileMenuItemViewModel)_inputMenu.DataContext).AreChildrenExpanded = false;
                ((TileMenuItemViewModel)_inputMenu.DataContext).IsBeingRemoved = true;
                _inputMenu.Dispose();
                menuCanvas.Children.Remove(_inputMenu);
                _inputMenu = null;
            }
            if (_visualizationMenu != null)
            {
                ((TileMenuItemViewModel)_visualizationMenu.DataContext).AreChildrenExpanded = false;
                ((TileMenuItemViewModel)_visualizationMenu.DataContext).IsBeingRemoved = true;
                _visualizationMenu.Dispose();
                menuCanvas.Children.Remove(_visualizationMenu);
                _visualizationMenu = null;
            }

        }

        private void Refresh_OnClick(object sender, RoutedEventArgs e)
        {
            clearAndDisposeMenus();
            MainViewController.Instance.LoadConfigs();
        }

        private async void SettingsButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            SettingsDialogView signInDialog = new SettingsDialogView(
                new Settings()
                {
                    Dataset = MainViewController.Instance.MainModel.Dataset,
                    Mode = MainViewController.Instance.MainModel.Mode,
                    Seed = MainViewController.Instance.MainModel.Seed, 
                    Participant = MainViewController.Instance.MainModel.Participant,
                    DelayInMs = MainViewController.Instance.MainModel.DelayInMs
                });


            await signInDialog.ShowAsync();

            if (signInDialog.Load)
            {
                clearAndDisposeMenus();

                MainViewController.Instance.MainModel.Dataset = signInDialog.Settings.Dataset;
                MainViewController.Instance.MainModel.Seed = signInDialog.Settings.Seed;
                MainViewController.Instance.MainModel.Mode = signInDialog.Settings.Mode;
                MainViewController.Instance.MainModel.Participant = signInDialog.Settings.Participant;
                MainViewController.Instance.MainModel.DelayInMs = signInDialog.Settings.DelayInMs;

                if (MainViewController.Instance.MainModel.Dataset == Dataset.ds1)
                {
                    MainViewController.Instance.LoadData(MainViewController.Instance.MainModel.DatasetConfigurations.First(ds => ds.Name == "cars"));
                }
                else if(MainViewController.Instance.MainModel.Dataset == Dataset.ds2)
                {
                    MainViewController.Instance.LoadData(MainViewController.Instance.MainModel.DatasetConfigurations.First(ds => ds.Name == "wine"));
                }
                else if (MainViewController.Instance.MainModel.Dataset == Dataset.ds3)
                {
                    MainViewController.Instance.LoadData(MainViewController.Instance.MainModel.DatasetConfigurations.First(ds => ds.Name == "census"));
                }
                else if (MainViewController.Instance.MainModel.Dataset == Dataset.ds4)
                {
                    MainViewController.Instance.LoadData(MainViewController.Instance.MainModel.DatasetConfigurations.First(ds => ds.Name == "titanic"));
                }
                await Logger.CreateInstance(MainViewController.Instance.MainModel);
                Logger.Instance. Log("loadDataset");
            }
            else
            {
                
            }
        }

        public void SetFilterQuery(string filterQuery)
        {
            tbFilter.Text = filterQuery;
            if (tbFilter.Text.Trim() != filterQuery)
            {
                tbFilter.Background = Application.Current.Resources.MergedDictionaries[0]["highlightBrush"] as SolidColorBrush;
            }
            else
            {
                tbFilter.Background = Application.Current.Resources.MergedDictionaries[0]["backgroundBrush"] as SolidColorBrush;
            }
        }


        public void SetBrushQuery(string brushQuery)
        {
            _currentBrushQuery = brushQuery;
            updateQueryTextBox(tbBrush, _currentBrushQuery);
        }

        private string _currentBrushQuery = "";
        private void TbBrush_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                _currentBrushQuery = ((TextBox) sender).Text.Trim();
                ((TextBox)sender).Background = Application.Current.Resources.MergedDictionaries[0]["backgroundBrush"] as SolidColorBrush;

                bool correct = parseExpression(_currentBrushQuery, errorTbBrush);
                if (correct)
                {
                    Logger.Instance?.Log("BrushQueryTextBox", new JProperty("valid", true), new JProperty("brushQuery", _currentBrushQuery));
                    MainViewController.Instance.SetBrushQuery(_currentBrushQuery);
                }
                else
                {
                    Logger.Instance?.Log("BrushQueryTextBox", new JProperty("valid", false), new JProperty("brushQuery", _currentBrushQuery));
                }
            }
        }

        private void TbBrush_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            updateQueryTextBox(sender as TextBox, _currentBrushQuery);
        }

        private void updateQueryTextBox(TextBox tb, string currentQuery)
        {
            if (tb.Text.Trim() != currentQuery)
            {
                tb.Background = Application.Current.Resources.MergedDictionaries[0]["highlightBrush"] as SolidColorBrush;
            }
            else
            {
                tb.Background = Application.Current.Resources.MergedDictionaries[0]["backgroundBrush"] as SolidColorBrush;
            }
        }

        private string _currentFilterQuery = "";
        private void TbFilter_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                _currentFilterQuery = ((TextBox) sender).Text.Trim();
                ((TextBox)sender).Background = Application.Current.Resources.MergedDictionaries[0]["backgroundBrush"] as SolidColorBrush;

                bool correct = parseExpression(_currentFilterQuery, errorTbFilter);
                if (correct)
                {
                    Logger.Instance?.Log("FilterQueryTextBox", new JProperty("valid", true), new JProperty("filterQuery", _currentFilterQuery));
                    MainViewController.Instance.MainModel.FilterQuery = _currentFilterQuery;
                    fireQueryUpdate();
                }
                else
                {
                    Logger.Instance?.Log("FilterQueryTextBox", new JProperty("valid", false), new JProperty("filterQuery", _currentFilterQuery));
                }
            }
        }

        private void TbFilter_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            updateQueryTextBox(sender as TextBox, _currentFilterQuery);
        }

        private bool parseExpression(string expr, TextBlock errorTextBox)
        {
            var interpreter = new Interpreter();
            try
            {
                var originModel = (MainViewController.Instance.MainModel.SchemaModel as SimSchemaModel).RootOriginModel;
                List<Parameter> parameters = originModel.CreateParameters();
                List<object> testValues = originModel.CreateParameterTestValues();
                Lambda parsedExpression = interpreter.Parse(expr, parameters.ToArray());
                var result = parsedExpression.Invoke(testValues.ToArray());

                errorTextBox.Text = "";
                return true;
            }
            catch (Exception exception)
            {
                errorTextBox.Text = exception.Message;
            }
            return false;
        }

        private void fireQueryUpdate()
        {
            MainViewController.Instance.VisualizationViewModel1.QueryModel.FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
            MainViewController.Instance.VisualizationViewModel2.QueryModel.FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
            MainViewController.Instance.VisualizationViewModel3.QueryModel.FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
            MainViewController.Instance.VisualizationViewModel4.QueryModel.FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }
    }
}
