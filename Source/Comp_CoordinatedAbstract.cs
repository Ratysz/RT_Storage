﻿using Verse;

namespace RT_Storage
{
	public class CompProperties_CoordinatedAbstract : CompProperties
	{

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

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			parent.Map.GetStorageCoordinator().RegisterComponent(this);
		}

		public override void PostDeSpawn(Map map)
		{
			map.GetStorageCoordinator().DeregisterComponent(this);
			base.PostDeSpawn(map);
		}
	}
}