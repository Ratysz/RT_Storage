﻿using Harmony;
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

	/*[HarmonyPatch]
	static class Patch_TryFindBestBillIngredients_BaseValidator
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
			return instructions.MethodReplacer(
				AccessTools.Property(typeof(Thing), nameof(Thing.Position)).GetGetMethod(),
				AccessTools.Method(
					typeof(Patch_TryFindBestBillIngredients_BaseValidator),
					nameof(Patch_TryFindBestBillIngredients_BaseValidator.GetPositionModified)));
		}

		static IntVec3 GetPositionModified(Thing instance)
		{
			Utility.Debug("Aw fuck yeah!");
			return instance.Position;
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
				nameof(Patch_TFBBI_RegProc_Comparison.FindClosestOutput));
			var instrList = instructions.ToList();
			for (int i = 0; i < instrList.Count; i++)
			{
				if (instrList[i].opcode == OpCodes.Callvirt
					&& instrList[i].operand == markerMethod)
				{
					Utility.Debug("");
					yield return new Utility.CodeInstruction(OpCodes.Ldarg_0);
					yield return new Utility.CodeInstruction(OpCodes.Ldfld, instrList[i + 2].operand);
					yield return new Utility.CodeInstruction(OpCodes.Call, sneakyMethod);
					Utility.Debug("");
				}
				else
				{
					Utility.PrintInstruction(instrList[i]);
					yield return instrList[i];
				}
			}
		}

		static IntVec3 FindClosestOutput(Thing thing, IntVec3 rootCell)
		{
			Utility.Debug($"Finding closest input : {thing} : {rootCell}");
			var cell = thing.Map.GetStorageCoordinator().FindClosestOutput(thing, rootCell);
			return cell == null ? thing.Position : cell;
		}
	}*/
}