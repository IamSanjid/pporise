using MoonSharp.Interpreter;
using PPOBot.Utils;
using PPOProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;

namespace PPOBot.Scripting
{
	public class LuaScript : BaseScript
	{
		private bool _actionExecuted;
		public readonly BotClient Bot;
		private readonly IList<string> _libsContent;

#if DEBUG
		public int TimeoutDelay = 60000;
#else
		public int TimeoutDelay = 4000;
#endif

		private Script _lua;
		private readonly string _path;
		private readonly string _content;
		private IDictionary<string, IList<DynValue>> _hookedFunctions;

		public LuaScript(BotClient bot, string path, string content, IList<string> libsContent)
		{
			Bot = bot;
			_path = Path.GetDirectoryName(path);
			_content = content;
			_libsContent = libsContent;
		}
		//Asynchronous
		public override async Task Initialize()
		{
			//Asynchronous
			await CreateLuaInstance();
			Name = _lua.Globals.Get("name").CastToString();
			Author = _lua.Globals.Get("author").CastToString();
			Description = _lua.Globals.Get("description").CastToString();
		}

		public override void Start()
		{
			//CallFunctionSafe("onStart");
			DynValue function = _lua.Globals.Get("onStart");
			if (function.Type == DataType.Function && function.Type != DataType.Nil)
				_lua.Call(function);

		}

		public override void Stop()
		{
			//CallFunctionSafe("onStop");
			DynValue function = _lua.Globals.Get("onStop");
			if (function.Type == DataType.Function && function.Type != DataType.Nil)
				_lua.Call(function);
		}

		public override void Pause()
		{
			//CallFunctionSafe("onPause");
			DynValue function = _lua.Globals.Get("onPause");
			if (function.Type == DataType.Function && function.Type != DataType.Nil)
				_lua.Call(function);
		}

		public override void Resume()
		{
			//CallFunctionSafe("onResume");
			DynValue function = _lua.Globals.Get("onResume");
			if (function.Type == DataType.Function && function.Type != DataType.Nil)
				_lua.Call(function);
		}

		public override void OnBattleMessage(string message)
		{
			CallFunctionSafe("onBattleMessage", message);
		}

		public override void OnSystemMessage(string message)
		{
			CallFunctionSafe("onSystemMessage", message);
		}

		public override void OnLearningMove(string moveName, int pokemonIndex)
		{
			CallFunctionSafe("onLearningMove", moveName, pokemonIndex);
		}

		public override bool ExecuteNextAction()
		{
			var functionName = Bot.Game.Battle ? "onBattleAction" : "onPathAction";
			_actionExecuted = false;
			try
			{
				CallFunction(functionName, true);
			}
			catch (ScriptRuntimeException ex)
			{
				throw new Exception(ex.DecoratedMessage, ex);
			}

			return _actionExecuted;
		}

		private void CallFunctionSafe(string functionName, params object[] args)
		{
			try
			{
				try
				{
					CallFunction(functionName, false, args);
				}
				catch (ScriptRuntimeException ex)
				{
					throw new Exception(ex.DecoratedMessage, ex);
				}
			}
			catch (Exception ex)
			{
#if DEBUG
				Fatal("Error during the execution of '" + functionName + "': " + ex);
#else
				File.WriteAllText($"[{DateTime.Now.ToShortTimeString()}]Error-of-script.txt", ex.ToString());
				Fatal("Error during the execution of '" + functionName + "': " + ex.Message);
#endif
			}
		}

		private void CallContent(string content)
		{
			try
			{
				TaskUtils.CallActionWithTimeout(() => _lua.DoString(content),
					() => throw new Exception("The execution of the script timed out."), TimeoutDelay);
			}
			catch (SyntaxErrorException ex)
			{
				throw new Exception(ex.DecoratedMessage, ex);
			}
		}

		private void CallFunction(string functionName, bool isPathAction, params object[] args)
		{
			if (_hookedFunctions.ContainsKey(functionName))
			{
				foreach (var function in _hookedFunctions[functionName])
				{
					CallDynValueFunction(function, "hook:" + functionName, args);
					if (isPathAction && _actionExecuted) return;
				}
			}

			CallDynValueFunction(_lua.Globals.Get(functionName), functionName, args);
		}

		private void CallDynValueFunction(DynValue function, string functionName, params object[] args)
		{
			if (function.Type != DataType.Function) return;
			TaskUtils.CallActionWithTimeout(() => _lua.Call(function, args),
				delegate { Fatal("The execution of the script timed out (" + functionName + ")."); }, TimeoutDelay);
		}

		private bool ValidateAction(string source, bool inBattle)
		{
			if (_actionExecuted)
			{
				Fatal("error: " + source + ": the script can only execute one action per frame.");
				return false;
			}

			if (Bot.Game.Battle != inBattle)
			{
				if (inBattle)
				{
					Fatal("error: " + source + " you cannot execute a battle action while not in a battle.");
				}
				else
				{
					Fatal("error: " + source + " you cannot execute a path action while in a battle.");
				}

				return false;
			}

			return true;
		}

		private bool ExecuteAction(bool result)
		{
			if (result)
			{
				_actionExecuted = true;
			}

			return result;
		}

