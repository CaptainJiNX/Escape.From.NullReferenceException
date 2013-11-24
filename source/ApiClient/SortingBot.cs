using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiClient
{
	public class SortingBot
	{
		private GameContext _gameContext;
		private readonly Character _sortingBot;
		private Task _runningTask;
		private CancellationTokenSource _tokenSource;
		private readonly HashSet<string> _evaluatedItems = new HashSet<string>();
		private Position _fleeeeee;

		public SortingBot(GameContext gameContext, Character sortingBot)
		{
			_gameContext = gameContext;
			_sortingBot = sortingBot;
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

					ExecuteEeeerything(_gameContext.ScanAndUpdate(_sortingBot.Id));
					Thread.Sleep(TimeSpan.FromMilliseconds(500));
				}

			}
			catch (TaskCanceledException)
			{
				// kthxbai!
			}
			catch (Exception ex)
			{
				_gameContext.AddMessage(ex.Message);
				throw;
			}
		}

		private bool ShouldPickUp(Item item)
		{
			if (_evaluatedItems.Any(x => x == item.Id))
				return false;

			var itemInfo = _gameContext.GetInfoFor(item.Id);

			if (itemInfo.IsPotion && !itemInfo.IsGaseousPotion && EnoughPotionSlotsAvailable())
			{
				return true;
			}

			if (itemInfo.IsWeapon && EnoughWeaponSlotsAvailable())
			{
				var currentDamageInfo = _gameContext.GetDamageInfo(_sortingBot.WieldedWeaponName);

				if (currentDamageInfo == null || !currentDamageInfo.IsExplored)
				{
					return true;
				}

				var itemDamageInfo = _gameContext.GetDamageInfo(itemInfo.Name);

				if (itemDamageInfo == null || !itemDamageInfo.IsExplored)
				{
					return true;
				}

				return (currentDamageInfo.AverageBaseDamage < itemDamageInfo.AverageBaseDamage);
			}

			if (itemInfo.IsArmor)
			{
				var ac = _gameContext.GetArmorClass(itemInfo.Name);
				return ac == null || ac.Value > (GetBaseAc());
			}

			_evaluatedItems.Add(item.Id);
			_gameContext.AddMessage(string.Format("Ignoring item {0}", item.Name));
			return false;
		}

		private int GetBaseAc()
		{
			return 10 - _sortingBot.ArmorClass;
		}

		private bool EnoughPotionSlotsAvailable()
		{
			var numberOfPotionsInInventory = _sortingBot.Inventory.Count(x => _gameContext.GetInfoFor(x).IsPotion);
			return numberOfPotionsInInventory < 5;
		}

		private bool EnoughWeaponSlotsAvailable()
		{
			var numberOfWeaponsInInventory = _sortingBot.Inventory.Count(x => _gameContext.GetInfoFor(x).IsWeapon);
			return numberOfWeaponsInInventory < 3;
		}

		private readonly List<Item> _itemsToSort = new List<Item>();

		private void ExecuteEeeerything(ScanResult result)
		{
			var map = _gameContext.GetMap(result.Map);

			foreach (var item in _sortingBot.VisibleItems)
			{
				if (_itemsToSort.All(x => x.Id != item.Id))
				{
					_itemsToSort.Add(item);
				}
			}

			var moreToExplore = _gameContext.Scout(_sortingBot.Id);

			if (!moreToExplore)
			{

				//var position
				var itemToSort = _itemsToSort.OrderBy(x => x.Name).FirstOrDefault();
				
				if (itemToSort == null)
					return;

				 //itemToSort.Position
			}
		}

	}
}
