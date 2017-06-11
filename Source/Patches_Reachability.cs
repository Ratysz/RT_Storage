using Harmony;
using RimWorld;
using Verse;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using System.Linq;
using Verse.AI;
using UnityEngine;

namespace RT_Storage
{
	[HarmonyPatch]
	static class Patch_TFBBI_RegProc
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
	static class Patch_TFBBI_BaseValidator
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
				typeof(Patch_TFBBI_BaseValidator),
				nameof(Patch_TFBBI_BaseValidator.ClosestOutputOrPosition));
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
			return Patch_TFBBI_RegProc_Comparison.ClosestOutputOrPosition(thing, otherThing.InteractionCell);
		}
	}

	[HarmonyPatch]
	static class Patch_TFBBI_RegProc_Comparison
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
				typeof(Patch_TFBBI_RegProc_Comparison),
				nameof(Patch_TFBBI_RegProc_Comparison.ClosestOutputOrPosition));
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

	[HarmonyPatch]
	static class Patch_GotoThing_InitAction
	{
		static MethodBase TargetMethod()
		{
			return typeof(Toils_Goto)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("4F4"))
				.GetMethods(AccessTools.all)
				.FirstOrDefault(method => method.ReturnType == typeof(void));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo markerField = AccessTools.Field(typeof(Pawn), nameof(Pawn.pather));
			MethodInfo markerMethod = AccessTools.Method(typeof(Job), nameof(Job.GetTarget));
			MethodInfo sneakyMethod = AccessTools.Method(
				typeof(Patch_GotoThing_InitAction),
				nameof(Patch_GotoThing_InitAction.ClosestOutputOrPosition));
			FieldInfo toilField = typeof(Toils_Goto)
				 .GetNestedTypes(AccessTools.all)
				 .FirstOrDefault(type => type.FullName.Contains("4F4"))
				 .GetFields(AccessTools.all)
				 .FirstOrDefault(field => field.FieldType == typeof(Toil));
			FieldInfo targetIndexField = typeof(Toils_Goto)
				 .GetNestedTypes(AccessTools.all)
				 .FirstOrDefault(type => type.FullName.Contains("4F4"))
				 .GetFields(AccessTools.all)
				 .FirstOrDefault(field => field.FieldType == typeof(TargetIndex));
			int patchState = 0;
			foreach (var instr in instructions)
			{
				switch (patchState)
				{
					case 0:
						yield return instr;
						if (instr.opcode == OpCodes.Ldfld && instr.operand == markerField)
						{
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							yield return new CodeInstruction(OpCodes.Ldfld, toilField);
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							yield return new CodeInstruction(OpCodes.Ldfld, targetIndexField);
							yield return new CodeInstruction(OpCodes.Call, sneakyMethod);
							patchState++;
						}
						break;
					case 1:
						if (instr.opcode == OpCodes.Callvirt && instr.operand == markerMethod)
						{
							patchState++;
						}
						break;
					case 2:
						yield return instr;
						break;
				}
			}
		}

		static LocalTargetInfo ClosestOutputOrPosition(Toil toil, TargetIndex targetIndex)
		{
			LocalTargetInfo targetInfo = toil.actor.jobs.curJob.GetTarget(targetIndex);
			Thing thing = targetInfo.Thing;
			if (thing != null)
			{
				Comp_StorageAbstract comp = thing.Position.GetStorageComponent<Comp_StorageAbstract>(thing.Map);
				if (comp != null)
				{
					Thing output = comp.FindClosestOutputParent(toil.actor.Position);
					if (output != null)
					{
						return new LocalTargetInfo(output);
					}
				}
			}
			return toil.actor.CurJob.GetTarget(targetIndex);
		}
	}

	[HarmonyPatch]
	static class Patch_RBFSW_regionProcessor
	{
		static Type type = typeof(GenClosest).GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("RegionwiseBFSWorker"));
		static FieldInfo[] captureFields = type.GetFields(AccessTools.all);
		static FieldInfo[] classFields = typeof(GenClosest).GetFields(AccessTools.all);
		static MethodBase TargetMethod()
		{
			return type.GetMethods(AccessTools.all)
				.FirstOrDefault(method => method.Name.Contains("C00"));
		}
		static FieldInfo bestPrioField = captureFields
			 .FirstOrDefault(field => field.Name.Contains("bestPrio"));
		static FieldInfo closestDistSquaredField = captureFields
			 .FirstOrDefault(field => field.Name.Contains("closestDistSquared"));
		static FieldInfo closestThingField = captureFields
			 .FirstOrDefault(field => field.FieldType == typeof(Thing));
		static FieldInfo maxDistSquaredField = captureFields
			 .FirstOrDefault(field => field.Name.Contains("maxDistSquared"));
		static FieldInfo priorityGetterField = captureFields
			 .FirstOrDefault(field => field.FieldType == typeof(Func<Thing, float>));
		static FieldInfo reqField = captureFields
			 .FirstOrDefault(field => field.FieldType == typeof(ThingRequest));
		static FieldInfo rootField = captureFields
			 .FirstOrDefault(field => field.FieldType == typeof(IntVec3));
		static FieldInfo traverseParamsField = captureFields
			 .FirstOrDefault(field => field.FieldType == typeof(TraverseParms));
		static FieldInfo validatorField = captureFields
			 .FirstOrDefault(field => field.FieldType == typeof(Predicate<Thing>));

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool patched = false;
			foreach (var instr in instructions)
			{
				yield return instr;
				if (!patched && instr.opcode == OpCodes.Blt)
				{
					patched = true;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, lookInOutputs);
				}
			}
		}

		static MethodInfo lookInOutputs = AccessTools.Method(typeof(Patch_RBFSW_regionProcessor),
				nameof(Patch_RBFSW_regionProcessor.LookInOutputs));
		static void LookInOutputs(object instance, Region region)
		{
			float bestPrio = instance.Get<float>(bestPrioField);
			float closestDistSquared = instance.Get<float>(closestDistSquaredField);
			Thing closestThing = instance.Get<Thing>(closestThingField);
			float maxDistSquared = instance.Get<float>(maxDistSquaredField);
			Func<Thing, float> priorityGetter = instance.Get<Func<Thing, float>>(priorityGetterField);
			ThingRequest req = instance.Get<ThingRequest>(reqField);
			IntVec3 root = instance.Get<IntVec3>(rootField);
			TraverseParms traverseParams = instance.Get<TraverseParms>(traverseParamsField);
			Predicate<Thing> validator = instance.Get<Predicate<Thing>>(validatorField);

			var buildingsInRegion = region.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
			foreach (var building in buildingsInRegion)
			{
				var comp = building.Position.GetStorageComponent<Comp_StorageOutput>(building.Map);
				if (comp != null && comp.GetStoredThings() != null
					&& ReachabilityWithinRegion.ThingFromRegionListerReachable(building, region, PathEndMode.ClosestTouch, traverseParams.pawn))
				{
					float distSq = (float)(building.Position - root).LengthHorizontalSquared;
					if (distSq < maxDistSquared && distSq <= closestDistSquared)
					{
						foreach (var thing in comp.GetStoredThings())
						{
							if (req.Accepts(thing))
							{
								float priority = (priorityGetter == null) ? 0f : priorityGetter(thing);
								if (priority >= bestPrio)
								{
									if ((priority > bestPrio || distSq < closestDistSquared)
										&& (validator == null || validator(thing)))
									{
										closestThing = thing;
										closestDistSquared = distSq;
										bestPrio = priority;
									}
								}
							}
						}
					}
				}
			}

			instance.Set(bestPrioField, bestPrio);
			instance.Set(closestDistSquaredField, closestDistSquared);
			instance.Set(closestThingField, closestThing);
		}
	}
}
