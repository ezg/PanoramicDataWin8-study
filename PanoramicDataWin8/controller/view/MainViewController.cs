using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;
using GeoAPI.Geometries;
using Newtonsoft.Json.Linq;
using PanoramicDataWin8.controller.data;
using PanoramicDataWin8.controller.data.sim;
using PanoramicDataWin8.controller.data.tuppleware;
using PanoramicDataWin8.controller.input;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.sim;
using PanoramicDataWin8.model.data.tuppleware;
using PanoramicDataWin8.model.view;
using PanoramicDataWin8.utils;
using PanoramicDataWin8.view.inq;
using PanoramicDataWin8.view.vis;
using PanoramicDataWin8.view.vis.render;

namespace PanoramicDataWin8.controller.view
{
    public class MainViewController
    {
        private Gesturizer _gesturizer = new Gesturizer();
        private static MainViewController _instance;
        private Grid _gridV1 = null;
        private Grid _gridV2 = null;
        private Grid _gridV3 = null;
        private Grid _gridV4 = null;

        public VisualizationViewModel VisualizationViewModel1 { get; private set; } = null;
        public VisualizationViewModel VisualizationViewModel2 { get; private set; } = null;
        public VisualizationViewModel VisualizationViewModel3 { get; private set; } = null;
        public VisualizationViewModel VisualizationViewModel4 { get; private set; } = null;

        private MainViewController(InkableScene root, Grid v1, Grid v2, Grid v3, Grid v4, MainPage mainPage)
        {
            _root = root;
            _mainPage = mainPage;
            _gridV1 = v1;
            _gridV2 = v2;
            _gridV3 = v3;
            _gridV4 = v4;

            _mainModel = new MainModel();
            
            InputFieldViewModel.InputFieldViewModelDropped += InputFieldViewModelDropped;
            InputFieldViewModel.InputFieldViewModelMoved += InputFieldViewModelMoved;

            InputGroupViewModel.InputGroupViewModelDropped += InputGroupViewModelDropped;
            InputGroupViewModel.InputGroupViewModelMoved += InputGroupViewModelMoved;
            
            VisualizationTypeViewModel.VisualizationTypeViewModelDropped += VisualizationTypeViewModel_VisualizationTypeViewModelDropped;
            VisualizationTypeViewModel.VisualizationTypeViewModelMoved += VisualizationTypeViewModel_VisualizationTypeViewModelMoved;
        }

        public async void LoadConfigs()
        {
            var installedLoc = Package.Current.InstalledLocation;
            var configLoc = await installedLoc.GetFolderAsync(@"Assets\data\config");
            string mainConifgContent = await installedLoc.GetFileAsync(@"Assets\data\main.ini").AsTask().ContinueWith(t => Windows.Storage.FileIO.ReadTextAsync(t.Result)).Result;
            var backend = mainConifgContent.Split(new string[] {"\n"}, StringSplitOptions.RemoveEmptyEntries)
                .First(l => l.ToLower().StartsWith("backend"))
                .Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim().ToLower();
            var startDataSet = mainConifgContent.Split(new string[] {"\n"}, StringSplitOptions.RemoveEmptyEntries)
                .First(l => l.ToLower().StartsWith("startdataset"))
                .Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim().ToLower();

            _mainModel.DatasetConfigurations.Clear();
            if (backend.ToLower() == "sim")
            {
                var configs = await configLoc.GetFilesAsync();
                foreach (var file in configs)
                {
                    var content = await Windows.Storage.FileIO.ReadTextAsync(file);
                    _mainModel.DatasetConfigurations.Add(DatasetConfiguration.FromContent(content, file.Name));
                }

                //MainViewController.Instance.LoadData(MainViewController.Instance.MainModel.DatasetConfigurations.First(ds => ds.Name == "titanic"));
            }
        }

