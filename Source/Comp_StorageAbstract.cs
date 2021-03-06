﻿using System;
using System.Collections.Generic;
using Verse;

namespace RT_Storage
{
	public class CompProperties_StorageAbstract : CompProperties_CoordinatedAbstract
	{

	}

	public abstract class Comp_StorageAbstract : Comp_CoordinatedAbstract
	{
		new public CompProperties_StorageAbstract properties
		{
			get
			{
				return (CompProperties_StorageAbstract)props;
			}
		}
		protected List<Comp_StorageOutput> linkedOutputs = new List<Comp_StorageOutput>();
		protected List<Thing> linkedOutputParents = new List<Thing>();
		protected List<Comp_StorageInput> linkedInputs = new List<Comp_StorageInput>();
		protected List<Thing> linkedInputParents = new List<Thing>();
		protected List<StorageReservation> reservations = new List<StorageReservation>();

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			foreach (var output in linkedOutputs)
			{
				output.Notify_StorageRemoved();
			}
			linkedOutputs.Clear();
			linkedOutputParents.Clear();
			foreach (var input in linkedInputs)
			{
				input.Notify_StorageRemoved();
			}
			linkedInputs.Clear();
			linkedInputParents.Clear();
			reservations.Clear();
		}

		virtual public void Notify_IOAdded(Comp_StorageIOAbstract io)
		{
			if (io.GetType().IsAssignableFrom(typeof(Comp_StorageInput)))
			{
				linkedInputs.Add((Comp_StorageInput)io);
				linkedInputParents.Add(io.parent);
				previousRootCellIn = IntVec3.Invalid;
				cachedInputParent = null;
			}
			else if (io.GetType().IsAssignableFrom(typeof(Comp_StorageOutput)))
			{
				linkedOutputs.Add((Comp_StorageOutput)io);
				linkedOutputParents.Add(io.parent);
				previousRootCellOut = IntVec3.Invalid;
				cachedOutputParent = null;
			}
		}

		virtual public void Notify_IORemoved(Comp_StorageIOAbstract io)
		{
			if (io.GetType().IsAssignableFrom(typeof(Comp_StorageInput)))
			{
				linkedInputs.Remove((Comp_StorageInput)io);
				linkedInputParents.Remove(io.parent);
				previousRootCellIn = IntVec3.Invalid;
				cachedInputParent = null;
			}
			else if (io.GetType().IsAssignableFrom(typeof(Comp_StorageOutput)))
			{
				linkedOutputs.Remove((Comp_StorageOutput)io);
				linkedOutputParents.Remove(io.parent);
				previousRootCellOut = IntVec3.Invalid;
				cachedOutputParent = null;
			}
		}

		virtual public void Notify_ReservationRemoved(StorageReservation reservation)
		{
			Utility.Debug($"Removing reservation {reservation}");
			reservations.Remove(reservation);
		}

		virtual public bool Reserve(Pawn pawn, Thing thing)
		{
			if (!active)
			{
				return false;
			}
			int count = Math.Min(pawn.carryTracker.AvailableStackSpace(thing.def),
				Math.Min(pawn.CurJob.count, thing.stackCount));
			var newReservation = new StorageReservation(this, pawn, thing, count);
			reservations.Add(newReservation);
			parent.Map.GetStorageCoordinator().AddReservation(newReservation);
			Utility.Debug($"{pawn} has reserved for {(count)} of {thing.def} ({thing})");
			return true;
		}

		virtual public IEnumerable<StorageReservation> GetStorageReservations()
		{
			foreach (var reservation in reservations)
			{
				yield return reservation;
			}
		}

		virtual public IEnumerable<Thing> GetStoredThings()
		{
			return null;
		}

		virtual public int CanAccept(Thing thing)
		{
			return 0;
		}

		virtual public bool Store(Thing thing, out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			resultingThing = null;
			return false;
		}

		protected Thing cachedOutputParent = null;
		protected IntVec3 previousRootCellOut = IntVec3.Invalid;
		virtual public Thing FindClosestOutputParent(IntVec3 rootCell)
		{
			if (rootCell != previousRootCellOut)
			{
				previousRootCellOut = rootCell;
				Thing closestThing = GenClosest.ClosestThingReachable(
					rootCell,
					parent.Map,
					ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
					Verse.AI.PathEndMode.OnCell,
					TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false),
					9999.0f,
					thing => linkedOutputParents.Contains(thing),
					linkedOutputParents);
				cachedOutputParent = closestThing;
			}
			return cachedOutputParent;
		}

		protected Thing cachedInputParent = null;
		protected IntVec3 previousRootCellIn = IntVec3.Invalid;
		virtual public Thing FindClosestInputParent(IntVec3 rootCell)
		{
			if (rootCell != previousRootCellIn)
			{
				previousRootCellIn = rootCell;
				Thing closestThing = GenClosest.ClosestThingReachable(
					rootCell,
					parent.Map,
					ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
					Verse.AI.PathEndMode.OnCell,
					TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false),
					9999.0f,
					thing => linkedInputParents.Contains(thing),
					linkedInputParents);
				cachedInputParent = closestThing;
			}
			return cachedInputParent;
		}
	}
}
