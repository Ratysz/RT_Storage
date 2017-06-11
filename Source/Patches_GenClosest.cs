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
