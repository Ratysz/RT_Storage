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
		public void Notify_ComponentSpawned(Comp_CoordinatedAbstract component)
		{
			Type type = component.GetType();
			if (typeof(Comp_StorageInput).IsAssignableFrom(type))
			{
				Comp_StorageInput comp = (Comp_StorageInput)component;
				inputs.Add(comp);
				SlotGroup slotGroup = comp.GetSlotGroup();
				if (slotGroup != null)
				{
					List<IntVec3> cells;
					if (!map_slotGroup_inputCells.TryGetValue(slotGroup, out cells))
					{
						cells = new List<IntVec3>();
						map_slotGroup_inputCells.Add(slotGroup, cells);
					}
					cells.AddRange(comp.specificCells);
				}
				else
				{
					unconnectedInputs.Add(comp);
				}
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageOutput).IsAssignableFrom(type))
			{
				outputs.Add((Comp_StorageOutput)component);
				previousRootCell = IntVec3.Invalid;
			}
			else if (typeof(Comp_StorageAbstract).IsAssignableFrom(type))
			{
				storages.Add((Comp_StorageAbstract)component);
			}
			foreach (var cell in component.specificCells)
			{
				List<Comp_CoordinatedAbstract> compsInCell;
				if (!map_cell_comps.TryGetValue(cell, out compsInCell))
				{
					compsInCell = new List<Comp_CoordinatedAbstract>();
					map_cell_comps.Add(cell, compsInCell);
				}
				compsInCell.Add(component);
			}
		}

		public void Notify_ComponentDeSpawned(Comp_CoordinatedAbstract component)
		{
			Type type = component.GetType();
			if (typeof(Comp_StorageInput).IsAssignableFrom(type))
			{
				Comp_StorageInput comp = (Comp_StorageInput)component;
				SlotGroup slotGroup = comp.GetSlotGroup();
				List<IntVec3> cells;
				if (slotGroup != null && map_slotGroup_inputCells.TryGetValue(slotGroup, out cells))
				{
					foreach (var cell in comp.specificCells)
					{
						cells.Remove(cell);
					}
					if (cells.Count == 0)
					{
						map_slotGroup_inputCells.Remove(slotGroup);
					}
				}
				inputs.Remove(comp);
				unconnectedInputs.Remove(comp);
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageOutput).IsAssignableFrom(type))
			{
				outputs.Remove((Comp_StorageOutput)component);
				previousRootCell = IntVec3.Invalid;
			}
			else if (typeof(Comp_StorageAbstract).IsAssignableFrom(type))
			{
				Comp_StorageAbstract comp = (Comp_StorageAbstract)component;
				storages.Remove(comp);
				foreach (var reservation in reservations.ToList())
				{
					if (reservation.storage == comp)
					{
						reservations.Remove(reservation);
					}
				}
			}
			foreach (var cell in component.specificCells)
			{
				List<Comp_CoordinatedAbstract> compsInCell;
				map_cell_comps.TryGetValue(cell, out compsInCell);
				compsInCell.Remove(component);
				if (compsInCell.Count == 0)
				{
					map_cell_comps.Remove(cell);
				}
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
					cells.AddRange(input.specificCells);
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
					cells.AddRange(input.specificCells);
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