		private async Task CreateLuaInstance()
		{
			//Asynchronous
			await Task.Run(() =>
			{
				_hookedFunctions = new Dictionary<string, IList<DynValue>>();
				_lua = new Script(CoreModules.Preset_SoftSandbox | CoreModules.LoadMethods)
				{
					Options =
					{
						ScriptLoader = new CustomScriptLoader(_path) {ModulePaths = new[] {"?.lua"}},
						CheckThreadAccess = false
					}
				};
				_lua.Globals["stringContains"] = new Func<string, string, bool>(StringContains);
				_lua.Globals["log"] = new Action<string>(Log);
				_lua.Globals["fatal"] = new Action<string>(Fatal);
				_lua.Globals["logout"] = new Action<string>(Logout);
				_lua.Globals["playSound"] = new Action<string>(PlaySound);
				_lua.Globals["registerHook"] = new Action<string, DynValue>(RegisterHook);
				_lua.Globals["flashWindow"] = new Action(FlashBotWindow);
				//battle
				_lua.Globals["isOpponentShiny"] = new Func<bool>(IsOpponentShiny);
				_lua.Globals["isAlreadyCaught"] = new Func<bool>(IsAlreadyCaught);
				_lua.Globals["isOpponentEffortValue"] = new Func<string, bool>(IsOpponentEffortValue);
				_lua.Globals["getOpponentEffortValue"] = new Func<string, int>(GetOpponentEffortValue);
				_lua.Globals["getOpponentIV"] = new Func<string, int>(GetOpponentIv);
				_lua.Globals["getOpponentType"] = new Func<string[]>(GetOpponentType);
				_lua.Globals["getOpponentName"] = new Func<string>(GetOpponentName);
				_lua.Globals["getOpponentHealth"] = new Func<int>(GetOpponentHealth);
				_lua.Globals["getOpponentMaxHealth"] = new Func<int>(GetOpponentMaxHealth);
				_lua.Globals["getOpponentHealthPercent"] = new Func<int>(GetOpponentHealthPercent);
				_lua.Globals["getOpponentLevel"] = new Func<int>(GetOpponentLevel);
				_lua.Globals["getActivePokemonNumber"] = new Func<int>(GetActivePokemonNumber);
				_lua.Globals["getOpponentStatus"] = new Func<string>(GetOpponentStatus);
				_lua.Globals["getOpponenetNature"] = new Func<string>(GetOpponenetNature);
				_lua.Globals["getOpponenetAbility"] = new Func<string>(GetOpponenetAbility);
				_lua.Globals["isOpponentRare"] = new Func<bool>(IsOpponentRare);
				_lua.Globals["useMoveAt"] = new Func<DynValue, bool>(UseMoveAt); //lol1
				_lua.Globals["useFirstMove"] = new Func<bool>(UseFirstMove); //lol2
				_lua.Globals["useMove"] = new Func<string, bool>(UseMove);
				_lua.Globals["isInBattle"] = new Func<bool>(IsInBattle);
				_lua.Globals["run"] = new Func<bool>(Run);
				_lua.Globals["sendAnyPokemon"] = new Func<bool>(SendAnyPokemon);
				_lua.Globals["sendUsablePokemon"] = new Func<bool>(SendUsablePokemon);
				_lua.Globals["sendPokemon"] = new Func<int, bool>(SendPokemon);
				_lua.Globals["attack"] = new Func<bool>(Attack);
				_lua.Globals["weakAttack"] = new Func<bool>(WeakAttack);
				_lua.Globals["isOppenentDataReceived"] = new Func<bool>(IsOppenentDataReceived);
				_lua.Globals["isWildBattle"] = new Func<bool>(IsWildBattle);
				//path
				_lua.Globals["startBattle"] = new Func<bool>(StartBattle);
				_lua.Globals["startFishing"] = new Func<DynValue, bool>(StartFishing);
				_lua.Globals["stopFishing"] = new Func<bool>(StopFishing);
				_lua.Globals["startAnyColorRockMining"] = new Func<DynValue, bool>(StartAnyColorRockMining);
				_lua.Globals["startColoredRockMining"] = new Func<DynValue, DynValue[], bool>(StartColoredRockMining);
				_lua.Globals["stopMining"] = new Func<bool>(StopMining);
				_lua.Globals["startSurfBattle"] = new Func<bool>(StartSurfBattle);
				_lua.Globals["teleportTo"] = new Func<DynValue[], bool>(TeleportTo);
				_lua.Globals["getMapName"] = new Func<string>(GetMapName);
				_lua.Globals["isFishing"] = new Func<bool>(IsFishing);
				_lua.Globals["isMining"] = new Func<bool>(IsMining);
				_lua.Globals["isAutoEvolve"] = new Func<bool>(IsAutoEvolve);
				_lua.Globals["enableAutoEvolve"] = new Func<bool>(EnableAutoEvolve);
				_lua.Globals["disableAutoEvolve"] = new Func<bool>(DisableAutoEvolve);
				_lua.Globals["getPlayerX"] = new Func<int>(GetPlayerX);
				_lua.Globals["getPlayerY"] = new Func<int>(GetPlayerY);
				_lua.Globals["isAnyRockMinable"] = new Func<bool>(IsAnyRockMinable);
				_lua.Globals["isRockAtMinable"] = new Func<int, int, bool>(IsRockAtMinable);
				_lua.Globals["moveToCell"] = new Func<int, int, string, bool>(MoveToCell);
				_lua.Globals["moveLinearX"] = new Func<int, int, int, string, bool>(MoveLinearX);
				_lua.Globals["moveLinearY"] = new Func<int, int, int, string, bool>(MoveLinearY);
				//Pokemon
				_lua.Globals["isTeamSortedByLevelAscending"] = new Func<bool>(IsTeamSortedByLevelAscending);
				_lua.Globals["isTeamSortedByLevelDescending"] = new Func<bool>(IsTeamSortedByLevelDescending);
				_lua.Globals["isTeamRangeSortedByLevelAscending"] = new Func<int, int, bool>(IsTeamRangeSortedByLevelAscending);
				_lua.Globals["isTeamRangeSortedByLevelDescending"] = new Func<int, int, bool>(IsTeamRangeSortedByLevelDescending);
				_lua.Globals["swapPokemon"] = new Func<int, int, bool>(SwapPokemon);
				_lua.Globals["swapPokemonWithLeader"] = new Func<string, bool>(SwapPokemonWithLeader);
				_lua.Globals["sortTeamByLevelAscending"] = new Func<bool>(SortTeamByLevelAscending);
				_lua.Globals["sortTeamByLevelDescending"] = new Func<bool>(SortTeamByLevelDescending);
				_lua.Globals["sortTeamRangeByLevelAscending"] = new Func<int, int, bool>(SortTeamRangeByLevelAscending);
				_lua.Globals["sortTeamRangeByLevelDescending"] = new Func<int, int, bool>(SortTeamRangeByLevelDescending);
				_lua.Globals["getTeamSize"] = new Func<int>(GetTeamSize);
				_lua.Globals["getPokemonHealth"] = new Func<int, int>(GetPokemonHealth);
				_lua.Globals["getPokemonStatus"] = new Func<int, string>(GetPokemonStatus);
				_lua.Globals["getPokemonLevel"] = new Func<int, int>(GetPokemonLevel);
				_lua.Globals["getPokemonName"] = new Func<int, string>(GetPokemonName);
				_lua.Globals["getPokemonHeldItem"] = new Func<int, string>(GetPokemonHeldItem);
				_lua.Globals["isPokemonUsable"] = new Func<int, bool>(IsPokemonUsable);
				_lua.Globals["getUsablePokemonCount"] = new Func<int>(GetUsablePokemonCount);
				_lua.Globals["isPokemonShiny"] = new Func<int, bool>(IsPokemonShiny);
				_lua.Globals["getPokemonIV"] = new Func<int, string, int>(GetPokemonIndividualValue);
				_lua.Globals["getPokemonEV"] = new Func<int, string, int>(GetPokemonEffortValue);
				_lua.Globals["getPokemonHealthPercent"] = new Func<int, int>(GetPokemonHealthPercent);
				_lua.Globals["getPokemonMaxHealth"] = new Func<int, int>(GetPokemonMaxHealth);
				_lua.Globals["getPokemonAbility"] = new Func<int, string>(GetPokemonAbility);
				_lua.Globals["hasMove"] = new Func<int, string, bool>(HasMove);
				_lua.Globals["hasPokemonInTeam"] = new Func<string, bool>(HasPokemonInTeam);
				//Item/Shop
				_lua.Globals["hasItem"] = new Func<string, bool>(HasItem);
				_lua.Globals["getItemQuantity"] = new Func<string, int>(GetItemQuantity);
				_lua.Globals["isShopOpen"] = new Func<bool>(IsShopOpen);
				_lua.Globals["openShop"] = new Func<bool>(OpenShop);
				_lua.Globals["buyItem"] = new Func<string, int, bool>(BuyItem);
				_lua.Globals["getMoney"] = new Func<int>(GetMoney);
				_lua.Globals["giveItemToPokemon"] = new Func<string, int, bool>(GiveItemToPokemon);
				_lua.Globals["takeItemFromPokemon"] = new Func<int, bool>(TakeItemFromPokemon);
				_lua.Globals["useItem"] = new Func<string, bool>(UseItem); //lol1
				_lua.Globals["useItemOnPokemon"] = new Func<string, int, bool>(UseItemOnPokemon);
				_lua.Globals["useFirstItem"] = new Func<bool>(UseFirstItem); //lol2
				//move learning
				_lua.Globals["forgetMove"] = new Func<string, bool>(ForgetMove);
				_lua.Globals["forgetAnyMoveExcept"] = new Func<DynValue[], bool>(ForgetAnyMoveExcept);
				//miscs
				_lua.Globals["getMiningLevel"] = new Func<int>(GetMiningLevel);
				_lua.Globals["getMiningTotalExperience"] = new Func<int>(GetMiningTotalExperience);
				_lua.Globals["getMiningCurrentExperience"] = new Func<int>(GetMiningCurrentExperience);
				_lua.Globals["getFishingLevel"] = new Func<int>(GetFishingLevel);
				_lua.Globals["getFishingCurrentExperience"] = new Func<int>(GetFishingCurrentExperience);
				_lua.Globals["getFishingTotalExperience"] = new Func<int>(GetFishingTotalExperience);
				_lua.Globals["countColoredRocks"] = new Func<DynValue, int>(CountColoredRocks);
				_lua.Globals["login"] = new Action<string, string, int, string, int, string, string>(Login);
				_lua.Globals["relog"] = new Action<DynValue[]>(Relog);
				_lua.Globals["startScript"] = new Func<bool>(StartScript);
				_lua.Globals["invoke"] = new Action<DynValue, float, DynValue[]>(Invoke);
				_lua.Globals["cancelInvokes"] = new Action(CancelInvokes);

				// ReSharper disable once InvertIf
				if (_libsContent.Count > 0)
				{
					foreach (var content in _libsContent)
					{
						CallContent(content);
					}
				}
				CallContent(_content);
			});
		}

