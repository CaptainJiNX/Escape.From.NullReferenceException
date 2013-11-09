using System;

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
	}
}