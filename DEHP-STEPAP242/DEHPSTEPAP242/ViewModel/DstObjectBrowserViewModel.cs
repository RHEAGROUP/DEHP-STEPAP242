﻿
namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;
    using ReactiveUI;

    using CDP4Common.CommonData;
    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPSTEPAP242.Views.Dialogs;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;

    using STEP3DAdapter;
    using System.Reactive.Linq;
    using System.Windows;

    /// <summary>
    /// The <see cref="DstObjectBrowserViewModel"/> is the view model 
    /// of the <see cref="DstObjectBrowser"/> and provides the 
    /// High Level Representation (aka HLR) of a STEP-AP242 file.
    /// 
    /// Information is provided as a Self-Referential Data Source.
    /// 
    /// To represent data in a tree structure, the data source should contain the following fields:
    /// - Key Field: This field should contain unique values used to identify nodes.
    /// - Parent Field: This field should contain values that indicate parent nodes.
    /// 
    /// <seealso cref="Step3dRowViewModel"/>
    /// </summary>
    public class DstObjectBrowserViewModel : ReactiveObject, IDstObjectBrowserViewModel, IHaveContextMenuViewModel
    {
        #region Private Interface References

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;
        
        #endregion Private Interface References

        #region IDstObjectBrowserViewModel interface

        /// <summary>
        /// Backing field for <see cref="IsBusy"/>
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Backing field for <see cref="Step3DHLR"/>
        /// </summary>
        private List<Step3dRowViewModel> step3DHLR = new List<Step3dRowViewModel>();

        /// <summary>
        /// Gets or sets the Step3D High Level Representation structure.
        /// </summary>
        public List<Step3dRowViewModel> Step3DHLR
        {
            get => this.step3DHLR;
            private set => this.RaiseAndSetIfChanged(ref this.step3DHLR, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedPart"/>
        /// </summary>
        private Step3dRowViewModel selectedPart;

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="Step3dRowViewModel"/>
        /// </summary>
        public Step3dRowViewModel SelectedPart
        {
            get => this.selectedPart;
            set => this.RaiseAndSetIfChanged(ref this.selectedPart, value);
        }

        /// <summary>
        /// Create the HLR tree from the Parts/Relations.
        /// 
        /// The HLR is a Self-Referential Data Source. In this approach
        /// each item in the list has a pair values describing the tree link.
        /// they are ID and ParentID.
        /// 
        /// The ID and ParentID are assigned in a way they are different 
        /// at each level, enabling that sub-trees can be repeated at
        /// different levels.
        /// 
        /// Any <see cref="STEP3D_Part"/> item without <see cref="STEP3D_PartRelation"/> 
        /// defining a specific instance, it will placed as child of the main root item.
        /// </summary>
        /// <param name="step3d">A <see cref="STEP3DFile"/> instance</param>
        public void UpdateHLR()
        {
            if (dstController.IsLoading)
            {
                IsBusy = true;
                return;
            }

            SelectedPart = null;

            var builder = AppContainer.Container.Resolve<IHLRBuilder>();

            Step3DHLR = builder.CreateHLR(dstController.Step3DFile);

            IsBusy = false;

            if (this.CanMap())
            {
                this.OpenMappingConfigurationManagerExecute();
            }
        }

        #endregion

        #region IHaveContextMenuViewModel interface

        /// <summary>
        /// Gets the Context Menu for this browser
        /// </summary>
        public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new ReactiveList<ContextMenuItemViewModel>();

        /// <summary>
        /// Gets the command that allows to map the selected part
        /// </summary>
        public ReactiveCommand<object> MapCommand { get; set; }

        public ReactiveCommand<object> OpenMappingConfigurationManagerCommand { get; set; }

        /// <summary>
        /// Populate the context menu for this browser
        /// </summary>
        public void PopulateContextMenu()
        {
            ContextMenu.Clear();

            if (this.CanMap() == false)
            {
                return;
            }

#if USE_CANMAP_OBSERVABLE
            // Nothing to do, validation already done in the "canMap" observable of the command
#else
            if (this.SelectedPart != null)
            {
                ContextMenu.Add(new ContextMenuItemViewModel(
                    $"Map {SelectedPart.Description}", "",
                    MapCommand,
                    MenuItemKind.Export,
                    ClassKind.NotThing)
                );
            }
#endif

            ContextMenu.Add(new ContextMenuItemViewModel(
                    "Select Mapping Configuration...", "",
                    OpenMappingConfigurationManagerCommand,
                    MenuItemKind.None,
                    ClassKind.NotThing)
                );
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstObjectBrowserViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        public DstObjectBrowserViewModel(IDstController dstController, INavigationService navigationService, IHubController hubController)
        {
            this.dstController = dstController;
            this.navigationService = navigationService;
            this.hubController = hubController;

            // Show automatically the mapping configuration after:
            // a) STEP file loaded
            this.WhenAnyValue(vm => vm.dstController.IsLoading)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHLR());
            // b) Iteration is open
            this.WhenAnyValue(vm => vm.hubController.OpenIteration)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                    {
                        if (this.CanMap())
                        {
                            this.OpenMappingConfigurationManagerExecute();
                        }
                    }
                );

            InitializeCommands();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if general mapping is possible
        /// </summary>
        /// <returns>True if possible</returns>
        private bool CanMap()
        {
            return (this.Step3DHLR != null && this.Step3DHLR.Count > 0) &&
                this.hubController.OpenIteration != null &&
                this.dstController.MappingDirection is MappingDirection.FromDstToHub &&
                !this.IsBusy;
        }

        /// <summary>
        /// Initializes the <see cref="ICommand"/> of this view model
        /// </summary>
        private void InitializeCommands()
        {
#if USE_CANMAP_OBSERVABLE
            var canMap = this.WhenAny(
                vm => vm.SelectedPart,
                vm => vm.hubController.OpenIteration,
                vm => vm.dstController.MappingDirection,
                vm => vm.isBusy,
                (part, iteration, mappingDirection, busy) =>
                    part.Value != null && iteration.Value != null && 
                    mappingDirection.Value is MappingDirection.FromDstToHub &&
                    !busy.Value
                );

            //TODO: helper debug line, for some reason MapCommand is not activated after a second load
            canMap.Subscribe(x => Debug.WriteLine($"canMap={x}, vm.IsBusy={this.IsBusy}"));

            MapCommand = ReactiveCommand.Create(canMap);
            MapCommand.Subscribe(_ => this.MapCommandExecute());
#else
            MapCommand = ReactiveCommand.Create();
            MapCommand.Subscribe(_ => this.MapCommandExecute());

            OpenMappingConfigurationManagerCommand = ReactiveCommand.Create();
            OpenMappingConfigurationManagerCommand.Subscribe(_ => this.OpenMappingConfigurationManagerExecute());
#endif
        }

        /// <summary>
        /// Executes the <see cref="MapCommand"/>
        /// </summary>
        /// <remarks>
        /// The mapping is performed only on the <see cref="SelectedPart"/> through
        /// the <see cref="IMappingConfigurationDialogViewModel"/>
        /// </remarks>
        private void MapCommandExecute()
        {
            var viewModel = AppContainer.Container.Resolve<IMappingConfigurationDialogViewModel>();

            this.AssignMapping();
            
            viewModel.SelectedThing = this.SelectedPart;

            viewModel.UpdatePropertiesBasedOnMappingConfiguration();

            this.navigationService.ShowDialog<MappingConfigurationDialog, IMappingConfigurationDialogViewModel>(viewModel);
        }

        /// <summary>
        /// Opens the <see cref="MappingConfigurationManagerDialog"/>
        /// </summary>
        private void OpenMappingConfigurationManagerExecute()
        {
            this.navigationService.ShowDialog<MappingConfigurationManagerDialog>();
        }

        /// <summary>
        /// Assings a mapping configuration associated to the selected part
        /// </summary>
        private void AssignMapping()
        {
            if (this.dstController.ExternalIdentifierMap is null)
            {
                return;
            }

            this.SelectedPart?.MappingConfigurations.Clear();
            this.SelectedPart?.MappingConfigurations.AddRange(
                this.dstController.ExternalIdentifierMap.Correspondence.Where(
                    x => x.ExternalId == this.SelectedPart.ElementName ||
                         x.ExternalId == this.SelectedPart.ParameterName));
        }

        #endregion
    }
}