        public static void CreateInstance(InkableScene root, Grid v1, Grid v2, Grid v3, Grid v4,  MainPage mainPage)
        {
            _instance = new MainViewController(root, v1, v2, v3, v4, mainPage);
            _instance.LoadConfigs();
        }
        
        public static MainViewController Instance
        {
            get
            {
                return _instance;
            }
        }

        private InkableScene _root;
        public InkableScene InkableScene
        {
            get
            {
                return _root;
            }
        }

        private MainModel _mainModel;
        public MainModel MainModel
        {
            get
            {
                return _mainModel;
            }
        }

        private MainPage _mainPage;
        public MainPage MainPage
        {
            get
            {
                return _mainPage;
            }
        }

        public void LoadData(DatasetConfiguration datasetConfiguration)
        {
            if (datasetConfiguration.Backend.ToLower() == "sim")
            {
                if (_mainModel.SchemaModel != null)
                {
                    _mainModel.SchemaModel.QueryExecuter.Dispose();
                }
                var newSchemaModel = new SimSchemaModel();
                
                _mainModel.ThrottleInMillis = datasetConfiguration.ThrottleInMillis;
                _mainModel.SampleSize = datasetConfiguration.SampleSize;
                newSchemaModel.QueryExecuter = new SimQueryExecuter();
                newSchemaModel.RootOriginModel = new SimOriginModel(datasetConfiguration);
                newSchemaModel.RootOriginModel.LoadInputFields();
                newSchemaModel.RootOriginModel.CreateSimDataProvider();

                _mainModel.SchemaModel = newSchemaModel;
            }


            // refresh visualization views
            VisualizationContainerView visualizationContainerView = new VisualizationContainerView();
            VisualizationViewModel1 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = VisualizationViewModel1;
            if (_gridV1.Children.Count > 0)
            {
                ((VisualizationContainerView) _gridV1.Children[0]).Dispose();
            }
            _gridV1.Children.Clear();
            _gridV1.Children.Add(visualizationContainerView);
            VisualizationViewModel1.QueryModel.QueryModelUpdated += QueryModel_QueryModelUpdated;

            visualizationContainerView = new VisualizationContainerView();
            VisualizationViewModel2 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = VisualizationViewModel2;
            if (_gridV2.Children.Count > 0)
            {
                ((VisualizationContainerView)_gridV2.Children[0]).Dispose();
            }
            _gridV2.Children.Clear();
            _gridV2.Children.Add(visualizationContainerView);
            VisualizationViewModel2.QueryModel.QueryModelUpdated += QueryModel_QueryModelUpdated;

            visualizationContainerView = new VisualizationContainerView();
            VisualizationViewModel3 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = VisualizationViewModel3;
            if (_gridV3.Children.Count > 0)
            {
                ((VisualizationContainerView)_gridV3.Children[0]).Dispose();
            }
            _gridV3.Children.Clear();
            _gridV3.Children.Add(visualizationContainerView);
            VisualizationViewModel3.QueryModel.QueryModelUpdated += QueryModel_QueryModelUpdated;

            visualizationContainerView = new VisualizationContainerView();
            VisualizationViewModel4 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = VisualizationViewModel4;
            if (_gridV4.Children.Count > 0)
            {
                ((VisualizationContainerView)_gridV4.Children[0]).Dispose();
            }
            _gridV4.Children.Clear();
            _gridV4.Children.Add(visualizationContainerView);
            VisualizationViewModel4.QueryModel.QueryModelUpdated += QueryModel_QueryModelUpdated;
        }

