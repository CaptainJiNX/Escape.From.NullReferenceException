using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiClient
{
	public class Room
	{
		public uint RoomId { get; set; }
		public Position Position { get; set; }
	}

	[Serializable]
	public class Map
	{
		private readonly Dictionary<Position, uint> _positions = new Dictionary<Position, uint>();
		private bool _hasChanges;

		public Map(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public IEnumerable<Position> AllPositions
		{
			get { return _positions.Keys.OrderBy(x => x.Y).ThenBy(x => x.X); }
		}

		public Room GetRoom(Position pos)
		{
			return !IsRoom(pos) ? null : new Room {RoomId = GetRoomId(pos), Position = pos};
		}

		public IEnumerable<Room> AllRooms
		{
			get
			{
				var rooms = new HashSet<uint>();

				foreach (var room in AllPositions
					.Where(IsRoom)
					.Select(x => new {RoomId = GetRoomId(x), Position = x}))
				{
					if(rooms.Contains(room.RoomId)) continue;
					rooms.Add(room.RoomId);
					yield return new Room {RoomId = room.RoomId, Position = room.Position};
				}
			}
		}

		public IEnumerable<Position> GetRoomPath(Room fromRoom, Room toRoom)
		{
			if (fromRoom == null || toRoom == null) return Enumerable.Empty<Position>();

			return PathFinder.CalculatePath(fromRoom.Position, toRoom.Position,
				pos => IsWalkable(pos, fromRoom.RoomId, toRoom.RoomId));
		}

		public void Update(ScanResult result)
		{
			foreach (var tuple in result.ConvertAreaToPositions())
			{
				UpdatePosition(tuple.Item1, tuple.Item2);
			}

			UpdatePosition(result.StairsDown, (uint) TileFlags.STAIR_DOWN);
			UpdatePosition(result.StairsUp, (uint) TileFlags.STAIR_UP);
		}

		private void UpdatePosition(Position pos, uint val)
		{
			if (pos == null || (_positions.ContainsKey(pos) && GetPositionValue(pos) == val)) return;
			_hasChanges = true;
			_positions[pos] = val;
		}

		public void SetPositionValue(Position position, uint value)
		{
			UpdatePosition(position, value);
		}

		public uint GetPositionValue(Position position)
		{
			return _positions.ContainsKey(position) ? _positions[position] : uint.MaxValue;
		}

		public bool HasChanges()
		{
			return _hasChanges;
		}

		public void ClearChanges()
		{
			_hasChanges = false;
		}

		private bool IsRoom(Position pos)
		{
			return GetRoomId(pos) > 0;
		}

		public uint GetRoomId(Position pos)
		{
			return (uint) TileFlags.ROOM_ID & GetPositionValue(pos);
		}

		public bool IsWalkable(Position pos)
		{
			var value = (TileFlags)GetPositionValue(pos);
			if (value == TileFlags.UNKNOWN) return true;
			if (value == TileFlags.NOTHING) return false;
			if ((value & (TileFlags.PERIMETER | TileFlags.BLOCKED)) > 0) return false;
			return true;
		}

		public bool IsWalkable(Position pos, uint fromRoom, uint toRoom)
		{
			if (IsRoom(pos))
			{
				var roomId = GetRoomId(pos);
				return IsWalkable(pos) && roomId == fromRoom || roomId == toRoom;
			}

			return IsWalkable(pos);
		}

		public Position GetClosestWalkablePositionWithUnknownNeighbour(Position fromPos, Func<Position, bool> predicate)
		{
			return AllPositions.Where(IsWalkable)
			                   .Where(x => x.GetNeighbours().Any(n => GetPositionValue(n) == uint.MaxValue))
			                   .OrderBy(fromPos.Distance)
			                   .FirstOrDefault(predicate);
		}

		public Position GetRandomWalkablePosition(Func<Position, bool> predicate)
		{
			return AllPositions.Where(IsWalkable)
			                   .OrderBy(x => Guid.NewGuid())
			                   .FirstOrDefault(predicate);
		}

		public Position MaxPos
		{
			get { return new Position(_positions.Keys.Max(pos => pos.X), _positions.Keys.Max(pos => pos.Y)); }
		}
	}
}