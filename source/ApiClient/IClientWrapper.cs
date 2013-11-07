using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public interface IClientWrapper
	{
		JObject GetCharTemplate();
		JObject GetParty();
		JObject GetCharacter(string charId);
		JObject DeleteCharacter(string charId);
		JObject CreateCharacter(string name, int str, int con, int dex, int @int, int wis);
		JObject AllocatePoints(Attribute attr, string charId);
		JObject Quaff(string itemId, string charId);
		JObject Wield(string itemId, string charId);
		JObject Unwield(string itemId, string charId);
		JObject Equip(string itemId, string charId);
		JObject Unequip(string itemId, string charId);
		JObject Move(string charId, Direction direction);
		JObject Planeshift(string charId, string planeName);
		JObject Scan(string charId);
		ItemInfo GetInfoFor(string id);
		JObject LevelUp(string charId);
		JObject LevelDown(string charId);
		JObject Get(string charId);
		JObject Drop(string itemId, string charId);
	}
}