        private void QueryModel_QueryModelUpdated(object sender, QueryModelUpdatedEventArgs e)
        {
            if (e.QueryModelUpdatedEventType == QueryModelUpdatedEventType.FilterModels)
            {
                var visModels = new List<VisualizationViewModel>(new VisualizationViewModel[] {VisualizationViewModel1, VisualizationViewModel2, VisualizationViewModel3, VisualizationViewModel4});
                QueryModel brushQueryModel = null;
                foreach (var visualizationViewModel in visModels)
                {
                    if (visualizationViewModel.QueryModel == sender)
                    {
                        brushQueryModel = visualizationViewModel.QueryModel;
                    }
                }
                setBrushQuery(brushQueryModel);
            }
            else if (e.QueryModelUpdatedEventType == QueryModelUpdatedEventType.Structure &&
                     MainModel.BrushQueryModel == sender)
            {
                setBrushQuery(MainModel.BrushQueryModel);
            }
        }

        private void setBrushQuery(QueryModel queryModel)
        {
            var query = string.Join(" || ", queryModel.FilterModels.Select(fm => fm.ToPythonString())) + "";
            MainPage.SetBrushQuery(query);
            if (query == "")
            {
                MainModel.BrushQueryModel = null;
            }
            else
            {
                MainModel.BrushQueryModel = queryModel;
            }

            Logger.Instance?.Log("BrushQueryVisualization", MainModel.BrushQueryModel, new JProperty("valid", true), new JProperty("brushQuery", query));

            MainModel.BrushQuery = query;
            var visModels = new List<VisualizationViewModel>(new VisualizationViewModel[] { VisualizationViewModel1, VisualizationViewModel2, VisualizationViewModel3, VisualizationViewModel4 });
            foreach (var visualizationViewModel in visModels)
            {
                if (visualizationViewModel.QueryModel != queryModel)
                {
                    visualizationViewModel.QueryModel.ClearFilterModels();
                }
                else
                {
                    visualizationViewModel.FireRequestRender();
                }
            }
        }


        public void SetBrushQuery(string brushQuery)
        {
            var visModels = new List<VisualizationViewModel>(new VisualizationViewModel[] { VisualizationViewModel1, VisualizationViewModel2, VisualizationViewModel3, VisualizationViewModel4 });
            MainModel.BrushQueryModel = null;
            MainModel.BrushQuery = brushQuery;
            foreach (var visualizationViewModel in visModels)
            {
                visualizationViewModel.QueryModel.ClearFilterModels();
            }
        }

        public void SetFilterQuery(QueryModel queryModel)
        {
            var query = string.Join(" || ", queryModel.FilterModels.Select(fm => fm.ToPythonString())) + "";
            MainPage.SetFilterQuery(query);
        }

        public VisualizationViewModel CreateVisualizationViewModel(TaskModel taskModel, InputOperationModel inputOperationModel)
        {
            VisualizationViewModel visModel = VisualizationViewModelFactory.CreateDefault(_mainModel.SchemaModel, taskModel, inputOperationModel != null ? inputOperationModel.InputModel : null);
            addAttachmentViews(visModel);
            //_visualizationViewModels.Add(visModel);
            return visModel;
        }

        public VisualizationViewModel CreateVisualizationViewModel(TaskModel taskModel, VisualizationType visualizationType)
        {
            VisualizationViewModel visModel = VisualizationViewModelFactory.CreateDefault(_mainModel.SchemaModel, taskModel, visualizationType);
            addAttachmentViews(visModel);
            //_visualizationViewModels.Add(visModel);
            return visModel;
        }

