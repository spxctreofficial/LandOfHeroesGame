using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class CardLogicController : MonoBehaviour {
	public static CardLogicController instance;
	public Card summonCard; // debugging
	public int dealToIndex; // debugging

	private void Awake() {
		if (instance == null)
			instance = this;
		else {
			Destroy(gameObject);
		}
	}
	private void Update() {
		if (Input.GetKeyDown(KeyCode.Alpha5)) {
			GameController.instance.champions[dealToIndex].hand.DealSpecificCard(summonCard);
		}
	}

	public IEnumerator CardSelect(Card card) {
		var player = GameController.instance.champions[0];
		foreach (var champion in GameController.instance.champions) {
			if (!champion.isPlayer) continue;
			player = champion;
			if (player.isDead) {
				Debug.Log("The player is dead!");
				yield break;
			}
		}
		switch (GameController.instance.gamePhase) {
			case GamePhase.ActionPhase:
				if (player.isMyTurn) {
					if (player.isAttacking) {
						if (GameController.instance.gambleButton.isBlocking) break;
						if (player.attackingCard != null) player.attackingCard.ToggleCardVisibility(true);
						player.attackingCard = card;
						card.ToggleCardVisibility(true);
						GameController.instance.playerActionTooltip.text = "Confirm the attack, or change selected card and/or target.";
						GameController.instance.confirmButton.Show();
						GameController.instance.gambleButton.Hide();

						if (player.currentTarget != null) break;
						GameController.instance.playerActionTooltip.text = "Choose a target or change selected card.";
						GameController.instance.confirmButton.Hide();
						break;
					}
					switch (card.cardSuit) {
						case CardSuit.SPADE:
							StartCoroutine(SpadeLogic(card, player));
							break;
						case CardSuit.HEART:
							StartCoroutine(HeartLogic(card, player));
							break;
						case CardSuit.CLUB:
							StartCoroutine(ClubLogic(card, player));
							break;
						case CardSuit.DIAMOND:
							StartCoroutine(DiamondLogic(card, player));
							break;
					}
					break;
				}
				else {
					foreach (var champion in GameController.instance.champions) {
						if (GameController.instance.gambleButton.isBlocking) break;
						if (champion.currentTarget != player || !champion.isAttacking) continue;

						if (player.defendingCard != null) player.defendingCard.ToggleCardVisibility(true);
						player.defendingCard = card;
						card.ToggleCardVisibility(true);
						GameController.instance.playerActionTooltip.text = "Confirm the defense, or change selected card.";
						GameController.instance.confirmButton.Show();
					}
				}

				if (player.discardAmount > 0) {
					PlayerDiscard(card, player, "Forced");
				}
				else {
					Debug.Log("It is not the player's Action Phase!");
				}
				break;
			case GamePhase.EndPhase:
				if (player.isMyTurn && player.discardAmount > 0) {
					PlayerDiscard(card, player, "Forced");
				}
				break;
		}
	}
	public IEnumerator BotCardLogic(ChampionController champion) {
		float PauseDuration() {
			switch (GameController.instance.difficulty) {
				case GameController.Difficulty.Noob:
				case GameController.Difficulty.Novice:
					return Random.Range(2f, 4f);
				case GameController.Difficulty.Warrior:
					return Random.Range(1f, 3f);
				case GameController.Difficulty.Champion:
					return Random.Range(0.4f, 1.25f);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		float CardScanDuration() {
			switch (GameController.instance.difficulty) {
				case GameController.Difficulty.Noob:
				case GameController.Difficulty.Novice:
					return 2.75f;
				case GameController.Difficulty.Warrior:
					return Random.Range(0.75f, 2f);
				case GameController.Difficulty.Champion:
					return Random.Range(0.25f, 1f);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		if (champion.isDead) {
			Debug.LogWarning("Attempted to apply logic to dead champion!");
			GameController.instance.NextTurnCalculator(champion);
			yield break;
		}

		GameController.instance.playerActionTooltip.text = "The " + champion.name + "'s Turn: Action Phase";

		yield return new WaitForSeconds(PauseDuration());

		foreach (Transform child in champion.hand.transform) {
			var card = child.GetComponent<Card>();
			if (card.cardSuit != CardSuit.CLUB) continue;

			switch (GameController.instance.difficulty) {
				case GameController.Difficulty.Noob:
				case GameController.Difficulty.Novice:
					break;
				default:
					if (card.cardValue > 10 && Random.Range(0f, 1f) < 0.9f) {
						Debug.Log("The " + champion.name + " refuses to trade in a CLUB worth: " + card.cardValue);
						continue;
					}
					break;
			}

			yield return StartCoroutine(ClubLogic(card, champion));
			yield return new WaitForSeconds(CardScanDuration());
		}

		yield return new WaitForSeconds(PauseDuration());

		foreach (Transform child in champion.hand.transform) {
			var card = child.GetComponent<Card>();
			if (card.cardSuit != CardSuit.DIAMOND) continue;
			if (champion.diamondsBeforeExhaustion == 0 && (card.cardValue < 5 || card.cardValue > 8)) {
				Debug.Log("The " + champion.name + " can't play this DIAMOND.");
				break;
			}

			yield return StartCoroutine(DiamondLogic(card, champion));
			yield return new WaitForSeconds(CardScanDuration());
		}

		yield return new WaitForSeconds(PauseDuration());

		// Spades
		foreach (Transform child in champion.hand.transform) {
			var card = child.GetComponent<Card>();
			if (card.cardSuit != CardSuit.SPADE) continue;
			if (champion.spadesBeforeExhaustion == 0) {
				Debug.Log("The " + champion.name + " is exhausted. Cannot attack.");
				break;
			}

			bool wontAttack = false;
			switch (GameController.instance.difficulty) {
				case GameController.Difficulty.Noob:
					break;
				case GameController.Difficulty.Novice:
					if (champion.currentHP <= 0.3f * champion.maxHP && Random.Range(0f, 1f) < 0.25f) {
						Debug.Log(champion.name + " realizes that they might fuck up and die!");
						champion.spadesBeforeExhaustion--;
						wontAttack = true;
					}
					break;
				case GameController.Difficulty.Warrior:
					if (champion.currentHP <= 0.2f * champion.maxHP && Random.Range(0f, 1f) < 0.45f) {
						Debug.Log("The " + champion.name + " doesn't want to attack!");
						champion.spadesBeforeExhaustion--;
						wontAttack = true;
					}
					break;
				case GameController.Difficulty.Champion:
					if ((champion.currentHP <= 0.2f * champion.maxHP && Random.Range(0f, 1f) < 0.65f) || Random.Range(0f, 1f) < 0.15f) {
						Debug.Log("The " + champion.name + " doesn't want to attack!");
						champion.spadesBeforeExhaustion--;
						wontAttack = true;
					}
					break;
			}
			if (wontAttack) break;

			yield return StartCoroutine(SpadeLogic(card, champion));
			yield return new WaitForSeconds(CardScanDuration());
		}

		yield return new WaitForSeconds(PauseDuration());

		foreach (Transform child in champion.hand.transform) {
			var card = child.GetComponent<Card>();
			if (champion.currentHP == champion.maxHP) break;
			if (champion.heartsBeforeExhaustion == 0) break;
			if (card.cardSuit != CardSuit.HEART) continue;

			switch (GameController.instance.difficulty) {
				case GameController.Difficulty.Champion:
					if (champion.currentHP + 20 >= 0.9f * champion.maxHP && card.cardValue == 13) {
						Debug.Log("Health would be clamped! The " + champion.name + " decides not to use an ACE of HEARTS to heal!");
						continue;
					}
					break;
			}

			yield return StartCoroutine(HeartLogic(card, champion));
			yield return new WaitForSeconds(CardScanDuration());
		}

		yield return new WaitForSeconds(PauseDuration());

		GameController.instance.StartEndPhase(champion);
	}

	public IEnumerator Deal(Hand hand, int amount = 4, bool flip = false, bool animate = true) {
		for (var x = 0; x < amount; x++) {
			var card = Instantiate(GameController.instance.cardIndex.playingCards[Random.Range(0, GameController.instance.cardIndex.playingCards.Count)], new Vector2(0, 0), Quaternion.identity).GetComponent<Card>();
			card.transform.SetParent(hand.transform, false);
			if (flip) card.ToggleCardVisibility();
			if (animate) {
				card.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
				StartCoroutine(card.GetComponent<SmartHover>().ScaleDown(new Vector3(1f, 1f, 1f)));
			}
			yield return new WaitForSeconds(0.25f);
		}
	}
	public void Discard(Card card, bool flip = false, bool animate = true) {
		card.transform.SetParent(GameController.instance.discardArea.transform, false);
		if (flip) card.ToggleCardVisibility();
		if (!animate) return;
		card.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
		StartCoroutine(card.GetComponent<SmartHover>().ScaleDown(new Vector3(1f, 1f, 1f)));
	}
	public IEnumerator CombatCalculation(ChampionController attacker, ChampionController defender, bool abilityCheck = true) {
		if (attacker.attackingCard == null) {
			Debug.LogError("No attacking card was specified on an initiated attack!");
			yield break;
		}
		defender.currentlyTargeted = true;
		defender.hasDefended = false;

		switch (attacker.isPlayer) {
			case true:
				if (!GameController.instance.gambleButton.isBlocking) Discard(attacker.attackingCard);
				break;
			case false:
				Discard(attacker.attackingCard, true);
				break;
		}
		switch (defender.isPlayer) {
			case true:
				GameController.instance.playerActionTooltip.text = "The " + attacker.name + " is attacking the " + defender.name + ". Defend with a card.";
				GameController.instance.gambleButton.Show();

				yield return new WaitUntil(() => defender.hasDefended && defender.defendingCard != null);
				break;
			case false:
				defender.defendingCard = defender.hand.GetCard("Defense");
				if (defender.defendingCard == null || Random.Range(0f, 1f) < 0.15f && defender.currentHP - attacker.attackDamage > 0) defender.defendingCard = Instantiate(GameController.instance.cardIndex.playingCards[Random.Range(0, GameController.instance.cardIndex.playingCards.Count)], new Vector2(0, 0), Quaternion.identity).GetComponent<Card>();
				break;
		}

		if (!defender.isPlayer) {
			yield return new WaitForSeconds(Random.Range(0.5f, 3f));
			Discard(defender.defendingCard);
		}
		else {
			Discard(defender.defendingCard);
			defender.defendingCard.ToggleCardVisibility();
		}
		defender.GetMatchStatistic().totalDefends++;
		attacker.attackingCard.ToggleCardVisibility(true);

		if (abilityCheck) {
			foreach (Transform child in attacker.abilityPanel.panel.transform) {
				var ability = child.GetComponent<AbilityController>();
				yield return StartCoroutine(ability.OnCombatCalculationAttacker(attacker.attackingCard, defender.defendingCard));
			}

			foreach (Transform child in defender.abilityPanel.panel.transform) {
				var ability = child.GetComponent<AbilityController>();
				yield return StartCoroutine(ability.OnCombatCalculationDefender(attacker.attackingCard, defender.defendingCard));
			}
		}

		if (attacker.attackingCard.cardValue > defender.defendingCard.cardValue) {
			yield return StartCoroutine(attacker.Attack(defender));

			attacker.GetMatchStatistic().successfulAttacks++;
			defender.GetMatchStatistic().failedDefends++;
		}
		else if (attacker.attackingCard.cardValue < defender.defendingCard.cardValue) {
			yield return StartCoroutine(attacker.Damage(defender.attackDamage, defender.attackDamageType, defender));

			attacker.GetMatchStatistic().failedAttacks++;
			defender.GetMatchStatistic().successfulDefends++;
		}
		else {
			Debug.Log("lol it tie");
			AudioController.instance.Play("SwordClashing");
		}
		Debug.Log(attacker.name + attacker.currentHP);
		Debug.Log(defender.name + defender.currentHP);

		GameController.instance.confirmButton.Hide();
		GameController.instance.endTurnButton.gameObject.SetActive(true);
		GameController.instance.playerActionTooltip.text = "The " + attacker.name + "'s Turn: Action Phase";
		attacker.isAttacking = false;
		attacker.attackingCard = null;
		attacker.currentTarget = null;
		defender.currentlyTargeted = false;
		defender.hasDefended = false;
		defender.defendingCard = null;
	}
	private IEnumerator SpadeLogic(Card card, ChampionController champion) {
		if (champion.spadesBeforeExhaustion <= 0) {
			GameController.instance.playerActionTooltip.text = "The" + champion.name + " cannot play any more SPADES! Choose another card.";
			yield break;
		}

		switch (champion.isPlayer) {
			case true:
				GameController.instance.endTurnButton.gameObject.SetActive(false);
				GameController.instance.gambleButton.Show();
				GameController.instance.playerActionTooltip.text = "Choose another card to represent your attack, or choose a target.";
				champion.isAttacking = true;
				champion.spadesBeforeExhaustion--;
				Discard(card);

				yield return new WaitUntil(() => champion.attackingCard != null && champion.currentTarget != null);
				GameController.instance.confirmButton.Show();
				break;
			case false:
				var gambled = false;

				if (card.cardValue > 10 && Random.Range(0f, 1f) < 0.75f) {
					Debug.Log("The " + champion.name + " refuses to use a SPADE worth: " + card.cardValue);
					yield break;
				}

				// Targeting Champion
				foreach (var targetChampion in GameController.instance.champions) {
					if (targetChampion == champion || targetChampion.isDead || targetChampion.team == champion.team) continue;

					var chance = targetChampion.hand.transform.childCount <= 3 ? 1f : 0.85f;
					if ((targetChampion.currentHP - champion.attackDamage <= 0 && Random.Range(0f, 1f) < chance) || targetChampion == champion.currentNemesis) {
						champion.currentTarget = targetChampion;
						break;
					}

					chance = targetChampion.isPlayer ? 0.7f : 0.55f;
					chance += champion.currentHP >= 0.75f * champion.maxHP ? 0.15f : 0f;
					if (Random.Range(0f, 1f) < chance) {
						champion.currentTarget = targetChampion;
						break;
					}
				}
				if (champion.currentTarget == null) {
					Debug.Log("The " + champion.name + " decides not to attack!");
					champion.spadesBeforeExhaustion--;
					break;
				}

				// Attacking Card
				if (champion.hand.transform.childCount - 1 != 0 || champion.currentTarget.hand.transform.childCount != 0 && Random.Range(0f, 1f) < 0.25f) {
					champion.attackingCard = champion.hand.GetAttackingCard(card);
				}
				else if (champion.attackingCard == null) {
					switch (GameController.instance.difficulty) {
						case GameController.Difficulty.Warrior:
						case GameController.Difficulty.Champion:
							champion.attackingCard = Instantiate(GameController.instance.cardIndex.playingCards[Random.Range(0, GameController.instance.cardIndex.playingCards.Count)], new Vector2(0, 0), Quaternion.identity).GetComponent<Card>();
							gambled = true;
							break;
					}
				}

				// Reviewing Choices
				switch (GameController.instance.difficulty) {
					case GameController.Difficulty.Warrior:
						if ((champion.attackingCard.cardValue <= card.cardValue ||
						     champion.attackingCard.cardValue <= 9)
						    && !gambled) {
							Debug.Log("The " + champion.name + " does not want to attack with the current configuration!");
							champion.attackingCard = null;
						}
						break;
					case GameController.Difficulty.Champion:
						var f = champion.currentTarget.currentHP <= 0.25f * champion.currentTarget.maxHP ? 0.4f : 0.75f;
						if ((champion.attackingCard.cardValue <= card.cardValue ||
						     champion.attackingCard.cardValue <= 9 ||
						     champion.hand.transform.childCount <= 2 && Random.Range(0f, 1f) < f)
						    && !gambled) {
							Debug.Log("The " + champion.name + " does not want to attack with the current configuration!");
							champion.attackingCard = null;
						}
						break;
				}

				// Confirming Attack
				Discard(card);
				champion.isAttacking = true;
				champion.spadesBeforeExhaustion--;
				champion.GetMatchStatistic().totalAttacks++;

				Debug.Log("The " + champion.name + " is attacking " + champion.currentTarget.name + " with a card with a value of " + champion.attackingCard.cardValue);

				yield return StartCoroutine(CombatCalculation(champion, champion.currentTarget));
				break;
		}
	}
	private IEnumerator HeartLogic(Card card, ChampionController champion) {
		if (champion.heartsBeforeExhaustion <= 0) {
			GameController.instance.playerActionTooltip.text = "You cannot play any more HEARTS! Choose another card.";
			yield break;
		}
		if (champion.currentHP >= champion.maxHP) {
			GameController.instance.playerActionTooltip.text = "Health is full! Choose another card.";
			yield break;
		}

		switch (card.cardValue) {
			default:
				StartCoroutine(champion.Heal(5));
				champion.heartsBeforeExhaustion--;
				Discard(card);
				break;
			case 7:
			case 8:
			case 9:
				if (champion.heartsBeforeExhaustion - 2 < 0) {
					GameController.instance.playerActionTooltip.text = "You will be exhausted! Choose another card.";
					break;
				}

				StartCoroutine(champion.Heal(10));
				champion.heartsBeforeExhaustion -= 2;
				Discard(card);
				break;
			case 10:
			case 11:
			case 12:
				if (champion.heartsBeforeExhaustion - 3 < 0) {
					GameController.instance.playerActionTooltip.text = "You will be exhausted! Choose another card.";
					break;
				}
				StartCoroutine(champion.Heal(20));
				champion.heartsBeforeExhaustion -= 3;
				Discard(card);
				break;
			case 13:
				if (champion.heartsBeforeExhaustion - 3 < 0) {
					GameController.instance.playerActionTooltip.text = "You will be exhausted! Choose another card.";
					break;
				}

				StartCoroutine(champion.Heal(40));
				champion.heartsBeforeExhaustion -= 3;
				Discard(card);
				break;
		}
	}
	private IEnumerator ClubLogic(Card card, ChampionController champion) {
		champion.hand.Deal(1, false, true, false);
		Discard(card);

		yield break;
	}
	private IEnumerator DiamondLogic(Card card, ChampionController champion) {
		if (champion.diamondsBeforeExhaustion <= 0 && (card.cardValue < 5 || card.cardValue > 8)) {
			GameController.instance.playerActionTooltip.text = "The " + champion.name + " cannot play more DIAMONDS! Choose another card.";
			yield break;
		}

		switch (card.cardValue) {
			case 1:
				if (!champion.isPlayer) {
					bool wontUse = false;
					switch (GameController.instance.difficulty) {
						case GameController.Difficulty.Noob:
						case GameController.Difficulty.Novice:
							break;
						case GameController.Difficulty.Warrior:
							if (champion.hand.transform.childCount >= 4) wontUse = true;
							break;
						case GameController.Difficulty.Champion:
							foreach (var selectedChampion in GameController.instance.champions) {
								if (selectedChampion == champion || selectedChampion.isDead
								                                 || selectedChampion.hand.transform.childCount < 5 && selectedChampion.currentHP - champion.attackDamage > 0) continue;
								wontUse = true;
								break;
							}
							break;
					}
					if (wontUse) break;
				}

				foreach (var selectedChampion in GameController.instance.champions) {
					selectedChampion.hand.Deal(2);
				}
				champion.diamondsBeforeExhaustion--;
				Discard(card);
				break;
			case 2:
				champion.diamondsBeforeExhaustion--;
				Discard(card);

				var tooltipCache = GameController.instance.playerActionTooltip.text;

				foreach (var selectedChampion in GameController.instance.champions) {
					if (selectedChampion.hand.transform.childCount == 0 || selectedChampion == champion || selectedChampion.isDead) continue;
					if (selectedChampion.isPlayer) {
						selectedChampion.discardAmount = 1;
						GameController.instance.playerActionTooltip.text = "Please discard " + selectedChampion.discardAmount + ".";

						yield return new WaitUntil(() => selectedChampion.discardAmount == 0);

						continue;
					}

					GameController.instance.playerActionTooltip.text = "Waiting for " + selectedChampion.name + ".";

					selectedChampion.discardAmount = 1;

					yield return new WaitForSeconds(Random.Range(0.2f, 2f));

					for (var discarded = 0; discarded < selectedChampion.discardAmount; discarded++) Discard(selectedChampion.hand.GetCard("Lowest"));

					selectedChampion.discardAmount = 0;
				}
				GameController.instance.playerActionTooltip.text = tooltipCache;
				break;
			case 3:
				if (!champion.isPlayer) {
					bool wontUse = false;
					switch (GameController.instance.difficulty) {
						case GameController.Difficulty.Noob:
						case GameController.Difficulty.Novice:
							break;
						case GameController.Difficulty.Warrior:
							if (champion.hand.transform.childCount >= 3) wontUse = true;
							break;
						case GameController.Difficulty.Champion:
							foreach (var selectedChampion in GameController.instance.champions) {
								if (selectedChampion == champion || selectedChampion.isDead
								                                 || selectedChampion.hand.transform.childCount < 5 && selectedChampion.currentHP - champion.attackDamage > 0) continue;
								wontUse = true;
								break;
							}
							break;
					}
					if (wontUse) break;
				}

				foreach (var selectedChampion in GameController.instance.champions) {
					selectedChampion.hand.Deal();
				}
				champion.diamondsBeforeExhaustion--;
				Discard(card);
				break;
			case 4:
				if (!champion.isPlayer) {
					bool jeopardized = false;
					foreach (var quickSelectChampion in GameController.instance.champions) {
						if (quickSelectChampion.team != champion.team || quickSelectChampion == champion || quickSelectChampion.isDead) continue;

						switch (GameController.instance.difficulty) {
							case GameController.Difficulty.Noob:
							case GameController.Difficulty.Novice:
								break;
							case GameController.Difficulty.Warrior:
								if (quickSelectChampion.currentHP - 20 <= 0) {
									Debug.Log("The " + champion.name + " does not want to jeopardize his teammate, " + quickSelectChampion.name + "!");
									jeopardized = true;
								}
								break;
							case GameController.Difficulty.Champion:
								switch (GameController.instance.gamemodes) {
									case GameController.Gamemodes.Competitive2v2:
										if (quickSelectChampion.currentHP - 20 <= 0) {
											int enemiesJeopardized = 0, enemiesLeft = 0;
											foreach (var enemyChampion in GameController.instance.champions) {
												if (enemyChampion.team == champion.team || enemyChampion == champion || enemyChampion == quickSelectChampion || enemyChampion.isDead) continue;
												if (enemyChampion.currentHP - 20 > 0) enemiesJeopardized++;
												enemiesLeft++;
											}
											if (enemiesLeft <= enemiesJeopardized && Random.Range(0f, 1f) < 0.8f || enemiesJeopardized != 0 && Random.Range(0f, 1f) < 0.25f) {
												Debug.Log("The " + champion.name + " does not want to jeopardize his teammate, " + quickSelectChampion.name + "!");
												jeopardized = true;
											}
										}
										break;
								}
								break;
						}
					}
					if (jeopardized) break;
				}

				champion.diamondsBeforeExhaustion--;
				Discard(card);

				tooltipCache = GameController.instance.playerActionTooltip.text;

				foreach (var selectedChampion in GameController.instance.champions) {
					if (selectedChampion == champion || selectedChampion.isDead) continue;
					if (selectedChampion.hand.transform.childCount == 0) {
						Debug.Log("The " + selectedChampion.name + " has no cards! Dealing damage automatically...");
						yield return StartCoroutine(selectedChampion.Damage(20, DamageType.Unblockable, champion));
						continue;
					}

					if (selectedChampion.isPlayer) {
						selectedChampion.discardAmount = Mathf.Min(champion.hand.transform.childCount, 2);
						GameController.instance.playerActionTooltip.text = "Please discard " + selectedChampion.discardAmount + ".";
						GameController.instance.confirmButton.Show();
						GameController.instance.confirmButton.textBox.text = "Skip";

						yield return new WaitUntil(() => selectedChampion.discardAmount <= 0);

						if (selectedChampion.discardAmount == -1) {
							yield return StartCoroutine(selectedChampion.Damage(20, DamageType.Unblockable, champion));
							selectedChampion.discardAmount = 0;
							GameController.instance.confirmButton.textBox.text = "Confirm";
						}

						continue;
					}

					GameController.instance.playerActionTooltip.text = "Waiting for " + selectedChampion.name + ".";

					yield return new WaitForSeconds(Random.Range(0.2f, 2f));

					var chance = selectedChampion.currentHP >= 0.75f * selectedChampion.maxHP ? 0.75f : 0.5f;
					if (Random.Range(0f, 1f) < chance && selectedChampion.currentHP - 20 > 0 || selectedChampion.hand.transform.childCount == 0) {
						yield return StartCoroutine(selectedChampion.Damage(20, DamageType.Unblockable, champion));
						continue;
					}

					selectedChampion.discardAmount = Mathf.Min(selectedChampion.hand.transform.childCount, 2);

					for (var discarded = 0; discarded < selectedChampion.discardAmount; discarded++) Discard(selectedChampion.hand.GetCard("Lowest"));

					selectedChampion.discardAmount = 0;
				}

				GameController.instance.playerActionTooltip.text = tooltipCache;

				break;
			case 5:
			case 6:
			case 7:
			case 8:
				champion.hand.Deal(1, false, true, false);
				Discard(card);
				break;
			case 9:
				if (champion.currentHP >= 0.8f * champion.maxHP && (champion.currentHP == champion.maxHP || Random.Range(0f, 1f) < 0.75f)) break;
				foreach (var selectedChampion in GameController.instance.champions) {
					yield return StartCoroutine(selectedChampion.Heal(10));
				}
				champion.diamondsBeforeExhaustion--;
				Discard(card);
				break;
			case 10:
				if (champion.currentHP >= 0.8f * champion.maxHP && (champion.currentHP == champion.maxHP || Random.Range(0f, 1f) < 0.9f)) break;
				foreach (var selectedChampion in GameController.instance.champions) {
					yield return StartCoroutine(selectedChampion.Heal(20));
				}
				champion.diamondsBeforeExhaustion--;
				Discard(card);
				break;
			case 11:
				if (!champion.isPlayer) {
					bool jeopardized = false;
					foreach (var quickSelectChampion in GameController.instance.champions) {
						if (quickSelectChampion.team != champion.team || quickSelectChampion == champion || quickSelectChampion.isDead) continue;

						switch (GameController.instance.difficulty) {
							case GameController.Difficulty.Noob:
							case GameController.Difficulty.Novice:
								break;
							case GameController.Difficulty.Warrior:
								if (quickSelectChampion.currentHP - 20 <= 0) {
									Debug.Log("The " + champion.name + " does not want to jeopardize his teammate, " + quickSelectChampion.name + "!");
									jeopardized = true;
								}
								break;
							case GameController.Difficulty.Champion:
								switch (GameController.instance.gamemodes) {
									case GameController.Gamemodes.Competitive2v2:
										if (quickSelectChampion.currentHP - 20 <= 0) {
											int enemiesJeopardized = 0, enemiesLeft = 0;
											foreach (var enemyChampion in GameController.instance.champions) {
												if (enemyChampion.team == champion.team || enemyChampion == champion || enemyChampion == quickSelectChampion || enemyChampion.isDead) continue;
												if (enemyChampion.currentHP - 20 > 0) enemiesJeopardized++;
												enemiesLeft++;
											}
											if (enemiesLeft <= enemiesJeopardized) {
												Debug.Log("The " + champion.name + " does not want to jeopardize his teammate, " + quickSelectChampion.name + "!");
												jeopardized = true;
											}
										}
										break;
								}
								break;
						}
					}
					if (jeopardized) break;
				}

				champion.diamondsBeforeExhaustion--;
				Discard(card);

				foreach (var selectedChampion in GameController.instance.champions) {
					if (selectedChampion == champion || selectedChampion.isDead) continue;

					yield return StartCoroutine(selectedChampion.Damage(20, DamageType.Fire, champion));

					yield return new WaitForSeconds(1f);
				}
				break;
			case 12:
				if (!champion.isPlayer) {
					bool jeopardized = false;
					foreach (var quickSelectChampion in GameController.instance.champions) {
						if (quickSelectChampion.team != champion.team || quickSelectChampion == champion || quickSelectChampion.isDead) continue;

						switch (GameController.instance.difficulty) {
							case GameController.Difficulty.Noob:
							case GameController.Difficulty.Novice:
								break;
							case GameController.Difficulty.Warrior:
								if (quickSelectChampion.currentHP - 40 <= 0) {
									Debug.Log("The " + champion.name + " does not want to jeopardize his teammate, " + quickSelectChampion.name + "!");
									jeopardized = true;
								}
								break;
							case GameController.Difficulty.Champion:
								switch (GameController.instance.gamemodes) {
									case GameController.Gamemodes.Competitive2v2:
										if (quickSelectChampion.currentHP - 40 <= 0) {
											int enemiesJeopardized = 0, enemiesLeft = 0;
											foreach (var enemyChampion in GameController.instance.champions) {
												if (enemyChampion.team == champion.team || enemyChampion == champion || enemyChampion == quickSelectChampion || enemyChampion.isDead) continue;
												if (enemyChampion.currentHP - 40 > 0) enemiesJeopardized++;
												enemiesLeft++;
											}
											if (enemiesLeft <= enemiesJeopardized && Random.Range(0f, 1f) < 0.65f || enemiesJeopardized != 0 && Random.Range(0f, 1f) < 0.65f) {
												Debug.Log("The " + champion.name + " does not want to jeopardize his teammate, " + quickSelectChampion.name + "!");
												jeopardized = true;
											}
										}
										break;
								}
								break;
						}
					}
					if (jeopardized) break;
				}

				champion.diamondsBeforeExhaustion--;
				Discard(card);

				tooltipCache = GameController.instance.playerActionTooltip.text;

				foreach (var selectedChampion in GameController.instance.champions) {
					if (selectedChampion == champion || selectedChampion.isDead) continue;
					if (selectedChampion.hand.transform.childCount == 0) {
						Debug.Log("The " + selectedChampion.name + " has no cards! Dealing damage automatically...");
						yield return StartCoroutine(selectedChampion.Damage(40, DamageType.Fire, champion));
						continue;
					}

					if (selectedChampion.isPlayer) {
						selectedChampion.discardAmount = Mathf.Min(champion.hand.transform.childCount, 4);
						GameController.instance.playerActionTooltip.text = "Please discard " + selectedChampion.discardAmount + ".";
						GameController.instance.confirmButton.Show();
						GameController.instance.confirmButton.textBox.text = "Skip";

						yield return new WaitUntil(() => selectedChampion.discardAmount <= 0);

						if (selectedChampion.discardAmount == -1) {
							yield return StartCoroutine(selectedChampion.Damage(40, DamageType.Fire, champion));
							selectedChampion.discardAmount = 0;
							GameController.instance.confirmButton.textBox.text = "Confirm";
						}

						continue;
					}

					GameController.instance.playerActionTooltip.text = "Waiting for " + selectedChampion.name + ".";

					yield return new WaitForSeconds(Random.Range(0.2f, 2f));

					var chance = selectedChampion.currentHP >= 0.75f * selectedChampion.maxHP ? 0.5f : 0.15f;
					if (Random.Range(0f, 1f) < chance && selectedChampion.currentHP - 40 > 0 || selectedChampion.hand.transform.childCount == 0) {
						yield return StartCoroutine(selectedChampion.Damage(40, DamageType.Fire, champion));
						continue;
					}

					selectedChampion.discardAmount = Mathf.Min(selectedChampion.hand.transform.childCount, 4);

					for (var discarded = 0; discarded < selectedChampion.discardAmount; discarded++) Discard(selectedChampion.hand.GetCard("Lowest"));

					selectedChampion.discardAmount = 0;
				}

				GameController.instance.playerActionTooltip.text = tooltipCache;

				break;
			default:
				Debug.Log("Not implemented yet. Skipping...");
				break;
		}
	}
	private void PlayerDiscard(Card card, ChampionController player, string type = "Normal") {
		switch (type) {
			case "Normal":

				Discard(card);
				player.discardAmount--;

				switch (player.discardAmount) {
					case 0:
						GameController.instance.NextTurnCalculator(player);
						break;
					default:
						GameController.instance.playerActionTooltip.text = "Please discard " + player.discardAmount + ".";
						break;
				}
				break;
			case "Forced":

				Discard(card);
				player.discardAmount--;
				GameController.instance.confirmButton.Hide();

				if (player.discardAmount != 0) {
					GameController.instance.playerActionTooltip.text = "Please discard " + player.discardAmount + ".";
				}
				else {

					GameController.instance.playerActionTooltip.text = "";
				}

				break;
		}
	}
}
