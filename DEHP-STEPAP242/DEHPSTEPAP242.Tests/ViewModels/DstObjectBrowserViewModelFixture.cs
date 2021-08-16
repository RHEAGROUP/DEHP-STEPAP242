﻿/** --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstObjectBrowserViewModelFixturecs" company="Open Engineering S.A.">
//    Copyright (c) 2021 Open Engineering S.A.
//
//    Author: Ivan Fontaine
//
//    Part of the code was based on the work performed by RHEA as result
//    of the collaboration in the context of "Digital Engineering Hub Pathfinder"
//    by Sam Gerené, Alex Vorobiev, Alexander van Delft and Nathanael Smiechowski.
//
//    This file is part of DEHP STEP-AP242 (STEP 3D CAD) adapter project.
//
//    The DEHP STEP-AP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
//
//    The DEHP STEP-AP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
*/

namespace DEHPSTEPAP242.Tests.ViewModels
{
    using Autofac;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Dal;
    using CDP4Dal.Operations;
    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Events;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.ViewModel;
    using DEHPSTEPAP242.ViewModel.Dialogs;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.Views.Dialogs;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class DstObjectBrowserViewModelFixture
    {
        private DstObjectBrowserViewModel viewmodel;
        private DstController dstController;
        private Mock<INavigationService> navigationService;
        private Mock<IHubController> hubController;
        private Mock<IExchangeHistoryService> exchangeHistoryService;
        private Mock<IDstHubService> dstHubService;
        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Mock<IMappingConfigurationDialogViewModel> mappingConfigurationDialog;
        private Iteration iteration;
        private Assembler assembler;
        private string examplesDir;
        private string cwd;
        private string MyParts_path;
        private Mock<IMappingEngine> mappingEngine;

        [SetUp]
        public void Setup()
        {
            var containerBuilder = new ContainerBuilder();

            this.mappingConfigurationDialog = new Mock<IMappingConfigurationDialogViewModel>();

            containerBuilder.RegisterInstance(this.mappingConfigurationDialog.Object).As<IMappingConfigurationDialogViewModel>();
            containerBuilder.RegisterType<HighLevelRepresentationBuilder>().As<IHighLevelRepresentationBuilder>();
            AppContainer.Container = containerBuilder.Build();
            cwd = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            examplesDir = cwd + "/../../../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";
            examplesDir = System.IO.Path.GetFullPath(examplesDir);
            MyParts_path = System.IO.Path.Combine(examplesDir, "MyParts.step");
            this.exchangeHistoryService = new Mock<IExchangeHistoryService>();
            this.exchangeHistoryService.Setup(x => x.Write()).Returns(Task.CompletedTask);
            this.hubController = new Mock<IHubController>();
            this.mappingEngine = new Mock<IMappingEngine>();
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(new DomainOfExpertise());
            this.dstHubService = new Mock<IDstHubService>();
            var uri = new Uri("http://t.e");
            this.assembler = new Assembler(uri);
            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));
            this.iteration =
                new Iteration(Guid.NewGuid(), this.assembler.Cache, uri)
                {
                    Container = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, uri)
                    {
                        EngineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, uri)
                        {
                            RequiredRdl = { new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri) },
                            Container = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri)
                            {
                                Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, uri)
                            }
                        }
                    }
                };

            this.assembler.Cache.TryAdd(new CacheKey(this.iteration.Iid, null), new Lazy<Thing>(() => this.iteration));

            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<ExternalIdentifierMap>(), It.IsAny<Action<Iteration, ExternalIdentifierMap>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<ElementDefinition>>(), It.IsAny<Action<Iteration, ElementDefinition>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.Delete(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(x => x.Write(It.IsAny<ThingTransaction>())).Returns(Task.CompletedTask);

            //this.hlrBuilder = new Mock<IHighLevelRepresentationBuilder>();

            // this.dstHubService = new Mock<IDstHubService>();

            //            this.mappingEngine = new Mock<IMappingEngine>();

            this.navigationService = new Mock<INavigationService>();
            this.navigationService = new Mock<INavigationService>();

            //this.dstController = new Mock<IDstController>();
            this.dstController = new DstController(this.hubController.Object,
               this.mappingEngine.Object, this.navigationService.Object, this.exchangeHistoryService.Object,
               this.dstHubService.Object, this.statusBarViewModel.Object);
            dstController.MappingDirection = MappingDirection.FromDstToHub;

            this.navigationService.Setup(x => x.ShowDialog<MappingConfigurationDialog, IMappingConfigurationDialogViewModel>(It.IsAny<MappingConfigurationDialogViewModel>()));
            viewmodel = new DstObjectBrowserViewModel(dstController, navigationService.Object, hubController.Object);
            this.dstController.Load(MyParts_path);
        }

        [Test]
        public void TestUpdateHLR()
        {
            var fil = dstController.Step3DFile;
            Assert.DoesNotThrow(() => viewmodel.UpdateHLR());
            Assert.IsTrue(viewmodel.Step3DHLR.Count == 5);
            viewmodel.SelectedPart = viewmodel.Step3DHLR[0];
        }

        private void SetPart()
        {
            viewmodel.UpdateHLR();
            viewmodel.SelectedPart = viewmodel.Step3DHLR[0];
        }

        [Test]
        public void TestPopulateContextMenu()
        {
            SetPart();
            viewmodel.MapCommand.Execute(null);
            Assert.DoesNotThrow(() => viewmodel.PopulateContextMenu());
        }

        [Test]
        public void TestMapCommand()
        {
            SetPart();
            Assert.DoesNotThrow(() => viewmodel.MapCommand.Execute(null));
        }

        [Test]
        public void TestAssignMapping()
        {
            SetPart();
            CDPMessageBus.Current.SendMessage(new UpdateHighLevelRepresentationTreeEvent(true));
        }
    }
}