        public void CopyVisualisationViewModel(VisualizationViewModel visualizationViewModel, Pt centerPoint)
        {
            VisualizationContainerView visualizationContainerView = new VisualizationContainerView();
            VisualizationViewModel newVisualizationViewModel = CreateVisualizationViewModel(visualizationViewModel.QueryModel.TaskModel, null);
            
            newVisualizationViewModel.Position = centerPoint - (visualizationViewModel.Size / 2.0);
            newVisualizationViewModel.Size = visualizationViewModel.Size;
            foreach (var usage in visualizationViewModel.QueryModel.UsageInputOperationModels.Keys)
            {
                foreach (var inputOperationModel in visualizationViewModel.QueryModel.UsageInputOperationModels[usage])
                {
                    newVisualizationViewModel.QueryModel.AddUsageInputOperationModel(usage, 
                        new InputOperationModel(inputOperationModel.InputModel)
                        {
                            AggregateFunction = inputOperationModel.AggregateFunction
                        });
                }
            }
            newVisualizationViewModel.Size = visualizationViewModel.Size;
            newVisualizationViewModel.QueryModel.VisualizationType = visualizationViewModel.QueryModel.VisualizationType;

            visualizationContainerView.DataContext = newVisualizationViewModel;
            InkableScene.Add(visualizationContainerView);

            newVisualizationViewModel.QueryModel.FireQueryModelUpdated(QueryModelUpdatedEventType.Structure);
        }

        private void addAttachmentViews(VisualizationViewModel visModel)
        {
            return;
            ;
            foreach (var attachmentViewModel in visModel.AttachementViewModels)
            {
                AttachmentView attachmentView = new AttachmentView()
                {
                    DataContext = attachmentViewModel
                };
                InkableScene.Add(attachmentView);
            }
        }


        public void RemoveVisualizationViewModel(VisualizationContainerView visualizationContainerView)
        {
            //_visualizationViewModels.Remove(visualizationContainerView.DataContext as VisualizationViewModel);
            //PhysicsController.Instance.RemovePhysicalObject(visualizationContainerView);
            MainViewController.Instance.InkableScene.Remove(visualizationContainerView);

            visualizationContainerView.Dispose();
            foreach (var attachmentView in MainViewController.Instance.InkableScene.Elements.Where(e => e is AttachmentView).ToList())
            {
                if ((attachmentView.DataContext as AttachmentViewModel).VisualizationViewModel == visualizationContainerView.DataContext as VisualizationViewModel)
                {
                    (attachmentView as AttachmentView).Dispose();
                    MainViewController.Instance.InkableScene.Remove(attachmentView);
                }
            }
        }
        
        void VisualizationTypeViewModel_VisualizationTypeViewModelMoved(object sender, VisualizationTypeViewModelEventArgs e)
        {
        }

        void VisualizationTypeViewModel_VisualizationTypeViewModelDropped(object sender, VisualizationTypeViewModelEventArgs e)
        {
            double width = VisualizationViewModel.WIDTH;
            double height = VisualizationViewModel.HEIGHT;
            Vec size = new Vec(width, height);
            Pt position = (Pt)new Vec(e.Bounds.Center.X, e.Bounds.Center.Y) - size / 2.0;

            VisualizationContainerView visualizationContainerView = new VisualizationContainerView();
            VisualizationViewModel visualizationViewModel = CreateVisualizationViewModel(null, (sender as VisualizationTypeViewModel).VisualizationType);
            visualizationViewModel.Position = position;
            visualizationViewModel.Size = size;
            visualizationContainerView.DataContext = visualizationViewModel;
            InkableScene.Add(visualizationContainerView);
        }

