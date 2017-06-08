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
			var cell = thing.Map.GetStorageCoordinator().FindClosestOutput(thing, rootCell);
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
					Utility.Debug($"yo, take a look a' this: {toil.actor.Position} | {output.Position} | {thing.Position}");
					if (output != null)
					{
						return new LocalTargetInfo(output);
					}
				}
			}
			return toil.actor.CurJob.GetTarget(targetIndex);
		}

		/*static FieldInfo targetIndexField = typeof(Toils_Goto)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("4F4"))
				.GetFields(AccessTools.all)
				.FirstOrDefault(field => field.FieldType == typeof(TargetIndex));

		static FieldInfo toilField = typeof(Toils_Goto)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("4F4"))
				.GetFields(AccessTools.all)
				.FirstOrDefault(field => field.FieldType == typeof(Toil));

		static bool Prefix(object __instance, out object[] __state)
		{
			__state = null;
			Toil toil = (Toil)toilField.GetValue(__instance);
			TargetIndex targetIndex = (TargetIndex)targetIndexField.GetValue(__instance);
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
						Job job = toil.actor.jobs.curJob;
						__state = new object[] { job, targetIndex, targetInfo };
						Utility.Debug($"prefix {__state[0]} | {__state[1]} | {__state[2]}");
						job.SetTarget(targetIndex, new LocalTargetInfo(output));
					}
				}
			}
			return true;
		}

		static void Postfix(object __instance, object[] __state)
		{
			if (__state != null)
			{
				Utility.Debug($"postfix {__state[0]} | {__state[1]} | {__state[2]}");
				((Job)__state[0]).SetTarget((TargetIndex)__state[1], (LocalTargetInfo)__state[2]);
			}
			__state = null;
		}*/
	}

	/*[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch("ThingsMatching")]
	static class Patch_ThingsMatching
	{
		static void Postfix(ref List<Thing> __result)
		{
			var fakeThings = new List<Thing>();
			bool addedFakeThing = false;
			foreach (Thing thing in new List<Thing>(__result))
			{
				if (thing.def.EverHaulable)
				{
					Comp_StorageAbstract comp = thing.Position.GetStorageComponent<Comp_StorageAbstract>(thing.Map);
					if (comp != null)
					{
						foreach (var output in comp.linkedOutputs)
						{
							addedFakeThing = true;
							FakeThing fakeThing = FakeThing.MakeFakeThing(thing, output.parent.Position);
							fakeThings.Add(fakeThing);
							Utility.Debug($"  ++  adding fake thing: {fakeThing} : {thing}");
						}
					}
				}
			}
			if (addedFakeThing)
			{
				foreach (var thing in fakeThings)
				{
					__result.Insert(0, thing);
				}
			}
		}
	}

	public class FakeThing : ThingWithComps
	{
		public static List<FakeThing> fakeThings = new List<FakeThing>();

		public static FakeThing MakeFakeThing(Thing original, IntVec3 position)
		{
			ThingDef def = DefDatabase<ThingDef>.GetNamed(original.def.defName);
			Type backupType = def.thingClass;
			def.thingClass = typeof(FakeThing);
			FakeThing fakeThing = (FakeThing)ThingMaker.MakeThing(def, original.Stuff);
			def.thingClass = backupType;
			fakeThing.referencedThing = original;
			fakeThing.stackCount = original.stackCount;
			fakeThing.Position = position;
			fakeThing.SpawnSetup(original.Map, false);
			fakeThings.Add(fakeThing);
			return fakeThing;
		}

		public Thing referencedThing;
	}

	[HarmonyPatch(typeof(GenClosest))]
	[HarmonyPatch("ClosestThing_Regionwise_ReachablePrioritized")]
	static class Patch_ClosestThing_Regionwise_ReachablePrioritized
	{
		static void Postfix()
		{
			foreach (var thing in FakeThing.fakeThings)
			{
				thing.Destroy();
			}
			FakeThing.fakeThings.Clear();
		}
	}*/

	/*[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch("Position", PropertyMethod.Getter)]
	public static class Patch_Position
	{
		public static IntVec3 cachedCell = IntVec3.Invalid;
		static bool Prefix(ref IntVec3 __result)
		{
			if (cachedCell != IntVec3.Invalid)
			{
				__result = IntVec3.Invalid;
				return false;
			}
			return true;
		}
	}*/
}