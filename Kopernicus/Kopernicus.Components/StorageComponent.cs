﻿/**
 * Kopernicus Planetary System Modifier
 * ====================================
 * Created by: BryceSchroeder and Teknoman117 (aka. Nathaniel R. Lewis)
 * Maintained by: Thomas P., NathanKell and KillAshley
 * Additional Content by: Gravitasi, aftokino, KCreator, Padishar, Kragrathea, OvenProofMars, zengei, MrHappyFace, Sigma88
 * ------------------------------------------------------------- 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 * 
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright 2011-2015 Squad. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 * 
 * https://kerbalspaceprogram.com
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace Kopernicus
{
    /// <summary>
    /// A component that stores data in a planet
    /// </summary>
    public class StorageComponent : MonoBehaviour
    {
        /// <summary>
        /// The data stored by the component
        /// </summary>
        private static Dictionary<String, Dictionary<String, System.Object>> data { get; set; }

        static StorageComponent()
        {
            data = new Dictionary<String, Dictionary<String, Object>>();
        }

        void Awake()
        {
            if (!data.ContainsKey(name))
                data.Add(name, new Dictionary<String, Object>());
        }

        /// <summary>
        /// Gets data from the storage
        /// </summary>
        public T Get<T>(String id)
        {
            if (data.ContainsKey(id))
                return (T) data[name][id];
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Whether the storage contains this key
        /// </summary>
        public Boolean Has(String id)
        {
            return data[name].ContainsKey(id);
        }

        /// <summary>
        /// Writes data into the storage
        /// </summary>
        public void Set<T>(String id, T value)
        {
            if (data.ContainsKey(id))
                data[name][id] = value;
            else
                data[name].Add(id, value);
        }
    }
}