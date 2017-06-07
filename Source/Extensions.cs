﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RT_Storage
{
	public static class Extensions
	{
		public static MapComponent_StorageCoordinator GetStorageCoordinator(this Map map)
		{
			MapComponent_StorageCoordinator coordinator = map.GetComponent<MapComponent_StorageCoordinator>();
			if (coordinator == null)
			{
				coordinator = new MapComponent_StorageCoordinator(map);
				map.components.Add(coordinator);
			}
			return coordinator;
		}

		public static CompType GetStorageComponent<CompType>(this IntVec3 cell, Map map)
			where CompType : Comp_CoordinatedAbstract
		{
			var comps = map.GetStorageCoordinator().GetCompsInCell(cell);
			if (comps != null)
			{
				foreach (var comp in comps)
				{
					if (comp as CompType != null)
					{
						return (CompType)comp;
					}
				}
			}
			return null;
		}

		public static List<IntVec3> GetExtraCells(this SlotGroup slotGroup)
		{
			return slotGroup.parent.Map.GetStorageCoordinator().GetExtraCells(slotGroup);
		}

		public static bool HasStorageInputs(this SlotGroup slotGroup)
		{
			return slotGroup.parent.Map.GetStorageCoordinator().HasStorageInputs(slotGroup);
		}
	}
}
