﻿/**
 * Kopernicus Planetary System Modifier
 * Copyright (C) 2014 Bryce C Schroeder (bryce.schroeder@gmail.com), Nathaniel R. Lewis (linux.robotdude@gmail.com)
 * 
 * http://www.ferazelhosting.net/~bryce/contact.html
 * 
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
 * which is copyright 2011-2014 Squad. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 * 
 * https://kerbalspaceprogram.com
 */

using System;
using UnityEngine;

namespace Kopernicus
{
	namespace Configuration
	{
		namespace ModLoader
		{
			[RequireConfigType(ConfigType.Node)]
			public class VertexSimplexHeightMap : ModLoader, IParserEventSubscriber
			{
				// Actual PQS mod we are loading
				private PQSMod_VertexSimplexHeightMap _mod;

				// The deformity of the simplex terrain
				[ParserTarget("deformity")]
				private NumericParser<double> deformity
				{
					set { _mod.deformity = value.value; }
				}

				// The frequency of the simplex terrain
				[ParserTarget("frequency")]
				private NumericParser<double> frequency
				{
					set { _mod.frequency = value.value; }
				}

				// Height end
				[ParserTarget("heightEnd")]
				private NumericParser<float> heightEnd
				{
					set { _mod.heightEnd = value.value; }
				}

				// Height start
				[ParserTarget("heightStart")]
				private NumericParser<float> heightStart
				{
					set { _mod.heightStart = value.value; }
				}

				// The greyscale map texture used
				[ParserTarget("map")]
				private MapSOParser_GreyScale<MapSO> heightMap
				{
					set { _mod.heightMap = value.value; }
				}

				// Octaves of the simplex terrain
				[ParserTarget("octaves")]
				private NumericParser<double> octaves
				{
					set { _mod.octaves = value.value; }
				}

				// Persistence of the simplex terrain
				[ParserTarget("persistence")]
				private NumericParser<double> persistence
				{
					set { _mod.persistence = value.value; }
				}

				// The seed of the simplex terrain
				[ParserTarget("seed")]
				private NumericParser<int> seed
				{
					set { _mod.seed = value.value; }
				}

				void IParserEventSubscriber.Apply(ConfigNode node)
				{

				}

				void IParserEventSubscriber.PostApply(ConfigNode node)
				{

				}

				public VertexSimplexHeightMap()
				{
					// Create the base mod
					GameObject modObject = new GameObject("VertexSimplexHeightMap");
					modObject.transform.parent = Utility.Deactivator;
					_mod = modObject.AddComponent<PQSMod_VertexSimplexHeightMap>();
					base.mod = _mod;
				}
			}
		}
	}
}

