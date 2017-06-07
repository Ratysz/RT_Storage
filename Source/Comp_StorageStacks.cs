using System;
using Verse;
using RimWorld;

namespace RT_Storage
{
	public class CompProperties_StorageStacks : CompProperties_StorageAbstract
	{
		public int maxStacks = 4;
		public CompProperties_StorageStacks()
		{
			compClass = typeof(Comp_StorageStacks);
		}
	}

	public class Comp_StorageStacks : Comp_StorageAbstract
	{
		new public CompProperties_StorageStacks properties
		{
			get
			{
				return (CompProperties_StorageStacks)props;
			}
		}
		public int maxStacks
		{
			get
			{
				return properties.maxStacks;
			}
		}

		public override int CanAccept(Thing thing)
		{
			if (!parent.GetSlotGroup().Settings.AllowedToAccept(thing))
			{
				return 0;
			}
			foreach (var cell in parent.OccupiedRect())
			{
				var things = cell.GetThingList(parent.Map);
				int storeables = 0;
				bool freeSpace = true;
				foreach (var cellThing in things)
				{
					if (cellThing.CanStackWith(thing)
						&& cellThing.stackCount < cellThing.def.stackLimit)
					{
						return cellThing.def.stackLimit - cellThing.stackCount;
					}
					if (cellThing.def.EverStoreable)
					{
						storeables++;
						if (storeables >= maxStacks)
						{
							freeSpace = false;
							break;
						}
					}
				}
				if (freeSpace)
				{
					return thing.def.stackLimit;
				}
			}
			return 0;
		}

		public override IntVec3 ObtainCell(Thing thing)
		{
			if (!parent.GetSlotGroup().Settings.AllowedToAccept(thing))
			{
				return IntVec3.Invalid;
			}
			foreach (var cell in parent.OccupiedRect())
			{
				var things = cell.GetThingList(parent.Map);
				int storeables = 0;
				bool freeSpace = true;
				foreach (var cellThing in things)
				{
					if (cellThing.CanStackWith(thing)
						&& cellThing.stackCount < cellThing.def.stackLimit)
					{
						return cell;
					}
					else if (cellThing.def.EverStoreable)
					{
						storeables++;
						if (storeables >= maxStacks)
						{
							freeSpace = false;
							break;
						}
					}
				}
				if (freeSpace)
				{
					return cell;
				}
			}
			return IntVec3.Invalid;
		}

		public override bool Store(Thing thing, IntVec3 cell, out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			Thing thingToStore = thing;
			while (thing.stackCount > thing.def.stackLimit)
			{
				Thing output;
				bool result = Store(thing.SplitOff(thing.def.stackLimit), cell, out output);
				if (thing.stackCount == 0)
				{
					resultingThing = output;
					placedAction?.Invoke(thing, thing.stackCount);
					return result;
				}
			}
			if (thing.def.stackLimit > 1)
			{
				var thingList = cell.GetThingList(parent.Map);
				int storeables = 0;
				int stackCount = thing.stackCount;
				Thing partiallyAbsorbedThing = null;

				foreach (var thingOnCell in thingList)
				{
					if (thingOnCell.def.EverStoreable)
					{
						storeables++;
					}
					if (thingOnCell.CanStackWith(thing))
					{
						if (thingOnCell.TryAbsorbStack(thing, true))
						{
							resultingThing = thingOnCell;
							placedAction?.Invoke(thingOnCell, stackCount);
							return true;
						}
						else
						{
							if (partiallyAbsorbedThing == null)
							{
								partiallyAbsorbedThing = thingOnCell;
							}
						}
					}
				}
				if (storeables >= maxStacks)
				{
					resultingThing = null;
					if (placedAction != null && stackCount != thing.stackCount && partiallyAbsorbedThing != null)
					{
						placedAction(partiallyAbsorbedThing, stackCount - thing.stackCount);
					}
					if (thingToStore != thing)
					{
						thingToStore.TryAbsorbStack(thing, false);
					}
					return false;
				}
			}
			resultingThing = GenSpawn.Spawn(thing, cell, parent.Map);
			placedAction?.Invoke(thing, thing.stackCount);
			return true;
		}
	}
}
