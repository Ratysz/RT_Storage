using Verse;

namespace RT_Storage
{
	public class CompProperties_StorageOutput : CompProperties_StorageIOAbstract
	{
		public CompProperties_StorageOutput()
		{
			compClass = typeof(Comp_StorageOutput);
		}
	}

	public class Comp_StorageOutput : Comp_StorageIOAbstract
	{
		new public CompProperties_StorageOutput properties
		{
			get
			{
				return (CompProperties_StorageOutput)props;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (linkedStorage != null)
			{
				linkedStorage.linkedOutputs.Add(this);
				linkedStorage.linkedOutputParents.Add(parent);
			}
		}

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			if (linkedStorage != null)
			{
				linkedStorage.linkedOutputs.Remove(this);
				linkedStorage.linkedOutputParents.Remove(parent);
			}
		}
	}
}
