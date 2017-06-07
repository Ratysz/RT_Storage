﻿using System;
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

		virtual public int CanAccept(Thing thing)
		{
			if (linkedStorage == null)
			{
				return 0;
			}
			return linkedStorage.CanAccept(thing);
		}

		public IntVec3 ObtainCell(Thing thing)
		{
			if (linkedStorage == null)
			{
				return IntVec3.Invalid;
			}
			return linkedStorage.ObtainCell(thing);
		}

		virtual public bool Store(Thing thing, IntVec3 cell, out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			if (linkedStorage == null)
			{
				resultingThing = null;
				return false;
			}
			return linkedStorage.Store(thing, cell, out resultingThing, placedAction);
		}
	}
}