# PPORise
A free, open-source and advanced bot for Pokémon Planet.

# TODO
1) Add NPC(s) features.
2) Add map feature.
3) Add pathfinding api.
4) Add PC Features.


## Libraries

* [MoonSharp](http://www.moonsharp.org/) - Lua interpreter
* [Json.NET](http://www.newtonsoft.com/json) - JSON framework


# Icons

Found the Icon from: https://image.dek-d.com/27/0690/3719/125744257 (How I found it? A long story mate.)


# Lua API(s)
* Events:
onStart() -> This function will be called when your script going to be started.
onStop() -> This function will be called when your script going to be stopped.
onPause() -> This function will be called when your script going to pe paused.
onResume() -> This function will be called when your script going to be resumed.
onPathAction() -> This function is called always but not in battle. Use this function 
to perform automatic path actions like startBattle(), startFishing() etc. look below for informations.
onBattleAction() -> This function is called while you're in a battle. Use this function to perform
battle automatic actions like attack(), weakAttack(), useItem("Ultra Ball") etc.
onLearningMove(moveName, pokemonIndex) -> This function is called when one of your Pokémon
going to learn a move. You can use forgetMove("move name") - forgets a move of the new move learning
Pokémon.
* Functions:
log("message") -> Prints texts on the bot log box.
fatal("message") -> Prints texts on the bot log box and stops the script/bot.
logout("message") -> Prints texts and log outs.
flashWindow -> Flashes the bot window with orange color.
playSound("sound file location") -> Plays a sound.
relog(seconds, "message") -> it just logs out and relogs after specific time.
countColoredRocks("Rock Name") -> Counts specified color rock of current map.
getFishingTotalExperience() -> Returns the fishing total experience.
getFishingCurrentExperience() -> Returns the fishing current experience.
getFishingLevel() -> Returns the fishing level.
getMiningTotalExperience() -> Returns the mining total experience.
getMiningCurrentExperience() -> Returns the mining current experience.
getMiningLevel() -> Returns the mining level.
isOpponentShiny() -> finds out if opponent is shiny or not.
isOpponentEffortValue("type") -> returns true if opponent give specific/desired EV(Effort Value).
getOpponentType() -> gets opponent types returns a string array.
getOpponentName() -> gets opponent name.
getOpponentHealth() -> gets opponent current health.
getOpponentMaxHealth() -> gets opponent max health.
getOpponentHealthPercent() -> gets opponent current health into %.
getOpponentLevel() -> gets opponent level.
getActivePokemonNumber() -> gets active Pokemon uid/index. 
E.g. getActivePokemonNumber() == 1 if it returns true that means our first Pokemon is facing the battle.
getOpponentStatus() -> gets opponent status like sleep, poison etc.
isOpponentRare() -> finds out if opponent is rare or legendary.
useFirstMove() -> will use first move of active Pokemon.
useMoveAt(move index) -> will use move at the position of the given index of active Pokemon (Remeber A Pokemon conatains 4 moves in this game).
useMove(Move Name) -> will use the given move.
isInBattle() -> to find out is the bot in battle or not.
run() -> runs away from the active wild battle.
sendAnyPokemon() -> sends any Pokemon.
sendUsablePokemon() -> sends a usable Pokemon.
sendPokemon(index) -> sends specific Pokemon which the position of index in team.
attack() -> uses best attack against Opponent.
weakAttack() -> uses weakest attack(which can damage opponent, to use non-damaging moves like Hypnosis use "useMove('Hypnosis')") against opponent. (E.g. This will use False Swipe if you have that Pokémon your team).
isOppenentDataReceived() -> Returns true if received all stat datas about Opponent.
getOpponentIV(stat_type) -> Gets opponent's specific stat IV.
useItem(item Name) -> Uses an item doesn't care if in battle or not.
useFirstItem() -> Uses first item, can be only used in battles.
getItemQuantity("item name") -> Returns the quantity of the specified item in the inventory.
hasItem("item name") -> Returns true if the specified item is in the inventory.
useItemOnPokemon("item name", pokemon_index) -> Uses the specified item on the specified pokémon.
takeItemFromPokemon(pokemon_index) -> Takes the held item from the specified pokemon.
giveItemToPokemon("item name", pokemon_index) -> Gives the specified item on the specified pokemon.
buyItem("item name", amount) -> Buys the specified item from the opened shop.
openShop() -> Opens a Pokemart Shop. You must be in a Pokemart.
isShopOpen() -> Returns true if there is a shop opened.
startBattle() -> Starts a wild battle.
startFishing("rod name") -> starts fishing with given rod.
stopFishing() -> stops fishing.
startSurfBattle() -> Starts a surf wild battle.
teleportTo(Map Name, X, Y) -> Put map name and put coordinators. It will teleport you to a specific map. :D
getMapName() -> gets current map name.
isFishing() -> returns true if you started fishing.
isAutoEvolve() -> returns true if you have enabled auto evolving feature.
enableAutoEvolve() -> enables auto evolve feature.
disableAutoEvolve() -> disables auto evolving feature.
stopMining() -> stops mining if once mining has started.
isAnyRockMinable() -> returns true if any rock is mine able of your current map.
getPlayerX() -> returns player x position.
getPlayerY() -> returns player y position.
isMining() -> returns true if you have started mining.
startAnyColorRockMining(axe_name) -> this will start mining randomly with specific axe on minable rocks.
startColoredRockMining(axe_name, Color) -> this will start mining with specific axe on specific color rocks.
Eg. startColoredRockMining("Old Pickaxe", "Red") this will mine only minable red color rocks with old pickaxe.
getPokemonHealth(index) -> gets specific user Pokemon current health. E.g if getPokemonHealth(1) here index is 1 so it will get health of 1st Pokemon.
getPokemonStatus(index) -> get specific user Pokemon's status. Works like getPokemonHealth.
getPokemonLevel(index) -> get specific user Pokemon's level. Works like getPokemonHealth.
getPokemonName(index) -> get specific user Pokemon's name. Works like getPokemonHealth.
isPokemonUsable(index) -> returns true if it has damageable moves and current health is greater than 0.
isPokemonShiny(index) -> returns true if specific user Pokemon is shiny. Works like getPokemonHealth.
hasMove(Pokemon_index, move_Name) -> Returns true if specified pokemon got specified move.
getPokemonAbility(index) -> get specific user Pokemon's ability. Works like getPokemonHealth.
getPokemonMaxHealth(index) -> get specific user Pokemon's max health. Works like getPokemonHealth.
getPokemonHealthPercent(index) -> get specific user Pokemon's health percentage. Works like getPokemonHealth.
getPokemonEV(index, stat_name) -> gets sepcific Pokemon's ev of specific stat.
getPokemonIV(index, stat_name) -> gets sepcific Pokemon's iv of specific stat.
getPokemonStatus(index) -> gets specific Pokemon's stat like sleep or poison.
hasPokemonInTeam("Pokemon Name") -> Returns true if the specified pokémon is present in the team.
getTeamSize() -> Returns the amount of Pokémon in the team.
getPokemonHeldItem() -> Returns the item held by the specified pokemon in the team, null if empty.
getUsablePokemonCount() -> Returns the amount of usable pokémon in the team.
isTeamSortedByLevelAscending() -> Returns true if the team is sorted by level in ascending order.
isTeamSortedByLevelDescending -> Returns true if the team is sorted by level in descending order.
isTeamRangeSortedByLevelAscending(fromIndex, toIndex) -> Returns true if the specified part of the team is sorted by level in ascending order.
IsTeamRangeSortedByLevelDescending(fromIndex, toIndex) -> Returns true if the specified part of the team the team is sorted by level in descending order.
swapPokemon(pokemon1, pokemon2) -> Swaps the two Pokémon specified by their position in the team.
swapPokemonWithLeader("pokémonName") -> Swaps the first pokémon with the specified name with the leader of the team.
sortTeamByLevelAscending() -> Sorts the pokémon in the team by level in ascending order, one pokémon at a time.
sortTeamByLevelDescending() -> Sorts the pokémon in the team by level in descending order, one pokémon at a time.
sortTeamRangeByLevelAscending(fromIndex, toIndex) -> Sorts the specified part of the team by level in ascending order, one pokémon at a time.
sortTeamRangeByLevelDescending(fromIndex, toIndex) -> Sorts the specified part of the team by level in descending order, one pokémon at a time.
Only usable when learning a move, inside onLearningMove.
forgetAnyMoveExcept() -> E.g forgetAnyMoveExcept({"Fly", "Cut", "Psychic", "Thunder"})- Forgets any move expect some given moves to learn a new move.
forgetMove() -> E.g forgetMove("Dragon Rage") - Forgets the specified move, if existing, in order to learn a new one.
