﻿using System;
using System.Linq;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Text;

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
		public int maxStacksOnCell
		{
			get
			{
				return properties.maxStacks;
			}
		}
		public int maxStacks
		{
			get
			{
				return specificCellsCount * maxStacksOnCell;
			}
		}

		public override IEnumerable<Thing> GetStoredThings()
		{
			foreach (var cell in specificCells)
			{
				foreach (var thing in cell.GetThingList(parent.Map))
				{
					if (thing.def.EverStoreable)
					{
						yield return thing;
					}
				}
			}
		}

		private class VirtualThing
		{
			public ThingDef def;
			public ThingDef stuff;
			public int stackCount;

			public VirtualThing(Thing thing)
			{
				def = thing.def;
				stuff = thing.Stuff;
				stackCount = thing.stackCount;
			}

			public bool TryAbsorbStack(VirtualThing otherThing)
			{
				if (def == otherThing.def && stuff == otherThing.stuff)
				{
					int amount = Math.Min(def.stackLimit - stackCount, otherThing.stackCount);
					stackCount += amount;
					otherThing.stackCount -= amount;
					if (otherThing.stackCount <= 0)
					{
						return true;
					}
				}
				return false;
			}
		}

		public override int CanAccept(Thing thing)
		{
			if (!parent.GetSlotGroup().Settings.AllowedToAccept(thing))
			{
				return 0;
			}
			var builder = new StringBuilder("CanAccept: ");
			int fullStacks = 0;
			var storedThings = new List<VirtualThing>();
			builder.Append("|| stored things: ");
			foreach (var storedThing in GetStoredThings())
			{
				builder.Append($"({storedThing.stackCount} {storedThing.def}) ");
				if (storedThing.stackCount >= storedThing.def.stackLimit)
				{
					fullStacks++;
					if (fullStacks >= maxStacks)
					{
						builder.Append($"|| STORAGE ALREADY FULL");
						Utility.Debug(builder.ToString());
						return 0;
					}
				}
				else
				{
					storedThings.Add(new VirtualThing(storedThing));
				}
			}
			builder.Append("|| incoming things: ");
			var incomingThings = new List<VirtualThing>();
			foreach (var incomingThing in GetExpectedThings())
			{
				builder.Append($"({incomingThing.stackCount} {incomingThing.def}) ");
				if (incomingThing.stackCount >= incomingThing.def.stackLimit)
				{
					fullStacks++;
					if (fullStacks >= maxStacks)
					{
						builder.Append($"|| STORAGE RESERVED TO FULL");
						Utility.Debug(builder.ToString());
						return 0;
					}
				}
				else
				{
					incomingThings.Add(new VirtualThing(incomingThing));
				}
			}
			if (storedThings.Count + incomingThings.Count < maxStacks - fullStacks)
			{
				builder.Append($"|| CAN STORE {thing.stackCount} of {thing.def}");
				Utility.Debug(builder.ToString());
				return thing.stackCount;
			}
			builder.Append($"|| merging ");
			while (true)
			{
				foreach (var storedThing in storedThings.ToList())
				{
					foreach (var incomingThing in incomingThings.ToList())
					{
						if (storedThing.TryAbsorbStack(incomingThing))
						{
							incomingThings.Remove(incomingThing);
						}
					}
					if (storedThing.stackCount == storedThing.def.stackLimit)
					{
						fullStacks++;
						if (fullStacks >= maxStacks)
						{
							builder.Append($"|| STORAGE RESERVED TO FULL");
							Utility.Debug(builder.ToString());
							return 0;
						}
						storedThings.Remove(storedThing);
					}
				}
				if (incomingThings.Count == 0)
				{
					break;
				}
				if (storedThings.Count == 0)
				{
					storedThings.Add(incomingThings.First());
					incomingThings.Remove(incomingThings.First());
					if (incomingThings.Count == 0)
					{
						break;
					}
				}
			}
			builder.Append($"|| merged ");
			if (storedThings.Count + fullStacks == maxStacks)
			{
				var vThing = new VirtualThing(thing);
				foreach (var storedThing in storedThings)
				{
					if (storedThing.TryAbsorbStack(vThing))
					{
						builder.Append($"|| CAN STORE {thing.stackCount} of {thing.def}");
						Utility.Debug(builder.ToString());
						return thing.stackCount;
					}
				}
				builder.Append($"|| CAN STORE ONLY {thing.stackCount - vThing.stackCount} of {thing.def}");
				Utility.Debug(builder.ToString());
				return thing.stackCount - vThing.stackCount;
			}
			builder.Append($"|| CAN STORE {thing.stackCount} of {thing.def}");
			Utility.Debug(builder.ToString());
			return thing.stackCount;
		}

		public override bool Store(Thing thing, out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			int stacksPassed = 0;
			foreach (var storedThing in GetStoredThings())
			{
				if (storedThing.TryAbsorbStack(thing, true))
				{
					resultingThing = storedThing;
					placedAction?.Invoke(thing, thing.stackCount);
					return true;
				}
				stacksPassed++;
			}
			resultingThing = GenSpawn.Spawn(
				thing,
				specificCells.Skip(stacksPassed % specificCellsCount).First(),
				parent.Map);
			placedAction?.Invoke(thing, thing.stackCount);
			return true;
		}
	}
}