        void InputGroupViewModelMoved(object sender, InputGroupViewModelEventArgs e)
        {
            IGeometry mainPageBounds = e.Bounds.GetPolygon();
            List<InputGroupViewModelEventHandler> hits = new List<InputGroupViewModelEventHandler>();
            foreach (var element in MainPage.GetDescendants().Where(ele => ele is InputGroupViewModelEventHandler).Select(ele => ele as InputGroupViewModelEventHandler))
            {
                var geom = element.BoundsGeometry;
                if (geom != null && mainPageBounds.Intersects(geom))
                {
                    hits.Add(element);
                }
            }
            var orderderHits = hits.OrderBy(fe => (fe.BoundsGeometry.Centroid.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

            foreach (var element in MainPage.GetDescendants().Where(ele => ele is InputGroupViewModelEventHandler).Select(ele => ele as InputGroupViewModelEventHandler))
            {
                element.InputGroupViewModelMoved(
                        sender as InputGroupViewModel, e,
                        hits.Count() > 0 ? orderderHits[0] == element : false);
            }
        }


        void InputGroupViewModelDropped(object sender, InputGroupViewModelEventArgs e)
        {
            IGeometry mainPageBounds = e.Bounds.GetPolygon();
            List<InputGroupViewModelEventHandler> hits = new List<InputGroupViewModelEventHandler>();
            foreach (var element in MainPage.GetDescendants().OfType<InputGroupViewModelEventHandler>())
            {
                var geom = element.BoundsGeometry;
                if (geom != null && mainPageBounds.Intersects(geom))
                {
                    hits.Add(element);
                }
            }

            var orderderHits = hits.OrderBy(fe => (fe.BoundsGeometry.Centroid.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();
            foreach (var element in MainPage.GetDescendants().OfType<InputGroupViewModelEventHandler>())
            {
                element.InputGroupViewModelDropped(
                        sender as InputGroupViewModel, e,
                        hits.Count() > 0 && orderderHits[0] == element);
            }
        }
        
        void InputFieldViewModelMoved(object sender, InputFieldViewModelEventArgs e)
        {
            IGeometry mainPageBounds = e.Bounds.GetPolygon();
            List<InputFieldViewModelEventHandler> hits = new List<InputFieldViewModelEventHandler>();
            var tt = MainPage.GetDescendants().OfType<InputFieldViewModelEventHandler>().ToList();
            foreach (var element in tt)
            {
                var geom = element.BoundsGeometry;
                if (geom != null && mainPageBounds.Intersects(geom)) 
                {
                    hits.Add(element);
                }
            }
            var orderderHits = hits.OrderBy(fe => (fe.BoundsGeometry.Centroid.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();

            foreach (var element in MainPage.GetDescendants().OfType<InputFieldViewModelEventHandler>())
            {
                element.InputFieldViewModelMoved(
                        sender as InputFieldViewModel, e,
                        hits.Count() > 0 && orderderHits[0] == element);
            }
        }

        void InputFieldViewModelDropped(object sender, InputFieldViewModelEventArgs e)
        {
            IGeometry mainPageBounds = e.Bounds.GetPolygon();
            List<InputFieldViewModelEventHandler> hits = new List<InputFieldViewModelEventHandler>();
            foreach (var element in MainPage.GetDescendants().OfType<InputFieldViewModelEventHandler>())
            {
                var geom = element.BoundsGeometry;
                if (geom != null && mainPageBounds.Intersects(geom))
                {
                    hits.Add(element);
                }
            }

            double width = e.UseDefaultSize ? VisualizationViewModel.WIDTH : e.Bounds.Width;
            double height = e.UseDefaultSize ? VisualizationViewModel.HEIGHT : e.Bounds.Height;
            Vec size = new Vec(width, height);
            Pt position = (Pt) new Vec(e.Bounds.Center.X, e.Bounds.Center.Y) - size / 2.0;

            var orderderHits = hits.OrderBy(fe => (fe.BoundsGeometry.Centroid.GetVec() - e.Bounds.Center.GetVec()).LengthSquared).ToList();
            foreach (var element in MainPage.GetDescendants().OfType<InputFieldViewModelEventHandler>())
            {
                element.InputFieldViewModelDropped(
                        sender as InputFieldViewModel, e,
                        hits.Count() > 0 && orderderHits[0] == element);
            }

            /*if (!hits.Any())
            {
                VisualizationContainerView visualizationContainerView = new VisualizationContainerView();
                VisualizationViewModel visualizationViewModel = CreateVisualizationViewModel(null, e.InputOperationModel);
                visualizationViewModel.Position = position;
                visualizationViewModel.Size = size;
                visualizationContainerView.DataContext = visualizationViewModel;
                InkableScene.Add(visualizationContainerView);
            }*/
        }
    }
}
