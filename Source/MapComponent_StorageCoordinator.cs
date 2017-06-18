using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RT_Storage
{
	public class MapComponent_StorageCoordinator : MapComponent
	{
		private Dictionary<IntVec3, List<Comp_CoordinatedAbstract>> map_cell_comps = new Dictionary<IntVec3, List<Comp_CoordinatedAbstract>>();
		private Dictionary<IntVec3, List<Comp_StorageOutput>> map_cell_outputs = new Dictionary<IntVec3, List<Comp_StorageOutput>>();
		private Dictionary<SlotGroup, List<IntVec3>> map_slotGroup_inputCells = new Dictionary<SlotGroup, List<IntVec3>>();
		private List<Comp_StorageInput> unconnectedInputs = new List<Comp_StorageInput>();
		private List<Comp_StorageInput> inputs = new List<Comp_StorageInput>();
		private List<Comp_StorageOutput> outputs = new List<Comp_StorageOutput>();
		private List<Comp_StorageAbstract> storages = new List<Comp_StorageAbstract>();
		private List<StorageReservation> reservations = new List<StorageReservation>();

		public MapComponent_StorageCoordinator(Map map) : base(map)
		{

		}

		#region Notify
		public void Notify_ComponentSpawned(ThingComp component)
		{
			IntVec3 cell = component.parent.Position;
			Type type = component.GetType();
			if (typeof(Comp_StorageInput).IsAssignableFrom(type))
			{
				inputs.Add((Comp_StorageInput)component);
				SlotGroup slotGroup = ((Comp_StorageIOAbstract)component).GetSlotGroup();
				if (slotGroup != null)
				{
					List<IntVec3> cells;
					if (!map_slotGroup_inputCells.TryGetValue(slotGroup, out cells))
					{
						cells = new List<IntVec3>();
						map_slotGroup_inputCells.Add(slotGroup, cells);
					}
					cells.AddRange(component.parent.OccupiedRect().Cells);
				}
				else
				{
					unconnectedInputs.Add((Comp_StorageInput)component);
				}
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageOutput).IsAssignableFrom(type))
			{
				List<Comp_StorageOutput> outputsInCell;
				if (!map_cell_outputs.TryGetValue(cell, out outputsInCell))
				{
					outputsInCell = new List<Comp_StorageOutput>();
					map_cell_outputs.Add(cell, outputsInCell);
				}
				outputsInCell.Add((Comp_StorageOutput)component);
				outputs.Add((Comp_StorageOutput)component);
				previousRootCell = IntVec3.Invalid;
			}
			else if (typeof(Comp_StorageAbstract).IsAssignableFrom(type))
			{
				storages.Add((Comp_StorageAbstract)component);
			}
			List<Comp_CoordinatedAbstract> compsInCell;
			if (!map_cell_comps.TryGetValue(cell, out compsInCell))
			{
				compsInCell = new List<Comp_CoordinatedAbstract>();
				map_cell_comps.Add(cell, compsInCell);
			}
			compsInCell.Add((Comp_CoordinatedAbstract)component);
		}

		public void Notify_ComponentDeSpawned(ThingComp component)
		{
			Type type = component.GetType();
			IntVec3 cell = component.parent.Position;
			if (typeof(Comp_StorageInput).IsAssignableFrom(type))
			{
				SlotGroup slotGroup = ((Comp_StorageInput)component).GetSlotGroup();
				List<IntVec3> cells;
				if (slotGroup != null && map_slotGroup_inputCells.TryGetValue(slotGroup, out cells))
				{
					foreach (var parentCell in component.parent.OccupiedRect())
					{
						cells.Remove(parentCell);
					}
					if (cells.Count == 0)
					{
						map_slotGroup_inputCells.Remove(slotGroup);
					}
				}
				inputs.Remove((Comp_StorageInput)component);
				unconnectedInputs.Remove((Comp_StorageInput)component);
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageOutput).IsAssignableFrom(type))
			{
				List<Comp_StorageOutput> outputsInCell;
				map_cell_outputs.TryGetValue(cell, out outputsInCell);
				outputsInCell.Remove((Comp_StorageOutput)component);
				if (outputsInCell.Count == 0)
				{
					map_cell_outputs.Remove(cell);
				}
				outputs.Remove((Comp_StorageOutput)component);
				previousRootCell = IntVec3.Invalid;
			}
			else if (typeof(Comp_StorageAbstract).IsAssignableFrom(type))
			{
				storages.Remove((Comp_StorageAbstract)component);
				foreach (var reservation in reservations.ToList())
				{
					if (reservation.storage == (Comp_StorageAbstract)component)
					{
						reservations.Remove(reservation);
					}
				}
			}
			List<Comp_CoordinatedAbstract> compsInCell;
			map_cell_comps.TryGetValue(cell, out compsInCell);
			compsInCell.Remove((Comp_CoordinatedAbstract)component);
			if (compsInCell.Count == 0)
			{
				map_cell_comps.Remove(cell);
			}
		}

		public void Notify_SlotGroupAdded(SlotGroup slotGroup)
		{
			List<IntVec3> cells;
			if (!map_slotGroup_inputCells.TryGetValue(slotGroup, out cells))
			{
				cells = new List<IntVec3>();
				map_slotGroup_inputCells.Add(slotGroup, cells);
			}
			foreach (var input in inputs)
			{
				if (input.GetSlotGroup() == slotGroup)
				{
					cells.AddRange(input.parent.OccupiedRect().Cells);
				}
			}

		}

		public void Notify_SlotGroupRemoved(SlotGroup slotGroup)
		{
			map_slotGroup_inputCells.Remove(slotGroup);
		}

		public void Notify_ReservationsCleared(Pawn pawn)
		{
			foreach (var reservation in reservations.ToList())
			{
				if (reservation.pawn == pawn)
				{
					reservation.storage.Notify_ReservationRemoved(reservation);
					reservations.Remove(reservation);
				}
			}
		}

		public void Notify_OpportunityHaul(Pawn pawn, int additionalCount)
		{
			foreach (var reservation in reservations)
			{
				if (reservation.pawn == pawn)
				{
					int buffer = reservation.stackCount;
					reservation.stackCount = Math.Min(reservation.stackCount + additionalCount, pawn.carryTracker.MaxStackSpaceEver(reservation.def));
					Utility.Debug($"Updated reservation count (was {buffer}): {reservation}");
				}
			}
		}
		#endregion

		private IntVec3 cachedOutputCell = IntVec3.Invalid;
		private IntVec3 previousRootCell = IntVec3.Invalid;
		private IntVec3 previousStorageCell = IntVec3.Invalid;
		public IntVec3 FindClosestOutputCell(Thing thing, IntVec3 rootCell)
		{
			if (rootCell != previousRootCell || previousStorageCell != thing.Position)
			{
				previousRootCell = rootCell;
				previousStorageCell = thing.Position;
				var comp = previousStorageCell.GetStorageComponent<Comp_StorageAbstract>(map);
				if (comp != null)
				{
					Thing closestThing = comp.FindClosestOutputParent(previousRootCell);
					if (closestThing != null)
					{
						cachedOutputCell = closestThing.Position;
					}
				}
				else
				{
					cachedOutputCell = IntVec3.Invalid;
				}
			}
			return cachedOutputCell;
		}

		public bool HasStorageInputCells(SlotGroup slotGroup)
		{
			return map_slotGroup_inputCells.ContainsKey(slotGroup);
		}

		public List<IntVec3> GetStorageInputCells(SlotGroup slotGroup)
		{
			List<IntVec3> values;
			if (map_slotGroup_inputCells.TryGetValue(slotGroup, out values))
			{
				return values;
			}
			return null;
		}

		public List<Comp_CoordinatedAbstract> GetCompsInCell(IntVec3 cell)
		{
			List<Comp_CoordinatedAbstract> values;
			if (map_cell_comps.TryGetValue(cell, out values))
			{
				return values;
			}
			return null;
		}

		internal void AddReservation(StorageReservation reservation)
		{
			reservations.Add(reservation);
		}

		internal void RemoveReservation(StorageReservation reservation)
		{
			reservations.Remove(reservation);
		}

		public override void MapComponentTick()
		{
			base.MapComponentTick();
			if (unconnectedInputs.Count > 0)
			{
				var input = unconnectedInputs.First();
				SlotGroup slotGroup = input.GetSlotGroup();
				if (slotGroup != null)
				{
					List<IntVec3> cells;
					if (!map_slotGroup_inputCells.TryGetValue(slotGroup, out cells))
					{
						cells = new List<IntVec3>();
						map_slotGroup_inputCells.Add(slotGroup, cells);
					}
					cells.AddRange(input.parent.OccupiedRect().Cells);
				}
				unconnectedInputs.Remove(input);
			}
			//DebugDump();
		}

		public Comp_StorageAbstract DebugGetAnyStorage()
		{
			return storages.RandomElement();
		}

		private int dumpTimer = 0;
		private void DebugDump()
		{
			if (dumpTimer == 0)
			{
				Utility.Debug("||||||||||||||");
				Utility.Debug("+ dumping inputs");
				foreach (var kvp in inputs)
				{
					Utility.Debug($"\t{kvp.ToString()}");
				}
				Utility.Debug("+ dumping storages");
				foreach (var kvp in storages)
				{
					Utility.Debug($"\t{kvp.ToString()}");
				}
				Utility.Debug("+ dumping outputs");
				foreach (var kvp in outputs)
				{
					Utility.Debug($"\t{kvp.ToString()}");
				}
				Utility.Debug("+ dumping reservations");
				foreach (var kvp in reservations)
				{
					Utility.Debug($"\t{kvp.ToString()}");
				}
				Utility.Debug("+ dumping map_cell_comps");
				foreach (var kvp in map_cell_comps)
				{
					Utility.Debug($"\t{kvp.ToString()}");
					foreach (var iter2 in kvp.Value)
					{
						Utility.Debug($"\t\t{iter2.ToString()}");
					}
				}
				Utility.Debug("+ dumping map_cell_outputs");
				foreach (var kvp in map_cell_outputs)
				{
					Utility.Debug($"\t{kvp.ToString()}");
					foreach (var iter2 in kvp.Value)
					{
						Utility.Debug($"\t\t{iter2.ToString()}");
					}
				}
				Utility.Debug("+ dumping map_slotGroup_inputCells");
				foreach (var kvp in map_slotGroup_inputCells)
				{
					Utility.Debug($"\t{kvp.ToString()}");
					foreach (var iter2 in kvp.Value)
					{
						Utility.Debug($"\t\t{iter2.ToString()}");
					}
				}
				Utility.Debug("||||||||||||||");
				dumpTimer = 300;
			}
			dumpTimer--;
		}
	}
}