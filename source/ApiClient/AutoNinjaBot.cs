using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiClient
{
	public class AutoNinjaBot
	{
		private GameContext _gameContext;
		private readonly Character _awesomeNinjaBot;
		private Task _runningTask;
		private CancellationTokenSource _tokenSource;
		private readonly HashSet<string> _evaluatedItems = new HashSet<string>();
		private Position _fleeeeee;

		public AutoNinjaBot(GameContext gameContext, Character awesomeNinjaBot)
		{
			_gameContext = gameContext;
			_awesomeNinjaBot = awesomeNinjaBot;
			_tokenSource = new CancellationTokenSource();

			if (!_gameContext.PlayerHasPvPMode(_awesomeNinjaBot.Id))
			{
				_gameContext.ToggleAttackMode(_awesomeNinjaBot.Id);
			}

			if (!_gameContext.PlayerHasAttackMode(_awesomeNinjaBot.Id))
			{
				_gameContext.ToggleAttackMode(_awesomeNinjaBot.Id);
			}
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

					ExecuteEeeerything(_gameContext.ScanAndUpdate(_awesomeNinjaBot.Id));
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
				var currentDamageInfo = _gameContext.GetDamageInfo(_awesomeNinjaBot.WieldedWeaponName);

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
			return 10 - _awesomeNinjaBot.ArmorClass;
		}

		private void OptimizePotionZZZZ()
		{
			var potionsInInventory = _awesomeNinjaBot.Inventory.Select(i => _gameContext.GetInfoFor(i)).Where(x => x.IsPotion);
			
			// Drink ALL the potions!!!
			foreach (var potion in potionsInInventory.Where(x => !x.IsHealingPotion && !x.IsGaseousPotion))
			{
				_gameContext.QuaffPotion(_awesomeNinjaBot.Id, potion.Id);
			}

			if (_awesomeNinjaBot.HitPoints <= _awesomeNinjaBot.MaxHitPoints / 2)
			{
				_gameContext.QuickQuaff(_awesomeNinjaBot.Id, x => x.IsHealingPotion);
			}
		}

		private void OptimizeArmorZZZZ()
		{
			var armorInInventory =
				_awesomeNinjaBot.Inventory.Select(i => _gameContext.GetInfoFor(i))
						   .FirstOrDefault(x => x.IsArmor);

			if (armorInInventory == null)
			{
				return;
			}

			var ac = _gameContext.GetArmorClass(armorInInventory.Name);

			if (ac == null)
			{
				_gameContext.EquipArmor(_awesomeNinjaBot.Id, armorInInventory.Id);
				return;
			}

			if (ac.Value > GetBaseAc())
			{
				_gameContext.EquipArmor(_awesomeNinjaBot.Id, armorInInventory.Id);
				_gameContext.DropItem(_awesomeNinjaBot.Id, _awesomeNinjaBot.EquippedArmorId);
				return;
			}

			_gameContext.DropItem(_awesomeNinjaBot.Id, armorInInventory.Id);
		}

		private void OptimizeWeaponZZZZ()
		{
			var currentDamageInfo = _gameContext.GetDamageInfo(_awesomeNinjaBot.WieldedWeaponName);

			if (currentDamageInfo == null || !currentDamageInfo.IsExplored)
			{
				return;
			}

			var weaponsInInventory =
				_awesomeNinjaBot.Inventory.Select(i => _gameContext.GetInfoFor(i))
						   .Where(x => x.IsWeapon)
						   .Select(x => new { ItemId = x.Id, DamageInfo = _gameContext.GetDamageInfo(x.Id) });

			var allWeapons = weaponsInInventory.Concat(new[]
			{
				new
				{
					ItemId = _awesomeNinjaBot.WieldedWeaponId,
					DamageInfo = currentDamageInfo
				}
			})
			.ToList();

			var optimizedWeapon = allWeapons.FirstOrDefault(x => x.DamageInfo == null || !x.DamageInfo.IsExplored) ??
								  allWeapons.OrderByDescending(x => x.DamageInfo.AverageBaseDamage).FirstOrDefault();

			if (optimizedWeapon == null)
			{
				return;
			}

			if (optimizedWeapon.ItemId == _awesomeNinjaBot.WieldedWeaponId)
			{
				return;
			}

			_gameContext.WieldWeapon(_awesomeNinjaBot.Id, optimizedWeapon.ItemId);
			_gameContext.DropItem(_awesomeNinjaBot.Id, _awesomeNinjaBot.WieldedWeaponId);
		}

		private bool EnoughPotionSlotsAvailable()
		{
			var numberOfPotionsInInventory = _awesomeNinjaBot.Inventory.Count(x => _gameContext.GetInfoFor(x).IsPotion);
			return numberOfPotionsInInventory < 5;
		}

		private bool EnoughWeaponSlotsAvailable()
		{
			var numberOfWeaponsInInventory = _awesomeNinjaBot.Inventory.Count(x => _gameContext.GetInfoFor(x).IsWeapon);
			return numberOfWeaponsInInventory < 3;
		}

		private void ExecuteEeeerything(ScanResult result)
		{
			var map = _gameContext.GetMap(result.Map);

			foreach (var item in _awesomeNinjaBot.VisibleItems)
			{
				if (_gameContext.AvailableItemsForPickup.All(x => x.Id != item.Id))
				{
					_gameContext.AvailableItemsForPickup.Add(item);
				}
			}

			OptimizeArmorZZZZ();
			OptimizeWeaponZZZZ();
			OptimizePotionZZZZ();

			if (_awesomeNinjaBot.HitPoints < (_awesomeNinjaBot.MaxHitPoints/2))
			{
				if (_gameContext.PlayerHasAttackMode(_awesomeNinjaBot.Id))
					_gameContext.ToggleAttackMode(_awesomeNinjaBot.Id);
				if (_gameContext.PlayerHasPvPMode(_awesomeNinjaBot.Id))
					_gameContext.TogglePvPMode(_awesomeNinjaBot.Id);

				// Move to random pos and withdraw...
				_fleeeeee = _fleeeeee ?? map.GetRandomWalkablePosition(pos => _gameContext.PlayerCanWalkHere(_awesomeNinjaBot, map, pos));
				if (_fleeeeee == null)
				{
					return;
				}

				var nextFleeingPos = PathFinder.CalculatePath(_awesomeNinjaBot.Position, _fleeeeee, pos => _gameContext.PlayerCanWalkHere(_awesomeNinjaBot, map, pos))
					.Skip(1)
					.FirstOrDefault();
				if (nextFleeingPos == null) return;

				_gameContext.Move(_awesomeNinjaBot.Id, _awesomeNinjaBot.Position.Direction(nextFleeingPos));
				return;
			}

			_fleeeeee = null;

			if(!_gameContext.PlayerHasAttackMode(_awesomeNinjaBot.Id))
				_gameContext.ToggleAttackMode(_awesomeNinjaBot.Id);
			if(!_gameContext.PlayerHasPvPMode(_awesomeNinjaBot.Id))
				_gameContext.TogglePvPMode(_awesomeNinjaBot.Id);

			var monsterPositions = _gameContext.GetMonsters(_awesomeNinjaBot)
				.OrderBy(x => x.Position.Distance(_awesomeNinjaBot.Position))
				.Select(x => x.Position);

			var enemyPositions = _gameContext.GetEnemyCharacters(_awesomeNinjaBot)
				.OrderBy(x => x.Position.Distance(_awesomeNinjaBot.Position))
				.Select(x => x.Position);

			var monsterPos = monsterPositions.FirstOrDefault();
			
			Position enemyPos = null;
			if(_gameContext.PlayerHasPvPMode(_awesomeNinjaBot.Id))
				enemyPos = enemyPositions.FirstOrDefault();

			var nextAttackPos = new[] { enemyPos, monsterPos }
				.Where(pos => pos != null)
				.OrderBy(pos => pos.Distance(_awesomeNinjaBot.Position))
				.FirstOrDefault();

			if (nextAttackPos != null)
			{
				var nextMovePos = PathFinder.CalculatePath(_awesomeNinjaBot.Position, nextAttackPos, pos => _gameContext.PlayerCanWalkHere(_awesomeNinjaBot, map, pos))
					.Skip(1)
					.FirstOrDefault();
				
				if (nextMovePos != null)
				{
					_gameContext.Move(_awesomeNinjaBot.Id, _awesomeNinjaBot.Position.Direction(nextMovePos));
					return;
				}
			}

			var itemsToPickUp = _gameContext.AvailableItemsForPickup.Where(ShouldPickUp);
			var wantedItem = itemsToPickUp.OrderBy(i => i.Position.Distance(_awesomeNinjaBot.Position)).FirstOrDefault();

			if (wantedItem != null)
			{
				if (!wantedItem.Position.Equals(_awesomeNinjaBot.Position))
				{
					var path =
						PathFinder.CalculatePath(_awesomeNinjaBot.Position, wantedItem.Position,
												 pos => _gameContext.PlayerCanWalkHere(_awesomeNinjaBot, map, pos)).Skip(1);
					var nextPos = path.FirstOrDefault();
					if (nextPos != null)
					{
						_gameContext.Move(_awesomeNinjaBot.Id, _awesomeNinjaBot.Position.Direction(nextPos));
						return;
					}
				}
				else
				{
					_gameContext.PickUpItem(_awesomeNinjaBot.Id);
					return;
				}
			}

			_gameContext.Scout(_awesomeNinjaBot.Id);
		}

	}
}
