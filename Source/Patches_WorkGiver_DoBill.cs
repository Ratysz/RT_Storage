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
	[HarmonyPatch]
	static class Patch_TFBBI_baseValidator
	{
		static MethodBase TargetMethod()
		{
			return typeof(WorkGiver_DoBill)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("TryFindBestBillIngredients"))
				.GetMethods(AccessTools.all)
				.FirstOrDefault(
					method => method.ReturnType == typeof(bool)
					&& method.GetParameters()
						.FirstOrDefault(parameter => parameter.ParameterType == typeof(Thing)) != null);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo markerMethod = AccessTools.Property(typeof(Thing), nameof(Thing.Position)).GetGetMethod();
			MethodInfo sneakyMethod = AccessTools.Method(
				typeof(Patch_TFBBI_baseValidator),
				nameof(Patch_TFBBI_baseValidator.ClosestOutputOrPosition));
			bool patched = false;
			var instrList = instructions.ToList();
			for (int i = 0; i < instrList.Count; i++)
			{
				if (!patched && instrList[i].opcode == OpCodes.Callvirt && instrList[i].operand == markerMethod)
				{
					patched = true;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, instrList[i + 2].operand);
					yield return new CodeInstruction(OpCodes.Call, sneakyMethod);
				}
				else
				{
					yield return instrList[i];
				}
			}
		}

		static IntVec3 ClosestOutputOrPosition(Thing thing, Thing otherThing)
		{
			return Patch_TFBBI_regionProcessor_comparison.ClosestOutputOrPosition(thing, otherThing.InteractionCell);
		}
	}

	[HarmonyPatch]
	static class Patch_TFBBI_regionProcessor
	{
		static Type type = typeof(WorkGiver_DoBill).GetNestedTypes(AccessTools.all)
				 .FirstOrDefault(type => type.FullName.Contains("TryFindBestBillIngredients"));
		static FieldInfo[] captureFields = type.GetFields(AccessTools.all);
		static FieldInfo[] classFields = typeof(WorkGiver_DoBill).GetFields(AccessTools.all);
		static MethodBase TargetMethod()
		{
			return type.GetMethods(AccessTools.all)
				.FirstOrDefault(method => method.Name.Contains("15E"));
		}
		static FieldInfo processedThingsField = classFields
			 .FirstOrDefault(field => field.FieldType == typeof(HashSet<Thing>));
		static FieldInfo newRelevantThingsField = classFields
			 .FirstOrDefault(field => field.Name.Contains("newRelevantThings"));
		static FieldInfo pawnField = captureFields
			.FirstOrDefault(field => field.FieldType == typeof(Pawn));
		static FieldInfo billGiverField = captureFields
			.FirstOrDefault(field => field.FieldType == typeof(Thing));
		static FieldInfo baseValidatorField = captureFields
			.FirstOrDefault(field => field.FieldType == typeof(Predicate<Thing>));

		static bool Prefix(object __instance, Region r)
		{
			HashSet<Thing> processedThings = (HashSet<Thing>)processedThingsField.GetValue(__instance);
			List<Thing> newRelevantThings = (List<Thing>)newRelevantThingsField.GetValue(__instance);
			Pawn pawn = (Pawn)pawnField.GetValue(__instance);
			Thing billGiver = (Thing)billGiverField.GetValue(__instance);
			Predicate<Thing> baseValidator = (Predicate<Thing>)baseValidatorField.GetValue(__instance);

			var buildings = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial));
			foreach (var building in buildings)
			{
				var comp = building.Position.GetStorageComponent<Comp_StorageOutput>(building.Map);
				if (comp != null && comp.GetStoredThings() != null
					&& ReachabilityWithinRegion.ThingFromRegionListerReachable(building, r, PathEndMode.ClosestTouch, pawn))
				{
					foreach (var thing in comp.GetStoredThings())
					{
						if (!processedThings.Contains(thing)
							&& baseValidator(thing)
							&& (!thing.def.IsMedicine || !(billGiver is Pawn)))
						{
							processedThings.Add(thing);
							newRelevantThings.Add(thing);
						}
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch]
	static class Patch_TFBBI_regionProcessor_comparison
	{
		static MethodBase TargetMethod()
		{
			return typeof(WorkGiver_DoBill)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("TryFindBestBillIngredients"))
				.GetMethods(AccessTools.all)
				.FirstOrDefault(method => method.ReturnType == typeof(int));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo markerMethod = AccessTools.Property(typeof(Thing), nameof(Thing.Position)).GetGetMethod();
			MethodInfo sneakyMethod = AccessTools.Method(
				typeof(Patch_TFBBI_regionProcessor_comparison),
				nameof(Patch_TFBBI_regionProcessor_comparison.ClosestOutputOrPosition));
			var instrList = instructions.ToList();
			for (int i = 0; i < instrList.Count; i++)
			{
				if (instrList[i].opcode == OpCodes.Callvirt && instrList[i].operand == markerMethod)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, instrList[i + 2].operand);
					yield return new CodeInstruction(OpCodes.Call, sneakyMethod);
				}
				else
				{
					yield return instrList[i];
				}
			}
		}

		public static IntVec3 ClosestOutputOrPosition(Thing thing, IntVec3 rootCell)
		{
			var cell = thing.Map.GetStorageCoordinator().FindClosestOutputCell(thing, rootCell);
			if (cell != IntVec3.Invalid)
			{
				int distanceToOutput = (cell - rootCell).LengthHorizontalSquared;
				int distanceToThing = (thing.Position - rootCell).LengthHorizontalSquared;
				if (distanceToOutput < distanceToThing)
				{
					return cell;
				}
			}
			return thing.Position;
		}
	}
}
