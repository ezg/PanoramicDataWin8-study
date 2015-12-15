﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;
using GeoAPI.Geometries;
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

        private VisualizationViewModel _visualizationViewModel1 = null;
        private VisualizationViewModel _visualizationViewModel2 = null;
        private VisualizationViewModel _visualizationViewModel3 = null;
        private VisualizationViewModel _visualizationViewModel4 = null;

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

            //VisualizationViewModels.CollectionChanged += VisualizationViewModels_CollectionChanged;
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
                if (_mainModel.DatasetConfigurations.Any(ds => ds.Name.ToLower().Contains(startDataSet)))
                {
                    LoadData(_mainModel.DatasetConfigurations.First(ds => ds.Name.ToLower().Contains(startDataSet)));
                }
                else
                {
                    LoadData(_mainModel.DatasetConfigurations.First(ds => ds.Name.ToLower().Contains("nba")));
                }
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
            if (datasetConfiguration.Backend.ToLower() == "mssql")
            {
                _mainModel.SchemaModel = null; //new MSSQLSchemaModel(datasetConfiguration);
            }
            else if (datasetConfiguration.Backend.ToLower() == "sim")
            {
                var newSchemaModel = new SimSchemaModel();
                
                _mainModel.ThrottleInMillis = datasetConfiguration.ThrottleInMillis;
                _mainModel.SampleSize = datasetConfiguration.SampleSize;
                newSchemaModel.QueryExecuter = new SimQueryExecuter();
                newSchemaModel.RootOriginModel = new SimOriginModel(datasetConfiguration);
                newSchemaModel.RootOriginModel.LoadInputFields();

                _mainModel.SchemaModel = newSchemaModel;
            }


            // refresh visualization views
            VisualizationContainerView visualizationContainerView = new VisualizationContainerView();
            _visualizationViewModel1 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = _visualizationViewModel1;
            if (_gridV1.Children.Count > 0)
            {
                ((VisualizationContainerView) _gridV1.Children[0]).Dispose();
            }
            _gridV1.Children.Clear();
            _gridV1.Children.Add(visualizationContainerView);

            visualizationContainerView = new VisualizationContainerView();
            _visualizationViewModel2 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = _visualizationViewModel2;
            if (_gridV2.Children.Count > 0)
            {
                ((VisualizationContainerView)_gridV2.Children[0]).Dispose();
            }
            _gridV2.Children.Clear();
            _gridV2.Children.Add(visualizationContainerView);

            visualizationContainerView = new VisualizationContainerView();
            _visualizationViewModel3 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = _visualizationViewModel3;
            if (_gridV3.Children.Count > 0)
            {
                ((VisualizationContainerView)_gridV3.Children[0]).Dispose();
            }
            _gridV3.Children.Clear();
            _gridV3.Children.Add(visualizationContainerView);


            visualizationContainerView = new VisualizationContainerView();
            _visualizationViewModel4 = CreateVisualizationViewModel(null, VisualizationType.plot);
            visualizationContainerView.DataContext = _visualizationViewModel4;
            if (_gridV4.Children.Count > 0)
            {
                ((VisualizationContainerView)_gridV4.Children[0]).Dispose();
            }
            _gridV4.Children.Clear();
            _gridV4.Children.Add(visualizationContainerView);
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

        public LinkViewModel CreateLinkViewModel(LinkModel linkModel)
        {
            /*LinkViewModel linkViewModel = LinkViewModels.FirstOrDefault(lvm => lvm.ToVisualizationViewModel == VisualizationViewModels.Where(vvm => vvm.QueryModel == linkModel.ToQueryModel).First());
            if (linkViewModel == null)
            {
                linkViewModel = new LinkViewModel()
                {
                    ToVisualizationViewModel = VisualizationViewModels.Where(vvm => vvm.QueryModel == linkModel.ToQueryModel).First(),
                };
                _linkViewModels.Add(linkViewModel);
                LinkView linkView = new LinkView();
                linkView.DataContext = linkViewModel;
                _root.AddToBack(linkView);
            }
            if (!linkViewModel.LinkModels.Contains(linkModel))
            {
                linkViewModel.LinkModels.Add(linkModel);
                linkViewModel.FromVisualizationViewModels.Add(VisualizationViewModels.Where(vvm => vvm.QueryModel == linkModel.FromQueryModel).First());
            }

            return linkViewModel;
             */
            return null;
        }

        private bool isLinkAllowed(LinkModel linkModel)
        {
            List<LinkModel> linkModels = linkModel.FromQueryModel.LinkModels.Where(lm => lm.FromQueryModel == linkModel.FromQueryModel).ToList();
            linkModels.Add(linkModel);
            return !recursiveCheckForCiruclarLinking(linkModels, linkModel.FromQueryModel, new HashSet<QueryModel>());
        } 

        private bool recursiveCheckForCiruclarLinking(List<LinkModel> links, QueryModel current, HashSet<QueryModel> chain)
        {
            if (!chain.Contains(current))
            {
                chain.Add(current);
                bool ret = false;
                foreach (var link in links)
                {
                    ret = ret || recursiveCheckForCiruclarLinking(link.ToQueryModel.LinkModels.Where(lm => lm.FromQueryModel == link.ToQueryModel).ToList(), link.ToQueryModel, chain);
                }
                return ret;
            }
            else
            {
                return true;
            }
        }

        public void RemoveLinkViewModel(LinkModel linkModel)
        {
            /*
            foreach (var linkViewModel in LinkViewModels.ToArray()) 
            {
                if (linkViewModel.LinkModels.Contains(linkModel))
                {
                    linkViewModel.LinkModels.Remove(linkModel);
                }
                if (linkViewModel.LinkModels.Count == 0)
                {
                    LinkViewModels.Remove(linkViewModel);
                    _root.Remove(_root.Elements.First(e => e is LinkView && (e as LinkView).DataContext == linkViewModel));
                }
            }*/
        }

        void TaskModelMoved(object sender, TaskModelEventArgs e)
        {
            
        }

        void TaskModelDropped(object sender, TaskModelEventArgs e)
        {
            double width = VisualizationViewModel.WIDTH;
            double height = VisualizationViewModel.HEIGHT;
            Vec size = new Vec(width, height);
            Pt position = (Pt)new Vec(e.Bounds.Center.X, e.Bounds.Center.Y) - size / 2.0;

            IGeometry mainPageBounds = e.Bounds.GetPolygon();
            List<VisualizationContainerView> hits = new List<VisualizationContainerView>();
            foreach (var element in InkableScene.Elements.Where(ele => ele is VisualizationContainerView).Select(ele => ele as VisualizationContainerView))
            {
                var geom = element.GetBounds(InkableScene).GetPolygon();
                if (geom != null && mainPageBounds.Intersects(geom))
                {
                    hits.Add(element);
                }
            }

            bool found = false;
            foreach (var element in hits)
            {
                if ((element.DataContext as VisualizationViewModel).QueryModel.TaskModel != null)
                {
                    (element.DataContext as VisualizationViewModel).QueryModel.TaskModel = (sender as TaskModel);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                VisualizationContainerView visualizationContainerView = new VisualizationContainerView();
                VisualizationViewModel visualizationViewModel = CreateVisualizationViewModel((sender as TaskModel), null);
                visualizationViewModel.Position = position;
                visualizationViewModel.Size = size;
                visualizationContainerView.DataContext = visualizationViewModel;
                InkableScene.Add(visualizationContainerView);
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

        void VisualizationViewModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    (item as VisualizationViewModel).QueryModel.LinkModels.CollectionChanged -= LinkModels_CollectionChanged;
                    foreach (var link in (item as VisualizationViewModel).QueryModel.LinkModels)
                    {
                        RemoveLinkViewModel(link);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    (item as VisualizationViewModel).QueryModel.LinkModels.CollectionChanged += LinkModels_CollectionChanged;
                }
            }
        }

        void LinkModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    RemoveLinkViewModel(item as LinkModel);
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    CreateLinkViewModel(item as LinkModel);
                }
            }
        }
    }
}