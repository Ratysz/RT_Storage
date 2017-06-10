﻿using System;
using Harmony;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace RT_Storage
{
	public class CompProperties_StorageIOAbstract : CompProperties_CoordinatedAbstract
	{
		public Type linkToParentsStorage = null;
	}

	public class Comp_StorageIOAbstract : Comp_CoordinatedAbstract
	{
		new public CompProperties_StorageIOAbstract properties
		{
			get
			{
				return (CompProperties_StorageIOAbstract)props;
			}
		}
		protected Comp_StorageAbstract linkedStorage = null;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (properties.linkToParentsStorage != null)
			{
				linkedStorage = (Comp_StorageAbstract)AccessTools
					.Method(typeof(Extensions), "GetStorageComponent")
					.MakeGenericMethod(properties.linkToParentsStorage)
					.Invoke(null, new object[] { parent.OccupiedRect().BottomLeft, parent.Map });
			}
			else
			{
				linkedStorage = parent.Map.GetStorageCoordinator().DebugGetAnyStorage();
				Utility.Debug($"{this} connected to {linkedStorage} in {GetSlotGroup()}");
			}
			linkedStorage?.Notify_IOAdded(this);
		}

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			linkedStorage?.Notify_IORemoved(this);
		}

		public void Notify_StorageRemoved()
		{
			linkedStorage = null;
		}

		public SlotGroup GetSlotGroup()
		{
			if (linkedStorage == null)
			{
				return null;
			}
			return linkedStorage.parent.GetSlotGroup();
		}

		virtual public IEnumerable<Thing> GetStoredThings()
		{
			if (linkedStorage == null)
			{
				return null;
			}
			return linkedStorage.GetStoredThings();
		}
	}
}