		// API: Moves to specific cell
		private bool MoveToCell(int x, int y, string reason)
		{
			return Bot.MoveToCell(x, y, reason);
		}

		private bool MoveLinearX(int x1, int x2, int y, string forWhat = "battle")
		{
			return ExecuteAction(Bot.MoveLeftRight(x1, y, x2, y, forWhat));
		}

		private bool MoveLinearY(int y1, int y2, int x, string forWhat = "battle")
		{
			return ExecuteAction(Bot.MoveLeftRight(x, y1, x, y2, forWhat));
		}

		// API: Counts specific colored rocks.
		private int CountColoredRocks(DynValue value)
		{
			if (Bot.Game.MiningObjects is null || Bot.Game.MiningObjects.Count > 0 is false)
				return -1;
			return Bot.Game.MiningObjects.Count(rock => string.Equals(rock.Color, value.String, StringComparison.InvariantCultureIgnoreCase));
		}

		// API: Returns true if current battle is wild Pokemon battle.
		private bool IsWildBattle() => Bot.Game?.ActiveBattle.IsWildBattle != null && (bool) Bot.Game?.ActiveBattle.IsWildBattle;

		// API: Returns the fishing level
		private int GetFishingLevel() => Bot.Game.Fishing?.FishingLevel ?? -1;

		// API: Returns the fishing current experience
		private int GetFishingCurrentExperience() => Bot.Game.Fishing?.CurrentFishingXp ?? -1;

		// API: Returns the fishing total experience
		private int GetFishingTotalExperience() => Bot.Game.Fishing?.TotalFishingXp ?? -1;

		// API: Returns the mining total experience
		private int GetMiningTotalExperience() => Bot.Game.Mining?.TotalMiningXp ?? -1;

		// API: Returns the mining current experience
		private int GetMiningCurrentExperience()
		{
			if (Bot.Game.Mining is null) return -1;
			return Bot.Game.Mining.CurrentMiningXp;
		}

		// API: Returns the mining level.
		private int GetMiningLevel()
		{
			if (Bot.Game.Mining is null) return -1;
			return Bot.Game.Mining.MiningLevel;
		}

