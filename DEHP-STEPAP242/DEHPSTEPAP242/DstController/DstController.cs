﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.DstController
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    using ReactiveUI;

    using NLog;

    using CDP4Common.EngineeringModelData;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;

    using DEHPSTEPAP242.ViewModel;
    using STEP3DAdapter;
    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.MappingRules;
    using CDP4Dal.Operations;
    using CDP4Dal;
    using DEHPCommon.Events;
    using CDP4Common.CommonData;
    using System.Linq;
    using CDP4Common.Types;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        #region Private Members

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IMappingEngine"/>
        /// </summary>
        private readonly IMappingEngine mappingEngine;

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService;
        
        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        /// <summary>
        /// Gets this running tool name
        /// </summary>
        public string ThisToolName => this.GetType().Assembly.GetName().Name;

        #region IDstController interface

        /// <summary>
        /// Backing field for <see cref="Step3DFile"/>
        /// </summary>
        private STEP3DFile step3dFile;

        /// <summary>
        /// Gets or sets the <see cref="STEP3DFile"/> instance.
        /// 
        /// <seealso cref="Load"/>
        /// <seealso cref="LoadAsync"/>
        /// </summary>
        public STEP3DFile Step3DFile
        { 
            get => step3dFile;
            private set => step3dFile = value;
        }

        /// <summary>
        /// Returns the status of the last load action.
        /// </summary>
        public bool IsFileOpen => step3dFile?.HasFailed == false;

        /// <summary>
        /// The <see cref="IsLoading"/> that indicates the loading status flag.
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// Gets or sets the status flag for the load action.
        /// </summary>
        public bool IsLoading
        {
            get => isLoading;
            private set => this.RaiseAndSetIfChanged(ref isLoading, value);
        }

        /// <summary>
        /// Loads a STEP-AP242 file.
        /// <param name="filename">Full path to a STEP-AP242 file</param>
        public void Load(string filename)
        {
            IsLoading = true;

            var step = new STEP3DFile(filename);

            if (step.HasFailed)
            {
                IsLoading = false;

                // In case of error the current Step3DFile is not updated (keep previous)
                logger.Error($"Error loading STEP file: { step.ErrorMessage }");

                throw new InvalidOperationException($"Error loading STEP file: { step.ErrorMessage }");
            }

            // Update the new instance only when a load success
            Step3DFile = step;

            IsLoading = false;
        }

        /// <summary>
        /// Loads a STEP-AP242 file asynchronously.
        /// </summary>
        /// <param name="filename">Full path to a STEP-AP242 file</param>
        public async Task LoadAsync(string filename)
        {
            await Task.Run( () => Load(filename) );
        }

        /// <summary>
        /// Backing field for the <see cref="MappingDirection"/>
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection"/>
        /// </summary>
        public MappingDirection MappingDirection
        {
            get => this.mappingDirection;
            set => this.RaiseAndSetIfChanged(ref this.mappingDirection, value);
        }

        /// <summary>
        /// Gets the collection of <see cref="ExternalIdentifierMap"/>s
        /// </summary>
        public IEnumerable<ExternalIdentifierMap> AvailablExternalIdentifierMap =>
            this.hubController.AvailableExternalIdentifierMap(this.ThisToolName);

        /// <summary>
        /// Gets the colection of mapped <see cref="ElementDefinition"/>s and <see cref="Parameter"/>s
        /// </summary>
        public IEnumerable<ElementDefinition> DstMapResult { get; private set; } = new List<ElementDefinition>();

        /// <summary>
        /// Gets the colection of mapped <see cref="Step3dTargetSourceParameter"/> which needs to be updated in the transfer operation
        /// </summary>
        public List<Step3dTargetSourceParameter> TargetSourceParametersDstStep3dMaps { get; private set; } = new List<Step3dTargetSourceParameter>();

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ExternalIdentifierMap ExternalIdentifierMap { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="IdCorrespondences"/>
        /// </summary>
        public List<IdCorrespondence> IdCorrespondences { get; } = new List<IdCorrespondence>();

        /// <summary>
        /// Map the provided object using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dst3DPart">The <see cref="Step3dRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        public bool Map(Step3dRowViewModel dst3DPart)
        {
            var parts = new List<Step3dRowViewModel> { dst3DPart };

            //this.ElementDefinitionParametersDstStep3dMaps = (IEnumerable<ElementDefinition>)this.mappingEngine.Map(parts);
            //((List<ElementDefinition>)this.ElementDefinitionParametersDstStep3dMaps).AddRange((IEnumerable<ElementDefinition>)this.mappingEngine.Map(parts));

            var (elements, sources) = ((IEnumerable<ElementDefinition>, IEnumerable<Step3dTargetSourceParameter>))
               this.mappingEngine.Map(parts);

            ((List<ElementDefinition>)this.DstMapResult).AddRange(elements);
            this.TargetSourceParametersDstStep3dMaps.AddRange(sources);

            return true;
        }

        /// <summary>
        /// Creates and sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="ExternalIdentifierMap"/></param>
        /// <returns>A newly created <see cref="ExternalIdentifierMap"/></returns>
        public async Task<ExternalIdentifierMap> CreateExternalIdentifierMap(string newName)
        {
            var externalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Name = newName,
                ExternalToolName = this.ThisToolName,
                ExternalModelName = newName,
                Owner = this.hubController.CurrentDomainOfExpertise,
                Container = this.hubController.OpenIteration
            };

            await this.hubController.CreateOrUpdate<Iteration, ExternalIdentifierMap>(externalIdentifierMap,
                (i, m) => i.ExternalIdentifierMap.Add(m), true);

            return externalIdentifierMap;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="mappingEngine">The <<see cref="IMappingEngine"/></param>
        public DstController(IHubController hubController, IMappingEngine mappingEngine, IDstHubService dstHubService)
        {
            this.hubController = hubController;
            this.mappingEngine = mappingEngine;
            this.dstHubService = dstHubService;
        }

        #endregion

        /// <summary>
        /// Transfers the mapped parts to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Transfer()
        {
            if (this.MappingDirection == MappingDirection.FromDstToHub)
            {
                await this.TransferMappedThingsToHub();
            }
            else
            {
                //TODO: nothing in that direction
            }
        }

        /// <summary>
        /// Transfers the mapped parts to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task TransferMappedThingsToHub()
        {
            // Step 1: upload file
            string filePath = Step3DFile.FileName;
            var file = this.dstHubService.FindFile(filePath);

            await this.hubController.Upload(filePath, file);

            // Step 2: update Step3dParameter.source with FileRevision from uploaded file
            file = this.dstHubService.FindFile(filePath);
            var fileRevision = file.CurrentFileRevision;

            foreach (var sourceFieldToUpdate in this.TargetSourceParametersDstStep3dMaps)
            {
                sourceFieldToUpdate.UpdateSource(fileRevision);
            }

            // Step 3: create/update things
            try
            {
                var iterationClone = this.hubController.OpenIteration.Clone(false);
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone);

                foreach (var elementDefinition in this.DstMapResult)
                {
                    var elementDefinitionCloned = this.TransactionCreateOrUpdate(transaction, elementDefinition, iterationClone.Element);

                    foreach (var parameter in elementDefinition.Parameter)
                    {
                        _ = this.TransactionCreateOrUpdate(transaction, parameter, elementDefinitionCloned.Parameter);
                    }

                    foreach (var parameterOverride in elementDefinition.ContainedElement.SelectMany(x => x.ParameterOverride))
                    {
                        var elementUsageClone = (ElementUsage)parameterOverride.Container.Clone(false);
                        transaction.CreateOrUpdate(elementUsageClone);

                        _ = this.TransactionCreateOrUpdate(transaction, parameterOverride, elementUsageClone.ParameterOverride);
                    }
                }

                await this.hubController.Write(transaction);

                await this.UpdateParametersValueSets(iterationClone);

                await this.hubController.Refresh();
                CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Registers the provided <paramref cref="Thing"/> to be created or updated by the <paramref name="transaction"/>
        /// </summary>
        /// <typeparam name="TThing">The type of the <paramref name="containerClone"/></typeparam>
        /// <param name="transaction">The <see cref="IThingTransaction"/></param>
        /// <param name="thing">The <see cref="Thing"/></param>
        /// <param name="containerClone">The <see cref="ContainerList{T}"/> of the cloned container</param>
        /// <returns>A cloned <typeparamref name="TThing"/></returns>
        private TThing TransactionCreateOrUpdate<TThing>(IThingTransaction transaction, TThing thing, ContainerList<TThing> containerClone) where TThing : Thing
        {
            var clone = thing.Clone(false);

            if (clone.Iid == Guid.Empty)
            {
                clone.Iid = Guid.NewGuid();
                thing.Iid = clone.Iid;
                transaction.Create(clone);
                containerClone.Add((TThing)clone);
            }
            else
            {
                transaction.CreateOrUpdate(clone);
            }

            return (TThing)clone;
        }

        private async Task UpdateParametersValueSets(Thing clonedContainer)
        {
            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(clonedContainer), clonedContainer);
            this.UpdateParametersValueSets(transaction, this.DstMapResult.SelectMany(x => x.Parameter));
            this.UpdateParametersValueSets(transaction, this.DstMapResult.SelectMany(x => x.ContainedElement.SelectMany(p => p.ParameterOverride)));
            await this.hubController.Write(transaction);
        }

        private void UpdateParametersValueSets(IThingTransaction transaction, IEnumerable<Parameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out Parameter newParameter);

                var container = newParameter.Clone(false);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(container);
            }
        }

        private void UpdateParametersValueSets(IThingTransaction transaction, IEnumerable<ParameterOverride> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out ParameterOverride newParameter);

                var container = newParameter.Clone(false);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(container);
            }
        }

        private static void UpdateValueSet(ParameterValueSetBase clone, IValueSet valueSet)
        {
            clone.Reference = valueSet.Reference;
            clone.Computed = valueSet.Computed;
            clone.Manual = valueSet.Manual;
            clone.ActualState = valueSet.ActualState;
            clone.ActualOption = valueSet.ActualOption;
            clone.Formula = valueSet.Formula;
            clone.ValueSwitch = valueSet.ValueSwitch;
        }

    }
}
