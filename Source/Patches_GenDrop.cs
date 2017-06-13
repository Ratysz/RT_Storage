using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace RT_Storage
{
	[HarmonyPatch(typeof(GenDrop))]
	[HarmonyPatch("TryDropSpawn")]
	static class Patch_TryDropSpawn
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			/*return instructions.MethodReplacer(
				AccessTools.Method(typeof(GenDrop),
					nameof(GenDrop.TryDropSpawn)),
				AccessTools.Method(typeof(Patch_TryDropSpawn),
					nameof(Patch_TryDropSpawn.TryPlaceThing)));*/
			MethodInfo sneakyMethod = AccessTools.Method(typeof(Patch_TryDropSpawn),
				nameof(Patch_TryDropSpawn.TryPlaceThing));
			MethodInfo markerMethod = AccessTools.Method(typeof(GenDrop),
				nameof(GenDrop.TryDropSpawn));
			bool patched = false;
			/*foreach (var instr in instructions)
			{
				if (!patched && instr.opcode == OpCodes.Call && instr.operand == markerMethod)
				{
					patched = true;
					instr.operand = sneakyMethod;
				}
				yield return instr;
			}*/
			var instrList = instructions.ToList(); // TODO: Figure out why this works but not the others.
			for (int i = 0; i < instrList.Count; i++)
			{
				if (!patched && i == instrList.Count - 2)
				{
					patched = true;
					instrList[i].operand = sneakyMethod;
				}
				yield return instrList[i];
			}
		}

		static bool TryPlaceThing(Thing thing, IntVec3 dropCell, Map map, ThingPlaceMode mode,
			out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			Comp_StorageInput comp = dropCell.GetStorageComponent<Comp_StorageInput>(map);
			if (comp != null)
			{
				return comp.Store(thing, out resultingThing, placedAction);
			}
			return GenPlace.TryPlaceThing(thing, dropCell, map, mode, out resultingThing, placedAction);
		}
	}
}