		// API: Returns true if there is a shop opened.
		private bool IsShopOpen()
		{
			return Bot.Game.OpenedShop != null;
		}
		public static string FirstCharToUpper(string input)
		{
			if (string.IsNullOrEmpty(input))
				return "";
			input = input.ToLowerInvariant();
			return input.First().ToString().ToUpper() + input.Substring(1);
		}
		// API: Simulates Pokemon Planet's Shop open thing but not the GUI :D.
		private bool OpenShop()
		{
			if (IsShopOpen())
			{
				Fatal("Shop is already opened.");
				return false;
			}

			if (!GetMapName().ToLowerInvariant().Contains("mart"))
			{
				Fatal("You cannot shop in the current map. You must go to a Pokemart.");
				return false;
			}
			Bot.Game.OpenShop();
			return true;
		}
		// API: Returns the item held by the specified pokemon in the team, null if empty.
		private string GetPokemonHeldItem(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: getPokemonHeldItem: tried to retrieve the non-existing pokemon " + index + ".");
				return null;
			}
			string itemHeld = Bot.Game.Team[index - 1].ItemHeld;
			return itemHeld == string.Empty ? null : FirstCharToUpper(itemHeld);
		}
		// API: Buys the specified item from the opened shop.
		private bool BuyItem(string itemName, int quantity)
		{
			if (!ValidateAction("buyItem", false)) return false;

			if (Bot.Game.OpenedShop == null)
			{
				Fatal("error: buyItem can only be used when a shop is open.");
				return false;
			}
			var item = Bot.Game.OpenedShop.ShopItems.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase));

			if (item == null)
			{
				Fatal("error: buyItem: the item '" + itemName + "' does not exist in the opened shop.");
				return false;
			}

			return ExecuteAction(Bot.Game.BuyItem(item.Uid.GetValueOrDefault(), quantity));
		}
		// API: Gives the specified item on the specified pokemon.
		private bool GiveItemToPokemon(string itemName, int pokemonIndex)
		{
			if (!ValidateAction("giveItemToPokemon", false)) return false;

			if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
			{
				Fatal("error: giveItemToPokemon: tried to retrieve the non-existing pokémon " + pokemonIndex + ".");
				return false;
			}

			var item = Bot.Game.GetItemFromName(itemName);
			if (item == null || item.Quntity == 0)
			{
				Fatal("error: giveItemToPokemon: tried to give the non-existing item '" + itemName + "'.");
				return false;
			}

			return ExecuteAction(Bot.Game.GiveItemToPokemon(pokemonIndex, item.Uid));
		}

		// API: Takes the held item from the specified pokemon.
		private bool TakeItemFromPokemon(int index)
		{
			if (!ValidateAction("takeItemFromPokemon", false)) return false;

			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: takeItemFromPokemon: tried to retrieve the non-existing pokemon " + index + ".");
				return false;
			}

			if (Bot.Game.Team[index - 1].ItemHeld == string.Empty)
			{
				Fatal("error: takeItemFromPokemon: tried to take the non-existing held item from pokémon '" + index + "'.");
				return false;
			}

			return ExecuteAction(Bot.Game.TakeItemFromPokemon(index));
		}
		// API: Uses the specified item on the specified pokémon.
		private bool UseItemOnPokemon(string itemName, int pokemonIndex)
		{
			itemName = itemName.ToUpperInvariant();
			var item = Bot.Game.GetItemFromName(itemName.ToUpperInvariant());
			if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
			{
				Fatal("error: useItemOnPokemon: tried to retrieve the non-existing pokemon " + pokemonIndex + ".");
				return false;
			}

			if (item != null && item.Quntity > 0)
			{
				if (Bot.Game.IsInBattle && !item.IsEquipAble())
				{
					if (!ValidateAction("useItemOnPokemon", true)) return false;
					return ExecuteAction(Bot.Game.UseItem(item.Name));
				}
				if (!Bot.Game.IsInBattle && !item.IsEquipAble())
				{
					if (!ValidateAction("useItemOnPokemon", false)) return false;
					Bot.Game.UseItem(item.Name, pokemonIndex);
					return ExecuteAction(true);
				}
			}
			return false;
		}
		// API: Returns true if the team is sorted by level in ascending order.
		private bool IsTeamSortedByLevelAscending()
		{
			return IsTeamSortedByLevel(true, 1, 6);
		}

		// API: Returns true if the team is sorted by level in descending order.
		private bool IsTeamSortedByLevelDescending()
		{
			return IsTeamSortedByLevel(false, 1, 6);
		}

		// API: Returns true if the specified part of the team is sorted by level in ascending order.
		private bool IsTeamRangeSortedByLevelAscending(int fromIndex, int toIndex)
		{
			return IsTeamSortedByLevel(true, fromIndex, toIndex);
		}

		// API: Returns true if the specified part of the team the team is sorted by level in descending order.
		private bool IsTeamRangeSortedByLevelDescending(int fromIndex, int toIndex)
		{
			return IsTeamSortedByLevel(false, fromIndex, toIndex);
		}
		private bool IsTeamSortedByLevel(bool ascending, int from, int to)
		{
			from = Math.Max(from, 1);
			to = Math.Min(to, Bot.Game.Team.Count);

			var level = ascending ? 0 : int.MaxValue;
			for (var i = from - 1; i < to; ++i)
			{
				var pokemon = Bot.Game.Team[i];
				if (ascending && pokemon.Level < level) return false;
				if (!ascending && pokemon.Level > level) return false;
				level = pokemon.Level;
			}
			return true;
		}
		// API: Returns the amount of usable pokémon in the team.
		private int GetUsablePokemonCount()
		{
			return Bot.AI.UsablePokemonsCount;
		}
		// API: Flashes the bot window.
		private new void FlashBotWindow() => FlashWindow();
		// API: Returns true if the specified pokémon is present in the team.
		private bool HasPokemonInTeam(string pokemonName)
		{
			return Bot.Game.HasPokemonInTeam(pokemonName.ToUpperInvariant());
		}
		// API: Returns playing a custom sound.
		private void PlaySound(string file)
		{
			if (!File.Exists(file)) return;
			using (var player = new SoundPlayer(file))
			{
				player.Play();
			}
		}
		// API: Returns the amount of Pokémon in the team.
		private int GetTeamSize()
		{
			return Bot.Game.Team.Count;
		}
		// API: Returns true if there is any minable rock.
		private bool IsAnyRockMinable()
		{
			return Bot.Game.IsAnyMinableRocks();
		}
		private bool IsRockAtMinable(int x, int y)
		{
			return Bot.Game.IsMinable(x, y);
		}
		// API: Returns true if the opponent pokémon has already been caught and has a pokédex entry.
		private bool IsAlreadyCaught()
		{
			if (!Bot.Game.Battle || Bot.Game.ActiveBattle is null)
			{
				Fatal("error: isAlreadyCaught is only usable in battle.");
				return false;
			}
			return Bot.Game.ActiveBattle.IsAlreadyCaught;
		}
		// API: Returns the X-coordinate of the current cell.
		private int GetPlayerX()
		{
			return Bot.Game.PlayerX;
		}
		// API: Returns true if specified pokemon got specified move.
		private bool HasMove(int pokemonIndex, string moveName)
		{
			if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
			{
				Fatal("error: hasMove: tried to retrieve the non-existing pokemon " + pokemonIndex + ".");
				return false;
			}

			return Bot.Game.PokemonUidHasMove(pokemonIndex, moveName.ToUpperInvariant());
		}
		// API: Returns true if the specified item is in the inventory.
		private bool HasItem(string itemName)
		{
			return Bot.Game.HasItemName(itemName.ToUpperInvariant());
		}

		// API: Returns the quantity of the specified item in the inventory.
		private int GetItemQuantity(string itemName)
		{
			return Bot.Game.GetItemFromName(itemName.ToUpperInvariant())?.Quntity ?? 0;
		}

		// API: Returns the Y-coordinate of the current cell.
		private int GetPlayerY()
		{
			return Bot.Game.PlayerY;
		}
		private bool IsMining() => Bot.Game.IsMinning;
		private string GetOpponenetNature()
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponenetNature can only be used in battle.");
				return "";
			}
			if (Bot.Game.ActiveBattle.FullWildPokemon is null || Bot.Game.ActiveBattle.FullWildPokemon.Name != Bot.Game.ActiveBattle.WildPokemon.Name)
			{
				Fatal("error: getOpponenetNature: You must have to use a move to get oppponent full data.");
				return "";
			}
			if (!IsOppenentDataReceived())
			{
				Fatal("error: getOpponenetNature: You must have to use a move to get oppponent full data.");
				return "";
			}
			return Bot.Game.ActiveBattle.FullWildPokemon.Nature;
		}
		private string GetOpponenetAbility()
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponenetAbility can only be used in battle.");
				return "";
			}
			if (Bot.Game.ActiveBattle.FullWildPokemon is null || Bot.Game.ActiveBattle.FullWildPokemon.Name != Bot.Game.ActiveBattle.WildPokemon.Name)
			{
				Fatal("error: getOpponenetAbility: You must have to use a move to get oppponent full data.");
				return "";
			}
			if (!IsOppenentDataReceived())
			{
				Fatal("error: getOpponenetAbility: You must have to use a move to get oppponent full data.");
				return "";
			}
			return Bot.Game.ActiveBattle.FullWildPokemon.Ability.Name;
		}
		// API: Returns true when the opponent's full data is received.
		private bool IsOppenentDataReceived() =>
			Bot.Game.Battle
			&& Bot.Game.ActiveBattle.WildPokemon != null
			&& Bot.Game.ActiveBattle.FullWildPokemon != null;
		private int GetOpponentIv(string statType)
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponentIV can only be used in battle.");
				return -1;
			}
			if (!_stats.ContainsKey(statType.ToUpperInvariant()))
			{
				Fatal("error: getOpponentIV: the stat '" + statType + "' does not exist.");
				return -1;
			}
			if (Bot.Game.ActiveBattle.FullWildPokemon is null || Bot.Game.ActiveBattle.FullWildPokemon.Name != Bot.Game.ActiveBattle.WildPokemon.Name)
			{
				Fatal("error: getOpponentIV: You must have to use a move to get oppponent full data.");
				return -1;
			}
			if (!IsOppenentDataReceived())
			{
				Fatal("error: getOpponentIV: You must have to use a move to get oppponent full data.");
				return -1;
			}
			PokemonStats ivStats = Bot.Game.ActiveBattle.FullWildPokemon.IV;
			return ivStats.GetStat(_stats[statType.ToUpperInvariant()]);
		}
		// API: Returns the percentage of remaining health of the specified pokémon in the team.
		private int GetPokemonHealthPercent(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: getPokemonHealthPercent: tried to retrieve the non-existing pokemon " + index + ".");
				return 0;
			}
			Pokemon pokemon = Bot.Game.Team[index - 1];
			return pokemon.CurrentHealth * 100 / pokemon.MaxHealth;
		}
		// API: Ability of the pokemon of the current box matching the ID.
		private string GetPokemonAbility(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: getPokemonAbility: tried to retrieve the non-existing pokemon " + index + ".");
				return null;
			}
			Pokemon pokemon = Bot.Game.Team[index - 1];
			return pokemon.Ability.Name;
		}
		// API: Swaps the two pokémon specified by their position in the team.
		private bool SwapPokemon(int index1, int index2)
		{
			if (!ValidateAction("swapPokemon", false)) return false;

			return ExecuteAction(Bot.Game.SwapPokemon(index1, index2));
		}

		// API: Swaps the first pokémon with the specified name with the leader of the team.
		private bool SwapPokemonWithLeader(string pokemonName)
		{
			if (!ValidateAction("swapPokemonWithLeader", false)) return false;

			Pokemon pokemon = Bot.Game.FindFirstPokemonInTeam(pokemonName.ToUpperInvariant());
			if (pokemon == null)
			{
				Fatal("error: swapPokemonWithLeader: there is no pokémon '" + pokemonName + "' in the team.");
				return false;
			}
			if (pokemon.Uid == 1)
			{
				Fatal("error: swapPokemonWithLeader: '" + pokemonName + "' is already the leader of the team.");
				return false;
			}

			return ExecuteAction(Bot.Game.SwapPokemon(1, pokemon.Uid));
		}
		// API: Sorts the pokémon in the team by level in ascending order, one pokémon at a time.
		private bool SortTeamByLevelAscending()
		{
			if (!ValidateAction("sortTeamByLevelAscending", false)) return false;

			return ExecuteAction(SortTeamByLevel(true, 1, 6));
		}

		// API: Sorts the pokémon in the team by level in descending order, one pokémon at a time.
		private bool SortTeamByLevelDescending()
		{
			if (!ValidateAction("sortTeamByLevelDescending", false)) return false;

			return ExecuteAction(SortTeamByLevel(false, 1, 6));
		}

		// API: Sorts the specified part of the team by level in ascending order, one pokémon at a time.
		private bool SortTeamRangeByLevelAscending(int fromIndex, int toIndex)
		{
			if (!ValidateAction("sortTeamRangeByLevelAscending", false)) return false;

			return ExecuteAction(SortTeamByLevel(true, fromIndex, toIndex));
		}

		// API: Sorts the specified part of the team by level in descending order, one pokémon at a time.
		private bool SortTeamRangeByLevelDescending(int fromIndex, int toIndex)
		{
			if (!ValidateAction("sortTeamRangeByLevelDescending", false)) return false;

			return ExecuteAction(SortTeamByLevel(false, fromIndex, toIndex));
		}

		private bool SortTeamByLevel(bool ascending, int from, int to)
		{
			from = Math.Max(from, 1);
			to = Math.Min(to, Bot.Game.Team.Count);

			for (var i = from - 1; i < to - 1; ++i)
			{
				var currentIndex = i;
				var currentLevel = Bot.Game.Team[i].Level;
				for (var j = i + 1; j < to; ++j)
				{
					if ((ascending && Bot.Game.Team[j].Level < currentLevel) ||
						(!ascending && Bot.Game.Team[j].Level > currentLevel))
					{
						currentIndex = j;
						currentLevel = Bot.Game.Team[j].Level;
					}
				}

				if (currentIndex != i)
				{
					Bot.Game.SwapPokemon(i + 1, currentIndex + 1);
					return true;
				}
			}
			return false;
		}
		// API: Returns the maximum health of the specified pokémon in the team.
		private int GetPokemonMaxHealth(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: getPokemonMaxHealth: tried to retrieve the non-existing pokemon " + index + ".");
				return 0;
			}
			Pokemon pokemon = Bot.Game.Team[index - 1];
			return pokemon.MaxHealth;
		}
		private async void MoveLeftAndRight()
		{
			await MoveLeftAndRightAsync();
		}

		private async Task<bool> MoveLeftAndRightAsync()
		{
			if (!ValidateAction("moveLeftAndRight", false)) return false;
			return ExecuteAction(await LeftRightMovement());
		}
		// API: Returns the effort value for the specified stat of the specified pokémon in the team.
		private int GetPokemonEffortValue(int pokemonIndex, string statType)
		{
			if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
			{
				Fatal("error: getPokemonEffortValue: tried to retrieve the non-existing pokémon " + pokemonIndex + ".");
				return 0;
			}

			if (!_stats.ContainsKey(statType.ToUpperInvariant()))
			{
				Fatal("error: getPokemonEffortValue: the stat '" + statType + "' does not exist.");
				return 0;
			}

			return Bot.Game.Team[pokemonIndex - 1].EV.GetStat(_stats[statType.ToUpperInvariant()]);
		}
		// API: Returns the individual value for the specified stat of the specified pokémon in the team.
		private int GetPokemonIndividualValue(int pokemonIndex, string statType)
		{
			if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
			{
				Fatal("error: getPokemonIndividualValue: tried to retrieve the non-existing pokémon " + pokemonIndex + ".");
				return 0;
			}

			if (!_stats.ContainsKey(statType.ToUpperInvariant()))
			{
				Fatal("error: getPokemonIndividualValue: the stat '" + statType + "' does not exist.");
				return 0;
			}

			return Bot.Game.Team[pokemonIndex - 1].IV.GetStat(_stats[statType.ToUpperInvariant()]);
		}
		// API: Returns the amount of money in the inventory.
		private int GetMoney()
		{
			return Bot.Game.Money;
		}
		private bool ForgetMove(string moveName)
		{
			if (!Bot.MoveTeacher.IsLearning)
			{
				Fatal("error: ‘forgetMove’ can only be used when a pokémon is learning a new move.");
				return false;
			}

			moveName = moveName.ToUpperInvariant();
			Pokemon pokemon = Bot.Game.Team[Bot.MoveTeacher.PokemonUid];
			PokemonMove move = pokemon.Moves.FirstOrDefault(m => m?.Name.ToUpperInvariant() == moveName);

			if (move != null)
			{
				Bot.MoveTeacher.MoveToForget = move.Position - 1;
				return true;
			}
			return false;
		}
		private bool ForgetAnyMoveExcept(DynValue[] moveNames)
		{
			if (!Bot.MoveTeacher.IsLearning)
			{
				Fatal("error: ‘forgetAnyMoveExcept’ can only be used when a pokémon is learning a new move.");
				return false;
			}

			HashSet<string> movesInvariantNames = new HashSet<string>();
			foreach (DynValue value in moveNames)
			{
				movesInvariantNames.Add(value.CastToString().ToUpperInvariant());
			}

			Pokemon pokemon = Bot.Game.Team[Bot.MoveTeacher.PokemonUid];
			PokemonMove move = pokemon.Moves.FirstOrDefault(m => !movesInvariantNames.Contains(m?.Name.ToUpperInvariant()));

			if (move != null)
			{
				Bot.MoveTeacher.MoveToForget = move.Position - 1;
				return true;
			}
			return false;
		}
		private bool IsAutoEvolve()
		{
			return Bot.PokemonEvolver.IsEnabled;
		}
		private bool EnableAutoEvolve()
		{
			Bot.PokemonEvolver.IsEnabled = true;
			return Bot.PokemonEvolver.IsEnabled;
		}
		private bool DisableAutoEvolve()
		{
			Bot.PokemonEvolver.IsEnabled = false;
			return !Bot.PokemonEvolver.IsEnabled;
		}
		private int GetOpponentHealthPercent()
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponentHealthPercent can only be used in battle.");
				return 0;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return 0; }
			return (Bot.Game.ActiveBattle.FullWildPokemon != null
					   ? Bot.Game.ActiveBattle.FullWildPokemon.CurrentHealth
					   : Bot.Game.ActiveBattle.WildPokemon.CurrentHealth) * 100 / (Bot.Game.ActiveBattle.FullWildPokemon != null
					   ? Bot.Game.ActiveBattle.FullWildPokemon.MaxHealth
					   : Bot.Game.ActiveBattle.WildPokemon.MaxHealth);
		}
		private int GetOpponentMaxHealth()
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponentMaxHealth can only be used in battle.");
				return 0;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return 0; }
			return Bot.Game.ActiveBattle.FullWildPokemon != null ? Bot.Game.ActiveBattle.FullWildPokemon.MaxHealth : Bot.Game.ActiveBattle.WildPokemon.MaxHealth;
		}
		private int GetOpponentLevel()
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponentLevel can only be used in battle.");
				return 0;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return 0; }
			return Bot.Game.ActiveBattle.FullWildPokemon != null ? Bot.Game.ActiveBattle.FullWildPokemon.Level : Bot.Game.ActiveBattle.WildPokemon.Level;
		}
		private string[] GetOpponentType()
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponentType can only be used in battle.");
				return null;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return null; }
			int id = Bot.Game.ActiveBattle.FullWildPokemon != null ? Bot.Game.ActiveBattle.FullWildPokemon.Id : Bot.Game.ActiveBattle.WildPokemon.Id;

			if (id <= 0 || id >= TypesManager.Instance.Type1.Count())
			{
				return new [] { "Unknown", "Unknown" };
			}

			return new [] { TypesManager.Instance.Type1[id].ToString(), TypesManager.Instance.Type2[id].ToString() };
		}
		private string GetOpponentStatus()
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponentStatus can only be used in battle.");
				return null;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return null; }
			if (Bot.Game.ActiveBattle.FullWildPokemon != null)
				return Bot.Game.ActiveBattle.FullWildPokemon.Status;
			else
				return Bot.Game.ActiveBattle.WildPokemon.Status;
		}
		private static Dictionary<string, StatType> _stats = new Dictionary<string, StatType>()
		{
			{ "HP", StatType.Health },
			{ "HEALTH", StatType.Health },
			{ "ATK", StatType.Attack },
			{ "ATTACK", StatType.Attack },
			{ "DEF", StatType.Defence },
			{ "DEFENCE", StatType.Defence },
			{ "DEFENSE", StatType.Defence },
			{ "SPATK", StatType.SpAttack },
			{ "SPATTACK", StatType.SpAttack },
			{ "SPDEF", StatType.SpDefence },
			{ "SPDEFENCE", StatType.SpDefence },
			{ "SPDEFENSE", StatType.SpDefence },
			{ "SPD", StatType.Speed },
			{ "SPEED", StatType.Speed }
		};
		private int GetOpponentEffortValue(string statType)
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: getOpponentEffortValue can only be used in battle.");
				return -1;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return -1; }
			if (!_stats.ContainsKey(statType.ToUpperInvariant()))
			{
				Fatal("error: getOpponentEffortValue: the stat '" + statType + "' does not exist.");
				return -1;
			}
			if (!EffortValuesManager.Instance.BattleValues.ContainsKey(Bot.Game.ActiveBattle.FullWildPokemon?.Id ?? Bot.Game.ActiveBattle.WildPokemon.Id))
			{
				return -1;
			}

			PokemonStats stats = EffortValuesManager.Instance.BattleValues[
				Bot.Game.ActiveBattle.FullWildPokemon?.Id ?? Bot.Game.ActiveBattle.WildPokemon.Id];
			return stats.GetStat(_stats[statType.ToUpperInvariant()]);
		}
		private bool IsOpponentEffortValue(string statType)
		{
			if (!Bot.Game.IsInBattle)
			{
				Fatal("error: isOpponentEffortValue can only be used in battle.");
				return false;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return false; }
			if (!_stats.ContainsKey(statType.ToUpperInvariant()))
			{
				Fatal("error: isOpponentEffortValue: the stat '" + statType + "' does not exist.");
				return false;
			}
			if (!EffortValuesManager.Instance.BattleValues.ContainsKey(Bot.Game.ActiveBattle.FullWildPokemon?.Id ?? Bot.Game.ActiveBattle.WildPokemon.Id))
			{
				return false;
			}

			PokemonStats stats = EffortValuesManager.Instance.BattleValues[Bot.Game.ActiveBattle.FullWildPokemon?.Id ?? Bot.Game.ActiveBattle.WildPokemon.Id];
			return stats.HasOnly(_stats[statType.ToUpperInvariant()]);
		}
		private bool IsPokemonShiny(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: isPokemonShiny: tried to retrieve the non-existing pokemon " + index + ".");
				return false;
			}
			Pokemon pokemon = Bot.Game.Team[index - 1];
			return pokemon.IsShiny;
		}
		private bool IsPokemonUsable(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: isPokemonUsable: tried to retrieve the non-existing pokemon " + index + ".");
				return false;
			}
			return Bot.AI.IsPokemonUsable(Bot.Game.Team[index - 1]);
		}
		private string GetPokemonName(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal($"error: getPokemonName tried to get name of the non-existing pokemon {index}");
				return "";
			}
			lock (Bot)
			{
				return Bot.Game.Team[index - 1].Name;
			}
		}
		private int GetPokemonLevel(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal($"error: getPokemonStatus tried to get level of the non-existing pokemon {index}");
				return -1;
			}
			lock (Bot)
			{
				return Bot.Game.Team[index - 1].Level;
			}
		}
		private string GetPokemonStatus(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal($"error: getPokemonStatus tried to get status of the non-existing pokemon {index}");
				return "";
			}
			lock (Bot)
			{
				return Bot.Game.Team[index - 1].Status;
			}
		}
		private bool SendUsablePokemon()
		{
			if (!ValidateAction("sendUsablePokemon", true)) return false;
			return ExecuteAction(Bot.AI.SendUsablePokemon());
		}
		private bool Attack()
		{
			if (!ValidateAction("attack", true)) return false;
			return ExecuteAction(Bot.AI.Attack());
		}
		private bool WeakAttack()
		{
			if (!ValidateAction("weakAttack", true)) return false;
			return ExecuteAction(Bot.AI.WeakAttack());
		}
		private bool SendPokemon(int index)
		{
			if (!ValidateAction("sendPokemon", true)) return false;

			if (index < 1 || index > Bot.Game.Team.Count)
			{
				Fatal("error: sendPokemon: tried to send the non-existing pokemon " + index + ".");
				return false;
			}
			return ExecuteAction(Bot.AI.SendPokemon(index));
		}
		private bool SendAnyPokemon()
		{
			if (!ValidateAction("sendAnyPokemon", true)) return false;
			return ExecuteAction(Bot.AI.SendAnyPokemon());
		}
		private int GetPokemonHealth(int index)
		{
			if (index < 1 || index > Bot.Game.Team.Count)
			{
				return -1;
			}
			if (Bot.Game.Team.Count > 0)
				return Bot.Game.Team[index - 1].CurrentHealth;
			return -1;
		}
		private bool Run()
		{
			if (!ValidateAction("run", true)) return false;
			return ExecuteAction(Bot.AI.Run());
		}
		private string GetMapName() => Bot.Game.MapName;
		// API: Calls the specified function when the specified event occurs.
		private void RegisterHook(string eventName, DynValue callback)
		{
			if (callback.Type != DataType.Function)
			{
				Fatal("error: registerHook: the callback must be a function.");
				return;
			}
			if (!_hookedFunctions.ContainsKey(eventName))
			{
				_hookedFunctions.Add(eventName, new List<DynValue>());
			}
			_hookedFunctions[eventName].Add(callback);
		}
		private void Log(string msg)
		{
			LogMessage(msg);
		}
		private void Fatal(string message)
		{
			LogMessage(message);
			Bot.Stop();
		}
		private int GetActivePokemonNumber()
		{
			try
			{
				if (!Bot.Game.Battle)
				{
					Fatal("error: getActivePokemonNumber is only usable in battle.");
					return 0;
				}
				if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return 0; }
				return Bot.Game.ActiveBattle.ActivePokemon + 1;
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
			return 0;
		}
		private int GetOpponentHealth()
		{
			if (!Bot.Game.Battle)
			{
				Fatal("error: getOpponentHealth can only be used in battle.");
				return 0;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return 0; }
			return Bot.Game.ActiveBattle.FullWildPokemon?.CurrentHealth ?? Bot.Game.ActiveBattle.WildPokemon.CurrentHealth;
		}
		private string GetOpponentName()
		{
			if (!Bot.Game.Battle)
			{
				Fatal("error: getOpponentName can only be used in battle.");
				return null;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return ""; }
			return Bot.Game.ActiveBattle.WildPokemon.Name;
		}
		private bool IsOpponentRare()
		{
			if (!Bot.Game.Battle)
			{
				Fatal("error: isOpponentRare is only usable in battle.");
				return false;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return false; }
			if (Bot.Game.ActiveBattle.WildPokemon != null)
			{
				return Bot.Game.ActiveBattle.WildPokemon.IsRare;
			}
			return false;
		}
		private bool IsOpponentShiny()
		{
			if (!Bot.Game.Battle)
			{
				Fatal("error: isOpponentShiny is only usable in battle.");
				return false;
			}
			if (Bot.Game.ActiveBattle is null) { Bot.Game.WaitWhileInBattle(); return false; }
			if (Bot.Game.ActiveBattle.WildPokemon != null)
			{
				return Bot.Game.ActiveBattle.WildPokemon.IsShiny;
			}
			return false;
		}
		// API: Displays the specified message to the message log and logs out.
		private void Logout(string message)
		{
			LogMessage(message);
			Bot.Stop();
			Bot.Game.Logout();
		}
		// API: Teleports to a map. 
		private bool TeleportTo(params DynValue[] values)
		{
			if (values.Length != 2 && values.Length != 3 ||
				(values.Length == 1 && (values[0].Type != DataType.Table || values[0].Type != DataType.String)) ||
				(values.Length == 3
					&& (values[0].Type != DataType.String || values[1].Type != DataType.Number
					|| values[2].Type != DataType.Number)))
			{
				Fatal("error: teleportTo: must receive either one map name or one map name, one x value and one y value \nor a table which contains these values.");
				return false;
			}

			if (values.Length == 1)
			{
				values = values[0].Table.Values.ToArray();
			}
			if (values.Length == 3)
			{
				return TeleportTo(values[0].String, (int)values[1].Number, (int)values[2].Number);
			}
			else
			{
				return TeleportTo(values[0].String);
			}
		}
		private bool TeleportTo(string map, int x = int.MinValue, int y = int.MinValue)
		{
			return ExecuteAction(Bot.Game.LoadMap(true, map, x, y));
		}

		private async Task<bool> LeftRightMovement()
		{
			await MoveLeft();
			await MoveRight();
			return true;
		}
		private async Task MoveRight() => await Task.Delay(500).ContinueWith((pr) => Bot.Game.SendMovement("right"));
		private async Task MoveLeft() => await Task.Delay(500).ContinueWith((pr) => Bot.Game.SendMovement("left"));
		// API: Starts a wild battle.
		private bool StartBattle()
		{

			try
			{
				if (Bot.Game.IsFishing)
					StopFishing();
				if (Bot.Game.Battle)
				{
					Fatal("error: startBattle is only usable when you're not in battle.");
					return false;
				}
				return ExecuteAction(Bot.Game.StartWildBattle());
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}
		// API: Checks if the bot is fishing or not.
		private bool IsFishing()
			=> Bot.Game.IsFishing;
		// API: Starts fishing.
		private bool UseMove(string moveName)
		{
			if (!ValidateAction("useMove", true)) return false;

			return ExecuteAction(Bot.AI.UseMove(moveName));
		}
		private bool StartFishing(DynValue value)
		{
			if (Bot.Game.Battle || Bot.Game.IsFishing)
			{
				Fatal("error: startFishing is only usable when you're not in battle and not fishing.");
				return false;
			}

			if (string.IsNullOrEmpty(value.String))
			{
				Fatal("error: startFishing: there was no valid rod name supplied.");
				return false;
			}

			if (!HasItem(value.String))
			{
				Fatal($"error: startFishing: {value.String} is not in your inventory.");
				return false;
			}
			return ExecuteAction(Bot.Game.SendFishing(value.String));
		}
		private bool StartAnyColorRockMining(DynValue axe)
		{
			if (Bot.Game.Battle || Bot.Game.IsMinning)
			{
				Fatal("error: startAnyColorRockMining is only usable when you're not in battle and not mining.");
				return false;
			}

			if (string.IsNullOrEmpty(axe.String))
			{
				Fatal("error: startAnyColorRockMining: there should be a valid axe name to start mining.");
				return false;
			}
			return ExecuteAction(Bot.MiningAI.MineAnyRock(axe.String));
		}
		private bool StopMining()
		{
			if (!Bot.Game.IsMinning || Bot.Game.Battle)
			{
				Fatal("error: stopMining is only usable when you've started mining and when you're not in battle.");
				return false;
			}
			return ExecuteAction(Bot.Game.StopMining());
		}
		private bool StartColoredRockMining(DynValue axe, params DynValue[] values)
		{
			if (!HasItem(axe.String))
			{
				Fatal($"Please make sure you have {axe.String} in you're inventory. If you know you have then relog and try again.");
				return false;
			}
			if (values.Any(v => v.Type != DataType.String))
			{
				Fatal("error: startColoredRockMining: must receive either a table of string or some string values.");
				return false;
			}

			if (Bot.Game.Battle || Bot.Game.IsMinning)
			{
				Fatal("error: startColoredRockMining is only usable when you're not in battle and not mining.");
				return false;
			}

			if (string.IsNullOrEmpty(axe.String))
			{
				Fatal("error: startColoredRockMining there should be a valid axe name.");
				return false;
			}

			var colors = values.Select(s => s.String).ToArray();			
			return ExecuteAction(Bot.MiningAI.MineMultipleColoredRocks(axe.String, colors));
		}
		// API: Checks if battling or not
		private bool IsInBattle() => Bot.Game.Battle;
		// API: Stops fishing.
		private bool StopFishing()
		{
			if (!Bot.Game.IsFishing || Bot.Game.Battle)
			{
				Fatal("error: stopFishing is only usable when you've started fishing and when you're not in battle.");
				return false;
			}
			return ExecuteAction(Bot.Game.StopFishing());
		}
		// API: Starts a surf wild battle.
		private bool StartSurfBattle()
		{
			if (Bot.Game.IsFishing)
			{
				StopFishing();
			}
			if (Bot.Game.Battle)
			{
				Fatal("error: startSurfBattle is only usable when you're not in battle.");
				return false;
			}
			return ExecuteAction(Bot.Game.StartSurfWildBattle());
		}
		// API: Uses item only in battle and not on Pokemon.
		private bool UseItem(string value)
		{
			if (!HasItem(value))
			{
				return false;
			}
			if (GetPokemonHealth(GetActivePokemonNumber()) == 0)
				return false;
			return ExecuteAction(Bot.Game.UseItem(value));
		}
		private bool UseFirstItem()
		{
			if (!Bot.Game.Battle)
			{
				Fatal("error: useFirstItem only can be used in battles.");
				return false;
			}
			return ExecuteAction(Bot.Game.UseItem());
		}

		private bool UseMoveAt(DynValue index)
		{
			if (!ValidateAction("useMove", true)) return false;
			return ExecuteAction(Bot.AI.UseMove((int)index.Number));
		}

		private bool UseFirstMove()
		{
			if (!ValidateAction("useMove", true)) return false;
			return ExecuteAction(Bot.AI.UseMove(1));
		}
		// API: Returns true if the string contains the specified part, ignoring the case.
		private bool StringContains(string haystack, string needle)
		{
			return haystack.ToUpperInvariant().Contains(needle.ToUpperInvariant());
		}
		private void Login(string accountName, string password, int socks = 0, string host = "", int port = 0, string socksUser = "", string socksPass = "")
		{
			if (Bot.Game != null)
			{
				Fatal("error: login: tried to login while already logged in");
				return;
			}

			LogMessage("Connecting to the server...");
			Account account = new Account(accountName);
			account.Password = password;

			if (socks == 4 || socks == 5)
			{
				account.Socks.Version = (SocksVersion)socks;
				account.Socks.Host = host;
				account.Socks.Port = port;
				account.Socks.Username = socksUser;
				account.Socks.Password = socksPass;
			}

			Bot.Login(account);
		}
		public void Relog(params DynValue[] values)
		{
			if (values.Length != 2 && values.Length != 3 ||
				(values.Length == 1 && values[0].Type != DataType.Table) ||
				(values.Length == 3
					&& (values[0].Type != DataType.Number || values[1].Type != DataType.String
					|| values[2].Type != DataType.Boolean)))
			{
				Fatal("error: Relog: must receive either one float/number and one string/message or one float, one string/message and one bool value \nor a table which contains these values.");
				return;
			}

			if (values.Length == 1)
			{
				values = values[0].Table.Values.ToArray();
			}
			if (values.Length == 3)
			{
				Relog((float)values[0].Number, values[1].String, values[2].Boolean);
			}
			else
			{
				Relog((float)values[0].Number, values[1].String, false);
			}
		}
		/// <summary>
		/// Lua API for PPORise, it just logs out and relogs after specific time. 
		/// </summary>
		/// <param name="seconds"></param>
		/// <param name="message"></param>
		/// <param name="autoReconnect"></param>
		public override void Relog(float seconds, string message, bool autoReconnect)
		{

			DynValue name = DynValue.NewString(Bot.Account.Name);
			DynValue password = DynValue.NewString(Bot.Account.Password);

			if (Bot.Account.Socks.Version != SocksVersion.None)
			{
				DynValue socks = DynValue.NewNumber((int)Bot.Account.Socks.Version);
				DynValue host = DynValue.NewString(Bot.Account.Socks.Host);
				DynValue port = DynValue.NewNumber(Bot.Account.Socks.Port);
				DynValue socksUser = DynValue.NewString(Bot.Account.Socks.Username);
				DynValue socksPass = DynValue.NewString(Bot.Account.Socks.Password);
				Invoke(_lua.Globals.Get("login"), seconds, name, password, socks, host, port, socksUser, socksPass);
			}
			else
			{
				Invoke(_lua.Globals.Get("login"), seconds, name, password);
			}

			Invoke(_lua.Globals.Get("startScript"), seconds + 10);

			if (!autoReconnect)
			{
				if (Bot.Account.Socks.Version != SocksVersion.None)
				{
					LogoutApi(message, true);
				}
				else
				{
					LogoutApi(message, false);
				}
			}
			else
			{
				if (Bot.Account.Socks.Version != SocksVersion.None)
				{
					LogoutApi(message, true);
				}
				else
				{
					LogoutApi(message, false);
				}
			}
		}
		// API: Starts the loaded script (usable in the outer scope or with invoke)
		private bool StartScript()
		{
			if (Bot.Game != null && (Bot.Running == BotClient.State.Stopped || Bot.Running == BotClient.State.Paused))
			{
				Bot.Start();
				return true;
			}

			return false;
		}

		// API: Calls the specified function after the specified number of seconds
		public void Invoke(DynValue function, float seconds, params DynValue[] args)
		{
			if (function.Type != DataType.Function && function.Type != DataType.ClrFunction)
			{
				Fatal("error: invoke: tried to call an invalid function");
				return;
			}

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (seconds == 0)
			{
				_lua.Call(function, args);
				return;
			}

			Invoker invoker = new Invoker()
			{
				Function = function,
				Time = DateTime.UtcNow.AddSeconds(seconds),
				Script = this,
				Args = args
			};

			Invokes.Add(invoker);
		}
		// API for Relog API
		private void LogoutApi(string message, bool allowAutoReconnector)
		{
			LogMessage(message);
			Bot.Stop();
			Bot.LogoutApi(allowAutoReconnector);
		}
		// API: Cancels all queued Invokes
		private void CancelInvokes()
		{
			Bot.CancelInvokes();
		}
	}
	public class Invoker
	{
		public DynValue Function;
		public DateTime Time;
		public LuaScript Script;
		public DynValue[] Args;
		// ReSharper disable once RedundantDefaultMemberInitializer
		public bool Called = false;

		public void Call()
		{
			Called = true;
			Script.Invoke(Function, 0, Args);
		}
	}
}
