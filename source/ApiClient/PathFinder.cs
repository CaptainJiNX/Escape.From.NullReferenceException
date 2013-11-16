using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiClient
{
	public class PathFinder
	{
		public static IEnumerable<Position> CalculatePath(Position start, Position end, Func<Position, bool> isWalkable)
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

				var walkableNeighbours = closestNode.Position.GetNeighbours().Where(isWalkable);

				foreach (var neighbour in walkableNeighbours)
				{
					if (aStar.Contains(neighbour)) continue;

					var distanceToGoal = closestNode.DistanceToGoal + neighbour.Distance(closestNode.Position);
					var distanceFromStart = distanceToGoal + neighbour.Distance(end);

					open.Add(new Node(closestNode, neighbour)
					{
						DistanceToGoal = distanceToGoal,
						DistanceFromStart = distanceFromStart
					});

					aStar.Add(neighbour);
				}
			}

			return result;
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
