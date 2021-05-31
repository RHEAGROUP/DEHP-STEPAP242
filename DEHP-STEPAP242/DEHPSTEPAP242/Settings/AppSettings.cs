﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppSettings.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
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
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPAP242.Settings
{
    using DEHPCommon.UserPreferenceHandler;
    using System.Collections.Generic;

    /// <summary>
    /// Extends the <see cref="UserPreference"/> class and acts as a container
    /// for the locally saved user settings.
    /// </summary>
    public class AppSettings : UserPreference
    {
        /// <summary>
        /// The list of recently loaded file paths
        /// </summary>
        public List<string> RecentFiles { get; set; }

        /// <summary>
        /// The dictionary relating a STEP file name with an <see cref="ExternalIdentifierMap"/> name
        /// </summary>
        public Dictionary<string, string> MappingUsedByFiles { get; set; }

        /// <summary>
        /// Directory name for the local FileStore directory in the <see cref="FileStoreService"/>
        /// </summary>
        public string FileStoreDirectoryName { get; set; }

        /// <summary>
        /// Intructs the <see cref="FileStoreService"/> to automatically clean on service initialization
        /// </summary>
        public bool FileStoreCleanOnInit { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettings"/> class
        /// </summary>
        public AppSettings()
        {
            RecentFiles = new List<string>();
            MappingUsedByFiles = new Dictionary<string, string>();

            FileStoreDirectoryName = "TempHubFiles";
            FileStoreCleanOnInit = false;
        }
    }
}
