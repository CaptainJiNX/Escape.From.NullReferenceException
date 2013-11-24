using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiClient
{
	public class HenchMan
	{
		private GameContext _gameContext;
		private readonly Character _awesomeBot;
		private readonly string _friend;
		private Task _runningTask;
		private CancellationTokenSource _tokenSource;
		private const int distanceToKeep = 2;
		private readonly HashSet<string> _evaluatedItems = new HashSet<string>();

		public HenchMan(GameContext gameContext, Character awesomeBot, string friend)
		{
			_gameContext = gameContext;
			_awesomeBot = awesomeBot;
			_friend = friend;
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

					ExecuteEeeerything(_gameContext.ScanAndUpdate(_awesomeBot.Id));
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
				var currentDamageInfo = _gameContext.GetDamageInfo(_awesomeBot.WieldedWeaponName);

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
			return 10 - _awesomeBot.ArmorClass;
		}

		private void OptimizePotionZZZZ()
		{
			var potionsInInventory = _awesomeBot.Inventory.Select(i => _gameContext.GetInfoFor(i)).Where(x => x.IsPotion);
			
			// Drink ALL the potions!!!
			foreach (var potion in potionsInInventory.Where(x => !x.IsHealingPotion && !x.IsGaseousPotion))
			{
				_gameContext.QuaffPotion(_awesomeBot.Id, potion.Id);
			}

			if (_awesomeBot.HitPoints < _awesomeBot.MaxHitPoints)
			{
				_gameContext.QuickQuaff(_awesomeBot.Id, x => x.IsHealingPotion);
			}
		}

		private void OptimizeArmorZZZZ()
		{
			var armorInInventory =
				_awesomeBot.Inventory.Select(i => _gameContext.GetInfoFor(i))
						   .FirstOrDefault(x => x.IsArmor);

			if (armorInInventory == null)
			{
				return;
			}

			var ac = _gameContext.GetArmorClass(armorInInventory.Name);

			if (ac == null)
			{
				_gameContext.EquipArmor(_awesomeBot.Id, armorInInventory.Id);
				return;
			}

			if (ac.Value > GetBaseAc())
			{
				_gameContext.EquipArmor(_awesomeBot.Id, armorInInventory.Id);
				_gameContext.DropItem(_awesomeBot.Id, _awesomeBot.EquippedArmorId);
				return;
			}

			_gameContext.DropItem(_awesomeBot.Id, armorInInventory.Id);
		}

		private void OptimizeWeaponZZZZ()
		{
			var currentDamageInfo = _gameContext.GetDamageInfo(_awesomeBot.WieldedWeaponName);

			if (currentDamageInfo == null || !currentDamageInfo.IsExplored)
			{
				return;
			}

			var weaponsInInventory =
				_awesomeBot.Inventory.Select(i => _gameContext.GetInfoFor(i))
						   .Where(x => x.IsWeapon)
						   .Select(x => new { ItemId = x.Id, DamageInfo = _gameContext.GetDamageInfo(x.Id) });

			var allWeapons = weaponsInInventory.Concat(new[]
			{
				new
				{
					ItemId = _awesomeBot.WieldedWeaponId,
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

			if (optimizedWeapon.ItemId == _awesomeBot.WieldedWeaponId)
			{
				return;
			}

			_gameContext.WieldWeapon(_awesomeBot.Id, optimizedWeapon.ItemId);
			_gameContext.DropItem(_awesomeBot.Id, _awesomeBot.WieldedWeaponId);
		}

		private bool EnoughPotionSlotsAvailable()
		{
			var numberOfPotionsInInventory = _awesomeBot.Inventory.Count(x => _gameContext.GetInfoFor(x).IsPotion);
			return numberOfPotionsInInventory < 5;
		}

		private bool EnoughWeaponSlotsAvailable()
		{
			var numberOfWeaponsInInventory = _awesomeBot.Inventory.Count(x => _gameContext.GetInfoFor(x).IsWeapon);
			return numberOfWeaponsInInventory < 3;
		}

		private void ExecuteEeeerything(ScanResult result)
		{
			var map = _gameContext.GetMap(result.Map);
			var itemsToPickUp = _gameContext.AvailableItemsForPickup.Where(ShouldPickUp);
			var wantedItem = itemsToPickUp.OrderBy(i => i.Position.Distance(_awesomeBot.Position)).FirstOrDefault();

			if (wantedItem != null)
			{
				if (!wantedItem.Position.Equals(_awesomeBot.Position))
				{
					var path =
						PathFinder.CalculatePath(_awesomeBot.Position, wantedItem.Position,
												 pos => _gameContext.PlayerCanWalkHere(_awesomeBot, map, pos)).Skip(1);
					var nextPos = path.FirstOrDefault();
					if (nextPos != null)
					{
						_gameContext.Move(_awesomeBot.Id, _awesomeBot.Position.Direction(nextPos));
						return;
					}
				}
				else
				{
					_gameContext.PickUpItem(_awesomeBot.Id);
					return;
				}
			}

			OptimizeArmorZZZZ();
			OptimizeWeaponZZZZ();
			OptimizePotionZZZZ();

			var friendChar = _gameContext.GetPlayer(_friend);

			var myGoal = friendChar.AttackingPosition ?? friendChar.Position;
			var distance = _awesomeBot.Position.Distance(myGoal);

			var isAttacking = friendChar.AttackingPosition != null;

			if (isAttacking)
			{
				if (!_gameContext.PlayerHasPvPMode(_awesomeBot.Id))
				{
					_gameContext.ToggleAttackMode(_awesomeBot.Id);
				}

				if (!_gameContext.PlayerHasAttackMode(_awesomeBot.Id))
				{
					_gameContext.ToggleAttackMode(_awesomeBot.Id);
				}
			}
			else
			{
				if (_gameContext.PlayerHasPvPMode(_awesomeBot.Id))
				{
					_gameContext.ToggleAttackMode(_awesomeBot.Id);
				}

				if (_gameContext.PlayerHasAttackMode(_awesomeBot.Id))
				{
					_gameContext.ToggleAttackMode(_awesomeBot.Id);
				}
			}

			var myDistanceToKeep = isAttacking ? 0 : distanceToKeep;

			if (distance > myDistanceToKeep)
			{
				var newPos = PathFinder
					.CalculatePath(_awesomeBot.Position, myGoal, pos => _gameContext.PlayerCanWalkHere(_awesomeBot, map, pos))
					.Skip(1)
					.FirstOrDefault();

				_gameContext.Move(_awesomeBot.Id, _awesomeBot.Position.Direction(newPos));
			}
		}

	}
}
