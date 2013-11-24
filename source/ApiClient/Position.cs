using System;
using System.Collections.Generic;

namespace ApiClient
{
	[Serializable]
	public class Position
	{
		public Position(int x, int y)
		{
			X = x;
			Y = y;
		}

		public readonly int X;
		public readonly int Y;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			var other = (Position) obj;
			return X == other.X && Y == other.Y;
		}

		public override int GetHashCode()
		{
			return unchecked((X*397) ^ Y);
		}

		public override string ToString()
		{
			return String.Format("{0},{1}", X, Y);
		}

		public int Distance(Position to)
		{

			return Math.Max(Math.Abs(X - to.X), Math.Abs(Y - to.Y));
		}

		public IEnumerable<Position> GetNeighbours()
		{
			var north = Y - 1;
			var south = Y + 1;
			var east = X + 1;
			var west = X - 1;

			yield return new Position(X, north);
			yield return new Position(east, Y);
			yield return new Position(X, south);
			yield return new Position(west, Y);
			yield return new Position(east, north);
			yield return new Position(east, south);
			yield return new Position(west, north);
			yield return new Position(west, south);
		}

		public Direction Direction(Position to)
		{
			if (to == null) return ApiClient.Direction.None;

			if (to.X > X)
			{
				if(to.Y > Y) return ApiClient.Direction.DownRight;
				if(to.Y < Y) return ApiClient.Direction.UpRight;
				return ApiClient.Direction.Right;
			}

			if (to.X < X)
			{
				if(to.Y > Y) return ApiClient.Direction.DownLeft;
				if(to.Y < Y) return ApiClient.Direction.UpLeft;
				return ApiClient.Direction.Left;
			}

			if (to.Y > Y) return ApiClient.Direction.Down;
			if (to.Y < Y) return ApiClient.Direction.Up;

			return ApiClient.Direction.None;
		}
	}
}