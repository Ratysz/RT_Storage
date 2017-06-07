using System;
using System.Collections.Generic;
using Verse;

namespace RT_Storage
{
	public class CompProperties_StorageAbstract : CompProperties_CoordinatedAbstract
	{

	}

	public abstract class Comp_StorageAbstract : Comp_CoordinatedAbstract
	{
		new public CompProperties_StorageAbstract properties
		{
			get
			{
				return (CompProperties_StorageAbstract)props;
			}
		}
		public List<Comp_StorageOutput> linkedOutputs = new List<Comp_StorageOutput>();
		public List<Thing> linkedOutputParents = new List<Thing>();

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			foreach (var output in linkedOutputs)
			{
				output.linkedStorage = null;
			}
			linkedOutputs.Clear();
			linkedOutputParents.Clear();
		}

		virtual public int CanAccept(Thing thing)
		{
			return 0;
		}

		virtual public IntVec3 ObtainCell(Thing thing)
		{
			return IntVec3.Invalid;
		}

		virtual public bool Store(Thing thing, IntVec3 cell, out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			resultingThing = null;
			return false;
		}
	}
}
