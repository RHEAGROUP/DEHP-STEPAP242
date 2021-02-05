﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstDataSourceViewModel.cs" company="RHEA System S.A.">
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
    using ReactiveUI;
    using Autofac;

    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.Views.Dialogs;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon;
    using DEHPSTEPAP242.Services.DstHubService;

    /// <summary>
    /// The <see cref="DstDataSourceViewModel"/> is the view model for the panel that will display controls and data relative to EcosimPro
    /// </summary>
    public sealed class DstDataSourceViewModel : DataSourceViewModel, IDstDataSourceViewModel
    {
        #region Private Members

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService;

        #endregion

        #region IDstDataSourceViewModel interface
        /// <summary>
        /// Gets the <see cref="IDstBrowserHeaderViewModel"/>
        /// </summary>
        public IDstBrowserHeaderViewModel DstBrowserHeader { get; }

        /// <summary>
        /// Gets the <see cref="IDstObjectBrowserViewModel"/>
        /// </summary>
        public IDstObjectBrowserViewModel DstObjectBrowser { get; }

        /// <summary>
        /// Backing field for <see cref="IsFileInHub"/>
        /// </summary>
        private bool isFileInHub;

        /// <summary>
        /// Gets or sets a value indicating whether the TransfertCommand" is executing
        /// </summary>
        public bool IsFileInHub 
        {
            get => this.isFileInHub;
            private set => this.RaiseAndSetIfChanged(ref this.isFileInHub, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstDataSourceViewModel"/>
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="dstBrowserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        public DstDataSourceViewModel(INavigationService navigationService, 
            IDstController dstController, IDstBrowserHeaderViewModel dstBrowserHeader, 
            IDstObjectBrowserViewModel dstObjectBrowser,
            IHubController hubController,
            IDstHubService dstHubService) : base(navigationService)
        {
            this.dstController = dstController;
            this.hubController = hubController;
            this.DstBrowserHeader = dstBrowserHeader;
            this.DstObjectBrowser = dstObjectBrowser;
            this.dstHubService = dstHubService;

            this.WhenAnyValue(
                vm => vm.dstController.Step3DFile,
                vm => vm.hubController.OpenIteration
                ).Subscribe(_ => this.UpdateFileInHubStatus());

            this.InitializeCommands();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Load a new STEP AP242 file.
        /// </summary>
        protected override void LoadFileCommandExecute()
        {
            this.NavigationService.ShowDialog<DstLoadFile>();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if a current STEP AP242 file is stored in the Hub
        /// </summary>
        private void UpdateFileInHubStatus()
        {
            this.IsFileInHub = this.dstHubService.FindFile(this.dstController.Step3DFile?.FileName) != null;
        }

        #endregion
    }
}
