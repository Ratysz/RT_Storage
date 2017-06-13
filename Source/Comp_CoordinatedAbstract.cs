using System.Linq;
using System.Collections.Generic;
using Verse;

namespace RT_Storage
{
	public class CompProperties_CoordinatedAbstract : CompProperties
	{
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
		public IEnumerable<IntVec3> specificCells
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
		public int specificCellsCount
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

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			parent.Map.GetStorageCoordinator().Notify_ComponentSpawned(this);
		}

		public override void PostDeSpawn(Map map)
		{
			map.GetStorageCoordinator().Notify_ComponentDeSpawned(this);
			base.PostDeSpawn(map);
		}
	}
}
