﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
// 
//    This file is part of DEHPSTEPAP242
// 
//    The DEHPSTEPAP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHPSTEPAP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Windows.Input;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// <see cref="MainWindowViewModel"/> is the view model for <see cref="Views.MainWindow"/>
    /// </summary>
    public class MainWindowViewModel : ReactiveObject, IMainWindowViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Gets the view model that represents the 10-25 data source
        /// </summary>
        public IHubDataSourceViewModel HubDataSourceViewModel { get; private set; }

        /// <summary>
        /// Gets the view model that represents the STEP-AP242 data source
        /// </summary>
        public IDstDataSourceViewModel DstSourceViewModel { get; private set; }

        /// <summary>
        /// Gets the view model that represents the net change preview panel
        /// </summary>
        public IDstNetChangePreviewViewModel NetChangePreviewViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the status bar
        /// </summary>
        public IStatusBarControlViewModel StatusBarControlViewModel { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> that will change the mapping direction
        /// </summary>
        public ReactiveCommand<object> ChangeMappingDirection { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="MainWindowViewModel"/>
        /// </summary>
        /// <param name="hubHubDataSourceViewModelViewModel">A <see cref="IHubDataSourceViewModel"/></param>
        /// <param name="dstSourceViewModelViewModel">A <see cref="IHubDataSourceViewModel"/></param>
        /// <param name="statusBarControlViewModel">The <see cref="IStatusBarControlViewModel"/></param>
        public MainWindowViewModel(IHubDataSourceViewModel hubHubDataSourceViewModelViewModel, 
            IDstDataSourceViewModel dstSourceViewModelViewModel, 
            IStatusBarControlViewModel statusBarControlViewModel,
            IDstController dstController,
            IDstNetChangePreviewViewModel netChangePreviewViewModel)
        {
            this.dstController = dstController;
            this.HubDataSourceViewModel = hubHubDataSourceViewModelViewModel;
            this.DstSourceViewModel = dstSourceViewModelViewModel;
            this.NetChangePreviewViewModel = netChangePreviewViewModel;
            //this.TransferControlViewModel = transferControlViewModel;
            this.StatusBarControlViewModel = statusBarControlViewModel;

            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/>
        /// </summary>
        private void InitializeCommands()
        {
            this.ChangeMappingDirection = ReactiveCommand.Create();
            this.ChangeMappingDirection.Subscribe(_ => this.ChangeMappingDirectionExecute());
        }

        /// <summary>
        /// Executes the <see cref="ChangeMappingDirection"/>
        /// </summary>
        private void ChangeMappingDirectionExecute()
        {
            //this.SwitchPanelBehavior?.Switch();
            //this.dstController.MappingDirection = this.SwitchPanelBehavior?.MappingDirection ?? MappingDirection.FromDstToHub;
        }
    }
}
