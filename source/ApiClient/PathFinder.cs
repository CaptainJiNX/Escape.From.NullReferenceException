using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiClient
{
	public class PathFinder
	{
		private readonly Map _map;

		public PathFinder(Map map)
		{
			_map = map;
		}

		public IEnumerable<Position> CalculatePath(Position start, Position end, IEnumerable<Position> blocked)
		{
			var startNode = new Node(null, start);

			var aStar = new HashSet<Position>();
			var open = new List<Node> { startNode };

			var result = new LinkedList<Position>();

			while (open.Any())
			{
				var closestNode = open.OrderBy(node => node.DistanceFromStart).First();
				open.Remove(closestNode);

				if (closestNode.Position.Equals(end))
				{
					while (true)
					{
						result.AddFirst(closestNode.Position);

						if (closestNode.Parent == null)
						{
							return result;
						}

						closestNode = closestNode.Parent;
					}
				}

				foreach (var neighbor in GetNeighbours(closestNode.Position, blocked))
				{
					if (aStar.Contains(neighbor)) continue;

					var distanceToGoal = closestNode.DistanceToGoal + GetDistance(neighbor, closestNode.Position);
					var distanceFromStart = distanceToGoal + GetDistance(neighbor, end);

					open.Add(new Node(closestNode, neighbor)
					{
						DistanceToGoal = distanceToGoal,
						DistanceFromStart = distanceFromStart
					});

					aStar.Add(neighbor);
				}
			}

			return result;
		}

		private IEnumerable<Position> GetNeighbours(Position position, IEnumerable<Position> blocked)
		{
			var n = position.Y - 1;
			var s = position.Y + 1;
			var e = position.X + 1;
			var w = position.X - 1;

			var neighbours = new[]
			{
				new Position(position.X, n),
				new Position(e, position.Y),
				new Position(position.X, s),
				new Position(w, position.Y),
				new Position(e, n),
				new Position(e, s),
				new Position(w, n),
				new Position(w, s)
			};

			return neighbours.Where(CanWalkHere).Where(pos => !blocked.Any(b => b.Equals(pos)));
		}

		private bool CanWalkHere(Position pos)
		{
			var value = (TileFlags)_map.GetPositionValue(pos);
			if (value == TileFlags.NOTHING) return false;
			if ((value & (TileFlags.PERIMETER | TileFlags.BLOCKED)) > 0) return false;
			return true;
		}

		private static int GetDistance(Position from, Position to)
		{
			return Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));
		}

		private class Node
		{
			public Node Parent { get; private set; }
			public Position Position { get; private set; }
			public int DistanceFromStart { get; set; }
			public int DistanceToGoal { get; set; }

			public Node(Node parent, Position position)
			{
				Parent = parent;
				Position = position;
				DistanceFromStart = 0;
				DistanceToGoal = 0;
			}
		}
	}
}
