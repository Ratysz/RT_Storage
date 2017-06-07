using System;
using Harmony;
using Verse;
using RimWorld;

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
		public Comp_StorageAbstract linkedStorage = null;

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
		}

		public SlotGroup GetSlotGroup()
		{
			if (linkedStorage == null)
			{
				return null;
			}
			return linkedStorage.parent.GetSlotGroup();
		}
	}
}
