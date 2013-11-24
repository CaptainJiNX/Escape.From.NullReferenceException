using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiClient
{
	public class RoomWalker
	{
		private GameContext _gameContext;
		private readonly Character _player;
		private Task _runningTask;
		private CancellationTokenSource _tokenSource;

		public RoomWalker(GameContext gameContext, Character player)
		{
			_gameContext = gameContext;
			_player = player;
			_tokenSource = new CancellationTokenSource();
		}

		public void Start()
		{
			if (_runningTask != null)
				return;
				
			_runningTask = Task.Factory.StartNew(ExecuteRunning, _tokenSource.Token);
		}

		public void Stop()
		{
			_tokenSource.Cancel();
		}

		void ExecuteRunning()
		{
			try
			{
				while (true)
				{
					_tokenSource.Token.ThrowIfCancellationRequested();

					ExecuteEeeerything();
					Thread.Sleep(TimeSpan.FromMilliseconds(100));
				}

			}
			catch(Exception ex)
			{
				_gameContext.AddMessage(ex.Message);
				throw;
				// kthxbai!
			}
		}

		private Position GetGoal(Map map)
		{
			foreach (var position in _gameContext.GetRoomGoal(_player.Id))
			{
				var path = PathFinder.CalculatePath(_player.Position, position,
				                                    pos => _gameContext.PlayerCanWalkHere(_player, map, pos))
				                     .Skip(1);
				if (path.Any())
				{
					return path.First();
				}
			}

			return null;
		}

		private void ExecuteEeeerything()
		{
			var map = _gameContext.GetMap(_player.CurrentMap);

			var goal = GetGoal(map);

			if (goal == null)
			{
				// Select new random room goal.
				var newRoom = map.AllRooms.OrderBy(x => Guid.NewGuid()).First();
				var newPositions = map.AllPositions.Where(pos => map.GetRoomId(pos) == newRoom.RoomId).ToList();
				_gameContext.SetRoomGoal(_player.Id, newPositions);
				return;
			}

			_gameContext.Move(_player.Id, _player.Position.Direction(goal));
		}
		
	}
}