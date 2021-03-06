﻿using PanoramicDataWin8.view.vis.render;
using PanoramicDataWin8.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml.Shapes;
using PanoramicDataWin8.controller.view;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.view;
using PanoramicDataWin8.view.inq;
using PanoramicDataWin8.view.style;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace PanoramicDataWin8.view.vis
{
    public sealed partial class VisualizationContainerView : UserControl, IScribbable, InputFieldViewModelEventHandler
    {
        private Point _previousPoint = new Point();
        private Point _initialPoint = new Point();
        private Stopwatch _tapStart = new Stopwatch();
        private bool _movingStarted = false;
        private bool _fingerDown = false;

        private PointerManager _resizePointerManager = new PointerManager();
        private Point _resizePointerManagerPreviousPoint = new Point();

        private Renderer _renderer = null;

        public VisualizationContainerView()
        {
            this.InitializeComponent();

            this.Loaded += VisualizationContainerView_Loaded;
            this.DataContextChanged += visualizationContainerView_DataContextChanged;
            //MainViewController.Instance.InkableScene.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(InkableScene_PointerPressed), true);
        }

        public void Dispose()
        {
            if (_renderer != null)
            {
                _renderer.Dispose();
            }
        }

        void VisualizationContainerView_Loaded(object sender, RoutedEventArgs e)
        {
            this.PointerPressed += VisualizationContainerView_PointerPressed;
        }

        void visualizationContainerView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                VisualizationViewModel model = (args.NewValue as VisualizationViewModel);
                model.QueryModel.PropertyChanged += QueryModel_PropertyChanged;
                visualizationTypeUpdated();
            }
        }

        void QueryModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            if (e.PropertyName == queryModel.GetPropertyName(() => queryModel.VisualizationType))
            {
                visualizationTypeUpdated();
            }
        }

        void visualizationTypeUpdated()
        {
            VisualizationViewModel visualizationViewModel = (DataContext as VisualizationViewModel);
            if (contentGrid.Children.Count == 1)
            {
                (contentGrid.Children.First() as Renderer).Dispose();
            }
            contentGrid.Children.Clear();

            if (visualizationViewModel.QueryModel.TaskModel == null)
            {
                /*if (visualizationViewModel.QueryModel.VisualizationType == VisualizationType.bar)
                {
                    _renderer = new PlotRenderer();
                    contentGrid.Children.Add(_renderer);
                }*/
                if (visualizationViewModel.QueryModel.VisualizationType == VisualizationType.table)
                {
                    _renderer = new TableRenderer();
                    contentGrid.Children.Add(_renderer);
                }
                else if (visualizationViewModel.QueryModel.VisualizationType == VisualizationType.plot)
                {
                    _renderer = new PlotRenderer();
                    contentGrid.Children.Add(_renderer);
                }
               
                /*else if (visualizationViewModel.QueryModel.VisualizationType == VisualizationType.line)
                {
                    _renderer = new PlotRenderer();
                    contentGrid.Children.Add(_renderer);
                }
                else if (visualizationViewModel.QueryModel.VisualizationType == VisualizationType.map)
                {
                    _renderer = new PlotRenderer();
                    contentGrid.Children.Add(_renderer);
                }*/   
            }
        }

        private Rectangle selectionShape = null;
        void VisualizationContainerView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var state = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Pen ||
                (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
            {
                return;
            }
            
            //if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {

                VisualizationViewModel model = (DataContext as VisualizationViewModel);

                var properties = e.GetCurrentPoint(this).Properties;
                if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
                {
                    MainViewController.Instance.SetFilterQuery(model.QueryModel);
                }
                else
                {
                    _tapStart.Restart();
                    _previousPoint = e.GetCurrentPoint(MainViewController.Instance.InkableScene).Position;
                    _initialPoint = _previousPoint;
                    _movingStarted = false;
                    e.Handled = true;
                    this.CapturePointer(e.Pointer);
                    this.PointerMoved += VisualizationContainerView_PointerMoved;
                    this.PointerReleased += VisualizationContainerView_PointerReleased;
                    _fingerDown = true;

                    this.SendToFront();
                    foreach (var avm in model.AttachementViewModels)
                    {
                        avm.IsDisplayed = true;
                    }
                }
            }
        }

        async void VisualizationContainerView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(MainViewController.Instance.InkableScene).Position;
            if ((_initialPoint.GetVec() - currentPoint.GetVec()).Length2 > 100 || _movingStarted)
            {
                _movingStarted = true;
                Vec delta = _previousPoint.GetVec() - currentPoint.GetVec();
                VisualizationViewModel model = (DataContext as VisualizationViewModel);
                model.Position -= delta;

                if (selectionShape == null)
                {
                    selectionShape = new Rectangle();
                    selectionShape.Stroke = Application.Current.Resources.MergedDictionaries[0]["darkBrush"] as SolidColorBrush;
                    selectionShape.RadiusX = selectionShape.RadiusY = 4;
                    selectionShape.StrokeThickness = 1;
                    renderCanvas.Children.Add(selectionShape);
                }


                // to the right
                double w, h, x, y = 0;
                if (_initialPoint.X < currentPoint.X)
                {
                    w = currentPoint.X - _initialPoint.X;
                    x = _initialPoint.X;
                }
                else
                {
                    w = _initialPoint.X - currentPoint.X;
                    x = currentPoint.X;
                }
                // to the bottom
                if (_initialPoint.Y < currentPoint.Y)
                {
                    h = currentPoint.Y - _initialPoint.Y;
                    y = _initialPoint.Y;
                }
                else
                {
                    h = _initialPoint.Y - currentPoint.Y;
                    y = currentPoint.Y;
                }
                selectionShape.Width = Math.Max(w,1);
                selectionShape.Height = Math.Max(h, 1);
                var point = MainViewController.Instance.InkableScene.TransformToVisual(this).TransformPoint(new Point(x, y));
                selectionShape.RenderTransform = new TranslateTransform() {X = point.X, Y = point.Y};
            }
            _previousPoint = currentPoint;
            e.Handled = true;
        }

        void VisualizationContainerView_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_movingStarted)
            {
                renderCanvas.Children.Clear();
                selectionShape = null;
                _movingStarted = false;
                var current = e.GetCurrentPoint(MainViewController.Instance.InkableScene).Position;
                _renderer.StartSelection(_initialPoint);
                _renderer.MoveSelection(new Point(Math.Min(current.X, _initialPoint.X), Math.Min(current.Y, _initialPoint.Y)));
                _renderer.MoveSelection(new Point(Math.Max(current.X, _initialPoint.X), Math.Min(current.Y, _initialPoint.Y)));
                _renderer.MoveSelection(new Point(Math.Max(current.X, _initialPoint.X), Math.Max(current.Y, _initialPoint.Y)));
                _renderer.MoveSelection(new Point(Math.Min(current.X, _initialPoint.X), Math.Max(current.Y, _initialPoint.Y)));
                _renderer.EndSelection();
            }
            else if (_tapStart.ElapsedMilliseconds < 300)
            {
                _renderer.StartSelection(e.GetCurrentPoint(MainViewController.Instance.InkableScene).Position);
                _renderer.EndSelection();
            }
            _fingerDown = false;
            this.ReleasePointerCapture(e.Pointer);
            this.PointerMoved -= VisualizationContainerView_PointerMoved;
            this.PointerReleased -= VisualizationContainerView_PointerReleased;


            VisualizationViewModel model = (DataContext as VisualizationViewModel);
            foreach (var avm in model.AttachementViewModels)
            {
                avm.IsDisplayed = false;
            }

            //if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            //{
            //    var properties = e.GetCurrentPoint(this).Properties;
            //    if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            //    {
            //        MainViewController.Instance.RemoveVisualizationViewModel(this);
            //    }
            //}
        }

        public GeoAPI.Geometries.IGeometry Geometry
        {
            get
            {
                VisualizationViewModel model = this.DataContext as VisualizationViewModel;

                Rct bounds = new Rct(model.Position, model.Size);
                return bounds.GetPolygon();
            }
        }

        public List<IScribbable> Children
        {
            get
            {
                IScribbable scribbable = _renderer as IScribbable;
                if (scribbable != null)
                {
                    return new List<IScribbable>() { scribbable };
                }

                return new List<IScribbable>();
            }
        }

        public void InputFieldViewModelMoved(InputFieldViewModel sender, InputFieldViewModelEventArgs e, bool overElement)
        {
            InputFieldViewModelEventHandler inputModelEventHandler = _renderer as InputFieldViewModelEventHandler;
            if (inputModelEventHandler != null)
            {
                inputModelEventHandler.InputFieldViewModelMoved(sender, e, overElement);
            }
        }

        public void InputFieldViewModelDropped(InputFieldViewModel sender, InputFieldViewModelEventArgs e, bool overElement)
        {
            InputFieldViewModelEventHandler inputModelEventHandler = _renderer as InputFieldViewModelEventHandler;
            if (inputModelEventHandler != null)
            {
                inputModelEventHandler.InputFieldViewModelDropped(sender, e, overElement);
            }
        }

        public GeoAPI.Geometries.IGeometry BoundsGeometry
        {
            get
            {
                InputFieldViewModelEventHandler inputModelEventHandler = _renderer as InputFieldViewModelEventHandler;
                if (inputModelEventHandler != null)
                {
                    return inputModelEventHandler.BoundsGeometry;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool Consume(InkStroke inkStroke)
        {
            return false;
        }

        public bool IsDeletable
        {
            get { return true; }
        }
    }
}
