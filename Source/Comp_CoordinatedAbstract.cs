using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RT_Storage
{
	public class CompProperties_CoordinatedAbstract : CompProperties
	{
		public bool requirePower = false;
		public bool isFlickable = false;
		public bool useSpecificCells = false;
		public List<IntVec3> specificCellsOffsets = new List<IntVec3>();
	}

	public abstract class Comp_CoordinatedAbstract : ThingComp
	{
		public CompProperties_CoordinatedAbstract properties
		{
			get
			{
				return (CompProperties_CoordinatedAbstract)props;
			}
		}
		virtual public IEnumerable<IntVec3> specificCells
		{
			get
			{
				if (properties.useSpecificCells)
				{
					foreach (var cellOffset in properties.specificCellsOffsets)
					{
						yield return cellOffset.RotatedBy(parent.Rotation);
					}
				}
				foreach (var parentCell in parent.OccupiedRect())
				{
					yield return parentCell;
				}
			}
		}
		virtual public int specificCellsCount
		{
			get
			{
				if (properties.useSpecificCells)
				{
					return properties.specificCellsOffsets.Count;
				}
				return parent.OccupiedRect().Cells.ToList().Count;
			}
		}
		virtual public bool active
		{
			get
			{
				return (!properties.isFlickable || compFlickable.SwitchIsOn)
					&& (!properties.requirePower || compPowerTrader.PowerOn);
			}
		}
		private CompPowerTrader compPowerTrader;
		private CompFlickable compFlickable;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (properties.requirePower)
			{
				compPowerTrader = parent.TryGetComp<CompPowerTrader>();
				if (compPowerTrader == null)
				{
					throw new NullReferenceException($"{this} could not get parent's CompPowerTrader!");
				}
			}
			if (properties.isFlickable)
			{
				compFlickable = parent.TryGetComp<CompFlickable>();
				if (compFlickable == null)
				{
					throw new NullReferenceException($"{this} could not get parent's CompFlickable!");
				}
			}
			parent.Map.GetStorageCoordinator().Notify_ComponentSpawned(this);
		}

		public override void PostDeSpawn(Map map)
		{
			map.GetStorageCoordinator().Notify_ComponentDeSpawned(this);
			base.PostDeSpawn(map);
		}
	}
}
