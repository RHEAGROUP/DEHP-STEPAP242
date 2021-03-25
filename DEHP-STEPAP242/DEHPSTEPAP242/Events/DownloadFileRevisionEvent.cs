﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPAP242.Events
{
    /// <summary>
    /// An event for <see cref="CDP4Dal.CDPMessageBus"/>
    /// </summary>
    public class DownloadFileRevisionEvent
    {
        /// <summary>
        /// The target thing id
        /// </summary>
        public string TargetId { get; }

        /// <summary>
        /// Initializes a new <see cref="DownloadFileRevisionEvent"/>
        /// </summary>
        /// <param name="targetId">The target thing id</param>
        /// <param name="shouldHighlight">A Value indicating whether the higlighting of the target should be canceled</param>
        public DownloadFileRevisionEvent(string targetId)
        {
            this.TargetId = targetId;
        }
    }
}
