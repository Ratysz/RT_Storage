using System;
using Verse;

namespace RT_Storage
{
	public class CompProperties_StorageInput : CompProperties_StorageIOAbstract
	{
		public CompProperties_StorageInput()
		{
			compClass = typeof(Comp_StorageInput);
		}
	}

	public class Comp_StorageInput : Comp_StorageIOAbstract
	{
		new public CompProperties_StorageInput properties
		{
			get
			{
				return (CompProperties_StorageInput)props;
			}
		}

		virtual public bool Reserve(Pawn pawn, Thing thing)
		{
			if (!active || linkedStorage == null)
			{
				return false;
			}
			return linkedStorage.Reserve(pawn, thing);
		}

		virtual public int CanAccept(Thing thing)
		{
			if (!active || linkedStorage == null)
			{
				return 0;
			}
			return linkedStorage.CanAccept(thing);
		}

		virtual public bool Store(Thing thing, out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			if (!active || linkedStorage == null)
			{
				resultingThing = null;
				return false;
			}
			return linkedStorage.Store(thing, out resultingThing, placedAction);
		}
	}
}
