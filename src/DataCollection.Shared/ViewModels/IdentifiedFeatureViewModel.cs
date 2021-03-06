﻿/*******************************************************************************
  * Copyright 2018 Esri
  *
  *  Licensed under the Apache License, Version 2.0 (the "License");
  *  you may not use this file except in compliance with the License.
  *  You may obtain a copy of the License at
  *
  *  http://www.apache.org/licenses/LICENSE-2.0
  *
  *   Unless required by applicable law or agreed to in writing, software
  *   distributed under the License is distributed on an "AS IS" BASIS,
  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  *   See the License for the specific language governing permissions and
  *   limitations under the License.
******************************************************************************/

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.ExampleApps.DataCollection.Shared.Commands;
using Esri.ArcGISRuntime.ExampleApps.DataCollection.Shared.Extensions;
using Esri.ArcGISRuntime.ExampleApps.DataCollection.Shared.Messengers;
using Esri.ArcGISRuntime.ExampleApps.DataCollection.Shared.Models;
using Esri.ArcGISRuntime.ExampleApps.DataCollection.Shared.Properties;
using Esri.ArcGISRuntime.ExampleApps.DataCollection.Shared.Utilities;
using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.ExampleApps.DataCollection.Shared.ViewModels
{
    public class IdentifiedFeatureViewModel : BaseViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentifiedFeatureViewModel"/> class.
        /// </summary>
        public IdentifiedFeatureViewModel(Feature feature, FeatureTable featureTable, ConnectivityMode connectivityMode)
        {
            if (feature != null)
            {
                Feature = feature;
                PopupManager = new PopupManager(new Popup(feature, featureTable.PopupDefinition));
                Fields = FieldContainer.GetFields(PopupManager);
                FeatureTable = featureTable;
                ConnectivityMode = connectivityMode;
            }
        }

        private DestinationRelationshipViewModel _selectedDestinationRelationship;

        /// <summary>
        /// Gets or sets the active viewmodel for the destination relationships
        /// </summary>
        public DestinationRelationshipViewModel SelectedDestinationRelationship
        {
            get { return _selectedDestinationRelationship; }
            set
            {
                if (_selectedDestinationRelationship != value)
                {
                    // clear the other relationship only if the value isn't null
                    if (value != null)
                    {
                        if (SelectedOriginRelationship != null)
                        {
                            SelectedOriginRelationship.PropertyChanged -= SelectedOriginRelationship_PropertyChanged;
                            SelectedOriginRelationship = null;
                        }
                    }
                    _selectedDestinationRelationship = value;
                    OnPropertyChanged();
                }
            }
        }

        private OriginRelationshipViewModel _selectedOriginRelationship;

        /// <summary>
        /// Gets or sets the active viewmodel for the origin relationship
        /// </summary>
        public OriginRelationshipViewModel SelectedOriginRelationship
        {
            get { return _selectedOriginRelationship; }
            set
            {
                if (_selectedOriginRelationship != value)
                {
                    _selectedOriginRelationship = value;
                    OnPropertyChanged();

                    // clear the other relationship only if the value isn't null
                    if (value != null)
                    {
                        SelectedDestinationRelationship = null;

                        // Set PropertyChanged event handler
                        // This is only used for the Update tree condition custom workflow 
                        SelectedOriginRelationship.PropertyChanged += SelectedOriginRelationship_PropertyChanged;
                    }
                }
            }
        }

        private EditViewModel _editViewModel;

        /// <summary>
        /// Gets or sets the viewmodel for the current edit session
        /// </summary>
        public EditViewModel EditViewModel
        {
            get => _editViewModel;
            set
            {
                if (_editViewModel != value)
                {
                    _editViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the feature currently selected
        /// </summary>
        public Feature Feature { get; }

        /// <summary>
        /// Gets the feature table for the layer
        /// </summary>
        public FeatureTable FeatureTable { get; }

        /// <summary>
        /// Gets the PopupManager for the selected feature
        /// </summary>
        public PopupManager PopupManager { get; }

        /// <summary>
        /// Gets the underlying Field property for the PopupField in order to retrieve FieldType and Domain
        /// This is a workaround until Domain and FieldType are exposed on the PopupManager
        /// </summary>
        public IEnumerable<FieldContainer> Fields { get; }

        public ConnectivityMode ConnectivityMode { get; }

        /// <summary>
        /// Gets or sets the collection of view models that handle the related features to which the identified feature is Destination
        /// </summary>
        public ObservableCollection<DestinationRelationshipViewModel> DestinationRelationships { get; } = new ObservableCollection<DestinationRelationshipViewModel>();

        /// <summary>
        /// Gets or sets the collection of view models that handle the related features to which the identified feature is Origin
        /// </summary>
        public ObservableCollection<OriginRelationshipViewModel> OriginRelationships { get; } = new ObservableCollection<OriginRelationshipViewModel>();

        private ICommand _setSelectedDestinationRelationshipCommand;

        /// <summary>
        /// Gets the command to set the selected destination relationship
        /// </summary>
        public ICommand SetSelectedDestinationRelationshipCommand
        {
            get
            {
                return _setSelectedDestinationRelationshipCommand ?? (_setSelectedDestinationRelationshipCommand = new DelegateCommand(
                    (x) =>
                    {
                        if (x is DestinationRelationshipViewModel)
                        {
                            SelectedDestinationRelationship = (DestinationRelationshipViewModel)x;
                        }

                    }));
            }
        }

        private ICommand _setSelectedOriginRelationshipCommand;

        /// <summary>
        /// Gets the command to set the selected origin relationship
        /// </summary>
        public ICommand SetSelectedOriginRelationshipCommand
        {
            get
            {
                return _setSelectedOriginRelationshipCommand ?? (_setSelectedOriginRelationshipCommand = new DelegateCommand(
                    (x) =>
                    {
                        if (x is object[] parameterArray)
                        {
                            if (parameterArray[0] is OriginRelationshipViewModel && parameterArray[1] is PopupManager)
                            {
                                SelectedOriginRelationship = (OriginRelationshipViewModel)parameterArray[0];
                                SelectedOriginRelationship.SelectedRecordPopupManager = (PopupManager)parameterArray[1];
                            }
                        }
                    }));
            }
        }

        private ICommand _editFeatureCommand;

        /// <summary>
        /// Gets the command to begin editing the identified feature
        /// </summary>
        public ICommand EditFeatureCommand
        {
            get
            {
                return _editFeatureCommand ?? (_editFeatureCommand = new DelegateCommand(
                    (x) =>
                    {
                        // clear the related records user may have open
                        SelectedDestinationRelationship = null;
                        if (SelectedOriginRelationship != null && SelectedOriginRelationship.EditViewModel != null)
                        {
                            bool exitWithoutSaving = false;

                            // if user had edits, wait for response from the user if they truly want to exit editing the related record
                            UserPromptMessenger.Instance.ResponseValueChanged += handler;

                            UserPromptMessenger.Instance.RaiseMessageValueChanged(
                                Resources.GetString("DiscardEditsConfirmation_Title"),
                                Resources.GetString("DiscardEditsConfirmation_Message"),
                                false,
                                null,
                                Resources.GetString("DiscardButton_Content"));

                            void handler(object o, UserPromptResponseChangedEventArgs e)
                            {
                                {
                                    UserPromptMessenger.Instance.ResponseValueChanged -= handler;
                                    if (e.Response)
                                    {
                                        exitWithoutSaving = true;
                                    }
                                }
                            }

                            if (!exitWithoutSaving)
                                return;
                        }

                        SelectedOriginRelationship = null;
                        EditViewModel = new EditViewModel(ConnectivityMode);
                        PopupManager.StartEditing();
                    }));
            }
        }

        private ICommand _saveEditsCommand;

        /// <summary>
        /// Gets the command to save changes the user made to the feature
        /// </summary>
        public ICommand SaveEditsCommand
        {
            get
            {
                return _saveEditsCommand ?? (_saveEditsCommand = new DelegateCommand(
                    async (x) =>
                    {
                        if (EditViewModel != null)
                        {
                            var editResult = await EditViewModel.SaveEdits(PopupManager, FeatureTable, DestinationRelationships);

                            // close edit session once edit is complete
                            if (editResult != null)
                            {
                                EditViewModel = null;
                            }
                        }
                    }));
            }
        }

        private ICommand _clearRelationshipsCommand;

        /// <summary>
        /// Gets command to clear the related records user has selected
        /// </summary>
        public ICommand ClearRelationshipsCommand
        {
            get
            {
                return _clearRelationshipsCommand ?? (_clearRelationshipsCommand = new DelegateCommand(
                    (x) =>
                    {
                        SelectedOriginRelationship = null;
                        SelectedDestinationRelationship = null;
                    }));
            }
        }

        private ICommand _addOriginRelatedFeatureCommand;

        /// <summary>
        /// Gets the command to add an origin related feature 
        /// </summary>
        public ICommand AddOriginRelatedFeatureCommand
        {
            get
            {
                return _addOriginRelatedFeatureCommand ?? (_addOriginRelatedFeatureCommand = new DelegateCommand(
                    async (x) =>
                    {
                        if (x is OriginRelationshipViewModel)
                        {
                            SelectedOriginRelationship = x as OriginRelationshipViewModel;
                            try
                            {
                                // create a new record and load it
                                var feature = SelectedOriginRelationship.RelatedTable.CreateFeature();

                                if (feature != null && feature is ArcGISFeature)
                                {
                                    await ((ArcGISFeature)feature).LoadAsync();

                                    // get the corresponding PopupManager
                                    SelectedOriginRelationship.SelectedRecordPopupManager = new PopupManager(new Popup(feature, SelectedOriginRelationship.RelatedTable.PopupDefinition));

                                    // related new record to the feature
                                    ((ArcGISFeature)feature).RelateFeature((ArcGISFeature)Feature, SelectedOriginRelationship.RelationshipInfo);

                                    // open editor and finish creating the feature
                                    SelectedOriginRelationship.EditViewModel = new EditViewModel(ConnectivityMode);
                                    SelectedOriginRelationship.EditViewModel.CreateFeature(null, (ArcGISFeature)feature, SelectedOriginRelationship.SelectedRecordPopupManager);
                                }
                            }
                            catch (Exception ex)
                            {
                                UserPromptMessenger.Instance.RaiseMessageValueChanged(null, ex.Message, true, ex.StackTrace);
                            }
                        }
                    }));
            }
        }

        /// <summary>
        /// Gets relationship information for the identified feature and creates the corresponding viewmodels
        /// </summary>
        internal async Task GetRelationshipInfoForFeature(ArcGISFeature feature)
        {
            // clear related records from previous searches
            DestinationRelationships.Clear();
            OriginRelationships.Clear();

            // get RelationshipInfos from the table
            var relationshipInfos = feature.FeatureTable.GetRelationshipInfos(feature);

            // query only the related tables which match the application rules
            // save destination and origin type relationships separately as origin relates features are editable in the app
            foreach (var relationshipInfo in relationshipInfos)
            {
                var parameters = new RelatedQueryParameters(relationshipInfo);

                // only one related table should return given the specific relationship info passed as parameter
                var relatedTable = feature.FeatureTable.GetRelatedFeatureTable(relationshipInfo);
                var relationships = await feature.FeatureTable.GetRelatedRecords(feature, relationshipInfo);

                if (relationshipInfo.IsValidDestinationRelationship())
                {
                    try
                    {
                        // this is a one to many relationship so it will never return more than one result
                        var relatedFeatureQueryResult = relationships.Where(r => r.IsValidRelationship()).First();

                        var destinationRelationshipViewModel = new DestinationRelationshipViewModel(relationshipInfo, relatedTable, ConnectivityMode);
                        await destinationRelationshipViewModel.InitializeAsync(relatedFeatureQueryResult);

                        DestinationRelationships.Add(destinationRelationshipViewModel);
                    }
                    catch (Exception ex)
                    {
                        UserPromptMessenger.Instance.RaiseMessageValueChanged(null, Resources.GetString("QueryRelatedFeaturesError_Message"), true, ex.StackTrace);
                    }
                }
                else if (relationshipInfo.IsValidOriginRelationship())
                {
                    try
                    {
                        var originRelationshipViewModel = new OriginRelationshipViewModel(relatedTable, ConnectivityMode);

                        foreach (var relatedFeatureQueryResult in relationships.Where(r => r.IsValidRelationship()))
                        {
                            await originRelationshipViewModel.InitializeAsync(relatedFeatureQueryResult, relationshipInfo);
                            OriginRelationships.Add(originRelationshipViewModel);
                        }
                    }
                    catch (Exception ex)
                    {
                        UserPromptMessenger.Instance.RaiseMessageValueChanged(null, Resources.GetString("GetFeatureRelationshipError_Message"), true, ex.StackTrace);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes identified feature
        /// </summary>
        internal async Task<bool> DeleteFeature()
        {
            if (Feature != null)
            {
                try
                {
                    await FeatureTable?.DeleteFeature(Feature);
                    await FeatureTable?.ApplyEdits();

                    return true;
                }
                catch (Exception ex)
                {
                    UserPromptMessenger.Instance.RaiseMessageValueChanged(null, ex.Message, true, ex.StackTrace);
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Discards edits performed on a feature 
        /// </summary>
        internal bool DiscardChanges()
        {
            if (PopupManager.HasEdits())
            {
                bool cancelEdits = false;

                // wait for response from the user if the truly want to cancel the edit operation
                UserPromptMessenger.Instance.ResponseValueChanged += handler;

                UserPromptMessenger.Instance.RaiseMessageValueChanged(
                    Resources.GetString("DiscardEditsConfirmation_Title"),
                    Resources.GetString("DiscardEditsConfirmation_Message"),
                    false,
                    null,
                    Resources.GetString("DiscardButton_Content"));

                void handler(object o, UserPromptResponseChangedEventArgs e)
                {
                    {
                        UserPromptMessenger.Instance.ResponseValueChanged -= handler;
                        if (e.Response)
                        {
                            cancelEdits = true;
                        }
                    }
                }

                if (!cancelEdits)
                {
                    return false;
                }
            }

            // cancel the edits if the PopupManager doesn't have any edits or if the user chooses to
            EditViewModel.CancelEdits(PopupManager);
            EditViewModel = null;
            return true;
        }

        /// <summary>
        /// Event handler for property changed on the VM
        /// This is only used for the Update tree condition custom workflow 
        /// </summary>
        private async void SelectedOriginRelationship_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OriginRelationshipViewModel.EditViewModel))
            {
                if (SelectedOriginRelationship.EditViewModel == null && SelectedOriginRelationship.OriginRelatedRecords != null)
                {
                    // call method to update tree condition and dbh
                    await TreeSurveyWorkflows.UpdateIdentifiedFeature(SelectedOriginRelationship.OriginRelatedRecords, Feature, PopupManager);
                }
            }
        }
    }
}
