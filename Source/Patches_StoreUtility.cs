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
	[HarmonyPatch(typeof(StoreUtility))]
	[HarmonyPatch("TryFindBestBetterStoreCellFor")]
	static class Patch_TryFindBestBetterStoreCellFor
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo markerMethod = AccessTools.Property(typeof(SlotGroup), "CellsList").GetGetMethod();
			MethodInfo sneakyMethod = AccessTools.Method(typeof(Patch_TryFindBestBetterStoreCellFor), "PresortCells");
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Callvirt
					&& markerMethod == instruction.operand)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_2);
					yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
					yield return new CodeInstruction(OpCodes.Call, sneakyMethod);
				}
			}
		}

		static List<IntVec3> PresortCells(List<IntVec3> cells, Map map, SlotGroup slotGroup)
		{
			List<IntVec3> extraCells = slotGroup.GetStorageInputCells();
			if (extraCells != null)
			{
				List<IntVec3> copyCells = new List<IntVec3>(cells);
				foreach (var cell in extraCells)
				{
					copyCells.Insert(0, cell);
				}
				return copyCells;
			}
			return cells;
		}
	}

	[HarmonyPatch(typeof(StoreUtility))]
	[HarmonyPatch("IsValidStorageFor")]
	static class Patch_IsValidStorageFor
	{
		static bool Prefix(ref bool __result, IntVec3 c, Map map, Thing storable)
		{
			Comp_StorageInput comp = c.GetStorageComponent<Comp_StorageInput>(map);
			if (comp != null)
			{
				__result = comp.GetSlotGroup().Settings.AllowedToAccept(storable);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(StoreUtility))]
	[HarmonyPatch("IsGoodStoreCell")]
	static class Patch_IsGoodStoreCell
	{
		static bool Prefix(ref bool __result, IntVec3 c, Map map, Thing t, Pawn carrier, Faction faction)
		{
			Comp_StorageInput comp = c.GetStorageComponent<Comp_StorageInput>(map);
			if (comp != null)
			{
				__result = comp.CanAccept(t) > 0
					&& (carrier == null
						|| (carrier.Map.reachability.CanReach(
								(!t.SpawnedOrAnyParentSpawned) ? carrier.PositionHeld : t.PositionHeld,
								c,
								PathEndMode.ClosestTouch,
								TraverseParms.For(carrier, Danger.Deadly, TraverseMode.ByPawn, false))));
				return false;
			}
			return true;
		}
	}
}